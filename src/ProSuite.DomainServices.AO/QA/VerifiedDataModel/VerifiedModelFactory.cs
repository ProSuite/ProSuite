using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AO.DataModel;

namespace ProSuite.DomainServices.AO.QA.VerifiedDataModel
{
	public class VerifiedModelFactory : IVerifiedModelFactory
	{
		[NotNull]
		private readonly Func<Model, IFeatureWorkspace, IWorkspaceContext> _workspaceContextFactory;

		[NotNull] private readonly VerifiedDatasetHarvesterBase _datasetHarvester;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[CLSCompliant(false)]
		public VerifiedModelFactory(
			[NotNull] Func<Model, IFeatureWorkspace, IWorkspaceContext> workspaceContextFactory,
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

		[CLSCompliant(false)]
		public Model CreateModel(IWorkspace workspace,
		                         string name,
		                         ISpatialReference spatialReference,
		                         string databaseName,
		                         string schemaOwner)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			// NOTE: The schema owner is ignored by harvesting (it's probably intentional).
			VerifiedModel model =
				WorkspaceUtils.UsesQualifiedDatasetNames(workspace)
					? new VerifiedModel(name, workspace, _workspaceContextFactory, databaseName,
					                    schemaOwner)
					: new VerifiedModel(name, workspace, _workspaceContextFactory);

			if (spatialReference != null)
			{
				model.SpatialReferenceDescriptor =
					new SpatialReferenceDescriptor(spatialReference);
			}

			var featureWorkspace = (IFeatureWorkspace) workspace;

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
				DatasetUtils.GetDatasetNames(workspace, esriDatasetType.esriDTGeometricNetwork))
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
	}
}