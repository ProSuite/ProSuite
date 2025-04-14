using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainServices.AO.QA.VerifiedDataModel
{
	public class VerifiedModelFactory : IVerifiedModelFactory
	{
		[NotNull] private readonly IMasterDatabaseWorkspaceContextFactory _workspaceContextFactory;

		[NotNull] private readonly VerifiedDatasetHarvesterBase _datasetHarvester;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public VerifiedModelFactory(
			[NotNull] IMasterDatabaseWorkspaceContextFactory workspaceContextFactory,
			[NotNull] VerifiedDatasetHarvesterBase datasetHarvester)
		{
			Assert.ArgumentNotNull(workspaceContextFactory, nameof(workspaceContextFactory));

			_workspaceContextFactory = workspaceContextFactory;
			_datasetHarvester = datasetHarvester;
		}

		[PublicAPI]
		public bool HarvestAttributes
		{
			get => _datasetHarvester.HarvestAttributes;
			set => _datasetHarvester.HarvestAttributes = value;
		}

		[PublicAPI]
		public bool HarvestObjectTypes
		{
			get => _datasetHarvester.HarvestObjectTypes;
			set => _datasetHarvester.HarvestObjectTypes = value;
		}

		public Model CreateModel(IWorkspace workspace,
		                         string modelName,
		                         int modelId,
		                         string databaseName,
		                         string schemaOwner,
		                         IList<string> usedDatasetNames = null)
		{
			// TODO: This could be un-commented once 10.9.1 support has been dropped (and TopologyNameClass exists)
			//       Additionally, the line containing new TopologyNameClass() should be un-commented and the
			//       unit test re-activated.
			//if (usedDatasetNames != null)
			//{
			//	return CreateNeededModel(workspace, modelName, modelId, databaseName, schemaOwner,
			//	                         usedDatasetNames);
			//}

			var result = CreateFullModel(workspace, modelName, modelId, databaseName, schemaOwner);

			return result;
		}

		public void AssignMostFrequentlyUsedSpatialReference(
			Model model,
			IEnumerable<Dataset> usedDatasets)
		{
			ISpatialReference spatialReference = GetMainSpatialReference(model, usedDatasets);

			if (spatialReference != null)
			{
				model.SpatialReferenceDescriptor =
					SpatialReferenceDescriptorExtensions.CreateFrom(spatialReference);
			}
		}

		private Model CreateFullModel(
			IWorkspace workspace, string name, int modelId, string databaseName, string schemaOwner)

		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			VerifiedModel model =
				CreateVerifiedModel(name, workspace, modelId, databaseName, schemaOwner);

			var featureWorkspace = (IFeatureWorkspace) workspace;

			_datasetHarvester.ResetDatasets();

			using (_msg.IncrementIndentation("Reading datasets for '{0}'", name))
			{
				var harvestedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

				HarvestFeatureClasses(model, featureWorkspace, harvestedNames);
				HarvestTables(model, featureWorkspace, harvestedNames);
				HarvestTopologyDatasets(model, featureWorkspace, harvestedNames);
				HarvestTerrainDatasets(model, featureWorkspace, harvestedNames);
				HarvestGeometricNetworkDatasets(model, featureWorkspace, harvestedNames);
				HarvestRasterDatasets(model, featureWorkspace, harvestedNames);
				HarvestRasterMosaicDatasets(model, featureWorkspace, harvestedNames);

				_datasetHarvester.AddDatasets(model);

				// Only after the dataset are assigned to the model:
				_datasetHarvester.HarvestChildren();
			}

			_msg.InfoFormat("{0} dataset(s) read", model.Datasets.Count);

			return model;
		}

		private Model CreateNeededModel(IWorkspace workspace,
		                                string name,
		                                int modelId,
		                                string databaseName,
		                                string schemaOwner,
		                                [NotNull] IList<string> usedDatasetNames)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));
			Assert.ArgumentNotNull(usedDatasetNames, nameof(usedDatasetNames));

			VerifiedModel model =
				CreateVerifiedModel(name, workspace, modelId, databaseName, schemaOwner);

			_datasetHarvester.ResetDatasets();

			using (_msg.IncrementIndentation("Reading datasets for '{0}'", name))
			{
				var ws = (IFeatureWorkspace) workspace;
				IList<string> names = usedDatasetNames;
				string schema = model.DefaultDatabaseSchemaOwner;
				bool qn = model.ElementNamesAreQualified;

				var harvestedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

				Harvest(ws, names, esriDatasetType.esriDTFeatureClass,
				        () => new FeatureClassNameProxy(), // new FeatureClassNameClass(),
				        qn, schema, harvestedNames);
				Harvest(ws, names, esriDatasetType.esriDTTable, () => new TableNameClass(),
				        qn, schema, harvestedNames);
				//Harvest(ws, names, esriDatasetType.esriDTTopology, () => new TopologyNameClass(),
				//        qn, schema, harvestedNames);
				Harvest(ws, names, esriDatasetType.esriDTTerrain, () => new TinNameClass(),
				        qn, schema, harvestedNames); // TODO: verify
				Harvest(ws, names, esriDatasetType.esriDTGeometricNetwork,
				        () => new GeometricNetworkNameClass(),
				        qn, schema, harvestedNames);
				Harvest(ws, names, esriDatasetType.esriDTRasterDataset,
				        () => new RasterDatasetNameClass(),
				        qn, schema, harvestedNames);
				Harvest(ws, names, esriDatasetType.esriDTMosaicDataset,
				        () => new MosaicDatasetNameClass(),
				        qn, schema, harvestedNames);

				_datasetHarvester.AddDatasets(model);

				// Only after the dataset are assigned to the model:
				_datasetHarvester.HarvestChildren();
			}

			_msg.InfoFormat("{0} dataset(s) read", model.Datasets.Count);

			return model;
		}

		private VerifiedModel CreateVerifiedModel(string name, IWorkspace workspace, int modelId,
		                                          string databaseName, string schemaOwner)
		{
			// NOTE: The schema owner is ignored by harvesting (it's probably intentional).
			bool useQualitfiedDatasetNames = WorkspaceUtils.UsesQualifiedDatasetNames(workspace);

			if (! useQualitfiedDatasetNames)
			{
				databaseName = null;
				schemaOwner = null;
			}

			VerifiedModel model =
				new VerifiedModel(name, workspace, _workspaceContextFactory, databaseName,
				                  schemaOwner, SqlCaseSensitivity.SameAsDatabase,
				                  modelId);

			return model;
		}

		public static ISpatialReference GetMainSpatialReference(
			Model model, IEnumerable<Dataset> referencedDatasets)
		{
			var spatialDatasetReferenceCount =
				new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

			foreach (Dataset dataset in referencedDatasets)
			{
				if (! (dataset is ISpatialDataset))
				{
					continue;
				}

				if (! spatialDatasetReferenceCount.ContainsKey(dataset.Name))
				{
					spatialDatasetReferenceCount.Add(dataset.Name, 1);
				}
				else
				{
					spatialDatasetReferenceCount[dataset.Name]++;
				}
			}

			foreach (KeyValuePair<string, int> pair in
			         spatialDatasetReferenceCount.OrderByDescending(kvp => kvp.Value))
			{
				string datasetName = pair.Key;

				Dataset maxDataset = model.GetDatasetByModelName(datasetName);

				if (maxDataset == null)
				{
					continue;
				}

				// Using a simple dataset opener is good enough. There are no models with just terrains and geometric networks.
				IWorkspaceContext datasetContext =
					Assert.NotNull(model.MasterDatabaseWorkspaceContext);
				IOpenDataset datasetOpener = new SimpleDatasetOpener(datasetContext);

				ISpatialReference spatialReference = GetSpatialReference(maxDataset, datasetOpener);

				if (spatialReference != null)
				{
					return spatialReference;
				}
			}

			return null;
		}

		[CanBeNull]
		private static ISpatialReference GetSpatialReference([NotNull] Dataset dataset,
		                                                     [NotNull] IOpenDataset datasetOpener)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			IGeoDataset geoDataset = datasetOpener.OpenDataset(dataset) as IGeoDataset;

			return geoDataset?.SpatialReference;
		}

		private void HarvestDataset([NotNull] IDatasetName datasetName,
		                            [NotNull] ICollection<string> harvestedNames)
		{
			if (IsAlreadyHarvested(datasetName, harvestedNames))
			{
				return;
			}

			if (_datasetHarvester.IgnoreDataset(datasetName, out string reason))
			{
				_msg.WarnFormat("Ignoring dataset {0} because: {1}", datasetName.Name, reason);
				return;
			}

			_datasetHarvester.UseDataset(datasetName);
		}

		private void Harvest(
			[NotNull] IFeatureWorkspace workspace,
			[NotNull] IList<string> usedDatasetNames,
			esriDatasetType dtType,
			[NotNull] Func<IDatasetName> createName,
			bool useQualifiedNames,
			[CanBeNull] string defaultSchema,
			[NotNull] ICollection<string> harvestedNames)
		{
			IWorkspace2 ws = (IWorkspace2) workspace;
			foreach (string usedName in usedDatasetNames)
			{
				string name;
				if (useQualifiedNames)
				{
					if (usedName.IndexOf('.') < 0)
					{
						name = $"{defaultSchema}.{usedName}";
					}
					else
					{
						name = usedName;
					}
				}
				else
				{
					int idx = usedName.LastIndexOf('.');
					name = usedName.Substring(idx + 1);
				}

				if (ws.NameExists[dtType, name])
				{
					IWorkspaceName wsName = (IWorkspaceName) ((IDataset) ws).FullName;
					IDatasetName datasetName = createName();
					datasetName.WorkspaceName = wsName;
					datasetName.Name = name;

					HarvestDataset(datasetName, harvestedNames);
				}
			}
		}

		private void HarvestFeatureClasses([NotNull] Model model,
		                                   [NotNull] IFeatureWorkspace workspace,
		                                   [NotNull] ICollection<string> harvestedNames)
		{
			foreach (IDatasetName datasetName in
			         DatasetUtils.GetDatasetNames(workspace, esriDatasetType.esriDTFeatureClass))
			{
				HarvestDataset(datasetName, harvestedNames);
			}
		}

		private void HarvestTables([NotNull] Model model,
		                           [NotNull] IFeatureWorkspace workspace,
		                           [NotNull] ICollection<string> harvestedNames)
		{
			foreach (IDatasetName datasetName in
			         DatasetUtils.GetDatasetNames(workspace, esriDatasetType.esriDTTable))
			{
				HarvestDataset(datasetName, harvestedNames);
			}
		}

		private void HarvestTopologyDatasets(
			[NotNull] Model model,
			[NotNull] IFeatureWorkspace workspace,
			[NotNull] ICollection<string> harvestedNames)
		{
			foreach (IDatasetName datasetName in
			         DatasetUtils.GetDatasetNames(workspace, esriDatasetType.esriDTTopology))
			{
				HarvestDataset(datasetName, harvestedNames);
			}
		}

		private void HarvestRasterDatasets(
			[NotNull] Model model,
			[NotNull] IFeatureWorkspace workspace,
			[NotNull] ICollection<string> harvestedNames)
		{
			foreach (IDatasetName datasetName in
			         DatasetUtils.GetDatasetNames(workspace, esriDatasetType.esriDTRasterDataset))
			{
				HarvestDataset(datasetName, harvestedNames);
			}
		}

		private void HarvestRasterMosaicDatasets(
			[NotNull] Model model,
			[NotNull] IFeatureWorkspace workspace,
			[NotNull] ICollection<string> harvestedNames)
		{
			foreach (IDatasetName datasetName in
			         DatasetUtils.GetDatasetNames(workspace, esriDatasetType.esriDTMosaicDataset))
			{
				HarvestDataset(datasetName, harvestedNames);
			}
		}

		private void HarvestTerrainDatasets(
			[NotNull] Model model,
			[NotNull] IFeatureWorkspace workspace,
			[NotNull] ICollection<string> harvestedNames)
		{
			foreach (IDatasetName datasetName in
			         DatasetUtils.GetDatasetNames(workspace, esriDatasetType.esriDTTerrain))
			{
				HarvestDataset(datasetName, harvestedNames);
			}
		}

		private void HarvestGeometricNetworkDatasets(
			[NotNull] Model model,
			[NotNull] IFeatureWorkspace workspace,
			[NotNull] ICollection<string> harvestedNames)
		{
			foreach (IDatasetName datasetName in
			         DatasetUtils.GetDatasetNames(workspace,
			                                      esriDatasetType.esriDTGeometricNetwork))
			{
				HarvestDataset(datasetName, harvestedNames);
			}
		}

		private static bool IsAlreadyHarvested([NotNull] IDatasetName datasetName,
		                                       [NotNull] ICollection<string> harvested)
		{
			string name = datasetName.Name;
			if (harvested.Contains(name))
			{
				return true;
			}

			harvested.Add(name);
			return false;
		}

		// FeatureClassNameClass does not exist in AO11 !
		private class FeatureClassNameProxy : IFeatureClassName, IDatasetName
		{
			esriGeometryType IFeatureClassName.ShapeType { get; set; }
			IDatasetName IFeatureClassName.FeatureDatasetName { get; set; }
			esriFeatureType IFeatureClassName.FeatureType { get; set; }
			string IFeatureClassName.ShapeFieldName { get; set; }
			string IDatasetName.Name { get; set; }

			esriDatasetType IDatasetName.Type => esriDatasetType.esriDTFeatureClass;

			string IDatasetName.Category { get; set; }
			IWorkspaceName IDatasetName.WorkspaceName { get; set; }

			IEnumDatasetName IDatasetName.SubsetNames => null;
		}
	}
}
