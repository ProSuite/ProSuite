using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[SchemaTest]
	public class QaSchemaFieldDomainNameRegexDefinition : AlgorithmDefinition
	{
		public ITableSchemaDef Table { get; }
		public string Pattern { get; }
		public bool MatchIsError { get; }
		public string PatternDescription { get; }

		[Doc(nameof(DocStrings.QaSchemaFieldDomainNameRegex_0))]
		public QaSchemaFieldDomainNameRegexDefinition(
			[Doc(nameof(DocStrings.QaSchemaFieldDomainNameRegex_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaSchemaFieldDomainNameRegex_pattern))] [NotNull]
			string pattern,
			[Doc(nameof(DocStrings.QaSchemaFieldDomainNameRegex_matchIsError))]
			bool matchIsError,
			[Doc(nameof(DocStrings.QaSchemaFieldDomainNameRegex_patternDescription))] [CanBeNull]
			string patternDescription)
			: base(table)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNullOrEmpty(pattern, nameof(pattern));

			Table = table;
			Pattern = pattern;
			MatchIsError = matchIsError;
			PatternDescription = patternDescription;
		}
	}
}
