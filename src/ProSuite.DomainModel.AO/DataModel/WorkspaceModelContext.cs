using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	public class WorkspaceModelContext : IModelContext
	{
		private readonly bool _datasetsAreEditableInCurrentState;

		public WorkspaceModelContext([NotNull] IWorkspaceContext workspaceContext,
		                             bool datasetsAreEditableInCurrentState)
		{
			Assert.ArgumentNotNull(workspaceContext, nameof(workspaceContext));

			PrimaryWorkspaceContext = workspaceContext;
			_datasetsAreEditableInCurrentState = datasetsAreEditableInCurrentState;
		}

		public bool CanOpen(IDdxDataset dataset)
		{
			return PrimaryWorkspaceContext.CanOpen(dataset);
		}

		public IFeatureClass OpenFeatureClass(IVectorDataset dataset)
		{
			return PrimaryWorkspaceContext.OpenFeatureClass(dataset);
		}

		public ITable OpenTable(IObjectDataset dataset)
		{
			return PrimaryWorkspaceContext.OpenTable(dataset);
		}

		public IObjectClass OpenObjectClass(IObjectDataset dataset)
		{
			return PrimaryWorkspaceContext.OpenObjectClass(dataset);
		}

		public TopologyReference OpenTopology(ITopologyDataset dataset)
		{
			return PrimaryWorkspaceContext.OpenTopology(dataset);
		}

		public RasterDatasetReference OpenRasterDataset(IDdxRasterDataset dataset)
		{
			return PrimaryWorkspaceContext.OpenRasterDataset(dataset);
		}

		public TerrainReference OpenTerrainReference(ISimpleTerrainDataset dataset)
		{
			return PrimaryWorkspaceContext.OpenTerrainReference(dataset);
		}

		public MosaicRasterReference OpenSimpleRasterMosaic(IRasterMosaicDataset dataset)
		{
			return PrimaryWorkspaceContext.OpenSimpleRasterMosaic(dataset);
		}

		public IRelationshipClass OpenRelationshipClass(Association association)
		{
			return PrimaryWorkspaceContext.OpenRelationshipClass(association);
		}

		#region Implementation of IDatasetEditContext

		public bool IsEditableInCurrentState(IDdxDataset dataset)
		{
			return CanOpen(dataset) && _datasetsAreEditableInCurrentState;
		}

		#endregion

		#region Implementation of IModelContext

		public bool IsPrimaryWorkspaceBeingEdited()
		{
			return ((IWorkspaceEdit) PrimaryWorkspaceContext.Workspace).IsBeingEdited();
		}

		public IWorkspaceContext PrimaryWorkspaceContext { get; }

		public IWorkspaceContext GetWorkspaceContext(IDdxDataset dataset)
		{
			return PrimaryWorkspaceContext.CanOpen(dataset)
				       ? PrimaryWorkspaceContext
				       : null;
		}

		public IDdxDataset GetDataset(IDatasetName datasetName, bool isValid)
		{
			IWorkspace workspace = isValid
				                       ? WorkspaceUtils.OpenWorkspace(datasetName)
				                       : null;

			if (workspace != null &&
			    ! WorkspaceUtils.IsSameDatabase(workspace, PrimaryWorkspaceContext.Workspace))
			{
				return null;
			}

			return PrimaryWorkspaceContext.GetDatasetByGdbName(datasetName.Name);
		}

		#endregion
	}
}
