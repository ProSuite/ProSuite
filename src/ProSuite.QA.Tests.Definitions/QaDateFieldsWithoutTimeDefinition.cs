using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[AttributeTest]
	public class QaDateFieldsWithoutTimeDefinition : AlgorithmDefinition
	{
		public string DateFieldNames { get; set; }
		public ITableSchemaDef Table { get; }

		[Doc(nameof(DocStrings.QaDateFieldsWithoutTime_0))]
		public QaDateFieldsWithoutTimeDefinition(
			[Doc(nameof(DocStrings.QaDateFieldsWithoutTime_table))] [NotNull]
			ITableSchemaDef table)
			: this(table, GetAllDateFieldNames(table)) { }

		private static string GetAllDateFieldNames(ITableSchemaDef table)
		{
			throw new NotImplementedException();
		}

		[Doc(nameof(DocStrings.QaDateFieldsWithoutTime_1))]
		public QaDateFieldsWithoutTimeDefinition(
			[Doc(nameof(DocStrings.QaDateFieldsWithoutTime_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaDateFieldsWithoutTime_dateFieldName))] [NotNull]
			string dateFieldName)
			: this(table, new[] { dateFieldName }) { }

		[Doc(nameof(DocStrings.QaDateFieldsWithoutTime_2))]
		public QaDateFieldsWithoutTimeDefinition(
			[Doc(nameof(DocStrings.QaDateFieldsWithoutTime_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaDateFieldsWithoutTime_dateFieldNames))] [NotNull]
			IEnumerable<string>
				dateFieldNames)
			: base(table)
		{
			Assert.ArgumentNotNull(dateFieldNames, nameof(dateFieldNames));

		}
	}
}
