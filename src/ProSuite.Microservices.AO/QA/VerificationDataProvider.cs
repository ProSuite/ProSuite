using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;
using ProSuite.Microservices.Client.QA;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.AO.QA
{
	public class VerificationDataProvider : IVerificationDataProvider
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly IDatasetRepository _datasetRepository;
		private readonly IDomainTransactionManager _transactionManager;

		private readonly ThreadLocal<IVerificationContext> _verificationContext;

		private ThreadLocalWorkspace _threadLocalWorkspace;

		private readonly ThreadLocal<IDictionary<RelationshipClassQuery, ITable>> _relQueryTables =
			new ThreadLocal<IDictionary<RelationshipClassQuery, ITable>>(
				() => new Dictionary<RelationshipClassQuery, ITable>());

		private readonly ThreadLocal<IDictionary<ClassDef, ITable>> _tablesByClassDef =
			new ThreadLocal<IDictionary<ClassDef, ITable>>(
				() => new Dictionary<ClassDef, ITable>());

		/// <summary>
		/// Initializes a new instance of the <see cref="VerificationDataProvider"/> class.
		/// </summary>
		/// <param name="verificationContextFactory">The factory method that creates the verification context
		/// on the thread that provides the data.</param>
		/// <param name="datasetRepository"></param>
		/// <param name="transactionManager"></param>
		public VerificationDataProvider(
			[NotNull] Func<IVerificationContext> verificationContextFactory,
			[NotNull] IDatasetRepository datasetRepository,
			[NotNull] IDomainTransactionManager transactionManager)
		{
			_verificationContext =
				new ThreadLocal<IVerificationContext>(verificationContextFactory);

			_datasetRepository = datasetRepository;
			_transactionManager = transactionManager;
		}

		public IEnumerable<GdbData> GetData(DataRequest dataRequest)
		{
			yield return ProvideData(dataRequest);
		}

		public SchemaMsg GetGdbSchema(SchemaRequest schemaRequest)
		{
			SchemaMsg result = new SchemaMsg();

			_transactionManager.UseTransaction(
				() => { AddSchemaElementsTx(schemaRequest, result); });

			return result;
		}

		private IFeatureWorkspace VerificationContextWorkspace
		{
			get
			{
				if (_threadLocalWorkspace == null)
				{
					_threadLocalWorkspace =
						new ThreadLocalWorkspace(
							_verificationContext.Value.PrimaryWorkspaceContext.Workspace);
				}

				return (IFeatureWorkspace) _threadLocalWorkspace.Workspace;
			}
		}

		#region Schema request

		private void AddSchemaElementsTx(SchemaRequest schemaRequest, SchemaMsg result)
		{
			// Rel Tables are requested only once a test really requires them, never in conjunction
			// with the rest of the schema.
			if (schemaRequest.RelationshipClassQueries.Count > 0)
			{
				Assert.True(schemaRequest.DatasetIds.Count == 0,
				            "Rel-Table schema requests cannot be combined with dataset schema requests.");

				AddRelQueryTable(result, schemaRequest.RelationshipClassQueries);

				return;
			}

			IList<IObjectDataset> objectDatasets = new List<IObjectDataset>();
			foreach (int datasetId in schemaRequest.DatasetIds.Distinct())
			{
				Dataset dataset = _datasetRepository.Get(datasetId);

				IObjectDataset objectDataset = dataset as IObjectDataset;

				if (objectDataset == null)
				{
					_msg.WarnFormat(
						"Dataset <id> {0} ({1}) not found or not supported for data transfer.",
						datasetId, dataset);
					continue;
				}

				objectDatasets.Add(objectDataset);

				AddObjectClassMsg(objectDataset, result);
			}

			foreach (IErrorDataset issueDataset in VerificationContextUtils.GetIssueDatasets(
				         _verificationContext.Value))
			{
				AddObjectClassMsg(issueDataset, result);
			}

			IEnumerable<Association> associations = GetAssociations(objectDatasets);

			foreach (Association association in associations)
			{
				if (! objectDatasets.Contains(association.End1.ObjectDataset) &&
				    ! objectDatasets.Contains(association.End2.ObjectDataset))
				{
					continue;
				}

				AddRelationshipClassMsg(association, result);
			}
		}

		private void AddRelQueryTable(
			[NotNull] SchemaMsg result,
			[NotNull] IEnumerable<RelationshipClassQuery> relationshipClassQueries)
		{
			foreach (RelationshipClassQuery relQueryMsg in relationshipClassQueries)
			{
				ITable relTable;
				if (! _relQueryTables.Value.TryGetValue(relQueryMsg, out relTable))
				{
					IRelationshipClass relationshipClass =
						DatasetUtils.OpenRelationshipClass(
							VerificationContextWorkspace, relQueryMsg.RelationshipClassName);

					JoinType joinType = (JoinType) relQueryMsg.JoinType;

					IList<ITable> tables =
						relQueryMsg
							.Tables.Select(t => VerificationContextWorkspace.OpenTable(t))
							.ToList();

					// TODO: Use GetReadOnlyQueryTable to ensure unique OIDs!
					relTable = RelationshipClassUtils.GetQueryTable(
						relationshipClass, tables, joinType, relQueryMsg.WhereClause);

					// Add to cache
					_relQueryTables.Value.Add(relQueryMsg, relTable);
				}

				var tableMsg = ProtobufGdbUtils.ToObjectClassMsg(
					relTable, ((IObjectClass) relTable).ObjectClassID, true);

				result.RelclassDefinitions.Add(tableMsg);
			}
		}

		private void AddRelationshipClassMsg(Association association, SchemaMsg result)
		{
			IRelationshipClass relationshipClass =
				_verificationContext.Value.OpenRelationshipClass(association);

			Assert.NotNull(relationshipClass,
			               $"Cannot open relationship class for {association.Name}");

			ObjectClassMsg relTableMsg =
				ProtobufGdbUtils.ToRelationshipClassMsg(relationshipClass);

			// The currency for workspace handles in QA is the model ID.
			relTableMsg.WorkspaceHandle = association.Model.Id;

			result.RelclassDefinitions.Add(relTableMsg);

			// If it also a table, also add it as a table:
			bool isTable =
				relationshipClass.IsAttributed ||
				relationshipClass.Cardinality == esriRelCardinality.esriRelCardinalityManyToMany;

			if (isTable)
			{
				AddTable(relTableMsg, (ITable) relationshipClass, result);
			}
		}

		private static IEnumerable<Association> GetAssociations(
			[NotNull] IList<IObjectDataset> objectDatasets)
		{
			var associations = new Dictionary<int, Association>();
			foreach (IObjectDataset objectDataset in objectDatasets)
			{
				foreach (Association association in
				         objectDataset.GetAssociationEnds().Select(ae => ae.Association))
				{
					if (associations.ContainsKey(association.Id))
					{
						continue;
					}

					associations.Add(association.Id, association);
				}
			}

			return associations.Values;
		}

		private void AddObjectClassMsg([NotNull] IObjectDataset objectDataset,
		                               [NotNull] SchemaMsg result)
		{
			IObjectClass objectClass = _verificationContext.Value.OpenObjectClass(objectDataset);

			if (objectClass == null)
			{
				_msg.WarnFormat(
					"Object class not found for dataset <id> {0} ({1}).",
					objectDataset.Id, objectDataset);
				return;
			}

			ObjectClassMsg objectClassMsg =
				ProtobufGdbUtils.ToObjectClassMsg((ITable) objectClass,
				                                  objectClass.ObjectClassID,
				                                  true, DatasetUtils.GetAliasName(objectClass));

			// The currency for workspace handles in QA is the model ID.
			objectClassMsg.WorkspaceHandle = objectDataset.Model.Id;

			AddTable(objectClassMsg, (ITable) objectClass, result);
		}

		private void AddTable(ObjectClassMsg tableMsg, ITable table, SchemaMsg resultSchema)
		{
			resultSchema.ClassDefinitions.Add(tableMsg);

			// Also, remember the class for subsequent data queries:
			_tablesByClassDef.Value.Add(CreateClassDef(tableMsg), table);
		}

		private static ClassDef CreateClassDef(ObjectClassMsg objectClassMsg)
		{
			return new ClassDef
			       {
				       ClassHandle = objectClassMsg.ClassHandle,
				       WorkspaceHandle = objectClassMsg.WorkspaceHandle
			       };
		}

		#endregion

		#region Data request

		private GdbData ProvideData(DataRequest dataRequest)
		{
			ITable table;
			switch (dataRequest.TableCase)
			{
				case DataRequest.TableOneofCase.None:
					throw new ArgumentException("No table type defined.");
				case DataRequest.TableOneofCase.ClassDef:
					table = GetObjectClass(dataRequest.ClassDef);
					break;
				case DataRequest.TableOneofCase.RelQueryDef:
					table = GetQueryTable(dataRequest.RelQueryDef);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			IQueryFilter filter = CreateFilter(table, dataRequest.SubFields,
			                                   dataRequest.WhereClause, dataRequest.SearchGeometry);

			GdbData featureData = new GdbData();

			if (dataRequest.CountOnly)
			{
				filter.SubFields =
					GetSubFieldForCounting(table, dataRequest.RelQueryDef != null);

				featureData.GdbObjectCount = GdbQueryUtils.Count(table, filter);
			}
			else
			{
				if (table is IFeatureClass fc)
				{
					_msg.VerboseDebug(
						() => $"{DatasetUtils.GetName(fc)} shape field is {fc.ShapeFieldName}");
					_msg.VerboseDebug(
						() => $"{DatasetUtils.GetName(fc)} object id field is {fc.OIDFieldName}");
				}

				long classHandle = -1;
				if (table is IObjectClass objectClass)
				{
					classHandle = objectClass.ObjectClassID;
				}
				else if (table is IRelationshipClass relClass)
				{
					classHandle = relClass.RelationshipClassID;
				}

				foreach (IRow row in GdbQueryUtils.GetRows(table, filter, true))
				{
					try
					{
						GdbObjectMsg objectMsg =
							ProtobufGdbUtils.ToGdbObjectMsg(
								row, false, true, dataRequest.SubFields);

						objectMsg.ClassHandle = classHandle;

						featureData.GdbObjects.Add(objectMsg);
					}
					catch (Exception e)
					{
						_msg.Debug(
							$"Error converting {GdbObjectUtils.ToString(row)} to object message",
							e);
						throw;
					}
				}

				// Later, we could break up into several messages, if the total size gets too large
			}

			return featureData;
		}

		private ITable GetQueryTable(RelationshipClassQuery queryDef)
		{
			_msg.DebugFormat(
				"Handling data request for Query (RelaltionshipClass: {0}, Tables: {1}, Join Type {2}, where clause {3}",
				queryDef.RelationshipClassName, StringUtils.Concatenate(queryDef.Tables, ", "),
				queryDef.JoinType, queryDef.WhereClause);

			if (_relQueryTables.Value.TryGetValue(queryDef, out ITable table))
			{
				return table;
			}

			throw new InvalidOperationException(
				$"Rel Query Table {queryDef.RelationshipClassName} in model {queryDef.WorkspaceHandle} is not part of the known schema.");
		}

		private ITable GetObjectClass(ClassDef classDef)
		{
			_msg.DebugFormat("Handling data request for class ID {0} in workspace handle {1}",
			                 classDef.ClassHandle, classDef.WorkspaceHandle);

			if (_tablesByClassDef.Value.TryGetValue(classDef, out ITable table))
			{
				return table;
			}

			throw new InvalidOperationException(
				$"Class {classDef.ClassHandle} in model {classDef.WorkspaceHandle} is not part of the known schema.");
		}

		private static IQueryFilter CreateFilter([NotNull] ITable objectClass,
		                                         [CanBeNull] string subFields,
		                                         [CanBeNull] string whereClause,
		                                         [CanBeNull] ShapeMsg searchGeometryMsg)
		{
			subFields = EnsureOIDFieldName(subFields, objectClass);

			IFeatureClass featureClass = objectClass as IFeatureClass;

			if (featureClass == null)
			{
				return CreateFilter(subFields, whereClause);
			}

			IGeometry searchGeometry = ProtobufGeometryUtils.FromShapeMsg(
				searchGeometryMsg, DatasetUtils.GetSpatialReference(featureClass));

			if (searchGeometry == null)
			{
				return CreateFilter(subFields, whereClause);
			}

			IQueryFilter result = GdbQueryUtils.CreateSpatialFilter(featureClass, searchGeometry);

			SetSubfieldsAndWhereClause(result, subFields, whereClause);

			return result;
		}

		private static string EnsureOIDFieldName(string subFields, ITable objectClass)
		{
			if (string.IsNullOrEmpty(subFields))
			{
				return subFields;
			}

			if (subFields.Contains("*"))
			{
				return subFields;
			}

			if (objectClass.HasOID && ! subFields.Contains(objectClass.OIDFieldName))
			{
				return objectClass.OIDFieldName + "," + subFields;
			}

			return subFields;
		}

		private static IQueryFilter CreateFilter([CanBeNull] string subFields,
		                                         [CanBeNull] string whereClause)
		{
			IQueryFilter result = new QueryFilterClass();

			SetSubfieldsAndWhereClause(result, subFields, whereClause);

			return result;
		}

		private static string GetSubFieldForCounting(ITable objectClass,
		                                             bool isRelQueryTable)
		{
			if (isRelQueryTable && objectClass is IFeatureClass featureClass)
			{
				// Workaround for TOP-4975: crash for certain joins/extents if OID field 
				// (which was incorrectly changed by IName.Open()!) is used as only subfields field
				// Note: when not crashing, the resulting row count was incorrect when that OID field was used.
				return featureClass.ShapeFieldName;
			}

			return objectClass.OIDFieldName;
		}

		private static void SetSubfieldsAndWhereClause([NotNull] IQueryFilter result,
		                                               [CanBeNull] string subFields,
		                                               [CanBeNull] string whereClause)
		{
			if (! string.IsNullOrEmpty(subFields))
			{
				result.SubFields = subFields;
			}

			if (! string.IsNullOrEmpty(whereClause))
			{
				result.WhereClause = whereClause;
			}
		}

		#endregion
	}
}
