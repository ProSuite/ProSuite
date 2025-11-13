using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[AttributeTest]
	public class QaRegularExpression : ContainerTest
	{
		[NotNull] private readonly IReadOnlyTable _table;
		[NotNull] private readonly List<string> _fieldNames;
		private readonly bool _matchIsError;
		[CanBeNull] private readonly string _patternDescription;
		[NotNull] private readonly Regex _regex;
		[CanBeNull] private List<int> _fieldIndices;

		private FieldListType _fieldListType = FieldListType.RelevantFields;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string StringFormattingError = "StringFormattingError";

			public const string FieldValueMatchesRegularExpression =
				"FieldValueMatchesRegularExpression";

			public const string FieldValueDoesNotMatchRegularExpression =
				"FieldValueDoesNotMatchRegularExpression";

			public Code() : base("RegularExpression") { }
		}

		#endregion

		#region constructors

		[Doc(nameof(DocStrings.QaRegularExpression_0))]
		public QaRegularExpression(
				[Doc(nameof(DocStrings.QaRegularExpression_table))] [NotNull]
				IReadOnlyTable table,
				[Doc(nameof(DocStrings.QaRegularExpression_pattern))] [NotNull]
				string pattern,
				[Doc(nameof(DocStrings.QaRegularExpression_fieldName))] [NotNull]
				string fieldName)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(table, pattern, TestUtils.GetTokens(fieldName), false, null) { }

		[Doc(nameof(DocStrings.QaRegularExpression_1))]
		public QaRegularExpression(
				[Doc(nameof(DocStrings.QaRegularExpression_table))] [NotNull]
				IReadOnlyTable table,
				[Doc(nameof(DocStrings.QaRegularExpression_pattern))] [NotNull]
				string pattern,
				[Doc(nameof(DocStrings.QaRegularExpression_fieldNames))] [NotNull]
				IEnumerable<string> fieldNames)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(table, pattern, fieldNames, false, null) { }

		[Doc(nameof(DocStrings.QaRegularExpression_2))]
		public QaRegularExpression(
				[Doc(nameof(DocStrings.QaRegularExpression_table))] [NotNull]
				IReadOnlyTable table,
				[Doc(nameof(DocStrings.QaRegularExpression_pattern))] [NotNull]
				string pattern,
				[Doc(nameof(DocStrings.QaRegularExpression_fieldName))] [NotNull]
				string fieldName,
				[Doc(nameof(DocStrings.QaRegularExpression_matchIsError))]
				bool matchIsError)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(table, pattern, TestUtils.GetTokens(fieldName), matchIsError, null) { }

		[Doc(nameof(DocStrings.QaRegularExpression_3))]
		public QaRegularExpression(
				[Doc(nameof(DocStrings.QaRegularExpression_table))] [NotNull]
				IReadOnlyTable table,
				[Doc(nameof(DocStrings.QaRegularExpression_pattern))] [NotNull]
				string pattern,
				[Doc(nameof(DocStrings.QaRegularExpression_fieldNames))] [NotNull]
				IEnumerable<string> fieldNames,
				[Doc(nameof(DocStrings.QaRegularExpression_matchIsError))]
				bool matchIsError)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(table, pattern, fieldNames, matchIsError, null) { }

		[Doc(nameof(DocStrings.QaRegularExpression_4))]
		public QaRegularExpression(
			[Doc(nameof(DocStrings.QaRegularExpression_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaRegularExpression_pattern))] [NotNull]
			string pattern,
			[Doc(nameof(DocStrings.QaRegularExpression_fieldName))] [NotNull]
			string fieldName,
			[Doc(nameof(DocStrings.QaRegularExpression_matchIsError))]
			bool matchIsError,
			[Doc(nameof(DocStrings.QaRegularExpression_patternDescription))] [CanBeNull]
			string
				patternDescription)
			: this(table, pattern, TestUtils.GetTokens(fieldName), matchIsError,
			       patternDescription) { }

		[Doc(nameof(DocStrings.QaRegularExpression_5))]
		public QaRegularExpression(
			[Doc(nameof(DocStrings.QaRegularExpression_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaRegularExpression_pattern))] [NotNull]
			string pattern,
			[Doc(nameof(DocStrings.QaRegularExpression_fieldNames))] [NotNull]
			IEnumerable<string> fieldNames,
			[Doc(nameof(DocStrings.QaRegularExpression_matchIsError))]
			bool matchIsError,
			[Doc(nameof(DocStrings.QaRegularExpression_patternDescription))] [CanBeNull]
			string
				patternDescription)
			: base(table)
		{
			Assert.ArgumentNotNullOrEmpty(pattern, nameof(pattern));
			Assert.ArgumentNotNull(fieldNames, nameof(fieldNames));

			_table = table;
			_fieldNames = fieldNames.ToList();
			_matchIsError = matchIsError;
			_patternDescription = patternDescription;

			_regex = new Regex(pattern, RegexOptions.Compiled);

			// check if all fields exist
			ValidateFieldNames(table, _fieldNames);

			foreach (var fieldName in _fieldNames)
			{
				AddCustomQueryFilterExpression(fieldName);
			}
		}

		#endregion

		[InternallyUsedTest]
		public QaRegularExpression([NotNull] QaRegularExpressionDefinition definition)
			: this((IReadOnlyTable) definition.Table, definition.Pattern,
			       definition.FieldNames, definition.MatchIsError, definition.PatternDescription)
		{
			FieldListType = definition.FieldListType;
		}

		[Doc(nameof(DocStrings.QaRegularExpression_FieldListType))]
		[TestParameter(FieldListType.RelevantFields)]
		public FieldListType FieldListType { get => _fieldListType;
			set
			{
				_fieldListType = value;
				AddCustomQueryFilterExpression(
					string.Concat(GetRelevantNames(_table, _fieldNames, _fieldListType)
						              .Select(r => $"{r},")));
			}
		} 

		public override bool IsQueriedTable(int tableIndex)
		{
			return false;
		}

		public override bool IsGeometryUsedTable(int tableIndex)
		{
			return AreaOfInterest != null;
		}

		public override bool RetestRowsPerIntersectedTile(int tableIndex)
		{
			return false;
		}

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			if (_fieldIndices == null)
			{
				_fieldIndices = GetFieldIndices(_table, _fieldNames, FieldListType).ToList();
			}

			var errorCount = 0;

			foreach (int fieldIndex in _fieldIndices)
			{
				errorCount += CheckField(row, fieldIndex);
			}

			return errorCount;
		}

		private static void ValidateFieldNames([NotNull] IReadOnlyTable table,
		                                       [NotNull] IEnumerable<string> fieldNames)
		{
			var missingFields = fieldNames.Where(name => table.FindField(name) < 0)
			                              .ToList();

			if (missingFields.Count > 0)
			{
				throw new ArgumentException(
					string.Format("Field(s) not found in table {0}: {1}",
					              table.Name,
					              StringUtils.Concatenate(missingFields, ",")),
					nameof(fieldNames));
			}
		}

		private static IEnumerable<int> GetFieldIndices(
			[NotNull] IReadOnlyTable table,
			[NotNull] IEnumerable<string> fieldNames,
			FieldListType fieldListType)
		{
			return GetRelevantNames(table, fieldNames, fieldListType)
			       .Select(table.FindField)
			       .Where(index => index >= 0);
		}

		[NotNull]
		private static IEnumerable<string> GetRelevantNames(
			[NotNull] IReadOnlyTable table,
			[NotNull] IEnumerable<string> fieldNames,
			FieldListType fieldListType)
		{
			if (fieldListType == FieldListType.RelevantFields)
			{
				return fieldNames;
			}

			var excludedNames = new HashSet<string>(
				fieldNames.Where(name => name != null)
				          .Select(name => name.Trim()),
				StringComparer.OrdinalIgnoreCase);

			return DatasetUtils.GetFields(table.Fields)
			                   .Where(f => f.Type == esriFieldType.esriFieldTypeString &&
			                               f.Editable)
			                   .Select(f => f.Name)
			                   .Where(name => ! excludedNames.Contains(name));
		}

		private int CheckField([NotNull] IReadOnlyRow row, int fieldIndex)
		{
			object value = row.get_Value(fieldIndex);

			if (value == null || value is DBNull)
			{
				return NoError;
			}

			string fieldName;
			string description;
			string stringValue;

			try
			{
				stringValue = string.Format(CultureInfo.InvariantCulture, "{0}", value);
			}
			catch (Exception e)
			{
				description = GetFormatErrorDescription(row, fieldIndex, value, e, out fieldName);
				return ReportFieldError(description,
				                        Codes[Code.StringFormattingError], fieldName, row);
			}

			if (_matchIsError)
			{
				if (! _regex.Match(stringValue).Success)
				{
					// no match -> correct
					return NoError;
				}

				// the pattern matches, and a match is considered an error
				description = GetMatchErrorDescription(row, fieldIndex, out fieldName);
				return ReportFieldError(description,
				                        Codes[Code.FieldValueMatchesRegularExpression],
				                        fieldName, row);
			}

			if (_regex.Match(stringValue).Success)
			{
				// the pattern matches, and a match is considered correct
				return NoError;
			}

			// no match -> error
			description = GetNoMatchErrorDescription(row, fieldIndex, out fieldName);

			return ReportFieldError(description,
			                        Codes[Code.FieldValueDoesNotMatchRegularExpression],
			                        fieldName, row);
		}

		private int ReportFieldError([NotNull] string description,
		                             [CanBeNull] IssueCode issueCode,
		                             [NotNull] string fieldName,
		                             [NotNull] IReadOnlyRow row)
		{
			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(row), TestUtils.GetShapeCopy(row),
				issueCode, fieldName);
		}

		[NotNull]
		private static string GetFormatErrorDescription([NotNull] IReadOnlyRow row, int fieldIndex,
		                                                object value, [NotNull] Exception e,
		                                                [NotNull] out string fieldName)
		{
			return string.Format("Error formatting value in field {0}: {1} ({2})",
			                     TestUtils.GetFieldDisplayName(row, fieldIndex, out fieldName),
			                     value, e.Message);
		}

		[NotNull]
		private string GetMatchErrorDescription([NotNull] IReadOnlyRow row, int fieldIndex,
		                                        [NotNull] out string fieldName)
		{
			string fieldDisplayName = TestUtils.GetFieldDisplayName(row, fieldIndex,
				out fieldName);

			if (StringUtils.IsNotEmpty(_patternDescription))
			{
				return string.Format("The value in field '{0}' matches the pattern for '{1}'",
				                     fieldDisplayName, _patternDescription);
			}

			return string.Format("The value in field '{0}' matches the pattern '{1}'",
			                     fieldDisplayName, _regex);
		}

		[NotNull]
		private string GetNoMatchErrorDescription([NotNull] IReadOnlyRow row, int fieldIndex,
		                                          [NotNull] out string fieldName)
		{
			string fieldDisplayName = TestUtils.GetFieldDisplayName(row, fieldIndex,
				out fieldName);

			if (StringUtils.IsNotEmpty(_patternDescription))
			{
				return string.Format(
					"The value in field '{0}' does not match the pattern for '{1}'",
					fieldDisplayName, _patternDescription);
			}

			return string.Format("The value in field '{0}' does not match the pattern '{1}'",
			                     fieldDisplayName, _regex);
		}
	}
}
