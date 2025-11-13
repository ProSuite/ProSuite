using System.Collections.Generic;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.Schema;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[SchemaTest]
	public class QaSchemaReservedFieldNames : QaSchemaReservedFieldNamesBase
	{
		[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_0))]
		public QaSchemaReservedFieldNames(
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_reservedNames))] [NotNull]
			IEnumerable<string> reservedNames)
			: base(table, reservedNames) { }

		[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_1))]
		public QaSchemaReservedFieldNames(
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_reservedNamesString))] [NotNull]
			string reservedNamesString)
			: base(table, reservedNamesString) { }

		[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_2))]
		public QaSchemaReservedFieldNames(
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_reservedNamesTable))] [NotNull]
			IReadOnlyTable reservedNamesTable,
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_reservedNameFieldName))] [NotNull]
			string reservedNameFieldName,
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_reservedReasonFieldName))] [CanBeNull]
			string reservedReasonFieldName,
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_validNameFieldName))] [CanBeNull]
			string validNameFieldName)
			: base(table, reservedNamesTable, reservedNameFieldName, reservedReasonFieldName,
			       validNameFieldName) { }

		[InternallyUsedTest]
		public QaSchemaReservedFieldNames(
			[NotNull] QaSchemaReservedFieldNamesDefinition definition)
			: base(definition) { }
	}
}
