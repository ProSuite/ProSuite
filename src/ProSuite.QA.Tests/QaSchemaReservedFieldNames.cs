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
		[Doc("QaSchemaReservedFieldNames_0")]
		public QaSchemaReservedFieldNames(
			[Doc("QaSchemaReservedFieldNames_table")] [NotNull]
			ITable table,
			[Doc("QaSchemaReservedFieldNames_reservedNames")] [NotNull]
			IEnumerable<string> reservedNames)
			: base(table, reservedNames) { }

		[Doc("QaSchemaReservedFieldNames_1")]
		public QaSchemaReservedFieldNames(
			[Doc("QaSchemaReservedFieldNames_table")] [NotNull]
			ITable table,
			[Doc("QaSchemaReservedFieldNames_reservedNamesString")] [NotNull]
			string reservedNamesString)
			: base(table, reservedNamesString) { }

		[Doc("QaSchemaReservedFieldNames_2")]
		public QaSchemaReservedFieldNames(
			[Doc("QaSchemaReservedFieldNames_table")] [NotNull]
			ITable table,
			[Doc("QaSchemaReservedFieldNames_reservedNamesTable")] [NotNull]
			ITable reservedNamesTable,
			[Doc("QaSchemaReservedFieldNames_reservedNameFieldName")] [NotNull]
			string reservedNameFieldName,
			[Doc("QaSchemaReservedFieldNames_reservedReasonFieldName")] [CanBeNull]
			string reservedReasonFieldName,
			[Doc("QaSchemaReservedFieldNames_validNameFieldName")] [CanBeNull]
			string validNameFieldName)
			: base(table, reservedNamesTable, reservedNameFieldName, reservedReasonFieldName,
			       validNameFieldName) { }
	}
}
