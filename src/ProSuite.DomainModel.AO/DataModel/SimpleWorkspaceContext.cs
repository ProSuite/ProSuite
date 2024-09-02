#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ESRI.ArcGIS.DataSourcesRaster;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.AO.Surface.Raster;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	public class SimpleWorkspaceContext : WorkspaceContextBase
	{
		[NotNull] private readonly DdxModel _model;

		private Dictionary<string, WorkspaceDataset> _workspaceDatasetByModelName;
		private Dictionary<string, WorkspaceDataset> _workspaceDatasetByGdbDatasetName;

		private Dictionary<string, WorkspaceAssociation>
			_workspaceAssociationsByAssociationName;

		private Dictionary<string, WorkspaceAssociation> _workspaceAssociationByRelClassName;

		#region Constructors

		public SimpleWorkspaceContext(
			[NotNull] DdxModel model,
			[NotNull] IFeatureWorkspace featureWorkspace,
			[NotNull] IEnumerable<WorkspaceDataset> workspaceDatasets,
			[NotNull] IEnumerable<WorkspaceAssociation> workspaceAssociations)
			: base(featureWorkspace)
		{
			Assert.ArgumentNotNull(model, nameof(model));
			Assert.ArgumentNotNull(featureWorkspace, nameof(featureWorkspace));
			Assert.ArgumentNotNull(workspaceDatasets, nameof(workspaceDatasets));
			Assert.ArgumentNotNull(workspaceAssociations, nameof(workspaceAssociations));

			_model = model;

			UpdateContent(workspaceDatasets, workspaceAssociations);
		}

		#endregion

		public void UpdateContent(
			[NotNull] IEnumerable<WorkspaceDataset> workspaceDatasets,
			[NotNull] IEnumerable<WorkspaceAssociation> workspaceAssociations)
		{
			Assert.ArgumentNotNull(workspaceDatasets, nameof(workspaceDatasets));
			Assert.ArgumentNotNull(workspaceAssociations, nameof(workspaceAssociations));

			// TODO REFACTORMODEL get the associations involved in the workspace datasets only?
			// TODO assert that all datasets and associations are from this model?

			// get lists here to avoid duplicate evaluation of enumerable
			List<WorkspaceDataset> datasets = workspaceDatasets.ToList();
			List<WorkspaceAssociation> associations = workspaceAssociations.ToList();

			_workspaceDatasetByModelName = GetWorkspaceDatasetsByName(datasets);

			_workspaceDatasetByGdbDatasetName =
				datasets.ToDictionary(wsd => wsd.Name,
				                      StringComparer.OrdinalIgnoreCase);

			_workspaceAssociationsByAssociationName =
				GetWorkspaceAssociationsByName(associations);

			_workspaceAssociationByRelClassName =
				associations.ToDictionary(wsd => wsd.RelationshipClassName,
				                          StringComparer.OrdinalIgnoreCase);
		}

		public override bool CanOpen(IDdxDataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			return GetWorkspaceDataset(dataset) != null;
		}

		public override IObjectClass OpenObjectClass(IObjectDataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			WorkspaceDataset workspaceDataset = GetWorkspaceDataset(dataset);

			return workspaceDataset == null
				       ? null
				       : ModelElementUtils.OpenObjectClass(FeatureWorkspace,
				                                           workspaceDataset.Name,
				                                           dataset);
		}

		public override ITopology OpenTopology(ITopologyDataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			WorkspaceDataset workspaceDataset = GetWorkspaceDataset(dataset);

			return workspaceDataset == null
				       ? null
				       : TopologyUtils.OpenTopology(FeatureWorkspace, workspaceDataset.Name);
		}

		public override RasterDatasetReference OpenRasterDataset(IDdxRasterDataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			WorkspaceDataset workspaceDataset = GetWorkspaceDataset(dataset);

			return workspaceDataset == null
				       ? null
				       : new RasterDatasetReference(
					       DatasetUtils.OpenRasterDataset(Workspace, workspaceDataset.Name));
		}

		public override TerrainReference OpenTerrainReference(ISimpleTerrainDataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			IList<SimpleTerrainDataSource> terrainSources =
				ModelElementUtils.GetTerrainDataSources(dataset, OpenObjectClass);

			return new SimpleTerrain(dataset.Name, terrainSources, dataset.PointDensity, null);
		}

		public override MosaicRasterReference OpenSimpleRasterMosaic(IRasterMosaicDataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			IMosaicDataset mosaic = DatasetUtils.OpenMosaicDataset(Workspace, dataset.Name);

			var simpleRasterMosaic = new SimpleRasterMosaic(mosaic);

			return new MosaicRasterReference(simpleRasterMosaic);
		}

		public override IRelationshipClass OpenRelationshipClass(Association association)
		{
			Assert.ArgumentNotNull(association, nameof(association));

			WorkspaceAssociation workspaceAssociation = GetWorkspaceAssociation(association);

			if (workspaceAssociation == null)
			{
				return null;
			}

			return DatasetUtils.OpenRelationshipClass(
				FeatureWorkspace,
				workspaceAssociation.RelationshipClassName);
		}

		public override Dataset GetDatasetByGdbName(string gdbDatasetName)
		{
			Assert.ArgumentNotNullOrEmpty(gdbDatasetName, nameof(gdbDatasetName));

			// TODO for query classes: translate owner part also (observed for pg: query class is always owned by *connected* user)
			string gdbTableName = ModelElementUtils.GetBaseTableName(gdbDatasetName, this);

			WorkspaceDataset workspaceDataset;
			return _workspaceDatasetByGdbDatasetName.TryGetValue(
				       gdbTableName,
				       out workspaceDataset)
				       ? workspaceDataset.Dataset
				       : null;
		}

		public override Dataset GetDatasetByModelName(string modelDatasetName)
		{
			Assert.ArgumentNotNullOrEmpty(modelDatasetName, nameof(modelDatasetName));

			WorkspaceDataset workspaceDataset;
			return _workspaceDatasetByModelName.TryGetValue(modelDatasetName,
			                                                out workspaceDataset)
				       ? workspaceDataset.Dataset
				       : null;
		}

		public override Association GetAssociationByRelationshipClassName(
			string relationshipClassName)
		{
			Assert.ArgumentNotNullOrEmpty(relationshipClassName,
			                              nameof(relationshipClassName));

			WorkspaceAssociation workspaceAssociation;
			return _workspaceAssociationByRelClassName.TryGetValue(relationshipClassName,
				       out workspaceAssociation)
				       ? workspaceAssociation.Association
				       : null;
		}

		public override Association GetAssociationByModelName(string associationName)
		{
			Assert.ArgumentNotNullOrEmpty(associationName, nameof(associationName));

			WorkspaceAssociation workspaceAssociation;
			return _workspaceAssociationsByAssociationName.TryGetValue(
				       associationName,
				       out workspaceAssociation)
				       ? workspaceAssociation.Association
				       : null;
		}

		public override bool Contains(IDdxDataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			return _model.Contains(dataset) &&
			       _workspaceDatasetByModelName.ContainsKey(dataset.Name);
		}

		public override bool Contains(Association association)
		{
			Assert.ArgumentNotNull(association, nameof(association));

			return _model.Contains(association) &&
			       _workspaceAssociationsByAssociationName.ContainsKey(association.Name);
		}

		[NotNull]
		private static Dictionary<string, WorkspaceAssociation>
			GetWorkspaceAssociationsByName(
				[NotNull] IEnumerable<WorkspaceAssociation> associations)
		{
			var result = new Dictionary<string, WorkspaceAssociation>(
				StringComparer.OrdinalIgnoreCase);

			foreach (WorkspaceAssociation workspaceAssociation in associations)
			{
				string name = workspaceAssociation.Association.Name;

				if (result.ContainsKey(name))
				{
					throw new InvalidOperationException(
						string.Format(
							"More than one relationship class in the workspace is mapped to the same association in the data model ({0}):{1}" +
							"- {2}{1}" +
							"- {3}",
							name,
							Environment.NewLine,
							result[name].RelationshipClassName,
							workspaceAssociation.RelationshipClassName));
				}

				result.Add(name, workspaceAssociation);
			}

			return result;
		}

		[NotNull]
		private static Dictionary<string, WorkspaceDataset> GetWorkspaceDatasetsByName(
			[NotNull] IEnumerable<WorkspaceDataset> workspaceDatasets)
		{
			Assert.ArgumentNotNull(workspaceDatasets, nameof(workspaceDatasets));

			var result = new Dictionary<string, WorkspaceDataset>(
				StringComparer.OrdinalIgnoreCase);

			foreach (WorkspaceDataset workspaceDataset in workspaceDatasets)
			{
				string name = workspaceDataset.Dataset.Name;

				if (result.ContainsKey(name))
				{
					throw new InvalidOperationException(
						string.Format(
							"More than one dataset in the workspace is mapped to the same dataset in the data model ({0}):{1}" +
							"- {2}{1}" +
							"- {3}",
							name,
							Environment.NewLine,
							result[name].Name,
							workspaceDataset.Name));
				}

				result.Add(name, workspaceDataset);
			}

			return result;
		}

		[CanBeNull]
		public WorkspaceDataset GetWorkspaceDataset([NotNull] IDdxDataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			return ! Equals(dataset.Model, _model)
				       ? null
				       : GetWorkspaceDataset(dataset.Name);
		}

		[CanBeNull]
		private WorkspaceDataset GetWorkspaceDataset([NotNull] string modelDatasetName)
		{
			WorkspaceDataset result;
			return _workspaceDatasetByModelName.TryGetValue(
				       modelDatasetName, out result)
				       ? result
				       : null;
		}

		[CanBeNull]
		public WorkspaceAssociation GetWorkspaceAssociation(
			[NotNull] Association association)
		{
			Assert.ArgumentNotNull(association, nameof(association));

			if (! Equals(association.Model, _model))
			{
				return null;
			}

			WorkspaceAssociation result;
			return _workspaceAssociationsByAssociationName.TryGetValue(
				       association.Name, out result)
				       ? result
				       : null;
		}
	}
}
