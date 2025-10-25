using System.Text.RegularExpressions;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.Schema;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[SchemaTest]
	public class QaSchemaFieldNameRegex : QaSchemaTestBase
	{
		private readonly IReadOnlyTable _table;
		private readonly string _pattern;
		private readonly bool _matchIsError;
		private readonly string _patternDescription;
		private readonly Regex _regex;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string MatchingErrorPattern = "MatchingErrorPattern";
			public const string NotMatchingExpectedPattern = "NotMatchingExpectedPattern";

			public Code() : base("FieldNameRegex") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaSchemaFieldNameRegex_0))]
		public QaSchemaFieldNameRegex(
			[Doc(nameof(DocStrings.QaSchemaFieldNameRegex_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaSchemaFieldNameRegex_pattern))] [NotNull]
			string pattern,
			[Doc(nameof(DocStrings.QaSchemaFieldNameRegex_matchIsError))]
			bool matchIsError,
			[Doc(nameof(DocStrings.QaSchemaFieldNameRegex_patternDescription))] [CanBeNull]
			string
				patternDescription)
			: base(table)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNullOrEmpty(pattern, nameof(pattern));

			_table = table;
			_pattern = pattern;
			_matchIsError = matchIsError;
			_patternDescription = patternDescription;

			_regex = new Regex(pattern, RegexOptions.Compiled);
		}

		[InternallyUsedTest]
		public QaSchemaFieldNameRegex([NotNull] QaSchemaFieldNameRegexDefinition definition)
			: this((IReadOnlyTable) definition.Table, definition.Pattern,
			       definition.MatchIsError, definition.PatternDescription) { }

		public override int Execute()
		{
			int errorCount = 0;

			foreach (IField field in DatasetUtils.GetFields(_table.Fields))
			{
				// TODO ignore the system fields?

				errorCount += CheckField(field);
			}

			return errorCount;
		}

		private int CheckField([NotNull] IField field)
		{
			Assert.ArgumentNotNull(field, nameof(field));

			string fieldName = field.Name;
			const int noError = 0;

			if (_matchIsError)
			{
				if (! _regex.Match(fieldName).Success)
				{
					// no match -> correct
					return noError;
				}

				// the pattern matches, and a match is considered an error
				return ReportSchemaPropertyError(
					Codes[Code.MatchingErrorPattern], fieldName,
					GetMatchErrorDescription(fieldName));
			}

			if (_regex.Match(fieldName).Success)
			{
				// the pattern matches, and a match is considered correct
				return noError;
			}

			// no match -> error
			return ReportSchemaPropertyError(
				Codes[Code.NotMatchingExpectedPattern], fieldName,
				GetNoMatchErrorDescription(fieldName));
		}

		[NotNull]
		private string GetMatchErrorDescription([NotNull] string fieldName)
		{
			if (StringUtils.IsNotEmpty(_patternDescription))
			{
				return string.Format("The field name '{0}' matches the pattern for '{1}'",
				                     fieldName, _patternDescription);
			}

			return string.Format("The field name '{0}' matches the pattern '{1}'",
			                     fieldName, _pattern);
		}

		[NotNull]
		private string GetNoMatchErrorDescription([NotNull] string fieldName)
		{
			if (StringUtils.IsNotEmpty(_patternDescription))
			{
				return string.Format("The field name '{0}' does not match the pattern for '{1}'",
				                     fieldName, _patternDescription);
			}

			return string.Format("The field name '{0}' does not match the pattern '{1}'",
			                     fieldName, _pattern);
		}
	}
}
