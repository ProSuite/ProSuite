using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Notifications;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.QA
{
	/// <summary>
	/// Verification context for QA service that does not support updating errors in the
	/// verified model context.
	/// </summary>
	public class BackgroundVerificationContext : IVerificationContext,
	                                             IQueryTableContext,
	                                             IDetachedState
	{
		[NotNull] private readonly ICollection<Dataset> _verifiedDatasets;

		public BackgroundVerificationContext(
			[NotNull] IModelContext innerModelContext,
			[NotNull] SpatialReferenceDescriptor spatialReferenceDescriptor,
			[NotNull] ICollection<Dataset> verifiedDatasets)
		{
			InnerModelContext = innerModelContext;
			SpatialReferenceDescriptor = spatialReferenceDescriptor;
			_verifiedDatasets = verifiedDatasets;
		}

		public IModelContext InnerModelContext { get; }

		public void InitializeSchema(ICollection<Dataset> datasets)
		{
			if (InnerModelContext is IVirtualModelContext virtualContext)
			{
				virtualContext.InitializeSchema(datasets);
			}
		}

		#region IDatasetEditContext implementation

		public bool IsEditableInCurrentState(IDdxDataset dataset)
		{
			return false;
		}

		#endregion

		#region IVerificationContext implementation

		public ICollection<Dataset> GetVerifiedDatasets()
		{
			return _verifiedDatasets;
		}

		public bool CanWriteIssues => false;

		public bool CanNavigateIssues => false;

		public void UpdateCanWriteIssues() { }

		public IEnumerable<INotification> CannotWriteIssuesReasons =>
			new[] { new Notification("Not supported in background service") };

		public IEnumerable<INotification> CannotNavigateIssuesReasons =>
			new[] { new Notification("Not supported in background service") };

		// the error datasets are still needed (e.g. to load allowed errors)

		public SpatialReferenceDescriptor SpatialReferenceDescriptor { get; }

		public ErrorLineDataset LineIssueDataset { get; set; }

		public ErrorPolygonDataset PolygonIssueDataset { get; set; }

		public ErrorMultipointDataset MultipointIssueDataset { get; set; }

		public ErrorMultiPatchDataset MultiPatchIssueDataset { get; set; }

		public ErrorTableDataset NoGeometryIssueDataset { get; set; }

		#endregion

		#region IDatasetContext members

		public bool CanOpen(IDdxDataset dataset)
		{
			return InnerModelContext.CanOpen(dataset);
		}

		public IFeatureClass OpenFeatureClass(IVectorDataset dataset)
		{
			return InnerModelContext.OpenFeatureClass(dataset);
		}

		public ITable OpenTable(IObjectDataset dataset)
		{
			return InnerModelContext.OpenTable(dataset);
		}

		public IObjectClass OpenObjectClass(IObjectDataset dataset)
		{
			return InnerModelContext.OpenObjectClass(dataset);
		}

		public TopologyReference OpenTopology(ITopologyDataset dataset)
		{
			return InnerModelContext.OpenTopology(dataset);
		}

		//public ITerrain OpenTerrain(ITerrainDataset dataset)
		//{
		//	if (! (_modelContext is IDatasetContextEx modelContextEx))
		//	{
		//		throw new NotImplementedException(
		//			$"{_modelContext} does not implement IDatasetContextEx");
		//	}

		//	return modelContextEx.OpenTerrain(dataset);
		//}

		public TerrainReference OpenTerrainReference(ISimpleTerrainDataset dataset)
		{
			return InnerModelContext.OpenTerrainReference(dataset);
		}

		public MosaicRasterReference OpenSimpleRasterMosaic(IRasterMosaicDataset dataset)
		{
			return InnerModelContext.OpenSimpleRasterMosaic(dataset);
		}

		//public ITopology OpenTopology(ITopologyDataset dataset)
		//{
		//	if (! (_modelContext is IDatasetContextEx modelContextEx))
		//	{
		//		throw new NotImplementedException(
		//			$"{_modelContext} does not implement IDatasetContextEx");
		//	}

		//	return modelContextEx.OpenTopology(dataset);
		//}

		//public IGeometricNetwork OpenGeometricNetwork(IGeometricNetworkDataset dataset)
		//{
		//	if (! (_modelContext is IDatasetContextEx modelContextEx))
		//	{
		//		throw new NotImplementedException(
		//			$"{_modelContext} does not implement IDatasetContextEx");
		//	}

		//	return modelContextEx.OpenGeometricNetwork(dataset);
		//}

		public RasterDatasetReference OpenRasterDataset(IDdxRasterDataset dataset)
		{
			return InnerModelContext.OpenRasterDataset(dataset);
		}

		//public IMosaicLayer OpenMosaicLayer(IRasterMosaicDataset dataset)
		//{
		//	if (! (_modelContext is IDatasetContextEx modelContextEx))
		//	{
		//		throw new NotImplementedException(
		//			$"{_modelContext} does not implement IDatasetContextEx");
		//	}

		//	return modelContextEx.OpenMosaicLayer(dataset);
		//}

		public IRelationshipClass OpenRelationshipClass(Association association)
		{
			return InnerModelContext.OpenRelationshipClass(association);
		}

		#endregion

		#region IWorkspaceContextLookup members

		public IWorkspaceContext GetWorkspaceContext(IDdxDataset dataset)
		{
			return InnerModelContext.GetWorkspaceContext(dataset);
		}

		#endregion

		#region IModelContext members

		public bool IsPrimaryWorkspaceBeingEdited()
		{
			return InnerModelContext.IsPrimaryWorkspaceBeingEdited();
		}

		public IWorkspaceContext PrimaryWorkspaceContext =>
			InnerModelContext.PrimaryWorkspaceContext;

		public IDdxDataset GetDataset(IDatasetName datasetName, bool isValid)
		{
			return InnerModelContext.GetDataset(datasetName, isValid);
		}

		#endregion

		public void ReattachState(IUnitOfWork unitOfWork)
		{
			if (InnerModelContext is IDetachedState inner)
			{
				inner.ReattachState(unitOfWork);
			}
		}

		#region IQueryTableContext members

		public string GetRelationshipClassName(string associationName, DdxModel model)
		{
			if (InnerModelContext is IQueryTableContext queryTableContext)
			{
				return queryTableContext.GetRelationshipClassName(associationName, model);
			}

			Association association = DdxModelElementUtils.GetAssociationFromStoredName(
				associationName, model, ignoreUnknownAssociation: true);

			if (association == null)
			{
				return null;
			}

			IRelationshipClass relClass = InnerModelContext.OpenRelationshipClass(association);

			return relClass == null ? null : DatasetUtils.GetName(relClass);
		}

		public bool CanOpenQueryTables()
		{
			return InnerModelContext is IQueryTableContext queryTableContext &&
			       queryTableContext.CanOpenQueryTables();
		}

		public IReadOnlyTable OpenQueryTable(string relationshipClassName,
		                                     DdxModel model,
		                                     IList<IReadOnlyTable> tables,
		                                     JoinType joinType,
		                                     string whereClause)
		{
			if (InnerModelContext is IQueryTableContext queryTableContext &&
			    queryTableContext.CanOpenQueryTables())
			{
				return queryTableContext.OpenQueryTable(relationshipClassName, model, tables,
				                                        joinType, whereClause);
			}

			throw new NotImplementedException();
		}

		#endregion
	}
}
