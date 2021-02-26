using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared;
using ProSuite.Microservices.Server.AO.Geodatabase;
using ProSuite.QA.Container;

namespace ProSuite.Microservices.Server.AO.QA
{
	public class VirtualModelContext : IModelContext, IQueryTableContext, IVirtualModelContext
	{
		private IList<GdbWorkspace> _virtualWorkspaces;

		private readonly Model _primaryModel;

		private readonly Func<DataVerificationResponse, DataVerificationRequest> _dataRequestFunc;

		public VirtualModelContext(IList<GdbWorkspace> workspaces,
		                           Model model)
		{
			_virtualWorkspaces = workspaces;
			_primaryModel = model;

			GdbWorkspace primaryWorkspace = workspaces[0];

			PrimaryWorkspaceContext = GetWorkspaceContext(model, primaryWorkspace);
		}

		public VirtualModelContext(
			Func<DataVerificationResponse, DataVerificationRequest> dataRequestFunc,
			Model model)
		{
			_dataRequestFunc = dataRequestFunc;
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

			SetGdbSchema(ProtobufConversionUtils.CreateSchema(
				             dataResponse.Schema.ClassDefinitions,
				             dataResponse.Schema.RelclassDefinitions, _dataRequestFunc));
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

		public IRasterDataset OpenRasterDataset(IDdxRasterDataset dataset)
		{
			throw new NotImplementedException();
		}

		public TerrainReference OpenTerrainReference(ISimpleTerrainDataset dataset)
		{
			IFeatureWorkspace workspace = GetWorkspace(dataset.Model);
			return TinTerrainReference.Create(
				workspace.OpenFeatureDataset(dataset.FeatureDatasetName));
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

		public string GetRelationshipClassName(string associationName, Model model)
		{
			GdbWorkspace workspace =
				_virtualWorkspaces.FirstOrDefault(w => w.WorkspaceHandle == model.Id);

			Assert.NotNull(workspace, "GdbWorkspace for model {0} not available", model);

			return GetRelationshipClassName(workspace, associationName, model);
		}

		public bool CanOpenQueryTables()
		{
			return _dataRequestFunc != null || _virtualWorkspaces != null;
		}

		public ITable OpenQueryTable(string relationshipClassName,
		                             Model model,
		                             IList<ITable> tables,
		                             JoinType joinType,
		                             string whereClause)
		{
			if (_dataRequestFunc != null)
			{
				return GetRemoteQueryTable(relationshipClassName, model, tables, joinType,
				                           whereClause);
			}

			// If schema was cached locally (no link back) - Only used by unit test
			if (_virtualWorkspaces != null)
			{
				GdbWorkspace workspace =
					_virtualWorkspaces.FirstOrDefault(w => w.WorkspaceHandle == model.Id);

				if (workspace == null)
				{
					throw new InvalidOperationException($"Workspace for model {model} not found");
				}

				return workspace.OpenQueryTable(relationshipClassName);
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

		private ITable GetRemoteQueryTable([NotNull] string relationshipClassName,
		                                   [NotNull] Model model,
		                                   [NotNull] IList<ITable> tables,
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

			relClassQueryMsg.Tables.AddRange(tables.Select(DatasetUtils.GetName));

			if (! string.IsNullOrEmpty(whereClause))
			{
				relClassQueryMsg.WhereClause = whereClause;
			}

			dataRequest.SchemaRequest.RelationshipClassQueries.Add(relClassQueryMsg);

			DataVerificationRequest dataResponse = _dataRequestFunc(dataRequest);

			GdbWorkspace gdbWorkspace =
				Assert.NotNull(_virtualWorkspaces).First(w => w.WorkspaceHandle == model.Id);

			ObjectClassMsg queryTableMsg = dataResponse.Schema.RelclassDefinitions.First();

			Func<ITable, BackingDataset> createBackingDataset = null;

			if (_dataRequestFunc != null)
			{
				createBackingDataset = (t) =>
					new RemoteDataset(t, _dataRequestFunc, null, relClassQueryMsg);
			}

			// It is cached on the client side, in case various tests utilize the same definition.
			// TODO: Test!
			return ProtobufConversionUtils.FromObjectClassMsg(queryTableMsg, gdbWorkspace,
			                                                  createBackingDataset);
		}

		[NotNull]
		private static string GetRelationshipClassName([NotNull] IWorkspace masterWorkspace,
		                                               [NotNull] string associationName,
		                                               [NotNull] Model model)
		{
			// TODO: Copy from QaRelationTestFactory:

			if (masterWorkspace.Type != esriWorkspaceType.esriRemoteDatabaseWorkspace)
			{
				// the workspace uses unqualified names

				return ModelElementNameUtils.IsQualifiedName(associationName)
					       ? ModelElementNameUtils.GetUnqualifiedName(associationName)
					       : associationName;
			}

			// the workspace uses qualified names

			if (! ModelElementNameUtils.IsQualifiedName(associationName))
			{
				Assert.NotNullOrEmpty(
					model.DefaultDatabaseSchemaOwner,
					"The master database schema owner is not defined, cannot qualify unqualified association name ({0})",
					associationName);

				return ModelElementNameUtils.GetQualifiedName(
					model.DefaultDatabaseName,
					model.DefaultDatabaseSchemaOwner,
					ModelElementNameUtils.GetUnqualifiedName(associationName));
			}

			// the association name is already qualified

			if (StringUtils.IsNotEmpty(model.DefaultDatabaseSchemaOwner))
			{
				return ModelElementNameUtils.GetQualifiedName(
					model.DefaultDatabaseName,
					model.DefaultDatabaseSchemaOwner,
					ModelElementNameUtils.GetUnqualifiedName(associationName));
			}

			return associationName;
		}
	}
}
