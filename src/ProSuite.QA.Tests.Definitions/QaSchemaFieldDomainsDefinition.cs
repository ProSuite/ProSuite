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
	public class QaSchemaFieldDomainsDefinition : AlgorithmDefinition
	{
		public ITableSchemaDef Table { get; }

		[Doc(nameof(DocStrings.QaSchemaFieldDomains_0))]
		public QaSchemaFieldDomainsDefinition(
			[Doc(nameof(DocStrings.QaSchemaFieldDomains_table))] [NotNull]
			ITableSchemaDef table)
			: base(table)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			Table = table;
		}
	}
}
