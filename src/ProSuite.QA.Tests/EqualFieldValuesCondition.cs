using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestSupport;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Text;

namespace ProSuite.QA.Tests
{
	public class EqualFieldValuesCondition
	{
		private const string _allowedDifferenceConditionPrefix = "allowedDifferenceCondition=";
		private const string _ignorePrefix = "ignore=";
		private const string _ignoreConditionPrefix = "ignoreCondition=";

		private readonly bool _caseSensitive;
		[NotNull] private readonly List<FieldInfo> _fieldInfos;

		public EqualFieldValuesCondition([CanBeNull] string fields,
		                                 [CanBeNull] IEnumerable<string> fieldOptions,
		                                 [NotNull] IEnumerable<ITable> comparedTables,
		                                 bool caseSensitive)
		{
			Assert.ArgumentNotNull(comparedTables, nameof(comparedTables));

			_caseSensitive = caseSensitive;

			if (StringUtils.IsNotEmpty(fields))
			{
				_fieldInfos = GetFieldInfos(fields, fieldOptions,
				                            CollectionUtils.GetCollection(comparedTables));
			}
			else
			{
				// empty list
				_fieldInfos = new List<FieldInfo>();
			}
		}

		public IEnumerable<UnequalField> GetNonEqualFields(
			[NotNull] IRow row1, int tableIndex1,
			[NotNull] IRow row2, int tableIndex2)
		{
			foreach (FieldInfo fieldInfo in _fieldInfos)
			{
				string fieldMessage;
				if (! fieldInfo.AreValuesEqual(row1, tableIndex1,
				                               row2, tableIndex2,
				                               _caseSensitive, out fieldMessage))
				{
					yield return new UnequalField(fieldInfo.FieldName.ToUpper(),
					                              fieldMessage);
				}
			}
		}

		[NotNull]
		private List<FieldInfo> GetFieldInfos(
			[NotNull] string fields,
			[CanBeNull] IEnumerable<string> fieldOptions,
			[NotNull] ICollection<ITable> tables)
		{
			List<FieldInfo> result =
				ParseFieldInfos(fields, fieldOptions, _caseSensitive).ToList();

			foreach (FieldInfo fieldInfo in result)
			{
				foreach (ITable table in tables)
				{
					fieldInfo.AddComparedTable(table);
				}
			}

			return result;
		}

		[NotNull]
		public static IEnumerable<FieldInfo> ParseFieldInfos(
			[NotNull] string fields,
			[CanBeNull] IEnumerable<string> fieldOptions = null,
			bool caseSensitive = false)
		{
			Assert.ArgumentNotNull(fields, nameof(fields));

			IDictionary<string, AllowedDifferenceCondition> allowedDifferenceConditions;
			IDictionary<string, IValueTransformation> transformations =
				ParseFieldOptions(fieldOptions, caseSensitive,
				                  out allowedDifferenceConditions);

			var separators = new[] {',', ';'};
			const char escapeCharacter = '\\';

			var fieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (string fieldDefinition in StringUtils.Split(
				fields, separators, escapeCharacter, removeEmptyEntries: true))
			{
				string fieldName;
				string multiValueSeparator;
				ParseFieldDefinition(fieldDefinition, out fieldName, out multiValueSeparator);

				if (! fieldNames.Add(fieldName))
				{
					// field name already in set
					throw new InvalidConfigurationException($"Duplicate field name: {fieldName}");
				}

				IValueTransformation transformation;
				transformations.TryGetValue(fieldName, out transformation);

				AllowedDifferenceCondition allowedDifferenceCondition;
				allowedDifferenceConditions.TryGetValue(fieldName,
				                                        out allowedDifferenceCondition);

				yield return
					new FieldInfo(fieldName, multiValueSeparator, transformation,
					              allowedDifferenceCondition);
			}

			// check that there are no options specified for a field name that is not in the list
			foreach (string fieldNameWithOptions in transformations.Keys)
			{
				if (! fieldNames.Contains(fieldNameWithOptions))
				{
					throw new InvalidConfigurationException(
						$"Options specified for field name that is not checked for equal values: {fieldNameWithOptions}");
				}
			}
		}

		[NotNull]
		private static IDictionary<string, IValueTransformation> ParseFieldOptions(
			[CanBeNull] IEnumerable<string> fieldOptions, bool caseSensitive,
			[NotNull] out IDictionary<string, AllowedDifferenceCondition>
				allowedDifferenceConditions)
		{
			allowedDifferenceConditions = new Dictionary<string, AllowedDifferenceCondition>(
				StringComparer.OrdinalIgnoreCase);

			if (fieldOptions == null)
			{
				return new Dictionary<string, IValueTransformation>();
			}

			const char fieldSeparator = ':';

			var transformations = new Dictionary<string, ChainedValueTransformation>(
				StringComparer.OrdinalIgnoreCase);

			foreach (string fieldOption in fieldOptions)
			{
				if (StringUtils.IsNullOrEmptyOrBlank(fieldOption))
				{
					// ignore empty options
					continue;
				}

				int separatorIndex = fieldOption.IndexOf(fieldSeparator);
				if (separatorIndex <= 0 || separatorIndex >= fieldOption.Length - 1)
				{
					throw CreateInvalidFieldOptionException(fieldOption);
				}

				string fieldName = fieldOption.Substring(0, separatorIndex).Trim();
				string option = fieldOption.Substring(separatorIndex + 1).Trim();

				if (StringUtils.IsNullOrEmptyOrBlank(fieldName))
				{
					throw CreateInvalidFieldOptionException(fieldOption);
				}

				if (StringUtils.IsNullOrEmptyOrBlank(option))
				{
					throw CreateInvalidFieldOptionException(fieldOption);
				}

				if (option.StartsWith(_allowedDifferenceConditionPrefix,
				                      StringComparison.OrdinalIgnoreCase))
				{
					AllowedDifferenceCondition allowedDifferenceCondition =
						CreateAllowedDifferenceCondition(option, fieldName, caseSensitive);

					if (allowedDifferenceCondition != null)
					{
						if (allowedDifferenceConditions.ContainsKey(fieldName))
						{
							throw new InvalidConfigurationException(
								"Only one 'allowedDifferenceCondition' is supported per field");
						}

						allowedDifferenceConditions.Add(fieldName, allowedDifferenceCondition);
					}
				}
				else
				{
					ChainedValueTransformation chainedValueTransformation;
					if (! transformations.TryGetValue(fieldName, out chainedValueTransformation))
					{
						chainedValueTransformation = new ChainedValueTransformation();
						transformations.Add(fieldName, chainedValueTransformation);
					}

					chainedValueTransformation.Add(
						CreateValueTransformation(option, fieldName, caseSensitive));
				}
			}

			return transformations.ToDictionary
				<KeyValuePair<string, ChainedValueTransformation>, string, IValueTransformation>
				(pair => pair.Key,
				 pair => pair.Value,
				 StringComparer.OrdinalIgnoreCase);
		}

		[CanBeNull]
		private static AllowedDifferenceCondition CreateAllowedDifferenceCondition(
			[NotNull] string option, [NotNull] string fieldName,
			bool caseSensitive)
		{
			if (! option.StartsWith(_allowedDifferenceConditionPrefix,
			                        StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}

			string condition = option.Substring(_allowedDifferenceConditionPrefix.Length);

			return StringUtils.IsNullOrEmptyOrBlank(condition)
				       ? null
				       : new AllowedDifferenceCondition(condition, fieldName, caseSensitive);
		}

		[NotNull]
		private static IValueTransformation CreateValueTransformation(
			[NotNull] string option,
			[NotNull] string fieldName,
			bool caseSensitive)
		{
			if (option.StartsWith(_ignorePrefix))
			{
				string regexString = option.Substring(_ignorePrefix.Length);

				return new IgnoreMatchedCharsValueTransformation(regexString);
			}

			if (option.StartsWith(_ignoreConditionPrefix))
			{
				string condition = option.Substring(_ignoreConditionPrefix.Length);

				return new IgnoreConditionValueTransformation(condition, fieldName,
				                                              caseSensitive);
			}

			throw new InvalidConfigurationException(
				$"Invalid option specification for field name {fieldName}: {option}");
		}

		[NotNull]
		private static InvalidConfigurationException CreateInvalidFieldOptionException(
			[NotNull] string fieldOption)
		{
			return new InvalidConfigurationException(
				$"Invalid field option string: {fieldOption}");
		}

		private static void ParseFieldDefinition([NotNull] string fieldDefinition,
		                                         [NotNull] out string fieldName,
		                                         [CanBeNull] out string multiValueSeparator)
		{
			const char separator = ':';
			int separatorIndex = fieldDefinition.IndexOf(separator);
			if (separatorIndex < 0)
			{
				fieldName = fieldDefinition.Trim();
				multiValueSeparator = null;
				return;
			}

			if (separatorIndex == 0)
			{
				throw new InvalidConfigurationException(
					$"Invalid field definition (must start with field name): {fieldDefinition}");
			}

			fieldName = fieldDefinition.Substring(0, separatorIndex).Trim();

			if (separatorIndex + 1 == fieldDefinition.Length)
			{
				// separator is last character
				multiValueSeparator = null;
				return;
			}

			multiValueSeparator = fieldDefinition.Substring(separatorIndex + 1);
		}

		private class IgnoreMatchedCharsValueTransformation : IValueTransformation
		{
			[CanBeNull] private readonly Regex _ignoreRegex;

			public IgnoreMatchedCharsValueTransformation(string regexString)
			{
				Regex regex;
				try
				{
					regex = new Regex(regexString, RegexOptions.Compiled);
				}
				catch (ArgumentException ex)
				{
					throw new InvalidConfigurationException(
						$"Invalid regular expression: {ex.Message}", ex);
				}

				_ignoreRegex = regex;
			}

			public object TransformValue(IRow row, object value)
			{
				if (_ignoreRegex == null)
				{
					return value;
				}

				if (value == null || value is DBNull)
				{
					return value;
				}

				var stringValue = value as string;
				if (stringValue == null)
				{
					throw new InvalidConfigurationException(
						"regular expression is only applicable to text field values");
				}

				return _ignoreRegex.Replace(stringValue, string.Empty);
			}
		}

		private class IgnoreConditionValueTransformation : IValueTransformation
		{
			[NotNull] private readonly string _condition;
			private readonly string _fieldName;
			private readonly bool _caseSensitive;
			private const string _valueFieldName = "_VALUE";

			[NotNull] private readonly IDictionary<ITable, TableView> _tableViews =
				new Dictionary<ITable, TableView>();

			public IgnoreConditionValueTransformation([NotNull] string condition,
			                                          [NotNull] string fieldName,
			                                          bool caseSensitive)
			{
				Assert.ArgumentNotNull(condition, nameof(condition));

				_condition = condition;
				_fieldName = fieldName;
				_caseSensitive = caseSensitive;
			}

			public object TransformValue(IRow row, object value)
			{
				ITable table = row.Table;

				TableView tableView;
				if (! _tableViews.TryGetValue(table, out tableView))
				{
					tableView = CreateTableView(table, _fieldName, _condition, _caseSensitive);
					_tableViews.Add(table, tableView);
				}

				if (tableView == null)
				{
					return value;
				}

				tableView.ClearRows();
				DataRow dataRow = Assert.NotNull(tableView.Add(row));
				dataRow[_valueFieldName] = value;

				return tableView.FilteredRowCount == 1
					       ? null
					       : value;
			}

			[CanBeNull]
			private static TableView CreateTableView([NotNull] ITable table,
			                                         [NotNull] string fieldName,
			                                         [NotNull] string condition,
			                                         bool caseSensitive)
			{
				if (StringUtils.IsNullOrEmptyOrBlank(condition))
				{
					return null;
				}

				TableView result = TableViewFactory.Create(table, condition,
				                                           caseSensitive: caseSensitive,
				                                           useAsConstraint: false);

				result.AddColumn(_valueFieldName, GetFieldType(table, fieldName));

				result.Constraint = condition;

				return result;
			}
		}

		public class AllowedDifferenceCondition : RowPairCondition
		{
			[NotNull] private readonly string _fieldName;
			private readonly string _valueField1;
			private readonly string _valueField2;

			public AllowedDifferenceCondition([NotNull] string condition,
			                                  [NotNull] string fieldName,
			                                  bool caseSensitive = false)
				: base(condition,
				       isDirected: false, undefinedConditionIsFulfilled: false,
				       row1Alias: "G1", row2Alias: "G2",
				       caseSensitive: caseSensitive, conciseMessage: true)
			{
				Assert.ArgumentNotNull(condition, nameof(condition));
				Assert.ArgumentNotNullOrEmpty(fieldName, nameof(fieldName));

				_fieldName = fieldName;
				_valueField1 = $"{Row1Alias}._VALUE";
				_valueField2 = $"{Row2Alias}._VALUE";
			}

			protected override void AddUnboundColumns(Action<string, Type> addColumn,
			                                          IList<ITable> tables)
			{
				Assert.ArgumentNotNull(addColumn, nameof(addColumn));
				Assert.ArgumentNotNull(tables, nameof(tables));
				Assert.ArgumentCondition(tables.Count == 2, "Two tables expected");

				addColumn(_valueField1, GetFieldType(tables[0], _fieldName));
				addColumn(_valueField2, GetFieldType(tables[1], _fieldName));
			}

			public bool IsFulfilled([NotNull] IRow row1, int tableIndex1, object value1,
			                        [NotNull] IRow row2, int tableIndex2, object value2)
			{
				var fieldValues = new Dictionary<string, object>
				                  {
					                  {_valueField1, value1},
					                  {_valueField2, value2}
				                  };

				return IsFulfilled(row1, tableIndex1, row2, tableIndex2, fieldValues);
			}
		}

		[NotNull]
		private static Type GetFieldType([NotNull] ITable table, [NotNull] string fieldName)
		{
			int fieldIndex = table.FindField(fieldName);

			if (fieldIndex < 0)
			{
				throw new ArgumentException(
					$@"Field '{fieldName}' not found in table {DatasetUtils.GetName(table)}",
					nameof(fieldName));
			}

			IField field = table.Fields.Field[fieldIndex];

			return TestUtils.GetColumnType(field);
		}

		private class ChainedValueTransformation : IValueTransformation
		{
			[NotNull] private readonly List<IValueTransformation> _valueTransformations =
				new List<IValueTransformation>();

			public void Add([NotNull] IValueTransformation valueTransformation)
			{
				_valueTransformations.Add(valueTransformation);
			}

			public object TransformValue(IRow row, object value)
			{
				object result = value;

				foreach (IValueTransformation valueTransformation in _valueTransformations)
				{
					result = valueTransformation.TransformValue(row, result);
				}

				return result;
			}
		}

		// public for unit tests
		public class FieldInfo
		{
			[CanBeNull] private readonly IValueTransformation _valueTransformation;

			[CanBeNull] private readonly AllowedDifferenceCondition _allowedDifferenceCondition;

			[NotNull] private readonly IDictionary<ITable, TableFieldInfo> _tableFieldInfos =
				new Dictionary<ITable, TableFieldInfo>();

			[NotNull] private static readonly HashSet<string> _nullStringSet =
				new HashSet<string>(new[] {(string) null});

			public FieldInfo(
				[NotNull] string fieldName,
				[CanBeNull] string multiValueSeparator = null,
				[CanBeNull] IValueTransformation valueTransformation = null,
				[CanBeNull] AllowedDifferenceCondition allowedDifferenceCondition = null)
			{
				Assert.ArgumentNotNullOrEmpty(fieldName, nameof(fieldName));

				FieldName = fieldName;
				_valueTransformation = valueTransformation;
				_allowedDifferenceCondition = allowedDifferenceCondition;
				MultiValueSeparator = multiValueSeparator == null
					                      ? null
					                      : multiValueSeparator.Length == 0
						                      ? null
						                      : multiValueSeparator;
			}

			public void AddComparedTable([NotNull] ITable table)
			{
				if (_tableFieldInfos.ContainsKey(table))
				{
					return;
				}

				int fieldIndex = table.FindField(FieldName);

				Assert.True(fieldIndex >= 0,
				            "Field {0} does not exist in table {1}",
				            FieldName, DatasetUtils.GetName(table));

				esriFieldType fieldType = table.Fields.Field[fieldIndex].Type;

				_tableFieldInfos.Add(table, new TableFieldInfo(fieldIndex, fieldType));
			}

			[ContractAnnotation("=>true, message:canbenull; =>false, message:notnull")]
			public bool AreValuesEqual([NotNull] IRow row1, int tableIndex1,
			                           [NotNull] IRow row2, int tableIndex2,
			                           bool caseSensitive,
			                           [CanBeNull] out string message)
			{
				TableFieldInfo tableFieldInfo1 = GetTableFieldInfo(row1.Table);
				TableFieldInfo tableFieldInfo2 = GetTableFieldInfo(row2.Table);

				object value1 = row1.Value[tableFieldInfo1.FieldIndex];
				object value2 = row2.Value[tableFieldInfo2.FieldIndex];

				bool value1IsNull = value1 == null || value1 is DBNull;
				bool value2IsNull = value2 == null || value2 is DBNull;

				if (value1IsNull && value2IsNull)
				{
					message = null;
					return true;
				}

				// at least one of the values is not null

				if (tableFieldInfo1.IsTextField && tableFieldInfo2.IsTextField)
				{
					if (MultiValueSeparator != null)
					{
						if (! AreSetsEqual(row1, tableIndex1, value1 as string,
						                   row2, tableIndex2, value2 as string,
						                   MultiValueSeparator,
						                   caseSensitive))
						{
							message = GetNonEqualMessage(value1, tableFieldInfo1.FieldType,
							                             value2, tableFieldInfo2.FieldType);
							return false;
						}

						message = null;
						return true;
					}
				}

				if (AreValuesEqual(row1, tableIndex1, value1,
				                   row2, tableIndex2, value2,
				                   caseSensitive))
				{
					message = null;
					return true;
				}

				message = GetNonEqualMessage(value1, tableFieldInfo1.FieldType,
				                             value2, tableFieldInfo2.FieldType);
				return false;
			}

			public bool AreValuesEqual([NotNull] IRow row1, int tableIndex1, object value1,
			                           [NotNull] IRow row2, int tableIndex2, object value2,
			                           bool caseSensitive)
			{
				object transformedValue1 = GetTransformedValue(row1, value1);
				object transformedValue2 = GetTransformedValue(row2, value2);

				bool equal = FieldUtils.AreValuesEqual(transformedValue1,
				                                       transformedValue2,
				                                       caseSensitive);
				if (equal)
				{
					return true;
				}

				// check if the specific difference is allowed
				return _allowedDifferenceCondition != null &&
				       _allowedDifferenceCondition.IsFulfilled(row1, tableIndex1,
				                                               transformedValue1,
				                                               row2, tableIndex2,
				                                               transformedValue2);
			}

			[NotNull]
			public string FieldName { get; }

			[CanBeNull]
			public string MultiValueSeparator { get; }

			private bool AreSetsEqual(
				[NotNull] IRow row1, int tableIndex1, [CanBeNull] string value1,
				[NotNull] IRow row2, int tableIndex2, [CanBeNull] string value2,
				[NotNull] string separator,
				bool caseSensitive)
			{
				StringComparer equalityComparer = caseSensitive
					                                  ? StringComparer.Ordinal
					                                  : StringComparer.OrdinalIgnoreCase;

				HashSet<string> set1 = GetSet(row1, value1, separator, equalityComparer);
				HashSet<string> set2 = GetSet(row2, value2, separator, equalityComparer);

				if (set1.SetEquals(set2))
				{
					return true;
				}

				// sets are not equal - check allowed difference condition if defined
				return _allowedDifferenceCondition != null &&
				       AreAllDifferencesAllowed(row1, tableIndex1, set1,
				                                row2, tableIndex2, set2,
				                                _allowedDifferenceCondition);
			}

			private static bool AreAllDifferencesAllowed([NotNull] IRow row1, int tableIndex1,
			                                             [NotNull] HashSet<string> set1,
			                                             [NotNull] IRow row2, int tableIndex2,
			                                             [NotNull] HashSet<string> set2,
			                                             [NotNull]
			                                             AllowedDifferenceCondition condition)
			{
				HashSet<string> nonEmptySet1 = set1.Count == 0 ? _nullStringSet : set1;
				HashSet<string> nonEmptySet2 = set2.Count == 0 ? _nullStringSet : set2;

				return nonEmptySet1.All(v1 => nonEmptySet2.All(
					                        v2 => condition.IsFulfilled(row1, tableIndex1, v1,
					                                                    row2, tableIndex2, v2)));
			}

			[NotNull]
			private HashSet<string> GetSet(
				[NotNull] IRow row,
				[CanBeNull] string value,
				[NotNull] string separator,
				[NotNull] IEqualityComparer<string> equalityComparer)
			{
				var result = new HashSet<string>(equalityComparer);

				if (value == null)
				{
					return result; // empty set
				}

				string[] tokens = value.Split(new[] {separator},
				                              StringSplitOptions.RemoveEmptyEntries);

				foreach (string token in tokens)
				{
					string transformedToken = GetTransformedValue(row, token);

					if (! string.IsNullOrEmpty(transformedToken) &&
					    ! result.Contains(transformedToken))
					{
						result.Add(transformedToken);
					}
				}

				return result;
			}

			private T GetTransformedValue<T>([NotNull] IRow row, T value)
			{
				return _valueTransformation == null
					       ? value
					       : (T) _valueTransformation.TransformValue(row, value);
			}

			[NotNull]
			private string GetNonEqualMessage([CanBeNull] object value1,
			                                  esriFieldType fieldType1,
			                                  [CanBeNull] object value2,
			                                  esriFieldType fieldType2)
			{
				// TODO use FormatUtils if both are floating-point and not null?

				string value1Text = FormatValue(value1, fieldType1);
				string value2Text = FormatValue(value2, fieldType2);

				const string format = "{0}:{1},{2}";
				return string.Compare(value1Text, value2Text, StringComparison.Ordinal) < 0
					       ? string.Format(format, FieldName, value1Text, value2Text)
					       : string.Format(format, FieldName, value2Text, value1Text);
			}

			[NotNull]
			private static string FormatValue([CanBeNull] object value,
			                                  esriFieldType fieldType)
			{
				if (value == null)
				{
					return "<null>";
				}

				if (value is DBNull)
				{
					return "NULL";
				}

				CultureInfo culture = CultureInfo.InvariantCulture;

				switch (fieldType)
				{
					case esriFieldType.esriFieldTypeOID:
					case esriFieldType.esriFieldTypeSmallInteger:
					case esriFieldType.esriFieldTypeInteger:
						return string.Format(culture, "{0}", value);

					case esriFieldType.esriFieldTypeSingle:
					case esriFieldType.esriFieldTypeDouble:
						// TODO rounding?
						return string.Format(culture, "{0}", value);

					case esriFieldType.esriFieldTypeString:
						return string.Format(culture, "'{0}'", value);

					case esriFieldType.esriFieldTypeDate:
						return string.Format(culture, "{0}", value);

					case esriFieldType.esriFieldTypeGeometry:
						return "<geometry>";

					case esriFieldType.esriFieldTypeBlob:
						return "<blob>";

					case esriFieldType.esriFieldTypeRaster:
						return "<raster>";

					case esriFieldType.esriFieldTypeGUID:
					case esriFieldType.esriFieldTypeGlobalID:
						return string.Format(culture, "{0}", value);

					case esriFieldType.esriFieldTypeXML:
						return string.Format(culture, "{0}", value);

					default:
						return string.Format(culture, "{0}", value);
				}
			}

			[NotNull]
			private TableFieldInfo GetTableFieldInfo([NotNull] ITable table)
			{
				return _tableFieldInfos[table];
			}

			private class TableFieldInfo
			{
				public TableFieldInfo(int fieldIndex, esriFieldType fieldType)
				{
					FieldIndex = fieldIndex;
					FieldType = fieldType;
				}

				public int FieldIndex { get; }

				public esriFieldType FieldType { get; }

				public bool IsTextField => FieldType == esriFieldType.esriFieldTypeString;
			}
		}
	}
}
