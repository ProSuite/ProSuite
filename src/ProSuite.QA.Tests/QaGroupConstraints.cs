using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[AttributeTest]
	public class QaGroupConstraints : NonContainerTest
	{
		private const string _groupByColumn = "__GroupBy";

		private const string _distinctColumn = "__Distinct";

		private readonly IList<IReadOnlyTable> _tables;
		private readonly IList<string> _tableNames;
		private readonly IList<string> _groupByExpressions;
		private readonly bool _limitToTestedRows;

		private readonly IList<string> _distinctExpressions;
		private readonly int? _maxDistinctCount;
		private readonly int _minDistinctCount;
		private IList<TableView> _helpers;

		private RelatedTables _relatedTables;
		private Dictionary<RelatedTable, int> _relatedOidFields;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull] private IList<string> _existsRowGroupFiltersSql =
			new ReadOnlyList<string>(new List<string>());

		[CanBeNull] private IList<RowCondition> _existsRowGroupFilter;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string DistinctValues_TooMany = "DistinctValues.TooMany";

			public const string DistinctValues_NotExpectedNumber =
				"DistinctValues.NotExpectedNumber";

			public const string DistinctValues_NotInExpectedRange =
				"DistinctValues.NotInExpectedRange";

			public const string DistinctValues_TooFew = "DistinctValues.TooFew";

			public Code() : base("GroupConstraints") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaGroupConstraints_0))]
		public QaGroupConstraints(
			[Doc(nameof(DocStrings.QaGroupConstraints_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaGroupConstraints_groupByExpression))] [NotNull]
			string groupByExpression,
			[Doc(nameof(DocStrings.QaGroupConstraints_distinctExpression))] [NotNull]
			string distinctExpression,
			[Doc(nameof(DocStrings.QaGroupConstraints_maxDistinctCount))]
			int maxDistinctCount,
			[Doc(nameof(DocStrings.QaGroupConstraints_limitToTestedRows))]
			bool limitToTestedRows)
			: this(new[] { table }, new[] { groupByExpression }, new[] { distinctExpression },
			       0, maxDistinctCount, limitToTestedRows) { }

		[Doc(nameof(DocStrings.QaGroupConstraints_1))]
		public QaGroupConstraints(
			[Doc(nameof(DocStrings.QaGroupConstraints_tables))] [NotNull]
			IList<IReadOnlyTable> tables,
			[Doc(nameof(DocStrings.QaGroupConstraints_groupByExpressions))] [NotNull]
			IList<string> groupByExpressions,
			[Doc(nameof(DocStrings.QaGroupConstraints_distinctExpressions))] [NotNull]
			IList<string> distinctExpressions,
			[Doc(nameof(DocStrings.QaGroupConstraints_maxDistinctCount))]
			int maxDistinctCount,
			[Doc(nameof(DocStrings.QaGroupConstraints_limitToTestedRows))]
			bool limitToTestedRows)
			: this(tables, groupByExpressions, distinctExpressions,
			       0, maxDistinctCount, limitToTestedRows) { }

		// TODO document
		[Doc(nameof(DocStrings.QaGroupConstraints_1))]
		public QaGroupConstraints(
			[Doc(nameof(DocStrings.QaGroupConstraints_tables))] [NotNull]
			IList<IReadOnlyTable> tables,
			[Doc(nameof(DocStrings.QaGroupConstraints_groupByExpressions))] [NotNull]
			IList<string> groupByExpressions,
			[Doc(nameof(DocStrings.QaGroupConstraints_distinctExpressions))] [NotNull]
			IList<string> distinctExpressions,
			[Doc(nameof(DocStrings.QaGroupConstraints_minDistinctCount))]
			int minDistinctCount,
			[Doc(nameof(DocStrings.QaGroupConstraints_maxDistinctCount))]
			int maxDistinctCount,
			[Doc(nameof(DocStrings.QaGroupConstraints_limitToTestedRows))]
			bool limitToTestedRows)
			: base(tables)
		{
			Assert.AreEqual(tables.Count, groupByExpressions.Count,
			                "tables count and groupByExpressions count differ");
			Assert.AreEqual(tables.Count, distinctExpressions.Count,
			                "tables count and distinctExpressions count differ");
			Assert.ArgumentCondition(
				maxDistinctCount < 0 || maxDistinctCount >= minDistinctCount,
				"Maximum distinct count must be either negative (undefined) or >= the minimum distinct count ({0}): {1}",
				minDistinctCount, maxDistinctCount);

			_tables = tables;
			_groupByExpressions = groupByExpressions.ToList();

			_limitToTestedRows = limitToTestedRows;

			_distinctExpressions = distinctExpressions;
			_maxDistinctCount = maxDistinctCount >= 0
				                    ? (int?) maxDistinctCount
				                    : null;
			_minDistinctCount = minDistinctCount;

			_tableNames = new List<string>(tables.Count);
			foreach (IReadOnlyTable table in tables)
			{
				_tableNames.Add(table.Name);
			}
		}

		[InternallyUsedTest]
		public QaGroupConstraints(QaGroupConstraintsDefinition definition)
			: this(definition.Tables.Cast<IReadOnlyTable>()
			                 .ToList(),
			       definition.GroupByExpressions,
			       definition.DistinctExpressions,
			       definition.MinDistinctCount,
			       definition.MaxDistinctCount,
			       definition.LimitToTestedRows
			)
		{
			ExistsRowGroupFilters = definition.ExistsRowGroupFilters;
		}

		[TestParameter]
		public IList<string> ExistsRowGroupFilters
		{
			get { return _existsRowGroupFiltersSql; }
			set
			{
				Assert.ArgumentCondition(value == null ||
				                         value.Count == 0 || value.Count == 1 ||
				                         value.Count == _tables.Count,
				                         "unexpected number of group predicates " +
				                         "(must be 0, 1, or equal to the number of tables");

				_existsRowGroupFiltersSql = new ReadOnlyList<string>(value?.ToList() ??
						new List<string>());
				_existsRowGroupFilter = null;
			}
		}

		#region ITest Members

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

		public override int Execute(IEnumerable<IReadOnlyRow> selection)
		{
			Init();

			if (_limitToTestedRows)
			{
				return FindErrors(GetTableIndexRows(selection), null);
			}

			IEnumerable<TableIndexRow> rowsToTest = EnumerateRows(null);

			return FindErrors(rowsToTest,
			                  GetStatisticsPerGroup(GetTableIndexRows(selection)));
		}

		public override int Execute(IReadOnlyRow row)
		{
			return Execute(new[] { row });
		}

		protected override ISpatialReference GetSpatialReference()
		{
			return _tables.OfType<IReadOnlyGeoDataset>()
			              .Select(gds => gds.SpatialReference)
			              .FirstOrDefault();
		}

		#endregion

		public void SetRelatedTables([NotNull] IList<IReadOnlyTable> relatedTables)
		{
			Assert.True(_tables.Count == 1,
			            "Related tables are not supported for multiple test tables");

			_relatedTables = RelatedTables.Create(relatedTables, _tables[0]);
		}

		[NotNull]
		private IEnumerable<TableIndexRow> GetTableIndexRows(
			[NotNull] IEnumerable<IReadOnlyRow> rows)
		{
			var helpers = new Dictionary<IReadOnlyTable, IDictionary<int, TableView>>();
			// ITable with associated TableIndices/TableViews

			foreach (IReadOnlyRow row in rows)
			{
				var feature = row as IReadOnlyFeature;
				if (feature != null && AreaOfInterest != null &&
				    ((IRelationalOperator) AreaOfInterest).Disjoint(feature.Shape))
				{
					continue;
				}

				if (CancelTestingRow(row))
				{
					continue;
				}

				IReadOnlyTable table = row.Table;
				IDictionary<int, TableView> tableHelpers;
				if (! helpers.TryGetValue(table, out tableHelpers))
				{
					tableHelpers = CreateHelpers(table);
					helpers.Add(table, tableHelpers);
				}

				foreach (KeyValuePair<int, TableView> helper in tableHelpers)
				{
					if (helper.Value.MatchesConstraint(row))
					{
						yield return new TableIndexRow(helper.Key, row);
					}
				}
			}
		}

		[NotNull]
		private string GetGroup([NotNull] TableIndexRow row)
		{
			string group;
			GetDataRow(row, out group);
			return group;
		}

		[NotNull]
		private DataRow GetDataRow([NotNull] TableIndexRow tableIndexRow,
		                           [NotNull] out string group)
		{
			TableView helper = _helpers[tableIndexRow.TableIndex];

			var result = Assert.NotNull(helper.Add(tableIndexRow.Row), "no dataRow");

			object value = result[_groupByColumn];

			group = value == null || value is DBNull
				        ? string.Empty
				        : value.ToString();

			return result;
		}

		[NotNull]
		private IDictionary<int, TableView> CreateHelpers([NotNull] IReadOnlyTable table)
		{
			return _tables.Where(t => t == table)
			              .Select((t, i) => new KeyValuePair<int, TableView>(
				                      i, TableViewFactory.Create(t, GetConstraint(i))))
			              .ToDictionary(p => p.Key, p => p.Value);
		}

		[NotNull]
		private IDictionary<string, GroupStatistics> GetStatisticsPerGroup(
			[NotNull] IEnumerable<TableIndexRow> selection)
		{
			var result = new Dictionary<string, GroupStatistics>();

			foreach (TableIndexRow tableIndexRow in selection)
			{
				var feature = tableIndexRow.Row as IReadOnlyFeature;

				if (feature != null && AreaOfInterest != null &&
				    ((IRelationalOperator) AreaOfInterest).Disjoint(feature.Shape))
				{
					continue;
				}

				if (CancelTestingRow(tableIndexRow.Row))
				{
					continue;
				}

				string group = GetGroup(tableIndexRow);

				if (! result.ContainsKey(group))
				{
					result.Add(group, CreateGroupStatistics(group));
				}

				_helpers[tableIndexRow.TableIndex].ClearRows();
			}

			return result;
		}

		[NotNull]
		private IEnumerable<TableIndexRow> EnumerateRows([CanBeNull] IGeometry geometry)
		{
			const bool recycle = true;

			for (var tableIndex = 0; tableIndex < _tables.Count; tableIndex++)
			{
				IReadOnlyTable table = _tables[tableIndex];

				IGeometry filterGeometry = geometry;

				if (! (table is IReadOnlyFeatureClass))
				{
					filterGeometry = null;
				}

				ITableFilter queryFilterFullScan =
					CreateQueryFilter(filterGeometry, tableIndex);

				var rowsToTest = table.EnumRows(queryFilterFullScan, recycle);

				foreach (IReadOnlyRow row in rowsToTest)
				{
					yield return new TableIndexRow(tableIndex, row);
				}
			}
		}

		private int ExecuteGeometry([CanBeNull] IGeometry geometry)
		{
			Init();

			if (_limitToTestedRows)
			{
				return FindErrors(EnumerateRows(geometry), null);
			}

			IDictionary<string, GroupStatistics> statisticsPerGroup =
				geometry != null
					? GetStatisticsPerGroup(EnumerateRows(geometry))
					: null;

			return FindErrors(EnumerateRows(null), statisticsPerGroup);
		}

		private int FindErrors(
			[NotNull] IEnumerable<TableIndexRow> rowsToTest,
			[CanBeNull] IDictionary<string, GroupStatistics> groupsOfInterest)
		{
			IDictionary<string, GroupStatistics> statisticsPerGroup =
				groupsOfInterest ??
				new Dictionary<string, GroupStatistics>();

			foreach (TableIndexRow tableIndexRow in rowsToTest)
			{
				if (CancelTestingRow(tableIndexRow.Row))
				{
					continue;
				}

				string group;
				DataRow dataRow = GetDataRow(tableIndexRow, out group);

				GroupStatistics groupStatistics;
				if (groupsOfInterest == null)
				{
					if (! statisticsPerGroup.TryGetValue(group, out groupStatistics))
					{
						groupStatistics = CreateGroupStatistics(group);

						statisticsPerGroup.Add(group, groupStatistics);
					}
				}
				else if (! statisticsPerGroup.TryGetValue(group, out groupStatistics))
				{
					continue;
				}

				groupStatistics.Add(dataRow, tableIndexRow,
				                    () => GetExistsRowFilterValue(tableIndexRow));

				_helpers[tableIndexRow.TableIndex].ClearRows();
			}

			return statisticsPerGroup.Values
			                         .Where(stats => ! stats.IgnoreGroup)
			                         .Sum(stats => stats.CreateErrors(ReportError));
		}

		private bool GetExistsRowFilterValue([NotNull] TableIndexRow tableIndexRow)
		{
			return Assert.NotNull(_existsRowGroupFilter)[tableIndexRow.TableIndex]
			             .IsFulfilled(tableIndexRow.Row);
		}

		[NotNull]
		private GroupStatistics CreateGroupStatistics([NotNull] string group)
		{
			return new DistinctValueGroupStatistics(group,
			                                        _minDistinctCount, _maxDistinctCount,
			                                        _tableNames, _relatedTables,
			                                        _relatedOidFields);
		}

		[NotNull]
		private ITableFilter CreateQueryFilter([CanBeNull] IGeometry geometry,
		                                       int tableIndex)
		{
			return TestUtils.CreateFilter(geometry, AreaOfInterest,
			                              GetConstraint(tableIndex),
			                              _tables[tableIndex],
			                              _helpers[tableIndex]);
		}

		private void Init()
		{
			if (_helpers == null)
			{
				_helpers = _tables.Select((_, i) => CreateHelper(i))
				                  .ToList();
			}

			if (_existsRowGroupFilter == null)
			{
				_existsRowGroupFilter =
					_tables.Select((_, i) => CreatePredicate(i, _existsRowGroupFiltersSql))
					       .ToList();
			}
		}

		[NotNull]
		private RowCondition CreatePredicate(int tableIndex,
		                                     [NotNull] IList<string> predicates)
		{
			return new RowCondition(_tables[tableIndex],
			                        GetValueForTable(tableIndex, predicates),
			                        undefinedConstraintIsFulfilled: true,
			                        caseSensitive: GetSqlCaseSensitivity(tableIndex));
		}

		[CanBeNull]
		private static string GetValueForTable(int tableIndex,
		                                       [NotNull] IList<string> values)
		{
			return values.Count == 0 ? null :
			       values.Count == 1 ? values[0] :
			       values[tableIndex];
		}

		[NotNull]
		private TableView CreateHelper(int tableIndex)
		{
			IReadOnlyTable table = _tables[tableIndex];
			// all used expressions must exist in all tables

			// TODO parse/remove/use case sensitivity hint
			string expressions = _groupByExpressions[tableIndex] + "," +
			                     _distinctExpressions[tableIndex];

			var filterExpression = GetValueForTable(tableIndex,
			                                        _existsRowGroupFiltersSql);
			if (filterExpression != null)
			{
				expressions = expressions + "," + filterExpression;
			}

			bool caseSensitivity = GetSqlCaseSensitivity(tableIndex);

			if (_relatedTables == null && _tables.Count == 1)
			{
				IReadOnlyDataset ds = (IReadOnlyDataset) _tables[0];
				if (ds.FullName is IQueryName qn)
				{
					IFeatureWorkspace ws = (IFeatureWorkspace) ds.Workspace;
					List<IReadOnlyTable> relTables = new List<IReadOnlyTable>();
					foreach (string tableName in qn.QueryDef.Tables.Split(','))
					{
						IReadOnlyTable relTable =
							ReadOnlyTableFactory.Create(ws.OpenTable(tableName));
						if (relTable.HasOID &&
						    relTable.OIDFieldName.Equals(
							    "RID", StringComparison.InvariantCultureIgnoreCase))
						{
							// Ignore relation tables
							continue;
						}

						relTables.Add(relTable);
					}

					SetRelatedTables(relTables);
				}
			}

			if (_relatedTables != null)
			{
				foreach (var rt in _relatedTables.Related
				                                 .Where(t => t.OidFieldIndex >= 0))
				{
					expressions = AddField(expressions, rt.FullOidFieldName);
				}
			}

			TableView helper = TableViewFactory.Create(
				table, expressions, useAsConstraint: false,
				caseSensitive: caseSensitivity);
			helper.Constraint = string.Empty;

			DataColumn groupByColumn = helper.AddColumn(_groupByColumn, typeof(object));
			// TODO parse/remove/use case sensitivty hint
			groupByColumn.Expression = _groupByExpressions[tableIndex];

			DataColumn distinctColumn = helper.AddColumn(_distinctColumn, typeof(object));
			// TODO parse/remove/use case sensitivty hint
			distinctColumn.Expression = _distinctExpressions[tableIndex];

			if (_relatedTables != null)
			{
				_relatedOidFields = new Dictionary<RelatedTable, int>();

				foreach (RelatedTable relatedTable in _relatedTables.Related)
				{
					if (relatedTable.OidFieldIndex < 0)
					{
						_relatedOidFields.Add(relatedTable, -1);
						continue;
					}

					int oidIndex = helper.GetColumnIndex(relatedTable.FullOidFieldName);
					_relatedOidFields.Add(relatedTable, oidIndex);
				}
			}

			helper.ClearRows();
			return helper;
		}

		[NotNull]
		private static string AddField([NotNull] string fields,
		                               [CanBeNull] string addField)
		{
			if (string.IsNullOrEmpty(addField))
			{
				return fields;
			}

			if (fields.IndexOf(addField, StringComparison.InvariantCultureIgnoreCase) >=
			    0)
			{
				// already exists
				return fields;
			}

			fields += "," + addField;

			return fields;
		}

		#region nested types

		private class TableIndexRow
		{
			public readonly int TableIndex;
			public readonly IReadOnlyRow Row;

			public TableIndexRow(int tableIndex, [NotNull] IReadOnlyRow row)
			{
				TableIndex = tableIndex;
				Row = row;
			}

			public TestRowReference GetRowInfo()
			{
				var r = new TestRowReference(Row.OID, TableIndex);
				return r;
			}
		}

		private delegate int ErrorReporting([NotNull] string description,
		                                    [NotNull] InvolvedRows involvedRows,
		                                    [CanBeNull] IGeometry geometry,
		                                    [NotNull] IssueCode issueCode,
		                                    [CanBeNull] string affectedComponent,
		                                    bool reportIndividualParts = false,
		                                    [CanBeNull] IEnumerable<object> values = null);

		private abstract class GroupStatistics
		{
			private bool _existsRowFilterFulfilled;

			public void Add([NotNull] DataRow dataRow,
			                [NotNull] TableIndexRow row,
			                [NotNull] Func<bool> existsRowFilterValue)
			{
				if (! _existsRowFilterFulfilled)
				{
					_existsRowFilterFulfilled = existsRowFilterValue();
				}

				Add(dataRow, row);
			}

			public bool IgnoreGroup => ! _existsRowFilterFulfilled;

			public abstract int CreateErrors([NotNull] ErrorReporting errorReporting);

			protected abstract void Add([NotNull] DataRow dataRow,
			                            [NotNull] TableIndexRow row);
		}

		private class DistinctValueGroupStatistics : GroupStatistics
		{
			[NotNull] private readonly string _groupExpression;
			private readonly int _minDistinctCount;
			private readonly int? _maxDistinctCount;
			[NotNull] private readonly IList<string> _tableNames;
			[CanBeNull] private readonly RelatedTables _relatedTables;
			[CanBeNull] private readonly IDictionary<RelatedTable, int> _relatedOidFields;

			[NotNull] private readonly Dictionary<string, List<TestRowReference[]>> _distinctValues
				= new Dictionary<string, List<TestRowReference[]>>();

			/// <summary>
			/// Initializes a new instance of the <see cref="DistinctValueGroupStatistics"/> class.
			/// </summary>
			/// <param name="groupExpression">The group expression.</param>
			/// <param name="minDistinctCount">The min distinct count.</param>
			/// <param name="maxDistinctCount">The max distinct count.</param>
			/// <param name="tableNames">The table names.</param>
			/// <param name="relatedTables">The related tables (optional).</param>
			/// <param name="relatedOidFields">The related oid fields (optional).</param>
			public DistinctValueGroupStatistics(
				[NotNull] string groupExpression,
				int minDistinctCount,
				int? maxDistinctCount,
				[NotNull] IList<string> tableNames,
				[CanBeNull] RelatedTables relatedTables,
				[CanBeNull] IDictionary<RelatedTable, int> relatedOidFields)
			{
				Assert.ArgumentNotNull(groupExpression, nameof(groupExpression));
				Assert.ArgumentNotNull(tableNames, nameof(tableNames));

				_groupExpression = groupExpression;
				_minDistinctCount = minDistinctCount;
				_maxDistinctCount = maxDistinctCount;
				_tableNames = tableNames;
				_relatedTables = relatedTables;
				_relatedOidFields = relatedOidFields;
			}

			protected override void Add(DataRow dataRow, TableIndexRow row)
			{
				string value = GetDistinctExpressionValue(dataRow);

				List<TestRowReference[]> oidTuples;
				if (! _distinctValues.TryGetValue(value, out oidTuples))
				{
					oidTuples = new List<TestRowReference[]>();
					_distinctValues.Add(value, oidTuples);
				}

				oidTuples.Add(IsJoinedTable
					              ? GetJoinedRowOIDTuple(dataRow)
					              : new[] { row.GetRowInfo() });
			}

			[NotNull]
			private static string GetDistinctExpressionValue([NotNull] DataRow dataRow)
			{
				object valueRaw = dataRow[_distinctColumn];
				return valueRaw?.ToString() ?? string.Empty;
			}

			[NotNull]
			private TestRowReference[] GetJoinedRowOIDTuple([NotNull] DataRow dataRow)
			{
				Assert.NotNull(_relatedTables, "_relatedTables");
				Assert.NotNull(_relatedOidFields, "_relatedOidFields");

				var result =
					new TestRowReference[Assert.NotNull(_relatedTables).Related.Count];

				var relatedTableIndex = 0;
				foreach (RelatedTable relatedTable in Assert
				                                      .NotNull(_relatedTables).Related)
				{
					int fieldIndex = Assert.NotNull(_relatedOidFields)[relatedTable];
					int oid = -1;
					if (fieldIndex >= 0)
					{
						object oidValue = dataRow[fieldIndex];
						if (oidValue is int)
						{
							oid = (int) oidValue;
						}
					}

					result[relatedTableIndex] = new TestRowReference(oid, -1);
					relatedTableIndex++;
				}

				return result;
			}

			private bool IsJoinedTable => _relatedOidFields != null;

			public override int CreateErrors(ErrorReporting errorReporting)
			{
				int count = _distinctValues.Count;

				if (count >= _minDistinctCount &&
				    (_maxDistinctCount == null || count <= _maxDistinctCount))
				{
					return NoError;
				}

				return ReportDistinctError(errorReporting);
			}

			private int ReportDistinctError([NotNull] ErrorReporting errorReporting)
			{
				IGeometry errorGeometry;
				bool incompleteInvolvedRows;
				InvolvedRows involvedList = GetInvolvedRows(out errorGeometry,
				                                            out incompleteInvolvedRows);
				IssueCode issueCode;
				string errorMessage =
					GetErrorMessage(incompleteInvolvedRows, out issueCode);

				return errorReporting(
					errorMessage, involvedList, errorGeometry, issueCode, null);
			}

			[NotNull]
			private InvolvedRows GetInvolvedRows(
				[CanBeNull] out IGeometry errorGeometry,
				out bool incompleteInvolvedRows)
			{
				errorGeometry = null;
				var result = new InvolvedRows();

				const int maxInvolvedRows = 25;
				incompleteInvolvedRows = false;

				foreach (DistinctValue distinctValue in GetDistinctValuesForReport())
				{
					if (result.Count >= maxInvolvedRows)
					{
						incompleteInvolvedRows = true;
						break;
					}

					foreach (TestRowReference[] oidTuple in distinctValue.RowOIDTuples)
					{
						if (result.Count >= maxInvolvedRows)
						{
							incompleteInvolvedRows = true;
							break;
						}

						if (! IsJoinedTable)
						{
							// simple tables have just one oid in the tuple:
							result.Add(CreateInvolvedRow(oidTuple[0], _tableNames));
						}
						else
						{
							Assert.NotNull(_relatedTables, "_relatedTables");

							var relatedTableIndex = 0;
							var relatedRows =
								new List<InvolvedRow>(
									Assert.NotNull(_relatedTables).Related.Count);

							foreach (RelatedTable relatedTable in Assert
								         .NotNull(_relatedTables)
								         .Related)
							{
								long relatedTableOID =
									oidTuple[relatedTableIndex].ObjectId;
								relatedTableIndex++;

								if (relatedTableOID < 0)
								{
									continue;
								}

								relatedRows.Add(
									new InvolvedRow(relatedTable.TableName,
									                relatedTableOID));

								// why just use the first here?
								//if (errorGeometry == null)
								//{
								//    errorGeometry = relatedTable.GetGeometry(relatedTableOID);
								//}
							}

							InvolvedRowUtils.AddUniqueInvolvedRows(result, relatedRows);
						}
					}
				}

				return result;
			}

			private IEnumerable<DistinctValue> GetDistinctValuesForReport()
			{
				return _distinctValues.Count < _minDistinctCount
					       ? new List<DistinctValue>()
					       : GetLeastFrequentDistinctValues(
						       _distinctValues, _maxDistinctCount);
			}

			[NotNull]
			private static InvolvedRow CreateInvolvedRow(
				[NotNull] TestRowReference testRowReference,
				[NotNull] IList<string> tableNames)
			{
				long oid = testRowReference.ObjectId;
				int tableIndex = testRowReference.TableIndex;

				string tableName = tableNames[tableIndex];

				return new InvolvedRow(tableName, oid);
			}

			private string GetErrorMessage(bool incompleteInvolvedRows,
			                               out IssueCode issueCode)
			{
				var sb = new StringBuilder();
				int maxReport = _distinctValues.Count;

				if (_maxDistinctCount != null)
				{
					if (_minDistinctCount <= 0)
					{
						// min=0
						sb.AppendFormat(
							"Group '{0}' has {1} distinct values (maximum allowed number of distinct values: {2}):",
							_groupExpression, _distinctValues.Count, _maxDistinctCount);
						issueCode = Codes[Code.DistinctValues_TooMany];
					}
					else if (_minDistinctCount == _maxDistinctCount.Value)
					{
						// min=max, there is a single expected value
						sb.AppendFormat(
							"Group '{0}' has {1} distinct values (expected number of distinct values: {2}):",
							_groupExpression, _distinctValues.Count, _maxDistinctCount);
						issueCode = Codes[Code.DistinctValues_NotExpectedNumber];
					}
					else
					{
						// there is a different min and max distinct count
						sb.AppendFormat(
							"Group '{0}' has {1} distinct values (allowed number of distinct values: minimum {2}, maximum {3}):",
							_groupExpression, _distinctValues.Count, _minDistinctCount,
							_maxDistinctCount);
						issueCode = Codes[Code.DistinctValues_NotInExpectedRange];
					}

					if (_distinctValues.Count > _maxDistinctCount + 4)
					{
						maxReport = _maxDistinctCount.Value + 3;
					}
				}
				else
				{
					sb.AppendFormat(
						"Group '{0}' has {1} distinct values (minimum allowed number of distinct values: {2}):",
						_groupExpression, _distinctValues.Count, _minDistinctCount);
					issueCode = Codes[Code.DistinctValues_TooFew];
				}

				sb.AppendLine();

				AppendValues(sb, maxReport);

				if (incompleteInvolvedRows)
				{
					sb.AppendLine("Note: not all involved rows are listed");
				}

				return sb.ToString();
			}

			private void AppendValues([NotNull] StringBuilder sb, int maxReport)
			{
				var distinctValueIndex = 0;
				foreach (KeyValuePair<string, List<TestRowReference[]>> pair in
				         _distinctValues)
				{
					if (distinctValueIndex >= maxReport)
					{
						sb.AppendLine("...");
						break;
					}

					sb.AppendFormat("- Value: {0}, rows: {1} ",
					                pair.Key, pair.Value.Count);
					sb.AppendLine();

					distinctValueIndex++;
				}
			}

			[NotNull]
			private static IEnumerable<DistinctValue> GetLeastFrequentDistinctValues(
				[NotNull] IEnumerable<KeyValuePair<string, List<TestRowReference[]>>>
					distinctValues,
				int? maxDistinctCount)
			{
				List<DistinctValue> sorted =
					distinctValues.Select(pair => new DistinctValue(pair.Key, pair.Value))
					              .ToList();

				// sort in ascending row count order
				sorted.Sort((d1, d2) => d1.RowCount.CompareTo(d2.RowCount));

				if (maxDistinctCount == null)
				{
					foreach (DistinctValue distinctValue in sorted)
					{
						yield return distinctValue;
					}
				}
				else
				{
					for (var i = 0; i < sorted.Count - maxDistinctCount; i++)
					{
						yield return sorted[i];
					}
				}
			}

			private class DistinctValue
			{
				private readonly string _value;
				private readonly List<TestRowReference[]> _rowOIDTuples;

				public DistinctValue(string value,
				                     [NotNull] List<TestRowReference[]> rowOIDTuples)
				{
					_value = value;
					_rowOIDTuples = rowOIDTuples;
				}

				[NotNull]
				public IEnumerable<TestRowReference[]> RowOIDTuples => _rowOIDTuples;

				public int RowCount => _rowOIDTuples.Count;

				public override string ToString()
				{
					return string.Format("Value: {0}, #RowOIDTuples: {1}", _value,
					                     _rowOIDTuples.Count);
				}
			}
		}

		#endregion
	}
}
