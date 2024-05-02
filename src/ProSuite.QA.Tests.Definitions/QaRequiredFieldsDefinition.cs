using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[AttributeTest]
	public class QaRequiredFieldsDefinition : AlgorithmDefinition
	{
		public ITableSchemaDef Table { get; }

		[CanBeNull]
		public IEnumerable<string> RequiredFieldNames { get; }

		public bool AllowEmptyString { get; }
		public bool AllowMissingFields { get; }

		[Doc(nameof(DocStrings.QaRequiredFields_0))]
		public QaRequiredFieldsDefinition(
				[Doc(nameof(DocStrings.QaRequiredFields_table))] [NotNull]
				ITableSchemaDef table,
				[Doc(nameof(DocStrings.QaRequiredFields_requiredFieldNames))] [NotNull]
				IEnumerable<string> requiredFieldNames)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(table, requiredFieldNames, false) { }

		[Doc(nameof(DocStrings.QaRequiredFields_0))]
		public QaRequiredFieldsDefinition(
				[Doc(nameof(DocStrings.QaRequiredFields_table))] [NotNull]
				ITableSchemaDef table,
				[Doc(nameof(DocStrings.QaRequiredFields_requiredFieldNames))] [NotNull]
				IEnumerable<string> requiredFieldNames,
				[Doc(nameof(DocStrings.QaRequiredFields_allowEmptyStrings))]
				bool allowEmptyStrings)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(table, requiredFieldNames, allowEmptyStrings, false) { }

		/// <summary>
		/// Main constructor. 
		/// </summary>
		/// <param name="table"></param>
		/// <param name="requiredFieldNames">The list of required field names.
		/// Null means all editable non-null lists shall be required.</param>
		/// <param name="allowEmptyStrings"></param>
		/// <param name="allowMissingFields"></param>
		[Doc(nameof(DocStrings.QaRequiredFields_0))]
		public QaRequiredFieldsDefinition(
			[Doc(nameof(DocStrings.QaRequiredFields_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaRequiredFields_requiredFieldNames))] [CanBeNull]
			IEnumerable<string> requiredFieldNames,
			[Doc(nameof(DocStrings.QaRequiredFields_allowEmptyStrings))]
			bool allowEmptyStrings,
			[Doc(nameof(DocStrings.QaRequiredFields_allowMissingFields))]
			bool allowMissingFields)
			: base(table)
		{
			Table = table;
			RequiredFieldNames = requiredFieldNames;
			AllowEmptyString = allowEmptyStrings;
			AllowMissingFields = allowMissingFields;
		}

		[Doc(nameof(DocStrings.QaRequiredFields_0))]
		public QaRequiredFieldsDefinition(
			[Doc(nameof(DocStrings.QaRequiredFields_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaRequiredFields_requiredFieldNamesString))] [NotNull]
			string
				requiredFieldNamesString,
			[Doc(nameof(DocStrings.QaRequiredFields_allowEmptyStrings))]
			bool allowEmptyStrings,
			[Doc(nameof(DocStrings.QaRequiredFields_allowMissingFields))]
			bool allowMissingFields)
			: this(table, TestDefinitionUtils.GetTokens(requiredFieldNamesString),
			       allowEmptyStrings, allowMissingFields) { }

		[Doc(nameof(DocStrings.QaRequiredFields_0))]
		public QaRequiredFieldsDefinition(
			[Doc(nameof(DocStrings.QaRequiredFields_table))] [NotNull]
			ITableSchemaDef table,
			[Doc(nameof(DocStrings.QaRequiredFields_allowEmptyStrings))]
			bool allowEmptyStrings)
			: this(table, (IEnumerable<string>) null,
			       allowEmptyStrings, false)
		{
			// NOTE: The null enumerable for the requiredFieldNames means that all non-null editable fields are required.
			//       We cannot move the GetAllEditableFieldNames method here because it requires the Editable/Nullable
			//       properties on the ITableField.
		}
	}
}
