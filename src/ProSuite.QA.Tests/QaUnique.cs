using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.Collections;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	// TODO: consider ordering on multiple fields directly (ORDER BY and ITableSort)
	// TODO: consider using ITableSort also if search geometry is used
	// TODO: consider using ITableSort also if selection set is verified
	// TODO: consider option to use ITableSort instead of ORDER BY (if collation is known to be different between two Oracle instances)
	// TODO: add parameter to enforce case-sensitive comparison
	[UsedImplicitly]
	[AttributeTest]
	public class QaUnique : NonContainerTest
	{
		private const string _tableIndexColumn = "_qaUnique_tableIndexColumn";
		private const string _oidColumn = "_qaUnique_oidColumn";
		private const int _maxExpressionLength = 2000;
		private const int _maxTokens = 2000;

		private readonly int _maxRows;
		private readonly IList<IReadOnlyTable> _tables;
		private readonly IList<string[]> _uniqueFieldsList;
		private readonly IList<string> _firstUniqueFieldNames;
		private esriFieldType? _firstUniqueFieldType;
		private readonly IList<string> _commaSeparatedFieldNamesList;

		private int[] _compareFieldIndexes;
		private IList<ITableFilter> _globalFilters;
		private TableView _sortedView;
		private IList<TableView> _helperViews;
		private IList<IList<ColumnInfo>> _mappings;
		private IList<int> _firstUniqueFieldIndexes;

		private StringBuilder _inExpressionBuilder;
		private SimpleSet<string> _inExpressionTokens = new SimpleSet<string>(_maxTokens);
		private bool _hasNullValue;
		private bool _nullValueChecked;

		private RelatedTables _relatedTables;

		private Dictionary<RelatedTable, int> _relatedOidFields;

		private const int _defaultMaxRows = short.MaxValue;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string NotUnique = "NotUnique";

			public Code() : base("UniqueFields") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaUnique_0))]
		public QaUnique(
				[Doc(nameof(DocStrings.QaUnique_table))]
				IReadOnlyTable table,
				[Doc(nameof(DocStrings.QaUnique_unique))]
				string unique)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(table, unique, _defaultMaxRows) { }

		[Doc(nameof(DocStrings.QaUnique_0))]
		public QaUnique(
			[Doc(nameof(DocStrings.QaUnique_table))]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaUnique_unique))]
			string unique,
			[Doc(nameof(DocStrings.QaUnique_maxRows))] [DefaultValue(_defaultMaxRows)]
			int maxRows)
			: this(new[] { table }, new[] { unique })
		{
			_maxRows = maxRows;
		}

		[Doc(nameof(DocStrings.QaUnique_1))]
		public QaUnique(
			[Doc(nameof(DocStrings.QaUnique_tables))]
			IList<IReadOnlyTable> tables,
			[Doc(nameof(DocStrings.QaUnique_uniques))]
			IList<string> uniques)
			: base(tables)
		{
			Assert.ArgumentNotNull(tables, nameof(tables));
			Assert.ArgumentNotNull(uniques, nameof(uniques));

			Assert.ArgumentCondition(uniques.Count == 1 || uniques.Count == tables.Count,
			                         "uniques must contain 1 value, or 1 value per table");

			_tables = tables;

			_uniqueFieldsList = new List<string[]>();
			_firstUniqueFieldNames = new List<string>();
			_commaSeparatedFieldNamesList = new List<string>();
			foreach (string unique in uniques)
			{
				string[] uniqueFields =
					unique.ToUpper().Split(new[] { ",", ";", " " },
					                       StringSplitOptions.RemoveEmptyEntries);

				Assert.ArgumentCondition(uniqueFields.Length > 0,
				                         "at least one field name must be specified");

				string firstUniqueFieldName = uniqueFields[0];
				string commaSeparatedFieldNames =
					StringUtils.Concatenate(uniqueFields, ", ");

				_uniqueFieldsList.Add(uniqueFields);
				_firstUniqueFieldNames.Add(firstUniqueFieldName);
				_commaSeparatedFieldNamesList.Add(commaSeparatedFieldNames);
			}

			_maxRows = _defaultMaxRows;
		}

		[InternallyUsedTest]
		public QaUnique([NotNull] QaUniqueDefinition definition)
			: this(definition.Tables.Cast<IReadOnlyTable>().ToList(), definition.Uniques)
		{
			_maxRows = definition.MaxRows;
		}

		/// <summary>
		/// Gets or sets a value indicating whether to force in-memory table sorting (using ITableSort).
		/// </summary>
		/// <value>
		/// <c>true</c> if ITableSort-based sorting is enforced; otherwise, <c>false</c>.
		/// </value>
		/// <remarks>Used only from unit tests</remarks>
		public bool ForceInMemoryTableSorting { get; set; }

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

			const int minTableIndex = -1;
			var errorCount = 0;

			int tablesCount = _tables.Count;
			var constraintViews = new TableView[tablesCount];

			foreach (IReadOnlyRow row in selection)
			{
				var feature = row as IReadOnlyFeature;
				if (feature != null && AreaOfInterest != null &&
				    ((IRelationalOperator) AreaOfInterest).Disjoint(
					    feature.Shape))
				{
					continue;
				}

				for (var tableIndex = 0; tableIndex < _tables.Count; tableIndex++)
				{
					IReadOnlyTable table = _tables[tableIndex];

					if (row.Table != table)
					{
						continue;
					}

					if (constraintViews[tableIndex] == null)
					{
						constraintViews[tableIndex] = TableViewFactory.Create(table,
							GetConstraint(
								tableIndex));
					}

					TableView constraintView = constraintViews[tableIndex];

					if (! constraintView.MatchesConstraint(row))
					{
						continue;
					}

					if (CancelTestingRow(row))
					{
						continue;
					}

					int firstUniqueFieldIndex = _firstUniqueFieldIndexes[tableIndex];

					errorCount += AddToKeySet(row.get_Value(firstUniqueFieldIndex),
					                          _sortedView, minTableIndex);
				}
			}

			errorCount += CheckKeySet(_sortedView, minTableIndex);
			_sortedView.ClearRows();

			return errorCount;
		}

		public override int Execute(IReadOnlyRow row)
		{
			return Execute(new[] { row });
		}

		protected override ISpatialReference GetSpatialReference()
		{
			foreach (IReadOnlyTable table in _tables)
			{
				var geoDataset = table as IReadOnlyGeoDataset;
				if (geoDataset != null)
				{
					return geoDataset.SpatialReference;
				}
			}

			return null;
		}

		public void SetRelatedTables([NotNull] IList<IReadOnlyTable> relatedTables)
		{
			Assert.AreEqual(1, _tables.Count,
			                "Cannot set relatedTables if more than 1 table is checked");
			_relatedTables = RelatedTables.Create(relatedTables, _tables[0]);
		}

		private int ExecuteGeometry([CanBeNull] IGeometry geometry)
		{
			Init();
			var canUseTableSort = true;

			foreach (IReadOnlyTable table in _tables)
			{
				if (! (table is IReadOnlyFeatureClass))
				{
					geometry = null;
				}

				if (! CanUseTableSort(table))
				{
					canUseTableSort = false;
				}
			}

			bool checkAllRowsOrdered;
			IComparer<object> inMemoryObjectComparer;
			if (geometry == null)
			{
				if (! ForceInMemoryTableSorting &&
				    CanCompareUsingOrderByQueries(_tables, FirstUniqueFieldType))
				{
					inMemoryObjectComparer = null;
					checkAllRowsOrdered = true;
				}
				else if (canUseTableSort)
				{
					inMemoryObjectComparer = GetObjectComparer(FirstUniqueFieldType);
					checkAllRowsOrdered = inMemoryObjectComparer != null;
				}
				else
				{
					checkAllRowsOrdered = false;
					inMemoryObjectComparer = null;
				}
			}
			else
			{
				checkAllRowsOrdered = false;
				inMemoryObjectComparer = null;
			}

			return checkAllRowsOrdered
				       ? CheckAllRowsOrdered(inMemoryObjectComparer)
				       : CheckUnorderedRows(geometry);
		}

		private static bool CanUseTableSort([NotNull] IReadOnlyTable table)
		{
			IWorkspace workspace = table.Workspace;

			if (workspace is GdbWorkspace)
			{
				return false;
			}

			if (WorkspaceUtils.IsSDEGeodatabase(workspace))
			{
				return true;
			}

			if (WorkspaceUtils.IsPersonalGeodatabase(workspace))
			{
				return true;
			}

			if (! WorkspaceUtils.IsFileGeodatabase(workspace))
			{
				return false;
			}

			var queryName = table.FullName as IQueryName2;
			if (queryName == null)
			{
				return true;
			}

			// queryname-based feature class from file geodatabase: table sort no longer works 
			// correctly since 10.4 (tested up to and including 10.6)
			string version = RuntimeUtils.Version;
			double v;
			if (double.TryParse(version, out v))
			{
				return v < 10.4;
			}

			return false;
		}

		private static bool CanCompareUsingOrderByQueries(
			[NotNull] IEnumerable<IReadOnlyTable> tables,
			esriFieldType firstUniqueFieldType)
		{
			var workspaces = new HashSet<IWorkspace>();

			foreach (IReadOnlyTable table in tables)
			{
				IWorkspace workspace = table.Workspace;

				if (! IsOrderBySupported(workspace))
				{
					return false;
				}

				workspaces.Add(workspace);
			}

			if (workspaces.Count == 1)
			{
				// workspace supports ORDER BY, only one involved workspace -> supported
				return true;
			}

			// more than one workspace involved, all of them do support ORDER BY

			switch (firstUniqueFieldType)
			{
				case esriFieldType.esriFieldTypeDate:
				case esriFieldType.esriFieldTypeDouble:
				case esriFieldType.esriFieldTypeInteger:
				case esriFieldType.esriFieldTypeOID:
				case esriFieldType.esriFieldTypeSingle:
				case esriFieldType.esriFieldTypeSmallInteger:
					// ORDER BY always supported for numeric and date fields
					return true;

				case esriFieldType.esriFieldTypeString:
				case esriFieldType.esriFieldTypeGUID:
				case esriFieldType.esriFieldTypeGlobalID:
					return CanCompareOrdering(workspaces);

				case esriFieldType.esriFieldTypeGeometry:
				case esriFieldType.esriFieldTypeBlob:
				case esriFieldType.esriFieldTypeRaster:
				case esriFieldType.esriFieldTypeXML:
					// ORDER BY not supported
					return false;

				default:
					throw new InvalidOperationException(
						string.Format("Unsupported field type: {0}",
						              firstUniqueFieldType));
			}
		}

		private static bool CanCompareOrdering(
			[NotNull] ICollection<IWorkspace> workspaces)
		{
			Assert.ArgumentNotNull(workspaces, nameof(workspaces));
			Assert.ArgumentCondition(workspaces.Count > 0,
			                         "At least one workspace expected");

			if (workspaces.Count == 1)
			{
				return true;
			}

			// if all are of same type (fgdb / pgdb / egdb+dbmstype)
			string commonType = null;
			foreach (IWorkspace workspace in workspaces)
			{
				string workspaceType = GetWorkspaceType(workspace);

				if (workspaceType == null)
				{
					_msg.DebugFormat(
						"Unable to determine workspace type for workspace {0}",
						WorkspaceUtils.GetConnectionString(workspace, true));
					return false;
				}

				if (commonType == null)
				{
					commonType = workspaceType;
				}
				else
				{
					if (! string.Equals(commonType, workspaceType))
					{
						return false;
					}
				}
			}

			return true;
		}

		[CanBeNull]
		private static string GetWorkspaceType([NotNull] IWorkspace workspace)
		{
			if (workspace.Type == esriWorkspaceType.esriRemoteDatabaseWorkspace)
			{
				var connectionInfo = workspace as IDatabaseConnectionInfo2;
				return connectionInfo == null
					       ? null
					       : string.Format("sde:{0}", connectionInfo.ConnectionDBMS);
			}

			if (WorkspaceUtils.IsFileGeodatabase(workspace))
			{
				return "fgdb";
			}

			if (WorkspaceUtils.IsPersonalGeodatabase(workspace))
			{
				return "pgdb";
			}

			if (WorkspaceUtils.IsShapefileWorkspace(workspace))
			{
				return "shp";
			}

			return null;
		}

		private static bool IsOrderBySupported([NotNull] IWorkspace workspace)
		{
			esriWorkspaceType workspaceType = workspace.Type;

			switch (workspaceType)
			{
				case esriWorkspaceType.esriFileSystemWorkspace:
					// ORDER BY not supported for shapefiles/dbf tables
					return false;

				case esriWorkspaceType.esriLocalDatabaseWorkspace:
					// ok for both FGDB and PGDB, with or without unsaved edits - verified for 10.2.2
					if (RuntimeUtils.Is10_0 || RuntimeUtils.Is10_1)
					{
						// TODO might work also for 10.0 and 10.1 --> verify
						if (! WorkspaceUtils.IsFileGeodatabase(workspace) ||
						    WorkspaceUtils.HasEdits(workspace))
						{
							// workspace might not support ORDER BY
							return false;
						}
					}

					return true;

				case esriWorkspaceType.esriRemoteDatabaseWorkspace:
					// ORDER BY supported for ArcSDE workspaces
					return true;

				default:
					return false;
			}
		}

		[NotNull]
		private IList<string> GetUniqueFields(int tableIndex)
		{
			return _uniqueFieldsList.Count == 1
				       ? _uniqueFieldsList[0]
				       : _uniqueFieldsList[tableIndex];
		}

		[NotNull]
		private string GetFirstUniqueFieldName(int tableIndex)
		{
			return _firstUniqueFieldNames.Count == 1
				       ? _firstUniqueFieldNames[0]
				       : _firstUniqueFieldNames[tableIndex];
		}

		[NotNull]
		private string GetCommaSeparatedFieldNames(int tableIndex)
		{
			return _commaSeparatedFieldNamesList.Count == 1
				       ? _commaSeparatedFieldNamesList[0]
				       : _commaSeparatedFieldNamesList[tableIndex];
		}

		private int CheckAllRowsOrdered(
			[CanBeNull] IComparer<object> inMemoryObjectComparer)
		{
			SortedList<object, List<RowEnumerator>> enumerators = null;

			try
			{
				enumerators = InitEnumerators(inMemoryObjectComparer);

				var rowCount = 0;
				var errorCount = 0;

				while (enumerators.Count > 0)
				{
					List<RowEnumerator> sameKeyEnumerators = enumerators.Values[0];
					object key = enumerators.Keys[0];

					while (sameKeyEnumerators.Count > 0)
					{
						int maxIndex = sameKeyEnumerators.Count - 1;
						RowEnumerator rowEnumerator = sameKeyEnumerators[maxIndex];
						sameKeyEnumerators.RemoveAt(maxIndex);

						IReadOnlyRow row = Assert.NotNull(rowEnumerator.CurrentRow);

						if (! CancelTestingRow(row))
						{
							int tableIndex = rowEnumerator.TableIndex;
							DataRow added =
								Assert.NotNull(
									_sortedView.Add(row, _mappings[tableIndex]));
							added[_tableIndexColumn] = tableIndex;
							added[_oidColumn] = row.OID;
							rowCount++;
						}

						// remark: this may add a new entry to sameKeyEnumerators
						rowEnumerator.TryAddNext(enumerators);

						// Assert.AreEqual(enumerators.Values[0],sameKeyEnumerators);

						if (sameKeyEnumerators.Count == 0)
						{
							enumerators.Remove(key);

							if (rowCount > _maxRows)
							{
								errorCount += CheckOrderedRows(_sortedView, -1);
								_sortedView.ClearRows();
								rowCount = 0;
							}
						}
					}
				}

				errorCount += CheckOrderedRows(_sortedView.Clone(), -1);

				_sortedView.ClearRows();

				return errorCount;
			}
			finally
			{
				if (enumerators != null)
				{
					// enumerators is empty when everything worked fine,
					// but make sure everything is disposed in case of exceptions
					foreach (List<RowEnumerator> sameKeyEnumerators in enumerators.Values)
					{
						foreach (RowEnumerator enumerator in sameKeyEnumerators)
						{
							enumerator.Dispose();
						}
					}
				}
			}
		}

		[NotNull]
		private SortedList<object, List<RowEnumerator>> InitEnumerators(
			[CanBeNull] IComparer<object> inMemoryObjectComparer)
		{
			SortedList<object, List<RowEnumerator>> enumerators =
				inMemoryObjectComparer != null
					? new SortedList<object, List<RowEnumerator>>(inMemoryObjectComparer)
					: new SortedList<object, List<RowEnumerator>>(
						new ObjectComparer(null));

			int tableCount = _tables.Count;
			for (var tableIndex = 0; tableIndex < tableCount; tableIndex++)
			{
				RowEnumerator rowEnumerator = CreateRowEnumerator(tableIndex,
					inMemoryObjectComparer);

				rowEnumerator.TryAddNext(enumerators);
			}

			return enumerators;
		}

		[NotNull]
		private RowEnumerator CreateRowEnumerator(
			int tableIndex,
			[CanBeNull] IComparer<object> inMemoryObjectComparer)
		{
			IReadOnlyTable table = _tables[tableIndex];
			IList<string> uniqueFields = GetUniqueFields(tableIndex);
			string firstUniqueFieldName = GetFirstUniqueFieldName(tableIndex);

			ITableFilter filter = CreateQueryFilter(table, uniqueFields,
			                                        _helperViews[tableIndex], null);

			if (inMemoryObjectComparer != null)
			{
				return RowEnumerator.CreateTableSort(
					table, filter, firstUniqueFieldName, inMemoryObjectComparer,
					tableIndex);
			}

			return RowEnumerator.CreateOrderBy(
				table, filter, firstUniqueFieldName, tableIndex);
		}

		private esriFieldType FirstUniqueFieldType
		{
			get
			{
				if (_firstUniqueFieldType == null)
				{
					int fieldIndex = _tables[0].FindField(_firstUniqueFieldNames[0]);
					_firstUniqueFieldType = _tables[0].Fields.get_Field(fieldIndex).Type;
				}

				return _firstUniqueFieldType.Value;
			}
		}

		[CanBeNull]
		private static IComparer<object> GetObjectComparer(esriFieldType fieldType)
		{
			switch (fieldType)
			{
				case esriFieldType.esriFieldTypeDate:
				case esriFieldType.esriFieldTypeDouble:
				case esriFieldType.esriFieldTypeInteger:
				case esriFieldType.esriFieldTypeOID:
				case esriFieldType.esriFieldTypeSingle:
				case esriFieldType.esriFieldTypeSmallInteger:
					return new ObjectComparer(null);

				case esriFieldType.esriFieldTypeGUID:
				case esriFieldType.esriFieldTypeGlobalID:
					return CreateGuidComparer();

				case esriFieldType.esriFieldTypeString:
					return CreateStringComparer(caseSensitive: true);

				default:
					return null;
			}
		}

		[NotNull]
		private static IComparer<object> CreateStringComparer(bool caseSensitive)
		{
			// use IgnoreCase to make sure all upper/lower variants end up in same batch
			return new ObjectComparer(
				(x, y) => string.Compare(Convert.ToString(x),
				                         Convert.ToString(y),
				                         caseSensitive
					                         ? StringComparison.InvariantCulture
					                         : StringComparison
						                         .InvariantCultureIgnoreCase));
		}

		[NotNull]
		private static IComparer<object> CreateGuidComparer()
		{
			return new ObjectComparer(
				(x, y) => string.Compare(Convert.ToString(x), Convert.ToString(y),
				                         StringComparison.InvariantCultureIgnoreCase));
		}

		private int CheckUnorderedRows([CanBeNull] IGeometry geometry)
		{
			var errorCount = 0;

			// TODO check row count, if less than maximum value --> load all into _helper?

			for (var tableIndex = 0; tableIndex < _tables.Count; tableIndex++)
			{
				IReadOnlyTable table = _tables[tableIndex];
				IList<string> uniqueFields = GetUniqueFields(tableIndex);
				TableView helperView = _helperViews[tableIndex];
				int firstUniqueFieldIndex = _firstUniqueFieldIndexes[tableIndex];

				ITableFilter filter =
					CreateQueryFilter(table, uniqueFields, helperView, geometry);

				const bool recycle = true;
				foreach (IReadOnlyRow row in table.EnumRows(filter, recycle))
				{
					if (CancelTestingRow(row, recycleUnique: Guid.NewGuid()))
					{
						continue;
					}

					object newKey = row.get_Value(firstUniqueFieldIndex);

					errorCount += AddToKeySet(newKey, _sortedView, tableIndex);
				}

				errorCount = CheckKeySet(_sortedView, tableIndex);
				_sortedView.ClearRows();
			}

			return errorCount;
		}

		[NotNull]
		private ITableFilter CreateQueryFilter([NotNull] IReadOnlyTable table,
		                                       [NotNull] IEnumerable<string> uniqueFields,
		                                       TableView tableView,
		                                       [CanBeNull] IGeometry geometry)
		{
			// TODO set the required subfields (oid, shape, unique fields, PLUS key fields needed for QaRelConstraint!)

			ITableFilter result = TestUtils.CreateFilter(geometry, AreaOfInterest,
			                                             GetConstraint(table),
			                                             table, tableView);

			if (_relatedTables != null)
			{
				// TODO define which fields are really needed
				// - until then: use full field list
			}
			else
			{
				var subfields = new List<string> { table.OIDFieldName };
				subfields.AddRange(uniqueFields);

				TableFilterUtils.SetSubFields(result, subfields);
			}

			return result;
		}

		private int AddToKeySet([CanBeNull] object key,
		                        [NotNull] TableView sortedView,
		                        int minTableIndex)
		{
			var errorCount = 0;
			if (_inExpressionBuilder.Length > _maxExpressionLength)
			{
				errorCount += CheckKeySet(sortedView, minTableIndex);

				_inExpressionBuilder = new StringBuilder();
				if (_inExpressionTokens.Count > _maxTokens)
				{
					_inExpressionTokens = new SimpleSet<string>(_maxTokens);
				}
			}

			if (key == null || key == DBNull.Value)
			{
				_hasNullValue = true;
				return errorCount;
			}

			string token = GetExpressionToken(key);

			if (! _inExpressionTokens.Contains(token))
			{
				// the token is not already in the list
				if (_inExpressionBuilder.Length > 0)
				{
					_inExpressionBuilder.Append(",");
				}

				_inExpressionBuilder.Append(token);
				_inExpressionTokens.Add(token);
			}

			return errorCount;
		}

		[NotNull]
		private static string GetExpressionToken([NotNull] object value)
		{
			var stringValue = value as string;
			if (stringValue != null)
			{
				return FormatStringExpressionToken(stringValue);
			}

			if (value is Guid)
			{
				return string.Format("'{0}'", value);
			}

			// TODO add explicit support for DateTime

			// TODO revise, handle any non-numeric cases explicitly
			string keyString = string.Format(CultureInfo.InvariantCulture, "{0}", value);

			if (! double.TryParse(keyString,
			                      NumberStyles.Number,
			                      CultureInfo.InvariantCulture,
			                      out double _))
			{
				// the key is neither a string, nor a guid, nor numeric
				return string.Format("'{0}'", keyString);
			}

			return keyString;
		}

		[NotNull]
		private static string FormatStringExpressionToken([NotNull] string s)
		{
			const string apostrophe = "'";

			return string.Format("'{0}'",
			                     s.IndexOf(apostrophe, StringComparison.Ordinal) >= 0
				                     ? s.Replace(apostrophe, "''")
				                     : s);
		}

		private int CheckKeySet([NotNull] TableView sortedView, int minTableIndex)
		{
			bool checkNullValue = _hasNullValue && ! _nullValueChecked;
			if (_inExpressionBuilder.Length <= 0 && ! checkNullValue)
			{
				return 0;
			}

			bool checkExpressionAndNull =
				_inExpressionBuilder.Length > 0 && checkNullValue;

			var errorCount = 0;

			//[NotNull] ITable table,
			//					[NotNull] IQueryFilter filter,
			//					[NotNull] string firstUniqueFieldName,

			int tablesCount = _tables.Count;
			for (var tableIndex = 0; tableIndex < tablesCount; tableIndex++)
			{
				IReadOnlyTable table = _tables[tableIndex];
				ITableFilter filter = _globalFilters[tableIndex];
				string firstUniqueFieldName = GetFirstUniqueFieldName(tableIndex);

				//ISQLSyntax sql = (ISQLSyntax) ((IDataset) table).Workspace;
				//bool supportsIsNull = 0 != (sql.GetSupportedPredicates() &
				//							(int) esriSQLPredicates.esriSQL_IS_NULL);
				//Assert.True(supportsIsNull,"workspace does not support is null");

				string origWhere = filter.WhereClause;
				try
				{
					var where = new StringBuilder(! string.IsNullOrEmpty(origWhere)
						                              ? string.Format(
							                              "({0}) AND ", origWhere)
						                              : string.Empty);

					if (checkExpressionAndNull)
					{
						where.Append("(");
					}

					if (_inExpressionBuilder.Length > 0)
					{
						where.AppendFormat("{0} IN ({1})", firstUniqueFieldName,
						                   _inExpressionBuilder);
					}

					if (checkExpressionAndNull)
					{
						where.Append(" OR ");
					}

					if (checkNullValue)
					{
						where.AppendFormat("{0} IS NULL", firstUniqueFieldName);
					}

					if (checkExpressionAndNull)
					{
						where.Append(")");
					}

					filter.WhereClause = where.ToString();

					var cursor = table.EnumRows(filter, true);
					foreach (IReadOnlyRow row in cursor)
					{
						DataRow added =
							Assert.NotNull(sortedView.Add(row, _mappings[tableIndex]));
						added[_tableIndexColumn] = tableIndex;
						added[_oidColumn] = row.OID;
					}
				}
				finally
				{
					filter.WhereClause = origWhere;
				}
			}

			errorCount += CheckOrderedRows(sortedView, minTableIndex);
			sortedView.ClearRows();

			if (checkNullValue)
			{
				_nullValueChecked = true;
			}

			return errorCount;
		}

		private void Init()
		{
			_inExpressionBuilder = new StringBuilder();
			_inExpressionTokens = new SimpleSet<string>(_maxTokens);
			_hasNullValue = false;
			_nullValueChecked = false;

			if (_sortedView != null)
			{
				_sortedView.ClearRows();
				return;
			}

			int tablesCount = _tables.Count;
			_helperViews = new List<TableView>();
			_mappings = new List<IList<ColumnInfo>>();
			_globalFilters = new List<ITableFilter>(tablesCount);
			_firstUniqueFieldIndexes = new List<int>(tablesCount);

			List<int> masterTableIndices = null;

			for (var tableIndex = 0; tableIndex < tablesCount; tableIndex++)
			{
				string fields = GetCommaSeparatedFieldNames(tableIndex);
				IReadOnlyTable table = _tables[tableIndex];

				// TODO apparently not needed. 
				// Does nothing, and TableView also adds OID field
				AddField(fields, table.OIDFieldName);

				if (_relatedTables != null)
				{
					foreach (RelatedTable relatedTable in _relatedTables.Related)
					{
						if (relatedTable.OidFieldIndex < 0)
						{
							continue;
						}

						fields = AddField(fields, relatedTable.FullOidFieldName);
					}
				}

				const bool useAsConstraint = false;
				TableView helperView = TableViewFactory.Create(
					table, fields, useAsConstraint,
					GetSqlCaseSensitivity());
				helperView.Constraint = string.Empty;

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

						int oidIndex =
							helperView.GetColumnIndex(relatedTable.FullOidFieldName);
						_relatedOidFields.Add(relatedTable, oidIndex);
					}
				}

				_firstUniqueFieldIndexes.Add(
					table.FindField(GetFirstUniqueFieldName(tableIndex)));

				_helperViews.Add(helperView);
				_globalFilters.Add(TestUtils.CreateFilter(null, AreaOfInterest,
				                                          GetConstraint(tableIndex),
				                                          table, helperView));

				IList<ColumnInfo> mappings = new List<ColumnInfo>();

				if (masterTableIndices == null)
				{
					int columnCount = _helperViews[0].ColumnCount;
					masterTableIndices = new List<int>();
					for (var columnIndex = 0; columnIndex < columnCount; columnIndex++)
					{
						string masterName = _helperViews[0].GetColumnName(columnIndex);
						IList<string> uniqueFields = GetUniqueFields(0);
						masterTableIndices.Add(uniqueFields.IndexOf(masterName));
					}
				}

				foreach (int uniqueFieldIndex in masterTableIndices)
				{
					if (uniqueFieldIndex < 0)
					{
						mappings.Add(null);
					}
					else
					{
						string mapField = GetUniqueFields(tableIndex)[uniqueFieldIndex];
						int i = table.FindField(mapField);
						ColumnInfo mapping =
							new FieldColumnInfo(table, table.Fields.get_Field(i), i);
						mappings.Add(mapping);
					}
				}

				_mappings.Add(mappings);
			}

			_compareFieldIndexes = GetCompareFieldIndexes(GetUniqueFields(0),
			                                              _helperViews[0]);

			TableView masterView = _helperViews[0];

			masterView.AddColumn(_tableIndexColumn, typeof(int));
			masterView.AddColumn(_oidColumn, typeof(int));
			string sort = string.Format("{0}, {1}",
			                            GetCommaSeparatedFieldNames(0),
			                            _tableIndexColumn);

			masterView.Sort(sort);
			masterView.ClearRows();

			_sortedView = masterView;
		}

		[NotNull]
		private static int[] GetCompareFieldIndexes([NotNull] IList<string> uniqueFields,
		                                            [NotNull] TableView tableView)
		{
			int uniqueFieldCount = uniqueFields.Count;

			var result = new int[uniqueFieldCount];

			for (var i = 0; i < uniqueFieldCount; i++)
			{
				result[i] = tableView.GetColumnIndex(uniqueFields[i].Trim().ToLower());
			}

			return result;
		}

		[NotNull]
		private static string AddField([NotNull] string commaSeparatedFieldNames,
		                               [CanBeNull] string addField)
		{
			if (string.IsNullOrEmpty(addField))
			{
				return commaSeparatedFieldNames;
			}

			// TODO revise, must exclude partial match to *unqualifed* field name
			if (commaSeparatedFieldNames.IndexOf(
				    addField,
				    StringComparison.InvariantCultureIgnoreCase) >= 0)
			{
				// already exists
				return commaSeparatedFieldNames;
			}

			commaSeparatedFieldNames += "," + addField;

			return commaSeparatedFieldNames;
		}

		private int ReportError([NotNull] DataRow dataRow)
		{
			IGeometry geometry = null;
			InvolvedRows involvedList = new InvolvedRows();

			var tableIndex = (int) dataRow[_tableIndexColumn];
			IReadOnlyTable table = _tables[tableIndex];
			IList<string> uniqueFields = GetUniqueFields(tableIndex);
			string commaSeparatedFieldNames = GetCommaSeparatedFieldNames(tableIndex);

			if (_relatedOidFields == null) // simple Table
			{
				object oid = dataRow[_oidColumn];
				if (table is IReadOnlyFeatureClass)
				{
					if (oid is int)
					{
						geometry = TryGetErrorGeometry(table, (int) oid);
					}
				}

				if (oid is int)
				{
					involvedList.Add(new InvolvedRow(table.Name, (int) oid));
				}
			}
			else // joined Table
			{
				foreach (RelatedTable relatedTable in _relatedTables.Related)
				{
					int fieldIndex = _relatedOidFields[relatedTable];
					if (fieldIndex < 0)
					{
						continue;
					}

					object oid = dataRow[fieldIndex];

					if (! (oid is int))
					{
						continue;
					}

					involvedList.Add(new InvolvedRow(relatedTable.TableName, (int) oid));

					if (geometry == null)
					{
						geometry = TryGetErrorGeometry(relatedTable.Table, (int) oid);
					}
				}
			}

			string description = GetIssueDescription(dataRow, uniqueFields,
			                                         commaSeparatedFieldNames);

			string affectedComponent = commaSeparatedFieldNames;

			return ReportError(
				description, involvedList, geometry,
				Codes[Code.NotUnique], affectedComponent);
		}

		[NotNull]
		private string GetIssueDescription([NotNull] DataRow dataRow,
		                                   [NotNull] IList<string> uniqueFields,
		                                   [NotNull] string commaSeparatedFieldNames)
		{
			return string.Format(uniqueFields.Count == 1
				                     ? "Value {0} in field {1} is not unique"
				                     : "The value combination ({0}) in fields {1} is not unique",
			                     FormatValues(dataRow), commaSeparatedFieldNames);
		}

		[NotNull]
		private string FormatValues([NotNull] DataRow dataRow)
		{
			if (_compareFieldIndexes.Length == 1)
			{
				return FormatValue(dataRow[_compareFieldIndexes[0]]);
			}

			var sb = new StringBuilder();

			foreach (int index in _compareFieldIndexes)
			{
				string value = FormatValue(dataRow[index]);

				if (sb.Length == 0)
				{
					sb.Append(value);
				}
				else
				{
					sb.AppendFormat(", {0}", value);
				}
			}

			return sb.ToString();
		}

		[NotNull]
		private static string FormatValue(object value)
		{
			if (value == null)
			{
				return "<null>";
			}

			if (value is DBNull)
			{
				return "<NULL>";
			}

			if (value is string)
			{
				return string.Format("'{0}'", value);
			}

			// TODO culture?
			return value.ToString();
		}

		[CanBeNull]
		private static IGeometry TryGetErrorGeometry([NotNull] IReadOnlyTable table, int oid)
		{
			if (! (table is IReadOnlyFeatureClass))
			{
				return null;
			}

			try
			{
				IReadOnlyRow row = table.GetRow(oid);
				return TestUtils.GetShapeCopy(row);
			}
			catch (DataAccessException e)
			{
				_msg.Warn(
					$"Error loading {table.Name} <oid> {oid}. The respective error will have no geometry.",
					e);
			}

			return null;
		}

		private int CheckOrderedRows([NotNull] TableView tableView, int minTableIndex)
		{
			var errorCount = 0;
			DataRow dataRow0 = null;
			DataRow dataRow1 = null;
			var equalRowsFound = false;
			var alreadyReported = false;

			foreach (DataRowView rowView in tableView.GetFilteredRows())
			{
				if (! equalRowsFound)
				{
					dataRow0 = dataRow1;
				}

				dataRow1 = rowView.Row;

				if (Equals(dataRow0, dataRow1))
				{
					Assert.NotNull(dataRow0, "dataRow0 is null");

					if (minTableIndex > 0 && ! equalRowsFound)
					{
						var tableIndex = (int) dataRow0[_tableIndexColumn];
						if (tableIndex < minTableIndex)
						{
							alreadyReported = true;
						}
					}

					if (! alreadyReported)
					{
						if (! equalRowsFound)
						{
							errorCount += ReportError(dataRow0);
						}

						errorCount += ReportError(dataRow1);
					}

					equalRowsFound = true;
				}
				else
				{
					equalRowsFound = false;
					alreadyReported = false;
				}
			}

			return errorCount;
		}

		private bool Equals([CanBeNull] DataRow row0, [NotNull] DataRow row1)
		{
			if (row0 == null)
			{
				return false;
			}

			return _compareFieldIndexes.All(
				fieldIndex => row0[fieldIndex].Equals(row1[fieldIndex]));
		}

		private class RowEnumerator : IDisposable
		{
			[NotNull]
			public static RowEnumerator CreateOrderBy([NotNull] IReadOnlyTable table,
			                                          [NotNull] ITableFilter filter,
			                                          [NotNull] string firstUniqueFieldName,
			                                          int tableIndex)
			{
				filter.PostfixClause = string.Format(" ORDER BY {0}",
				                                     firstUniqueFieldName);

				int firstUniqueFieldIndex = table.FindField(firstUniqueFieldName);
				return new RowEnumerator(table.EnumRows(filter, recycle: true),
				                         firstUniqueFieldIndex, tableIndex);
			}

			[NotNull]
			public static RowEnumerator CreateTableSort([NotNull] IReadOnlyTable table,
			                                            [CanBeNull] ITableFilter filter,
			                                            [NotNull] string firstUniqueFieldName,
			                                            [NotNull] IComparer<object> comparer,
			                                            int tableIndex)
			{
				int firstUniqueFieldIndex = table.FindField(firstUniqueFieldName);

				ITable aoTable = (ITable) table.FullName.Open();
				ReadOnlyTable roTable = ReadOnlyTableFactory.Create(aoTable);
				ITableSort tableSort =
					TableSortUtils.CreateTableSort(aoTable, firstUniqueFieldName);

				tableSort.Compare = new FieldSortCallback(comparer);
				tableSort.QueryFilter = TableFilterUtils.GetQueryFilter(filter);
				tableSort.Sort(null);

				return new RowEnumerator(
					new EnumCursor(aoTable, tableSort.Rows).Select(
						r => new ReadOnlyRow(roTable, r)),
					firstUniqueFieldIndex, tableIndex);
			}

			[NotNull] private readonly IEnumerator<IReadOnlyRow> _cursor;
			private readonly int _firstUniqueFieldIndex;

			private bool _disposed;

			private RowEnumerator([NotNull] IEnumerable<IReadOnlyRow> cursor,
			                      int firstUniqueFieldIndex,
			                      int tableIndex)
			{
				Assert.ArgumentNotNull(cursor, nameof(cursor));

				_cursor = cursor.GetEnumerator();
				_firstUniqueFieldIndex = firstUniqueFieldIndex;
				TableIndex = tableIndex;
			}

			public int TableIndex { get; }

			public bool TryAddNext(
				[NotNull] IDictionary<object, List<RowEnumerator>> enumerators)
			{
				bool success = _cursor.MoveNext();
				CurrentRow = _cursor.Current;
				if (success)
				{
					object key = Assert.NotNull(CurrentRow).get_Value(_firstUniqueFieldIndex);

					if (key == null)
					{
						key = NullValue.Instance;
					}

					List<RowEnumerator> keyEnums;
					if (! enumerators.TryGetValue(key, out keyEnums))
					{
						keyEnums = new List<RowEnumerator>();
						enumerators.Add(key, keyEnums);
					}

					keyEnums.Add(this);
				}
				else
				{
					DisposeEnumerator();
				}

				return success;
			}

			public void Dispose()
			{
				DisposeEnumerator();
			}

			[CanBeNull]
			public IReadOnlyRow CurrentRow { get; private set; }

			private void DisposeEnumerator()
			{
				if (_disposed)
				{
					return;
				}

				if (Marshal.IsComObject(_cursor))
				{
					Marshal.ReleaseComObject(_cursor);
				}
				else if (_cursor is IDisposable)
				{
					((IDisposable) _cursor).Dispose();
				}

				_disposed = true;
			}
		}

		private class NullValue
		{
			public static readonly NullValue Instance = new NullValue();
		}

		private class ObjectComparer : IComparer<object>
		{
			private readonly Func<object, object, int> _baseCompare;

			public ObjectComparer([CanBeNull] Func<object, object, int> baseCompare)
			{
				_baseCompare = baseCompare;
				DbNullFirst = true;
			}

			public bool DbNullFirst { get; set; }

			public int Compare(object x, object y)
			{
				if (x == y)
				{
					return 0;
				}

				if (Convert.IsDBNull(x))
				{
					return DbNullFirst ? -1 : 1;
				}

				if (Convert.IsDBNull(y))
				{
					return DbNullFirst ? 1 : -1;
				}

				if (x == NullValue.Instance)
				{
					return -1;
				}

				if (y == NullValue.Instance)
				{
					return 1;
				}

				if (_baseCompare != null)
				{
					return _baseCompare(x, y);
				}

				return ((IComparable) x).CompareTo(y);
			}
		}

		/// <summary>
		///   Provides sorting by string value for Guids. This allows for compatibility between
		///   File-Geodatabase and DBMS-based Geodatabases.
		/// </summary>
		private class FieldSortCallback : ITableSortCallBack
		{
			[NotNull] private readonly IComparer<object> _objectComparer;

			#region Implementation of ITableSortCallBack

			public FieldSortCallback([NotNull] IComparer<object> objectComparer)
			{
				_objectComparer = objectComparer;
			}

			public int Compare(object value1, object value2, int fieldIndex,
			                   int fieldSortIndex)
			{
				return _objectComparer.Compare(value1, value2);
			}

			#endregion
		}
	}
}
