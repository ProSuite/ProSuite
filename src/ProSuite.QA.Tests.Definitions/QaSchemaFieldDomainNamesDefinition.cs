using ProSuite.Commons.Essentials.Assertions;
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
	public class QaSchemaFieldDomainNamesDefinition : AlgorithmDefinition
	{
		public ITableSchemaDef Table { get; }
		public string ExpectedPrefix { get; }
		public int MaximumLength { get; }
		public bool MustContainFieldName { get; }

		public ExpectedCase ExpectedCase { get; }

		[Doc(nameof(DocStrings.QaSchemaFieldDomainNames_0))]
		public QaSchemaFieldDomainNamesDefinition(
			[Doc(nameof(DocStrings.QaSchemaFieldDomainNames_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaSchemaFieldDomainNames_expectedPrefix))] [CanBeNull]
			string expectedPrefix,
			[Doc(nameof(DocStrings.QaSchemaFieldDomainNames_maximumLength))]
			int maximumLength,
			[Doc(nameof(DocStrings.QaSchemaFieldDomainNames_mustContainFieldName))]
			bool mustContainFieldName,
			[Doc(nameof(DocStrings.QaSchemaFieldDomainNames_expectedCase))]
			ExpectedCase expectedCase)
			: base(table)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			Table = table;
			ExpectedPrefix = expectedPrefix;
			MaximumLength = maximumLength;
			MustContainFieldName = mustContainFieldName;
			ExpectedCase = expectedCase;
		}
	}
}
