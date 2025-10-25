using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[SchemaTest]
	public class QaSchemaReservedFieldNamesDefinition : AlgorithmDefinition
	{
		public ITableSchemaDef Table { get; }
		public ITableSchemaDef ReservedNamesTable { get; }
		public IEnumerable<string> ReservedNames { get; }
		public string ReservedNameFieldName { get; }
		public string ReservedReasonFieldName { get; }
		public string ValidNameFieldName { get; }

		[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_0))]
		public QaSchemaReservedFieldNamesDefinition(
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_reservedNames))] [NotNull]
			IEnumerable<string> reservedNames)
			: base(table)
		{
			Table = table;
			ReservedNames = reservedNames;
		}

		[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_1))]
		public QaSchemaReservedFieldNamesDefinition(
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_reservedNamesString))] [NotNull]
			string reservedNamesString)
			: this(table, TestDefinitionUtils.GetTokens(reservedNamesString))
		{
			Table = table;
		}

		[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_2))]
		public QaSchemaReservedFieldNamesDefinition(
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_reservedNamesTable))] [NotNull]
			ITableSchemaDef reservedNamesTable,
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_reservedNameFieldName))] [NotNull]
			string reservedNameFieldName,
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_reservedReasonFieldName))] [CanBeNull]
			string reservedReasonFieldName,
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_validNameFieldName))] [CanBeNull]
			string validNameFieldName)
			: base(new[] { table, reservedNamesTable })
		{
			Table = table;
			ReservedNamesTable = reservedNamesTable;
			ReservedNameFieldName = reservedNameFieldName;
			ReservedReasonFieldName = reservedReasonFieldName;
			ValidNameFieldName = validNameFieldName;
		}
	}
}
