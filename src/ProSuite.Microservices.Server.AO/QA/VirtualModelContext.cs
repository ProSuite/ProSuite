using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.GeoDb;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared.Gdb;
using ProSuite.Microservices.Server.AO.Geodatabase;

namespace ProSuite.Microservices.Server.AO.QA
{
	public class VirtualModelContext : IModelContext, IQueryTableContext, IVirtualModelContext
	{
		private IList<GdbWorkspace> _virtualWorkspaces;

		private readonly DdxModel _primaryModel;

		private readonly Func<DataVerificationResponse, DataVerificationRequest> _dataRequestFunc;

		[CanBeNull] private readonly Func<DataVerificationRequest> _readNextBatchFunc;

		public VirtualModelContext(IList<GdbWorkspace> workspaces,
		                           DdxModel model)
		{
			_virtualWorkspaces = workspaces;
			_primaryModel = model;

			GdbWorkspace primaryWorkspace = workspaces[0];

			PrimaryWorkspaceContext = GetWorkspaceContext(model, primaryWorkspace);
		}

		public VirtualModelContext(
			Func<DataVerificationResponse, DataVerificationRequest> dataRequestFunc,
			DdxModel model,
			[CanBeNull] Func<DataVerificationRequest> readNextBatchFunc = null)
		{
			_dataRequestFunc = dataRequestFunc;
			_readNextBatchFunc = readNextBatchFunc;
			_primaryModel = model;

			// Empty until schema is set:
			PrimaryWorkspaceContext = new SimpleWorkspaceContext(
				model, new GdbWorkspace(new GdbTableContainer()),
				new List<WorkspaceDataset>(),
				new List<WorkspaceAssociation>());
		}

		public void InitializeSchema(ICollection<Dataset> datasets)
		{
			if (_dataRequestFunc == null)
			{
				return;
			}

			if (_virtualWorkspaces != null)
			{
				return;
			}

			var dataRequest = new DataVerificationResponse
			                  {
				                  SchemaRequest = new SchemaRequest()
			                  };

			dataRequest.SchemaRequest.DatasetIds.AddRange(datasets.Select(d => d.Id));

			DataVerificationRequest dataResponse = _dataRequestFunc(dataRequest);

			if (dataResponse.Schema == null)
			{
				// The client could not provide the schema (e.g. a dataset or relationship class
				// referenced by the model does not exist in the client's workspace). Fail with a
				// descriptive error instead of a NullReferenceException, mirroring how data
				// requests surface client-side errors (see RemoteDataset.ConfirmDataReceived).
				throw new DataAccessException(
					"The client failed to provide the schema for the requested datasets: " +
					dataResponse.ErrorMessage);
			}

			SetGdbSchema(ProtobufConversionUtils.CreateSchema(
				             dataResponse.Schema.ClassDefinitions,
				             dataResponse.Schema.RelclassDefinitions, _dataRequestFunc,
				             _readNextBatchFunc));
		}

		private void SetGdbSchema(IList<GdbWorkspace> gdbWorkspaces)
		{
			_virtualWorkspaces = gdbWorkspaces;

			GdbWorkspace primaryWorkspace =
				gdbWorkspaces.First(w => w.WorkspaceHandle == _primaryModel.Id);

			PrimaryWorkspaceContext = GetWorkspaceContext(_primaryModel, primaryWorkspace);
		}

		private static IWorkspaceContext GetWorkspaceContext(
			DdxModel model, GdbWorkspace gdbWorkspace)
		{
			return new SimpleWorkspaceContext(
				model, gdbWorkspace,
				GetWorkspaceDatasets(model, gdbWorkspace),
				new List<WorkspaceAssociation>());
		}

		private static IEnumerable<WorkspaceDataset> GetWorkspaceDatasets(
			DdxModel model, GdbWorkspace primaryWorkspace)
		{
			foreach (IDataset gdbDataset in primaryWorkspace.GetDatasets())
			{
				Dataset modelDataset = model.GetDatasetByModelName(gdbDataset.Name);

				if (modelDataset != null)
				{
					yield return new WorkspaceDataset(gdbDataset.Name, null, modelDataset);
				}
			}
		}

		#region IModelContext members

		public bool IsEditableInCurrentState(IDdxDataset dataset)
		{
			return false;
		}

		public bool CanOpen(IDdxDataset dataset)
		{
			return _virtualWorkspaces.Any(w => w.WorkspaceHandle == dataset.Model.Id);
		}

		public IFeatureClass OpenFeatureClass(IVectorDataset dataset)
		{
			GdbWorkspace workspace = GetWorkspace(dataset.Model);

			return workspace?.OpenFeatureClass(dataset.Name);
		}

		public ITable OpenTable(IObjectDataset dataset)
		{
			GdbWorkspace workspace = GetWorkspace(dataset.Model);

			return workspace?.OpenTable(dataset.Name);
		}

		public IObjectClass OpenObjectClass(IObjectDataset dataset)
		{
			return (IObjectClass) OpenTable(dataset);
		}

		public TopologyReference OpenTopology(ITopologyDataset dataset)
		{
			throw new NotImplementedException();
		}

		public RasterDatasetReference OpenRasterDataset(IDdxRasterDataset dataset)
		{
			// Raster datasets are not streamed from the client. They are opened directly from the
			// model's master (user connection) database on the server.
			return GetMasterDatabaseContext(dataset.Model).OpenRasterDataset(dataset);
		}

		public TerrainReference OpenTerrainReference(ISimpleTerrainDataset dataset)
		{
			IList<SimpleTerrainDataSource> terrainSources =
				ModelElementUtils.GetTerrainDataSources(dataset, OpenObjectClass);

			return new SimpleTerrain(dataset.Name, terrainSources, dataset.PointDensity, null);
		}

		public MosaicRasterReference OpenSimpleRasterMosaic(IRasterMosaicDataset dataset)
		{
			// Mosaic datasets are not streamed from the client. They are opened directly from the
			// model's master (user connection) database on the server.
			return GetMasterDatabaseContext(dataset.Model).OpenSimpleRasterMosaic(dataset);
		}

		public PointCloudReference OpenPointCloud(IPointCloudDataset dataset)
		{
			// Like mosaics, point cloud catalogs are not streamed from the client: the catalog is
			// opened from the model's master database on the server, and the LAS files it points
			// to must be reachable from the server process.
			return GetMasterDatabaseContext(dataset.Model).OpenPointCloud(dataset);
		}

		/// <summary>
		/// Gets the model's master-database (user connection) workspace context, used to open
		/// dataset types that are not transferred from the client (rasters and mosaics).
		/// </summary>
		[NotNull]
		private static IWorkspaceContext GetMasterDatabaseContext([NotNull] DdxModel model)
		{
			IWorkspaceContext masterContext = model.GetMasterDatabaseWorkspaceContext();

			return Assert.NotNull(
				masterContext,
				"Cannot open raster/mosaic dataset: the master database of model '{0}' is not " +
				"accessible from the server.", model.Name);
		}

		public IRelationshipClass OpenRelationshipClass(Association association)
		{
			IFeatureWorkspace workspace = GetWorkspace(association.Model);

			return workspace?.OpenRelationshipClass(association.Name);
		}

		public IWorkspaceContext GetWorkspaceContext(IDdxDataset dataset)
		{
			GdbWorkspace gdbWorkspace = GetWorkspace(dataset.Model);

			return GetWorkspaceContext(dataset.Model, gdbWorkspace);
		}

		public bool IsPrimaryWorkspaceBeingEdited()
		{
			return false;
		}

		public IWorkspaceContext PrimaryWorkspaceContext { get; private set; }

		public IDdxDataset GetDataset(IDatasetName datasetName, bool isValid)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region IQueryTableContext members

		public string GetRelationshipClassName(string associationName, DdxModel model)
		{
			GdbWorkspace workspace =
				_virtualWorkspaces.FirstOrDefault(w => w.WorkspaceHandle == model.Id);

			Assert.NotNull(workspace, "GdbWorkspace for model {0} not available", model);

			return QueryTableUtils.GetRelationshipClassName(workspace, associationName, model);
		}

		public bool CanOpenQueryTables()
		{
			return _dataRequestFunc != null || _virtualWorkspaces != null;
		}

		public IReadOnlyTable OpenQueryTable(string relationshipClassName,
		                                     DdxModel model,
		                                     IList<IReadOnlyTable> tables,
		                                     JoinType joinType,
		                                     string whereClause)
		{
			if (_dataRequestFunc != null)
			{
				return GetRemoteQueryTable(relationshipClassName, model, tables, joinType,
				                           whereClause);
			}

			// If schema was cached locally (no link back) - Only used by unit test (no joinType support etc.)
			if (_virtualWorkspaces != null)
			{
				GdbWorkspace workspace =
					_virtualWorkspaces.FirstOrDefault(w => w.WorkspaceHandle == model.Id);

				if (workspace == null)
				{
					throw new InvalidOperationException($"Workspace for model {model} not found");
				}

				ITable queryTable = workspace.OpenQueryTable(relationshipClassName);

				return ReadOnlyTableFactory.Create(queryTable);
			}

			throw new NotImplementedException();
		}

		#endregion

		private GdbWorkspace GetWorkspace([NotNull] DdxModel model)
		{
			// TODO: In case it is needed before InitializeSchema() is called, allow creation of
			//       a 'lazy' workspace that is just an empty container that gets initialized 
			//       with datasets from remote only when necessary.
			GdbWorkspace workspace =
				_virtualWorkspaces?.FirstOrDefault(w => w.WorkspaceHandle == model.Id);
			return workspace;
		}

		private IReadOnlyTable GetRemoteQueryTable([NotNull] string relationshipClassName,
		                                           [NotNull] DdxModel model,
		                                           [NotNull] IList<IReadOnlyTable> tables,
		                                           JoinType joinType,
		                                           [CanBeNull] string whereClause)
		{
			var dataRequest = new DataVerificationResponse
			                  {
				                  SchemaRequest = new SchemaRequest()
			                  };

			RelationshipClassQuery relClassQueryMsg =
				new RelationshipClassQuery
				{
					RelationshipClassName = relationshipClassName,
					WorkspaceHandle = model.Id,
					JoinType = (int) joinType,
				};

			relClassQueryMsg.Tables.AddRange(tables.Select(t => t.Name));

			if (! string.IsNullOrEmpty(whereClause))
			{
				relClassQueryMsg.WhereClause = whereClause;
			}

			dataRequest.SchemaRequest.RelationshipClassQueries.Add(relClassQueryMsg);

			DataVerificationRequest dataResponse = _dataRequestFunc(dataRequest);

			if (dataResponse?.Schema == null ||
			    dataResponse.Schema.RelclassDefinitions.Count == 0)
			{
				throw new InvalidOperationException(
					$"The client returned no schema for the query table based on " +
					$"relationship class '{relationshipClassName}' " +
					$"(tables: {string.Join(", ", tables.Select(t => t.Name))}). " +
					"The data request to the client most likely failed - check the " +
					"preceding warnings from the verification service (e.g. an error " +
					"writing to the response stream) for the underlying cause.");
			}

			GdbWorkspace gdbWorkspace =
				Assert.NotNull(_virtualWorkspaces).First(w => w.WorkspaceHandle == model.Id);

			ObjectClassMsg queryTableMsg = dataResponse.Schema.RelclassDefinitions.First();

			Assert.NotNull(_dataRequestFunc,
			               "The context is not set up to request query table data.");

			BackingDataset CreateBackingDataset(ITable t) =>
				new RemoteDataset(t, _dataRequestFunc, null, relClassQueryMsg,
				                  _readNextBatchFunc);

			// It is cached on the client side, in case various tests utilize the same definition.
			return ProtobufConversionUtils.FromQueryTableMsg(queryTableMsg, gdbWorkspace,
			                                                 CreateBackingDataset, tables);
		}
	}
}
