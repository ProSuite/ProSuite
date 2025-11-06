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
	public class QaEmptyNotNullTextFieldsDefinition : AlgorithmDefinition
	{
		public ITableSchemaDef Table { get; }
		public string[] NotNullTextFields { get; }

		[Doc(nameof(DocStrings.QaEmptyNotNullTextFields_0))]
		public QaEmptyNotNullTextFieldsDefinition(
			[Doc(nameof(DocStrings.QaEmptyNotNullTextFields_table))] [NotNull]
			ITableSchemaDef table)
			: base(table)
		{
			Table = table;
		}

		[Doc(nameof(DocStrings.QaEmptyNotNullTextFields_1))]
		public QaEmptyNotNullTextFieldsDefinition(
			[Doc(nameof(DocStrings.QaEmptyNotNullTextFields_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaEmptyNotNullTextFields_notNullTextFields))] [NotNull]
			string[] notNullTextFields)
			: base(table)
		{
			Assert.ArgumentNotNull(notNullTextFields, nameof(notNullTextFields));

			Table = table;
			NotNullTextFields = notNullTextFields;
		}
	}
}
