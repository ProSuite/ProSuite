using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.Schema;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	[CLSCompliant(false)]
	[UsedImplicitly]
	[SchemaTest]
	public class QaSchemaReservedFieldNames : QaSchemaReservedFieldNamesBase
	{
		[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_0))]
		public QaSchemaReservedFieldNames(
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_table))] [NotNull]
			ITable table,
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_reservedNames))] [NotNull]
			IEnumerable<string> reservedNames)
			: base(table, reservedNames) { }

		[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_1))]
		public QaSchemaReservedFieldNames(
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_table))] [NotNull]
			ITable table,
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_reservedNamesString))] [NotNull]
			string reservedNamesString)
			: base(table, reservedNamesString) { }

		[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_2))]
		public QaSchemaReservedFieldNames(
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_table))] [NotNull]
			ITable table,
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_reservedNamesTable))] [NotNull]
			ITable reservedNamesTable,
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_reservedNameFieldName))] [NotNull]
			string reservedNameFieldName,
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_reservedReasonFieldName))] [CanBeNull]
			string reservedReasonFieldName,
			[Doc(nameof(DocStrings.QaSchemaReservedFieldNames_validNameFieldName))] [CanBeNull]
			string validNameFieldName)
			: base(table, reservedNamesTable, reservedNameFieldName, reservedReasonFieldName,
			       validNameFieldName) { }
	}
}
