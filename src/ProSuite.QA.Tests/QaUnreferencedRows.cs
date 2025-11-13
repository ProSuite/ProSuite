using System;
using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Logging;
using ProSuite.QA.Container;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.KeySets;
using System.Linq;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[AttributeTest]
	public class QaUnreferencedRows : NonContainerTest
	{
		[NotNull] private readonly IReadOnlyTable _referencedTable;
		[NotNull] private readonly string _referencedTableKey;
		[NotNull] private readonly IList<IReadOnlyTable> _referencingTables;
		[NotNull] private readonly IList<string> _relations;

		private ITableFilter _referencedTableFilter;

		private ReferencedTableInfo _referencedTableInfo;
		private IList<ReferencingTableInfo> _referencingTableInfos;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string ValueNotReferenced = "ValueNotReferenced";
			public const string ConversionError = "ConversionError";

			public Code() : base("UnreferencedRows") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaUnreferencedRows_0))]
		public QaUnreferencedRows(
			[Doc(nameof(DocStrings.QaUnreferencedRows_referencedTable))] [NotNull]
			IReadOnlyTable referencedTable,
			[Doc(nameof(DocStrings.QaUnreferencedRows_referencingTables))] [NotNull]
			IList<IReadOnlyTable>
				referencingTables,
			[Doc(nameof(DocStrings.QaUnreferencedRows_relations))] [NotNull]
			IList<string> relations)
			: base(Union(new[] { referencedTable }, referencingTables))
		{
			Assert.ArgumentNotNull(referencedTable, nameof(referencedTable));
			Assert.ArgumentNotNull(referencingTables, nameof(referencingTables));
			Assert.ArgumentNotNull(relations, nameof(relations));

			Assert.ArgumentCondition(relations.Count == referencingTables.Count,
			                         "# of referencers != # of foreignKeys");

			_referencedTable = referencedTable;
			_referencingTables = referencingTables;
			_relations = relations;

			_referencedTableKey = GetUniqueReferencedTableKey(relations);
		}

		[InternallyUsedTest]
		public QaUnreferencedRows([NotNull] QaUnreferencedRowsDefinition definition)
			: this((IReadOnlyTable) definition.ReferencedTable,
			       definition.ReferencingTables.Cast<IReadOnlyTable>().ToList(),
			       definition.Relations)
		{ }

		#region Overrides of TestBase

		public override int Execute()
		{
			return ExecuteGeometry(null);
		}

		public override int Execute(IEnvelope boundingBox)
		{
			return ExecuteGeometry(boundingBox);
		}

		public override int Execute(IPolygon area)
		{
			return ExecuteGeometry(area);
		}

		public override int Execute(IEnumerable<IReadOnlyRow> selectedRows)
		{
			var referencedTableRows = new List<IReadOnlyRow>();

			foreach (IReadOnlyRow row in selectedRows)
			{
				if (row.Table != _referencedTable)
				{
					continue;
				}

				referencedTableRows.Add(row);
			}

			return VerifyRows(referencedTableRows);
		}

		public override int Execute(IReadOnlyRow row)
		{
			return row.Table != _referencedTable
				       ? NoError
				       : VerifyRows(new[] { row });
		}

		protected override ISpatialReference GetSpatialReference()
		{
			var geoDataset = _referencedTable as IReadOnlyGeoDataset;
			return geoDataset?.SpatialReference;
		}

		#endregion

		private int ExecuteGeometry([CanBeNull] IGeometry geometry)
		{
			if (! (_referencedTable is IReadOnlyFeatureClass))
			{
				geometry = null;
			}

			//TODO use TableJoinUtils where appropriate
			ITableFilter filter = TestUtils.CreateFilter(geometry, AreaOfInterest,
			                                             GetConstraint(0),
			                                             _referencedTable,
			                                             null);

			TableFilterUtils.SetSubFields(filter,
			                              _referencedTable.OIDFieldName,
			                              _referencedTableKey);

			var enumCursor = _referencedTable.EnumRows(filter, recycle: true);

			return VerifyRows(enumCursor);
		}

		private int VerifyRows([NotNull] IEnumerable<IReadOnlyRow> referencedTableRows)
		{
			EnsureReferenceInfos();

			const int max = 100000;

			var errorCount = 0;
			var count = 0;

			foreach (IReadOnlyRow row in referencedTableRows)
			{
				_referencedTableInfo.AddReferencedRow(row);

				count++;
				if (count > max)
				{
					errorCount +=
						VerifyRowsExist(_referencedTableInfo, _referencingTableInfos);
					count = 0;
				}
			}

			errorCount += VerifyRowsExist(_referencedTableInfo, _referencingTableInfos);

			return errorCount;
		}

		private int VerifyRowsExist(
			[NotNull] ReferencedTableInfo referencedTableInfo,
			[NotNull] IEnumerable<ReferencingTableInfo> referencingTableInfos)
		{
			int errorCount = RemoveExist(referencedTableInfo, referencingTableInfos);

			if (referencedTableInfo.KeySet.Count == 0)
			{
				return errorCount;
			}

			const bool recycle = true;
			foreach (IReadOnlyRow row in TableFilterUtils.GetRowsInList(
				         referencedTableInfo.Table,
				         referencedTableInfo.KeyFieldName,
				         referencedTableInfo.KeySet,
				         recycle,
				         _referencedTableFilter))
			{
				if (CancelTestingRow(row, recycleUnique: Guid.NewGuid()))
				{
					continue;
				}

				object key = row.get_Value(referencedTableInfo.KeyFieldIndex);

				string description =
					string.Format(
						"Value [{0}] is not referenced by any table",
						key);

				string fieldName;
				TestUtils.GetFieldDisplayName(row, referencedTableInfo.KeyFieldIndex,
				                              out fieldName);

				errorCount += ReportError(
					description, InvolvedRowUtils.GetInvolvedRows(row), TestUtils.GetShapeCopy(row),
					Codes[Code.ValueNotReferenced], fieldName);
			}

			referencedTableInfo.KeySet.Clear();

			return errorCount;
		}

		private int RemoveExist(
			[NotNull] ReferencedTableInfo referencedTableInfo,
			[NotNull] IEnumerable<ReferencingTableInfo> referencingTableInfos)
		{
			foreach (ReferencingTableInfo referencingTableInfo in referencingTableInfos)
			{
				foreach (IReadOnlyRow row in
				         GetReferencingRows(referencedTableInfo, referencingTableInfo))
				{
					object foreignKey = referencingTableInfo.GetForeignKey(row);

					string errorDescription;
					object convertedValue;
					if (! TryConvertForeignKey(referencingTableInfo, foreignKey,
					                           referencedTableInfo,
					                           out errorDescription, out convertedValue))
					{
						return ReportError(
							errorDescription, InvolvedRowUtils.GetInvolvedRows(row),
							TestUtils.GetShapeCopy(row), Codes[Code.ConversionError], null);
					}

					referencedTableInfo.RemoveObject(convertedValue);
				}
			}

			return NoError;
		}

		private bool TryConvertForeignKey(
			[NotNull] ReferencingTableInfo referencingTableInfo,
			[NotNull] object foreignKey,
			[NotNull] ReferencedTableInfo referencedTableInfo,
			[NotNull] out string errorDescription,
			[CanBeNull] out object convertedValue)
		{
			try
			{
				convertedValue = FieldUtils.ConvertAttributeValue(
					foreignKey,
					referencingTableInfo.ForeignKeyFieldType,
					referencedTableInfo.KeyFieldType);

				errorDescription = string.Empty;
				return true;
			}
			catch (Exception e)
			{
				errorDescription =
					FieldValueUtils.GetTypeConversionErrorDescription(
						_referencedTable, foreignKey,
						referencingTableInfo.ForeignKeyFieldName,
						referencedTableInfo.KeyFieldName,
						e.Message);

				convertedValue = null;
				return false;
			}
		}

		[NotNull]
		private static IEnumerable<IReadOnlyRow> GetReferencingRows(
			[NotNull] ReferencedTableInfo referencedTableInfo,
			[NotNull] ReferencingTableInfo referencingTableInfo)
		{
			const bool recycle = true;
			const int maxKeyCount = 20;

			if (referencedTableInfo.KeySet.Count <= maxKeyCount)
			{
				return TableFilterUtils.GetRowsInList(
					referencingTableInfo.Table,
					referencingTableInfo.ForeignKeyFieldName,
					referencedTableInfo.KeySet, recycle,
					referencingTableInfo.Filter);
			}

			var queryFilter =
				new AoTableFilter
				{
					SubFields = referencingTableInfo.Filter.SubFields,
					WhereClause = $"{referencingTableInfo.ForeignKeyFieldName} IS NOT NULL"
				};

			return referencingTableInfo.Table.EnumRows(queryFilter, recycle);
		}

		private void EnsureReferenceInfos()
		{
			if (_referencingTableInfos != null)
			{
				return;
			}

			_referencedTableInfo = new ReferencedTableInfo(_referencedTable,
			                                               _referencedTableKey);
			_referencingTableInfos = new List<ReferencingTableInfo>();

			for (var i = 0; i < _referencingTables.Count; i++)
			{
				IReadOnlyTable referencingTable = _referencingTables[i];

				int referencingTableIndex = i + 1; // first index is referenced table
				string whereClause = GetConstraint(referencingTableIndex);

				string relation = _relations[i];

				var referencingTableInfo = new ReferencingTableInfo(
					referencingTable, relation, whereClause);

				bool supported =
					KeySetUtils.IsSupportedTypeCombination(
						_referencedTableInfo.KeyFieldType,
						referencingTableInfo.ForeignKeyFieldType);

				Assert.ArgumentCondition(
					supported,
					"key fields have unsupported combination of types: " +
					"referencing table = {0}, foreign key = {1}, referenced key = {2}",
					referencingTableInfo.Table.Name,
					referencingTableInfo.ForeignKeyFieldType,
					_referencedTableInfo.KeyFieldType);

				_referencingTableInfos.Add(referencingTableInfo);
			}

			_referencedTableFilter = new AoTableFilter
			                         {
				                         WhereClause = GetConstraint(0)
			                         };

			TableFilterUtils.SetSubFields(_referencedTableFilter,
			                              _referencedTableInfo.KeyFieldName,
			                              _referencedTable.OIDFieldName);
		}

		[NotNull]
		private static string GetUniqueReferencedTableKey(
			[NotNull] IEnumerable<string> relations)
		{
			Assert.ArgumentNotNull(relations, nameof(relations));

			string result = null;
			foreach (string relation in relations)
			{
				string referencedTableKey = GetReferencedTableKey(relation);

				if (result == null)
				{
					result = referencedTableKey;
				}
				else if (! result.Equals(referencedTableKey,
				                         StringComparison.InvariantCultureIgnoreCase))
				{
					throw new ArgumentException(
						@"All relations must refer to the same key field on the referenced table",
						nameof(relations));
				}
			}

			return Assert.NotNull(
				result, "Unable to determine unique referenced table key");
		}

		[NotNull]
		private static string GetReferencedTableKey([NotNull] string relation)
		{
			Assert.ArgumentNotNullOrEmpty(relation, nameof(relation));

			IList<string> tokens = ParseRelationTokens(relation);

			Assert.True(tokens.Count > 0, "Invalid number of tokens in relation {0}",
			            relation);

			return tokens[0].Trim();
		}

		[NotNull]
		private static IList<string> ParseRelationTokens([NotNull] string relation)
		{
			Assert.ArgumentNotNullOrEmpty(relation, nameof(relation));

			return relation.Split(new[] { ',', ';', ' ' },
			                      StringSplitOptions.RemoveEmptyEntries);
		}

		private class ReferencedTableInfo
		{
			public ReferencedTableInfo([NotNull] IReadOnlyTable keyTable,
			                           [NotNull] string keyFieldName)
			{
				Table = keyTable;
				KeyFieldName = keyFieldName;
				KeyFieldIndex = keyTable.FindField(keyFieldName);

				const string format = "'field '{0}' not found in table '{1}'";
				Assert.ArgumentCondition(KeyFieldIndex >= 0,
				                         format, keyFieldName,
				                         keyTable.Name);

				KeyFieldType = KeySetUtils.GetFieldValueType(keyTable, KeyFieldIndex);
				KeySet = KeySetUtils.CreateKeySet(KeyFieldType);
			}

			[NotNull]
			public IReadOnlyTable Table { get; }

			[NotNull]
			public string KeyFieldName { get; }

			public int KeyFieldIndex { get; }

			public esriFieldType KeyFieldType { get; }

			[NotNull]
			public IKeySet KeySet { get; }

			public void AddReferencedRow([NotNull] IReadOnlyRow row)
			{
				object key = row.get_Value(KeyFieldIndex);

				if (key == DBNull.Value || key == null)
				{
					// TODO report as Error?
					return;
				}

				// TODO handle errors (e.g. invalid guid strings)
				bool added = KeySet.Add(key);

				if (! added)
				{
					_msg.DebugFormat(
						"Ignored duplicate key found in field '{0}' in table '{1}': {2}",
						KeyFieldName, Table.Name, key);
				}
			}

			public void RemoveObject(object foreignKey)
			{
				KeySet.Remove(foreignKey);
			}
		}

		private class ReferencingTableInfo
		{
			private readonly string _foreignKeyFieldName;

			private readonly int _foreignKeyFieldIndex;

			public ReferencingTableInfo([NotNull] IReadOnlyTable referencingTable,
			                            [NotNull] string relation,
			                            [CanBeNull] string whereClause)
			{
				Table = GetJoinedTable(referencingTable, relation, out _foreignKeyFieldName);

				Filter = new AoTableFilter { WhereClause = whereClause };

				TableFilterUtils.SetSubFields(Filter,
				                              _foreignKeyFieldName);

				_foreignKeyFieldIndex = Table.FindField(_foreignKeyFieldName);
				ForeignKeyFieldType =
					KeySetUtils.GetFieldValueType(Table, _foreignKeyFieldIndex);
			}

			[NotNull]
			private static IReadOnlyTable GetJoinedTable([NotNull] IReadOnlyTable referencingTable,
			                                             [NotNull] string relation,
			                                             [NotNull] out string foreignKeyFieldName)
			{
				IList<string> relationTokens = ParseRelationTokens(relation);

				int relationCount = relationTokens.Count;

				if (relationCount == 2)
				{
					foreignKeyFieldName = relationTokens[1];
					return referencingTable;
				}

				if (relationCount == 5)
				{
					string referencingTableName = referencingTable.Name;

					string relationReferencedTableFK = relationTokens[1];
					string relationTable = relationTokens[2];
					string relationToReferencingTableFK = relationTokens[3];
					string referencingTablePK = relationTokens[4];

					string resultTableName =
						$"{referencingTableName}_{relationTable.Replace(".", "_")}";

					foreignKeyFieldName = $"{relationTable}.{relationReferencedTableFK}";

					var workspace =
						(IFeatureWorkspace) referencingTable.Workspace;

					bool useInMemoryJoin =
						workspace is GdbWorkspace || workspace is VirtualWorkspace;

					if (useInMemoryJoin)
					{
						return CreateJoinedTable(workspace, referencingTable, referencingTablePK,
						                         relationTable, relationToReferencingTableFK,
						                         resultTableName);
					}

					IQueryDef queryDef = workspace.CreateQueryDef();

					queryDef.Tables =
						string.Format("{0},{1}", referencingTableName, relationTable);

					queryDef.SubFields = string.Format("{0}.{1},{2}.{3}",
					                                   referencingTableName,
					                                   referencingTablePK,
					                                   relationTable,
					                                   relationReferencedTableFK);

					queryDef.WhereClause = string.Format("{0}.{1} = {2}.{3}",
					                                     referencingTableName,
					                                     referencingTablePK,
					                                     relationTable,
					                                     relationToReferencingTableFK);

					IQueryName2 queryName = new TableQueryNameClass
					                        {
						                        CopyLocally = false,
						                        QueryDef = queryDef
					                        };

					var name = (IDatasetName) queryName;
					name.WorkspaceName = WorkspaceUtils.GetWorkspaceName(workspace);

					name.Name = resultTableName;

					if (_msg.IsVerboseDebugEnabled)
					{
						_msg.Debug("Creating query-based feature class");

						using (_msg.IncrementIndentation())
						{
							LogQueryName(queryName);
						}
					}

					try
					{
						return ReadOnlyTableFactory.Create((ITable) ((IName) queryName).Open());
					}
					catch (Exception e)
					{
						_msg.DebugFormat("Error creating query-based table: {0}", e.Message);
						LogQueryName(queryName);

						throw;
					}
				}

				throw new InvalidConfigurationException($"Cannot parse relation: {relation}");
			}

			private static IReadOnlyTable CreateJoinedTable(
				[NotNull] IFeatureWorkspace workspace,
				[NotNull] IReadOnlyTable referencingTable,
				[NotNull] string referencingTablePK,
				[NotNull] string relationTable,
				[NotNull] string relationToReferencingTableFK,
				[NotNull] string resultTableName)
			{
				ITable bridgeTable = workspace.OpenTable(relationTable);

				IReadOnlyTable relationTableReadOnly =
					bridgeTable as IReadOnlyTable ??
					ReadOnlyTableFactory.Create(bridgeTable);

				// NOTE: The inner join is symmetric. Use the relationTable as the left table which
				//       might contain fewer rows than the referencingTable.
				AssociationDescription associationDescription =
					new ForeignKeyAssociationDescription(
						referencingTable, referencingTablePK,
						relationTableReadOnly, relationToReferencingTableFK);

				GdbTable joinedTable = TableJoinUtils.CreateJoinedGdbTable(
					associationDescription, relationTableReadOnly, resultTableName, false);

				return joinedTable;
			}

			private static void LogQueryName([NotNull] IQueryName2 queryName)
			{
				IQueryDef queryDef = queryName.QueryDef;

				_msg.DebugFormat("query table name: {0}", ((IDatasetName) queryName).Name);
				_msg.DebugFormat("copy locally: {0}", queryName.CopyLocally);
				_msg.DebugFormat("primary key: [{0}]", queryName.PrimaryKey);
				_msg.DebugFormat("query:");
				_msg.DebugFormat("SELECT {0} FROM {1} WHERE {2}",
				                 queryDef.SubFields,
				                 queryDef.Tables,
				                 queryDef.WhereClause);
			}

			[NotNull]
			public string ForeignKeyFieldName => _foreignKeyFieldName;

			[NotNull]
			public IReadOnlyTable Table { get; }

			[NotNull]
			public ITableFilter Filter { get; }

			public esriFieldType ForeignKeyFieldType { get; }

			public object GetForeignKey([NotNull] IReadOnlyRow row)
			{
				return row.get_Value(_foreignKeyFieldIndex);
			}
		}
	}
}
