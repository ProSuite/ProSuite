using System.Text.RegularExpressions;
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
	public class QaSchemaFieldDomainNameRegex : QaSchemaTestBase
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

			public Code() : base("DomainNameRegex") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaSchemaFieldDomainNameRegex_0))]
		public QaSchemaFieldDomainNameRegex(
			[Doc(nameof(DocStrings.QaSchemaFieldDomainNameRegex_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaSchemaFieldDomainNameRegex_pattern))] [NotNull]
			string pattern,
			[Doc(nameof(DocStrings.QaSchemaFieldDomainNameRegex_matchIsError))]
			bool matchIsError,
			[Doc(nameof(DocStrings.QaSchemaFieldDomainNameRegex_patternDescription))] [CanBeNull]
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
		public QaSchemaFieldDomainNameRegex(
			[NotNull] QaSchemaFieldDomainNameRegexDefinition definition)
			: this((IReadOnlyTable) definition.Table, definition.Pattern, definition.MatchIsError,
			       definition.PatternDescription) { }

		public override int Execute()
		{
			int errorCount = 0;

			foreach (DomainUsage domainUsage in SchemaTestUtils.GetDomainUsages(_table))
			{
				errorCount += CheckDomain(domainUsage);
			}

			return errorCount;
		}

		private int CheckDomain([NotNull] DomainUsage domainUsage)
		{
			Assert.ArgumentNotNull(domainUsage, nameof(domainUsage));

			string domainName = domainUsage.DomainName;

			const int noError = 0;

			if (_matchIsError)
			{
				if (! _regex.Match(domainName).Success)
				{
					// no match -> correct
					return noError;
				}

				// the pattern matches, and a match is considered an error
				return ReportSchemaPropertyError(
					Codes[Code.MatchingErrorPattern], domainName,
					GetMatchErrorDescription(domainName));
			}

			if (_regex.Match(domainName).Success)
			{
				// the pattern matches, and a match is considered correct
				return noError;
			}

			// no match -> error
			return ReportSchemaPropertyError(
				Codes[Code.NotMatchingExpectedPattern], domainName,
				GetNoMatchErrorDescription(domainName));
		}

		[NotNull]
		private string GetMatchErrorDescription([NotNull] string domainName)
		{
			if (StringUtils.IsNotEmpty(_patternDescription))
			{
				return string.Format("The domain name '{0}' matches the pattern for '{1}'",
				                     domainName, _patternDescription);
			}

			return string.Format("The domain name '{0}' matches the pattern '{1}'",
			                     domainName, _pattern);
		}

		[NotNull]
		private string GetNoMatchErrorDescription([NotNull] string fieldName)
		{
			if (StringUtils.IsNotEmpty(_patternDescription))
			{
				return string.Format("The domain name '{0}' does not match the pattern for '{1}'",
				                     fieldName, _patternDescription);
			}

			return string.Format("The domain name '{0}' does not match the pattern '{1}'",
			                     fieldName, _pattern);
		}
	}
}
