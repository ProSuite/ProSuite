using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[SchemaTest]
	public class QaSchemaReservedFieldNamePropertiesDefinition : AlgorithmDefinition
	{
		public ITableSchemaDef Table { get; }
		public ITableSchemaDef ReservedNamesTable { get; }
		public string ReservedNameFieldName { get; }
		public string ReservedReasonFieldName { get; }
		public string ValidNameFieldName { get; }

		public ITableSchemaDef FieldSpecificationsTable { get; }

		[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_2))]
		public QaSchemaReservedFieldNamePropertiesDefinition(
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_reservedNamesTable))] [NotNull]
			ITableSchemaDef reservedNamesTable,
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_reservedNameFieldName))] [NotNull]
			string reservedNameFieldName,
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_reservedReasonFieldName))] [CanBeNull]
			string reservedReasonFieldName,
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_validNameFieldName))] [CanBeNull]
			string validNameFieldName,
			[CanBeNull] ITableSchemaDef fieldSpecificationsTable)
			: base(new[]{table,reservedNamesTable})
		{
			Table = table;
			ReservedNamesTable = reservedNamesTable;
			ReservedNameFieldName = reservedNameFieldName;
			ReservedReasonFieldName = reservedReasonFieldName;
			ValidNameFieldName = validNameFieldName;
			FieldSpecificationsTable = fieldSpecificationsTable;
		}
	}
}
