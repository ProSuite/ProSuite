using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container;
using ProSuite.QA.Container.PolygonGrower;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;

namespace ProSuite.QA.Tests.Network
{
	public abstract class QaGroupNetworkBase : QaNetworkBase
	{
		internal class FieldValueCode : LocalTestIssueCodes
		{
			public const string InvalidFieldValue_InvalidValueForSeparator =
				"InvalidFieldValue.InvalidValueForSeparator";

			public const string InvalidFieldValue_DuplicateGroupValueInField =
				"InvalidFieldValue.DuplicateGroupValueInField";

			public FieldValueCode() : base("LineGroups") { }
		}

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new FieldValueCode());

		protected QaGroupNetworkBase([NotNull] IEnumerable<ITable> featureClasses)
			: base(featureClasses, false) { }
	}

	public abstract class QaGroupNetworkBase<TDirectedRow> : QaGroupNetworkBase
		where TDirectedRow : class, IDirectedRow
	{
		protected class GroupByComparer : IComparer<Group>, IEqualityComparer<Group>
		{
			#region IComparer<Group> Members

			public bool Equals(Group x, Group y)
			{
				return Compare(x, y) == 0;
			}

			public int GetHashCode(Group x)
			{
				var code = 0;
				foreach (object value in x.Values)
				{
					code ^= value.GetHashCode();
				}

				return code;
			}

			public int Compare(Group x, Group y)
			{
				if (x == y)
				{
					return 0;
				}

				if (x == null)
				{
					return -1;
				}

				if (y == null)
				{
					return 1;
				}

				int length = x.Values.Count;
				for (var i = 0; i < length; i++)
				{
					object oX = x.Values[i];
					object oY = y.Values[i];

					if (DBNull.Value.Equals(oX))
					{
						if (DBNull.Value.Equals(oY))
						{
							continue;
						}

						return -1;
					}

					if (DBNull.Value.Equals(oY))
					{
						return 1;
					}

					var cX = oX as IComparable;
					if (cX == null)
					{
						continue;
					}

					int d = cX.CompareTo(oY);
					if (d != 0)
					{
						return d;
					}
				}

				return 0;
			}

			#endregion
		}

		protected class Group
		{
			public readonly List<object> Values;

			public Group([NotNull] List<object> values)
			{
				Values = values;
			}

			public int GetGroupIndex(IList<Group> groups)
			{
				var comparer = new GroupByComparer();
				for (var i = 0; i < groups.Count; i++)
				{
					if (comparer.Equals(this, groups[i]))
					{
						return i;
					}
				}

				return -1;
			}

			public override string ToString()
			{
				return StringUtils.Concatenate(Values, ",");
			}

			[NotNull]
			public string GetInfo([NotNull] IList<GroupBy> groupBys,
			                      bool trimSeparator = false)
			{
				var sb = new StringBuilder();

				for (var index = 0; index < Values.Count; index++)
				{
					GroupBy groupBy = groupBys[index];

					if (trimSeparator && sb.Length > 0)
					{
						// append separator only if needed
						sb.Append(";");
					}

					sb.AppendFormat("{0}={1}",
					                groupBy.GetFieldNamesString(),
					                Values[index]);

					if (! trimSeparator)
					{
						// always append separator
						// (for compatibility with old issue descriptions in QaGroupConnected; 
						// always trim after switching to new exception mechanism)
						sb.Append(";");
					}
				}

				return sb.ToString();
			}
		}

		protected class GroupBy
		{
			[NotNull] private readonly string _groupBy;
			[NotNull] private readonly List<string> _fields;
			[NotNull] private readonly List<string[]> _separators;

			private GroupBy([NotNull] string groupBy,
			                [NotNull] List<string> fields,
			                [NotNull] List<string[]> separators)
			{
				_groupBy = groupBy;
				_separators = separators;
				_fields = fields;
			}

			[NotNull]
			public static GroupBy Create([NotNull] string groupByString,
			                             [CanBeNull] string valueSeparator)
			{
				List<string> fields;
				List<string[]> separators;
				Parse(groupByString, valueSeparator, out fields, out separators);

				return new GroupBy(groupByString, fields, separators);
			}

			private static void Parse([NotNull] string groupByString,
			                          [CanBeNull] string valueSeparator,
			                          [NotNull] out List<string> fields,
			                          [NotNull] out List<string[]> separators)
			{
				fields = new List<string>();
				separators = new List<string[]>();

				var position = 0;

				var index = 0;
				while (index >= 0)
				{
					string field;
					string separator = null;

					index = groupByString.IndexOfAny(new[] {';', '('}, position);

					if (index < 0)
					{
						field = groupByString.Substring(position).Trim();
					}
					else
					{
						field = groupByString.Substring(position, index - position).Trim();
						position = index + 1;

						if (groupByString[index] == '(')
						{
							index = groupByString.IndexOf(')', position);

							Assert.True(index >= 0,
							            "Invalid groupBy string: {0} (missing closing parenthesis)",
							            groupByString);

							separator = groupByString.Substring(position, index - position);
							position = index + 1;
							index = groupByString.IndexOf(';', position);

							string rest = index < 0
								              ? groupByString.Substring(position)
								              : groupByString.Substring(position, index - position);

							Assert.True(string.IsNullOrEmpty(rest.Trim()),
							            "Invalid groupBy string: {0}",
							            groupByString);

							position = index + 1;
						}
					}

					fields.Add(field);

					if (separator != null)
					{
						if (separators.Count == 0)
						{
							for (var i = 1; i < fields.Count; i++)
							{
								separators.Add(null);
							}
						}

						separators.Add(new[] {separator});
					}
					else if (separators.Count > 0)
					{
						separators.Add(null);
					}
				}

				if (! string.IsNullOrEmpty(valueSeparator))
				{
					if (separators.Count == 0)
					{
						separators.Add(new[] {valueSeparator});
					}
					else
					{
						int separatorCount = separators.Count;
						for (var separatorIndex = 0;
						     separatorIndex < separatorCount;
						     separatorIndex++)
						{
							if (separators[separatorIndex] == null)
							{
								separators[separatorIndex] = new[] {valueSeparator};
							}
						}
					}
				}
			}

			[NotNull]
			public static Dictionary<int, List<int>> GetTableFieldIndexes(
				[NotNull] IList<ITable> tables,
				[NotNull] IList<GroupBy> groupBys)
			{
				int groupByCount = groupBys.Count;

				var result = new Dictionary<int, List<int>>(tables.Count);

				for (var tableIndex = 0; tableIndex < tables.Count; tableIndex++)
				{
					ITable table = tables[tableIndex];

					var fieldInfos = new List<int>(groupByCount);
					result.Add(tableIndex, fieldInfos);

					for (var groupByIndex = 0; groupByIndex < groupByCount; groupByIndex++)
					{
						string fieldName = groupBys[groupByIndex].GetFieldName(tableIndex);
						int fieldIndex = table.FindField(fieldName);

						if (fieldIndex < 0)
						{
							throw new ArgumentException(
								string.Format("Cannot find field {0} in table {1}",
								              fieldName, ((IDataset) table).Name));
						}

						fieldInfos.Add(fieldIndex);
					}
				}

				return result;
			}

			[NotNull]
			public string GetFieldNamesString()
			{
				var sb = new StringBuilder();

				foreach (string field in _fields)
				{
					if (sb.Length > 0)
					{
						sb.Append("|");
					}

					sb.AppendFormat("{0}", field);
				}

				return sb.ToString();
			}

			[CanBeNull]
			public string[] GetFieldSeparator(int tableIndex)
			{
				if (_separators.Count == 0)
				{
					return null;
				}

				if (_separators.Count == 1)
				{
					return _separators[0];
				}

				if (tableIndex >= _separators.Count)
				{
					string msg = string.Format("invalid groupBy definition '{0}' for table {1}",
					                           _groupBy, tableIndex + 1);
					throw new InvalidOperationException(msg);
				}

				return _separators[tableIndex];
			}

			[NotNull]
			public string GetFieldName(int tableIndex)
			{
				if (tableIndex < 0)
				{
					throw new ArgumentOutOfRangeException(nameof(tableIndex));
				}

				if (_fields.Count == 1)
				{
					return _fields[0];
				}

				if (tableIndex >= _fields.Count)
				{
					string msg = string.Format("invalid groupBy definition '{0}' for table {1}",
					                           _groupBy, tableIndex + 1);
					throw new InvalidOperationException(msg);
				}

				return _fields[tableIndex];
			}
		}

		protected class GroupEnds : GroupEnds<TDirectedRow>
		{
			public GroupEnds([NotNull] List<TDirectedRow> endRows)
				: base(endRows) { }
		}

		protected class GroupEnds<T> where T : IDirectedRow
		{
			[NotNull] private readonly List<T> _endRows;

			public List<T> EndRows => _endRows;

			public bool Cyclic { get; set; }

			public GroupEnds([NotNull] List<T> endRows)
			{
				_endRows = endRows;
			}
		}

		protected class LinkedListHelper<T> where T : class
		{
			[NotNull] private readonly List<LinkedList<T>> _connecteds;

			public LinkedListHelper()
			{
				_connecteds = new List<LinkedList<T>>();
			}

			public bool TryAdd(T x, T y)
			{
				LinkedList<T> xList = GetLinkedList(x);
				LinkedList<T> yList = GetLinkedList(y);

				bool validConnection;
				if (xList == null && yList == null)
				{
					var connected = new LinkedList<T>();
					connected.AddFirst(x);
					connected.AddLast(y);
					_connecteds.Add(connected);

					validConnection = true;
				}
				else if (xList == null)
				{
					validConnection = TryAddGroup(x, yList, y);
				}
				else if (yList == null)
				{
					validConnection = TryAddGroup(y, xList, x);
				}
				else
				{
					validConnection = TryJoinGroups(xList, yList, x, y);
					if (validConnection)
					{
						_connecteds.Remove(yList);
					}
				}

				return validConnection;
			}

			[CanBeNull]
			private LinkedList<T> GetLinkedList([NotNull] T group)
			{
				foreach (LinkedList<T> connected in _connecteds)
				{
					if (connected.Contains(group))
					{
						return connected;
					}
				}

				return null;
			}

			private static bool TryAddGroup([NotNull] T add,
			                                [NotNull] LinkedList<T> linked,
			                                [NotNull] T connect)
			{
				if (linked.First.Value == connect)
				{
					linked.AddFirst(add);
					return true;
				}

				if (linked.Last.Value == connect)
				{
					linked.AddLast(add);
					return true;
				}

				return false;
			}

			private static bool TryJoinGroups([NotNull] LinkedList<T> group,
			                                  [NotNull] LinkedList<T> otherGroup,
			                                  [NotNull] T groupEnd,
			                                  [NotNull] T otherEnd)
			{
				if (group == otherGroup)
				{
					return false;
				}

				int groupPos = GetGroupPos(group, groupEnd);
				if (groupPos == 0)
				{
					return false;
				}

				int otherPos = GetGroupPos(otherGroup, otherEnd);
				if (otherPos == 0)
				{
					return false;
				}

				if (groupPos == 1)
				{
					if (otherPos == 1)
					{
						LinkedListNode<T> last = otherGroup.Last;
						while (last != null)
						{
							group.AddLast(last.Value);
							last = last.Previous;
						}
					}
					else
					{
						LinkedListNode<T> first = otherGroup.First;
						while (first != null)
						{
							group.AddLast(first.Value);
							first = first.Next;
						}
					}
				}
				else
				{
					if (otherPos == 1)
					{
						LinkedListNode<T> last = otherGroup.Last;
						while (last != null)
						{
							group.AddFirst(last.Value);
							last = last.Previous;
						}
					}
					else
					{
						LinkedListNode<T> first = otherGroup.First;
						while (first != null)
						{
							group.AddFirst(first.Value);
							first = first.Next;
						}
					}
				}

				return true;
			}

			private static int GetGroupPos([NotNull] LinkedList<T> list, [NotNull] T element)
			{
				if (list.First.Value == element)
				{
					return -1;
				}

				if (list.Last.Value == element)
				{
					return 1;
				}

				return 0;
			}
		}

		private const int _conditionTableIndex = 0;

		[NotNull] private readonly IList<string> _groupByExpressions;

		[NotNull] private readonly PathRowComparer _pathRowComparer;

		[CanBeNull] private List<GroupBy> _groupBys;
		[CanBeNull] private Dictionary<int, List<int>> _tableFieldIndexes;
		[CanBeNull] private string _valueSeparator;

		private List<DataView> _groupConditionViews;

		protected QaGroupNetworkBase([NotNull] IEnumerable<ITable> featureClasses,
		                             [NotNull] IList<string> groupBy)
			: base(featureClasses)
		{
			_groupByExpressions = groupBy;
			_pathRowComparer = new PathRowComparer(new TableIndexRowComparer());
		}

		protected string ValueSeparatorBase
		{
			get { return _valueSeparator; }
			set
			{
				if (_valueSeparator != value)
				{
					_groupBys = null;
					_tableFieldIndexes = null;
				}

				_valueSeparator = value;
			}
		}

		protected IList<string> GroupConditionsBase { get; set; }

		[NotNull]
		protected List<GroupBy> GroupBys => _groupBys ?? (_groupBys = CreateGroupBys());

		[NotNull]
		private List<GroupBy> CreateGroupBys()
		{
			return
				_groupByExpressions.Select(
					                   expression =>
						                   GroupBy.Create(expression, ValueSeparatorBase))
				                   .ToList();
		}

		[NotNull]
		private List<DataView> GroupConditionViews =>
			_groupConditionViews ?? (_groupConditionViews = GetGroupConditionViews());

		[NotNull]
		private List<DataView> GetGroupConditionViews()
		{
			if (GroupConditionsBase == null || GroupConditionsBase.Count <= 0)
			{
				return new List<DataView>();
			}

			var conditionTable = new DataTable();

			foreach (GroupBy groupBy in GroupBys)
			{
				conditionTable.Columns.Add(
					groupBy.GetFieldName(_conditionTableIndex), typeof(object));
			}

			var conditionViews = new List<DataView>(GroupConditionsBase.Count);

			foreach (string condition in GroupConditionsBase)
			{
				var conditionView = new DataView(conditionTable);
				conditionView.RowFilter = condition;
				conditionViews.Add(conditionView);
			}

			return conditionViews;
		}

		protected PathRowComparer PathRowComparer => _pathRowComparer;

		[NotNull]
		private Dictionary<int, List<int>> TableFieldIndexes => _tableFieldIndexes ??
		                                                        (_tableFieldIndexes =
			                                                         GroupBy.GetTableFieldIndexes(
				                                                         InvolvedTables, GroupBys));

		protected sealed override void ConfigureQueryFilter(int tableIndex,
		                                                    IQueryFilter queryFilter)
		{
			base.ConfigureQueryFilter(tableIndex, queryFilter);

			foreach (GroupBy groupBy in GroupBys)
			{
				queryFilter.AddField(groupBy.GetFieldName(tableIndex));
			}
		}

		protected override int CompleteTileCore(TileInfo args)
		{
			//if (args.State == TileState.Initial)
			//{

			//	return NoError;
			//}

			int errorCount = base.CompleteTileCore(args);

			return errorCount;
		}

		protected int ResolveNodes()
		{
			var errorCount = 0;
			if (ConnectedLinesList == null)
			{
				return errorCount;
			}

			errorCount +=
				ConnectedLinesList.Sum(
					connectedRows => ResolveRows(connectedRows));
			return errorCount;
		}

		[NotNull]
		public List<T> GetDangle<T>([NotNull] T endRow, out bool isComplete)
			where T : class, INodesDirectedRow
		{
			var leaf = new List<T>();
			leaf.Add(endRow);

			NetNode<T> nextNode = endRow.FromNode.RowsCount == 1
				                      ? (NetNode<T>) endRow.ToNode
				                      : (NetNode<T>) endRow.FromNode;
			T preRow = endRow;
			while (nextNode != null && nextNode.Rows.Count == 2)
			{
				T nextRow = null;
				foreach (T directedRow in nextNode.Rows)
				{
					if (! _pathRowComparer.Equals(directedRow, preRow))
					{
						nextRow = directedRow;
					}
				}

				if (nextRow == null)
				{
					break;
				}

				leaf.Add(nextRow);

				nextNode = nextRow.FromNode == nextNode
					           ? (NetNode<T>) nextRow.ToNode
					           : (NetNode<T>) nextRow.FromNode;
				preRow = nextRow;
			}

			isComplete = nextNode != null;

			return leaf;
		}

		[NotNull]
		protected List<TDirectedRow> GetDangle(
			[NotNull] LinkedListNode<TDirectedRow> endRow)
		{
			var leaf = new List<TDirectedRow>();

			LinkedListNode<TDirectedRow> prev = endRow;
			LinkedListNode<TDirectedRow> next = endRow.Next ?? endRow.List.First;

			int listCount = endRow.List.Count;
			while (leaf.Count * 2 < listCount &&
			       PathRowComparer.Compare(prev.Value, next.Value) == 0)
			{
				leaf.Add(next.Value);
				prev = prev.Previous ?? endRow.List.Last;
				next = next.Next ?? endRow.List.First;
			}

			return leaf;
		}

		protected abstract TDirectedRow ConvertRow(DirectedRow row);

		private int ResolveRows([NotNull] List<DirectedRow> connectedRows)
		{
			var errorCount = 0;

			List<TDirectedRow> directedRows;

			var groupDict = new Dictionary<Group, List<TDirectedRow>>(new GroupByComparer());
			errorCount += FillGroupDict(connectedRows, groupDict, out directedRows, true);

			errorCount += OnNodeAssembled(directedRows, groupDict);

			return errorCount;
		}

		protected abstract int OnNodeAssembled(List<TDirectedRow> directedRows,
		                                       Dictionary<Group, List<TDirectedRow>>
			                                       groupDict);

		protected int FillGroupDict(
			[NotNull] List<DirectedRow> connectedRows,
			[NotNull] Dictionary<Group, List<TDirectedRow>> groupDict,
			out List<TDirectedRow> convertedRows, bool reportErrors)
		{
			connectedRows.Sort(new DirectedRow.RowByLineAngleComparer());

			var errorCount = 0;
			convertedRows = new List<TDirectedRow>(connectedRows.Count);
			foreach (DirectedRow connectedRow in connectedRows)
			{
				TDirectedRow directedRow = ConvertRow(connectedRow);
				convertedRows.Add(directedRow);

				int readErrors;
				List<List<object>> values = ReadGroupValues(
					connectedRow.Row.Row, connectedRow.Row.TableIndex, out readErrors,
					reportErrors);

				errorCount += readErrors;
				if (values == null)
				{
					continue;
				}

				foreach (Group group in GetGroups(values))
				{
					List<TDirectedRow> groupedRows;
					if (! groupDict.TryGetValue(group, out groupedRows))
					{
						groupedRows = new List<TDirectedRow>(connectedRows.Count);
						groupDict.Add(group, groupedRows);
					}

					groupedRows.Add(directedRow);
				}
			}

			return errorCount;
		}

		[CanBeNull]
		private List<List<object>> ReadGroupValues(
			[NotNull] IRow row, int tableIndex)
		{
			return ReadGroupValues(row, tableIndex, out int _, false);
		}

		[CanBeNull]
		private List<List<object>> ReadGroupValues(
			[NotNull] IRow row, int tableIndex,
			out int errorCount, bool reportErrors)
		{
			List<int> fieldIndexes = TableFieldIndexes[tableIndex];

			var valuesByField = new List<List<object>>();

			errorCount = 0;

			for (var i = 0; i < fieldIndexes.Count; i++)
			{
				int fieldIndex = fieldIndexes[i];
				GroupBy groupBy = GroupBys[i];
				string[] separator = groupBy.GetFieldSeparator(tableIndex);

				List<object> fieldValues = ReadFieldValues(row, fieldIndex, separator);

				if (fieldValues.Count > 1)
				{
					DistinctValues<object> nonUniqueValues;
					List<object> uniqueValues = GetUniqueValues(fieldValues, out nonUniqueValues);

					if (nonUniqueValues != null && reportErrors)
					{
						string fieldName = row.Fields.get_Field(fieldIndex).Name;

						foreach (DistinctValue<object> nonUniqueValue in nonUniqueValues.Values)
						{
							string description = string.Format(
								"Value {0} appears {1} times in field value",
								Format(nonUniqueValue), nonUniqueValue.Count);

							errorCount += ReportError(
								description,
								((IFeature) row).ShapeCopy,
								Codes[FieldValueCode.InvalidFieldValue_DuplicateGroupValueInField],
								fieldName,
								row);
						}
					}

					valuesByField.Add(uniqueValues);
				}
				else
				{
					valuesByField.Add(fieldValues);
				}
			}

			var valueErrorCount = 0;
			foreach (int valueFieldIndex in GetErrorValueFieldIndexes(valuesByField))
			{
				valueErrorCount++;
				if (reportErrors)
				{
					errorCount += ReportValueError(row, tableIndex, valueFieldIndex);
				}
			}

			if (valueErrorCount > 0)
			{
				valuesByField = null;
			}

			return valuesByField;
		}

		[NotNull]
		private static List<object> ReadFieldValues([NotNull] IRow row,
		                                            int fieldIndex,
		                                            [CanBeNull] string[] separator)
		{
			var result = new List<object>();

			object value = row.get_Value(fieldIndex);

			if (separator == null || DBNull.Value.Equals(value))
			{
				result.Add(value);
			}
			else
			{
				var fieldsText = value as string;
				if (fieldsText != null)
				{
					result.AddRange(fieldsText.Split(separator, StringSplitOptions.None));
				}
			}

			return result;
		}

		protected abstract int ReportAddGroupsErrors([NotNull] Group group,
		                                             [NotNull] IList<TDirectedRow> groupRows);

		protected abstract TDirectedRow Reverse(TDirectedRow row);

		private int ReportValueError([NotNull] IRow row,
		                             int tableIndex,
		                             int valueFieldIndex)
		{
			int tableFieldIndex = TableFieldIndexes[tableIndex][valueFieldIndex];
			string fieldName = row.Fields.get_Field(tableFieldIndex).Name;

			string description = string.Format(
				"Invalid value {0}='{1}' for separator {2}.",
				fieldName,
				row.get_Value(tableFieldIndex),
				GroupBys[valueFieldIndex].GetFieldSeparator(tableIndex));

			int errorCount = ReportError(description,
			                             ((IFeature) row).ShapeCopy,
			                             Codes[
				                             FieldValueCode
					                             .InvalidFieldValue_InvalidValueForSeparator],
			                             fieldName,
			                             row);

			return errorCount;
		}

		[NotNull]
		private IEnumerable<int> GetErrorValueFieldIndexes(
			[NotNull] IList<List<object>> values)
		{
			for (var valueFieldIndex = 0; valueFieldIndex < values.Count; valueFieldIndex++)
			{
				List<object> objects = values[valueFieldIndex];
				if (objects.Count != 0)
				{
					continue;
				}

				yield return valueFieldIndex;
			}
		}

		[NotNull]
		protected IList<Group> GetGroups(IRow row, int tableIndex)
		{
			List<List<object>> values = ReadGroupValues(row, tableIndex);

			if (values == null)
			{
				return new List<Group>();
			}

			return GetGroups(values);
		}

		[NotNull]
		private IList<Group> GetGroups(
			[NotNull] ICollection<List<object>> fieldValues)
		{
			var count = 1;
			foreach (List<object> fieldValue in fieldValues)
			{
				count *= fieldValue.Count;
			}

			int fieldCount = fieldValues.Count;

			var groups = new List<Group>(count);
			groups.Add(new Group(new List<object>(fieldCount)));

			foreach (List<object> fieldValue in fieldValues)
			{
				int valueCount = fieldValue.Count;
				List<Group> addGroups = null;
				if (valueCount > 1)
				{
					addGroups = new List<Group>((valueCount - 1) * groups.Count);
					for (var i = 1; i < valueCount; i++)
					{
						object value = fieldValue[i];
						foreach (Group orig in groups)
						{
							var copy = new Group(new List<object>(fieldCount));
							copy.Values.AddRange(orig.Values);
							copy.Values.Add(value);
							addGroups.Add(copy);
						}
					}
				}

				object value0 = fieldValue[0];
				foreach (Group orig in groups)
				{
					orig.Values.Add(value0);
				}

				if (addGroups != null)
				{
					groups.AddRange(addGroups);
				}
			}

			groups = ApplyGroupConditions(groups);

			return groups;
		}

		[NotNull]
		private List<Group> ApplyGroupConditions([NotNull] List<Group> groups)
		{
			if (GroupConditionsBase == null || GroupConditionsBase.Count == 0 ||
			    GroupConditionViews.Count == 0)
			{
				return groups;
			}

			var validGroups = new List<Group>(groups.Count);

			foreach (Group group in groups)
			{
				DataTable table = GroupConditionViews[0].Table;
				table.Clear();
				DataRow row = table.NewRow();

				for (var index = 0; index < group.Values.Count; index++)
				{
					GroupBy groupBy = GroupBys[index];
					row[groupBy.GetFieldName(_conditionTableIndex)] = group.Values[index];
				}

				table.Rows.Add(row);

				var invalid = false;
				foreach (DataView conditionView in GroupConditionViews)
				{
					invalid = conditionView.Count == 0;
					if (invalid)
					{
						break;
					}
				}

				table.Clear();

				if (! invalid)
				{
					validGroups.Add(group);
				}
			}

			return validGroups;
		}

		[NotNull]
		private static string Format([NotNull] DistinctValue<object> nonUniqueValue)
		{
			string format = nonUniqueValue.Value is string
				                ? "'{0}'"
				                : "{0}";
			return string.Format(format, nonUniqueValue.Value);
		}

		[NotNull]
		private static List<object> GetUniqueValues(
			[NotNull] List<object> fieldValues,
			[CanBeNull] out DistinctValues<object> nonUniqueValues)
		{
			var result = new List<object>(fieldValues.Count);
			nonUniqueValues = null;

			foreach (object value in fieldValues)
			{
				if (result.Contains(value))
				{
					if (nonUniqueValues == null)
					{
						nonUniqueValues = new DistinctValues<object>();
						nonUniqueValues.Add(value); // add the first occurrence
					}

					nonUniqueValues.Add(value);
				}
				else
				{
					result.Add(value);
				}
			}

			return result;
		}

		[NotNull]
		protected IEnumerable<InvolvedRow> GetUniqueInvolvedRows(
			[NotNull] ICollection<IRow> rows)
		{
			var result = new List<InvolvedRow>(rows.Count);
			var set = new HashSet<InvolvedRow>();

			foreach (IRow row in rows)
			{
				foreach (InvolvedRow involvedRow in GetInvolvedRows(row))
				{
					if (set.Add(involvedRow))
					{
						result.Add(involvedRow);
					}
				}
			}

			return result;
		}

		[NotNull]
		protected IEnumerable<InvolvedRow> GetUniqueInvolvedRows(
			[NotNull] ICollection<ITableIndexRow> rows)
		{
			var result = new List<InvolvedRow>(rows.Count);
			var set = new HashSet<InvolvedRow>();

			foreach (ITableIndexRow row in rows)
			{
				foreach (InvolvedRow involvedRow in GetInvolvedRows(row))
				{
					if (set.Add(involvedRow))
					{
						result.Add(involvedRow);
					}
				}
			}

			return result;
		}
	}
}
