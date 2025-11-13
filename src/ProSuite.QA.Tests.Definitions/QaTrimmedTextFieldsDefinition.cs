using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[AttributeTest]
	public class QaTrimmedTextFieldsDefinition : AlgorithmDefinition
	{
		public ITableSchemaDef Table { get; }
		public int AllowedWhiteSpaceOnlyCount { get; }
		public IEnumerable<string> TextFieldNames { get; }

		[Doc(nameof(DocStrings.QaTrimmedTextFields_0))]
		public QaTrimmedTextFieldsDefinition(
			[Doc(nameof(DocStrings.QaTrimmedTextFields_table))] [NotNull]
			ITableSchemaDef table)
			: this(table, 0, GetAllTextFieldNames(table), FieldListType.RelevantFields) { }

		[Doc(nameof(DocStrings.QaTrimmedTextFields_1))]
		public QaTrimmedTextFieldsDefinition(
			[Doc(nameof(DocStrings.QaTrimmedTextFields_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaTrimmedTextFields_allowedWhiteSpaceOnlyCount))]
			int
				allowedWhiteSpaceOnlyCount)
			: this(table, allowedWhiteSpaceOnlyCount,
			       GetAllTextFieldNames(table), FieldListType.RelevantFields) { }

		[Doc(nameof(DocStrings.QaTrimmedTextFields_2))]
		public QaTrimmedTextFieldsDefinition(
			[Doc(nameof(DocStrings.QaTrimmedTextFields_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaTrimmedTextFields_textFieldName))] [NotNull]
			string textFieldName)
			: this(table, 0, textFieldName) { }

		[Doc(nameof(DocStrings.QaTrimmedTextFields_3))]
		public QaTrimmedTextFieldsDefinition(
			[Doc(nameof(DocStrings.QaTrimmedTextFields_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaTrimmedTextFields_allowedWhiteSpaceOnlyCount))]
			int
				allowedWhiteSpaceOnlyCount,
			[Doc(nameof(DocStrings.QaTrimmedTextFields_textFieldName))] [NotNull]
			string textFieldName)
			: this(table, allowedWhiteSpaceOnlyCount,
			       new[] { textFieldName }, FieldListType.RelevantFields) { }

		[Doc(nameof(DocStrings.QaTrimmedTextFields_4))]
		public QaTrimmedTextFieldsDefinition(
			[Doc(nameof(DocStrings.QaTrimmedTextFields_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaTrimmedTextFields_allowedWhiteSpaceOnlyCount))]
			int
				allowedWhiteSpaceOnlyCount,
			[Doc(nameof(DocStrings.QaTrimmedTextFields_textFieldNames))] [NotNull]
			IEnumerable<string>
				textFieldNames)
			: this(table, allowedWhiteSpaceOnlyCount,
			       // ReSharper disable once IntroduceOptionalParameters.Global
			       textFieldNames, FieldListType.RelevantFields) { }

		[Doc(nameof(DocStrings.QaTrimmedTextFields_5))]
		public QaTrimmedTextFieldsDefinition(
			[Doc(nameof(DocStrings.QaTrimmedTextFields_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaTrimmedTextFields_allowedWhiteSpaceOnlyCount))]
			int
				allowedWhiteSpaceOnlyCount,
			[Doc(nameof(DocStrings.QaTrimmedTextFields_textFieldNamesString))] [CanBeNull]
			string
				textFieldNamesString,
			[Doc(nameof(DocStrings.QaTrimmedTextFields_fieldListType))]
			FieldListType fieldListType)
			: this(table, allowedWhiteSpaceOnlyCount,
			       TestDefinitionUtils.GetTokens(textFieldNamesString),
			       fieldListType) { }

		[Doc(nameof(DocStrings.QaTrimmedTextFields_6))]
		public QaTrimmedTextFieldsDefinition(
			[Doc(nameof(DocStrings.QaTrimmedTextFields_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaTrimmedTextFields_allowedWhiteSpaceOnlyCount))]
			int
				allowedWhiteSpaceOnlyCount,
			[Doc(nameof(DocStrings.QaTrimmedTextFields_textFieldNames))] [NotNull]
			IEnumerable<string>
				textFieldNames,
			[Doc(nameof(DocStrings.QaTrimmedTextFields_fieldListType))]
			FieldListType fieldListType)
			: base(table)
		{
			Assert.ArgumentNotNull(textFieldNames, nameof(textFieldNames));

			Table = table;
			AllowedWhiteSpaceOnlyCount = allowedWhiteSpaceOnlyCount;
			TextFieldNames = textFieldNames;
		}

		[NotNull]
		private static IEnumerable<string> GetAllTextFieldNames([NotNull] ITableSchemaDef table)
		{
			return table.TableFields
			            .Where(field => field.FieldType == FieldType.Text)
			            .Select(field => field.Name);
		}
	}
}
