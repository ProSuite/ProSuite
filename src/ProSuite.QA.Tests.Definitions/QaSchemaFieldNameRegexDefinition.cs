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
	public class QaSchemaFieldNameRegexDefinition : AlgorithmDefinition
	{
		public ITableSchemaDef Table { get; }
		public string Pattern { get; }
		public bool MatchIsError { get; }
		public string PatternDescription { get; }

		[Doc(nameof(DocStrings.QaSchemaFieldNameRegex_0))]
		public QaSchemaFieldNameRegexDefinition(
			[Doc(nameof(DocStrings.QaSchemaFieldNameRegex_table))] [NotNull]
			ITableSchemaDef table,
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

			Table = table;
			Pattern = pattern;
			MatchIsError = matchIsError;
			PatternDescription = patternDescription;
		}
	}
}
