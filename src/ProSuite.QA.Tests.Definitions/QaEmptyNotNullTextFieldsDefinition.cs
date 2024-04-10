using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[AttributeTest]
	public class QaEmptyNotNullTextFieldsDefinition : AlgorithmDefinition
	{
		private readonly IList<int> _notNullTextFieldIndices;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		public ITableSchemaDef Table { get; }
		public string[] NotNullTextFields { get; }

		#endregion

		#region Constructors

		[Doc(nameof(DocStrings.QaEmptyNotNullTextFields_0))]
		public QaEmptyNotNullTextFieldsDefinition(
			[Doc(nameof(DocStrings.QaEmptyNotNullTextFields_table))] [NotNull]
			ITableSchemaDef table)
			: this(table, GetNotNullTextFields(table)) { }

		private static string[] GetNotNullTextFields(ITableSchemaDef table)
		{
			throw new NotImplementedException();
		}

		[Doc(nameof(DocStrings.QaEmptyNotNullTextFields_1))]
		public QaEmptyNotNullTextFieldsDefinition(
			[Doc(nameof(DocStrings.QaEmptyNotNullTextFields_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaEmptyNotNullTextFields_notNullTextFields))] [NotNull]
			string[]
				notNullTextFields)
			: base(table)
		{
			Assert.ArgumentNotNull(notNullTextFields, nameof(notNullTextFields));

			var fieldIndices = new List<int>(notNullTextFields.Length);
			foreach (string notNullTextField in notNullTextFields)
			{
				int fieldIndex = table.FindField(notNullTextField);
				Assert.True(fieldIndex >= 0, "field '{0}' not found in table '{1}'",
							notNullTextField, table.Name);

				fieldIndices.Add(fieldIndex);
			}

			Table = table;
			NotNullTextFields = notNullTextFields;

		}
	}
}
#endregion
