using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[SchemaTest]
	public class QaSchemaFieldNamesDefinition : AlgorithmDefinition
	{
		public ITableSchemaDef Table { get; }
		public int MaximumLength { get; }
		public ExpectedCase ExpectedCase { get; }
		public int UniqueSubstringLength { get; }

		[Doc(nameof(DocStrings.QaSchemaFieldNames_0))]
		public QaSchemaFieldNamesDefinition(
			[Doc(nameof(DocStrings.QaSchemaFieldNames_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaSchemaFieldNames_maximumLength))]
			int maximumLength,
			[Doc(nameof(DocStrings.QaSchemaFieldNames_expectedCase))]
			ExpectedCase expectedCase,
			[Doc(nameof(DocStrings.QaSchemaFieldNames_uniqueSubstringLength))]
			int uniqueSubstringLength)
			: base(table)
		{
			Table = table;
			MaximumLength = maximumLength;
			ExpectedCase = expectedCase;
			UniqueSubstringLength = uniqueSubstringLength;
		}
	}
}
