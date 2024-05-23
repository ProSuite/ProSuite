using System.Collections.Generic;
using System.Linq;
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
	public class QaSchemaFieldPropertiesFromTableDefinition : AlgorithmDefinition
	{
		public ITableSchemaDef Table { get; }
		public ITableSchemaDef FieldSpecificationsTable { get; }
		public bool MatchAliasName { get; }

		[Doc(nameof(DocStrings.QaSchemaFieldPropertiesFromTable_0))]
		public QaSchemaFieldPropertiesFromTableDefinition(
			[Doc(nameof(DocStrings.QaSchemaFieldPropertiesFromTable_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaSchemaFieldPropertiesFromTable_fieldSpecificationsTable))]
			[NotNull]
			ITableSchemaDef fieldSpecificationsTable,
			[Doc(nameof(DocStrings.QaSchemaFieldPropertiesFromTable_matchAliasName))]
			bool matchAliasName)
			: base(new [] {table, fieldSpecificationsTable })
		{
			Assert.ArgumentNotNull(fieldSpecificationsTable, nameof(fieldSpecificationsTable));

			Table = table;
			FieldSpecificationsTable = fieldSpecificationsTable;
			MatchAliasName = matchAliasName;
		}
	}
}
