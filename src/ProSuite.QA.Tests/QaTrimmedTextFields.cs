using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.ParameterTypes;
using ProSuite.QA.Tests.Properties;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[AttributeTest]
	public class QaTrimmedTextFields : ContainerTest
	{
		private readonly List<int> _textFieldIndices;
		private readonly int _allowedWhiteSpaceOnlyCount;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string TrailingBlanks = "TrailingBlanks";
			public const string LeadingBlanks = "LeadingBlanks";
			public const string LeadingAndTrailingBlanks = "LeadingAndTrailingBlanks";
			public const string ErrorReadingString = "ErrorReadingString";
			public const string OnlyBlanks = "OnlyBlanks";

			public Code() : base("TrimmedTextFields") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaTrimmedTextFields_0))]
		public QaTrimmedTextFields(
			[Doc(nameof(DocStrings.QaTrimmedTextFields_table))] [NotNull]
			IReadOnlyTable table)
			: this(table, 0, GetAllTextFieldNames(table), FieldListType.RelevantFields) { }

		[Doc(nameof(DocStrings.QaTrimmedTextFields_1))]
		public QaTrimmedTextFields(
			[Doc(nameof(DocStrings.QaTrimmedTextFields_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaTrimmedTextFields_allowedWhiteSpaceOnlyCount))]
			int
				allowedWhiteSpaceOnlyCount)
			: this(table, allowedWhiteSpaceOnlyCount,
			       GetAllTextFieldNames(table), FieldListType.RelevantFields) { }

		[Doc(nameof(DocStrings.QaTrimmedTextFields_2))]
		public QaTrimmedTextFields(
			[Doc(nameof(DocStrings.QaTrimmedTextFields_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaTrimmedTextFields_textFieldName))] [NotNull]
			string textFieldName)
			: this(table, 0, textFieldName) { }

		[Doc(nameof(DocStrings.QaTrimmedTextFields_3))]
		public QaTrimmedTextFields(
			[Doc(nameof(DocStrings.QaTrimmedTextFields_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaTrimmedTextFields_allowedWhiteSpaceOnlyCount))]
			int
				allowedWhiteSpaceOnlyCount,
			[Doc(nameof(DocStrings.QaTrimmedTextFields_textFieldName))] [NotNull]
			string textFieldName)
			: this(table, allowedWhiteSpaceOnlyCount,
			       new[] {textFieldName}, FieldListType.RelevantFields) { }

		[Doc(nameof(DocStrings.QaTrimmedTextFields_4))]
		public QaTrimmedTextFields(
			[Doc(nameof(DocStrings.QaTrimmedTextFields_table))] [NotNull]
			IReadOnlyTable table,
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
		public QaTrimmedTextFields(
			[Doc(nameof(DocStrings.QaTrimmedTextFields_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaTrimmedTextFields_allowedWhiteSpaceOnlyCount))]
			int
				allowedWhiteSpaceOnlyCount,
			[Doc(nameof(DocStrings.QaTrimmedTextFields_textFieldNamesString))] [CanBeNull]
			string
				textFieldNamesString,
			[Doc(nameof(DocStrings.QaTrimmedTextFields_fieldListType))]
			FieldListType fieldListType)
			: this(table, allowedWhiteSpaceOnlyCount,
			       TestUtils.GetTokens(textFieldNamesString),
			       fieldListType) { }

		[Doc(nameof(DocStrings.QaTrimmedTextFields_6))]
		public QaTrimmedTextFields(
			[Doc(nameof(DocStrings.QaTrimmedTextFields_table))] [NotNull]
			IReadOnlyTable table,
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

			_allowedWhiteSpaceOnlyCount = allowedWhiteSpaceOnlyCount;

			_textFieldIndices = new List<int>(
				GetFieldIndices(table, textFieldNames, fieldListType));
		}

		[InternallyUsedTest]
		public QaTrimmedTextFields([NotNull] QaTrimmedTextFieldsDefinition definition)
			: this((IReadOnlyTable) definition.Table, definition.AllowedWhiteSpaceOnlyCount,
			       definition.TextFieldNames) { }

		[NotNull]
		private static IEnumerable<int> GetFieldIndices(
			[NotNull] IReadOnlyTable table,
			[NotNull] IEnumerable<string> textFieldNames,
			FieldListType fieldListType)
		{
			switch (fieldListType)
			{
				case FieldListType.IgnoredFields:
					return GetFieldIndicesFromIgnoredFieldNames(table, textFieldNames);

				case FieldListType.RelevantFields:
					return GetFieldIndicesFromRelevantFieldNames(table, textFieldNames);

				default:
					throw new ArgumentOutOfRangeException(
						nameof(fieldListType), fieldListType,
						string.Format("Unsupported field list type: {0}", fieldListType));
			}
		}

		[NotNull]
		private static IEnumerable<int> GetFieldIndicesFromRelevantFieldNames(
			[NotNull] IReadOnlyTable table,
			[NotNull] IEnumerable<string> textFieldNames)
		{
			IFields fields = table.Fields;

			foreach (string fieldName in textFieldNames)
			{
				Assert.NotNull(fieldName, "field name is null");

				int index = fields.FindField(fieldName);
				if (index < 0)
				{
					throw new ArgumentException(
						string.Format("Field not found in table {0}: {1}",
						              table.Name,
						              fieldName), nameof(textFieldNames));
				}

				if (fields.Field[index].Type != esriFieldType.esriFieldTypeString)
				{
					throw new ArgumentException(
						string.Format("Field {0} in table {1} is not a text field",
						              fieldName,
						              table.Name), nameof(textFieldNames));
				}

				yield return index;
			}
		}

		[NotNull]
		private static IEnumerable<int> GetFieldIndicesFromIgnoredFieldNames(
			[NotNull] IReadOnlyTable table,
			[NotNull] IEnumerable<string> ignoredFieldNames)
		{
			var ignoredFields = new SimpleSet<string>(
				ignoredFieldNames, StringComparer.InvariantCultureIgnoreCase);

			int fieldIndex = 0;
			foreach (IField field in DatasetUtils.GetFields(table.Fields))
			{
				if (field.Type == esriFieldType.esriFieldTypeString &&
				    ! ignoredFields.Contains(field.Name))
				{
					yield return fieldIndex;
				}

				fieldIndex++;
			}
		}

		public override bool IsQueriedTable(int tableIndex)
		{
			return false;
		}

		public override bool IsGeometryUsedTable(int tableIndex)
		{
			return AreaOfInterest != null;
		}

		public override bool RetestRowsPerIntersectedTile(int tableIndex)
		{
			return false;
		}

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			return _textFieldIndices.Sum(fieldIndex => Checkfield(row, fieldIndex));
		}

		private int Checkfield([NotNull] IReadOnlyRow row, int fieldIndex)
		{
			object value = row.get_Value(fieldIndex);

			if (value == null || value is DBNull)
			{
				return NoError;
			}

			string stringValue;
			string description;
			string fieldName;

			try
			{
				stringValue = (string) value;
			}
			catch (Exception e)
			{
				fieldName = row.Table.Fields.Field[fieldIndex].Name;
				description =
					string.Format(LocalizableStrings.QaTrimmedTextFields_InvalidStringValue,
					              fieldName, value, e.Message);

				return ReportError(
					description, InvolvedRowUtils.GetInvolvedRows(row), TestUtils.GetShapeCopy(row),
					Codes[Code.ErrorReadingString], fieldName);
			}

			// check for leading or trailing blanks
			string trimmed = stringValue.Trim();
			if (Equals(stringValue, trimmed))
			{
				// trimmed value equal to field value --> no leading/trailing blanks
				return NoError;
			}

			string fieldDisplayName = TestUtils.GetFieldDisplayName(row, fieldIndex,
				out fieldName);

			int blankCount = stringValue.Length - trimmed.Length;

			string blankPluralSuffix = blankCount == 1
				                           ? string.Empty
				                           : "s";

			IssueCode issueCode;
			if (trimmed.Length == 0)
			{
				// the text field contains only blanks
				if (_allowedWhiteSpaceOnlyCount == -1 ||
				    blankCount <= _allowedWhiteSpaceOnlyCount)
				{
					return NoError;
				}

				issueCode = Codes[Code.OnlyBlanks];
				description = string.Format(
					LocalizableStrings.QaTrimmedTextFields_ValueContainsOnlyBlanks,
					fieldDisplayName, blankCount, blankPluralSuffix);
			}
			else if (stringValue.StartsWith(trimmed))
			{
				issueCode = Codes[Code.TrailingBlanks];
				description = string.Format(
					LocalizableStrings.QaTrimmedTextFields_ValueHasTrailingBlank,
					fieldDisplayName, blankCount, blankPluralSuffix);
			}
			else if (stringValue.EndsWith(trimmed))
			{
				issueCode = Codes[Code.LeadingBlanks];
				description = string.Format(
					LocalizableStrings.QaTrimmedTextFields_ValueHasLeadingBlank,
					fieldDisplayName, blankCount, blankPluralSuffix);
			}
			else
			{
				issueCode = Codes[Code.LeadingAndTrailingBlanks];
				description = string.Format(
					LocalizableStrings.QaTrimmedTextFields_ValueHasLeadingAndTrailingBlanks,
					fieldDisplayName, blankCount);
			}

			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(row), TestUtils.GetShapeCopy(row),
				issueCode, fieldName);
		}

		[NotNull]
		private static IEnumerable<string> GetAllTextFieldNames([NotNull] IReadOnlyTable table)
		{
			return DatasetUtils.GetFields(table.Fields)
			                   .Where(field => field.Type == esriFieldType.esriFieldTypeString)
			                   .Select(field => field.Name);
		}
	}
}
