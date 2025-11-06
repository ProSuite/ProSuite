using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.ParameterTypes;
using ProSuite.QA.Tests.Properties;
using ProSuite.QA.Tests.Schema;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[SchemaTest]
	public class QaSchemaFieldNames : QaSchemaTestBase
	{
		private readonly IReadOnlyTable _table;
		private readonly int _maximumLength;
		private readonly ExpectedCase _expectedCase;
		private readonly int _uniqueSubstringLength;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string Case_NotAllUpper = "Case.NotAllUpper";
			public const string Case_NotAllLower = "Case.NotAllLower";
			public const string Case_NotMixed = "Case.NotMixed";
			public const string Case_AllLower = "Case.AllLower";
			public const string Case_AllUpper = "Case.AllUpper";
			public const string SubstringNotUnique = "SubstringNotUnique";
			public const string TextLength_TooShort = "TextLength.TooShort";
			public const string TextLength_TooLong = "TextLength.TooLong";

			public Code() : base("FieldNames") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaSchemaFieldNames_0))]
		public QaSchemaFieldNames(
			[Doc(nameof(DocStrings.QaSchemaFieldNames_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaSchemaFieldNames_maximumLength))]
			int maximumLength,
			[Doc(nameof(DocStrings.QaSchemaFieldNames_expectedCase))]
			ExpectedCase expectedCase,
			[Doc(nameof(DocStrings.QaSchemaFieldNames_uniqueSubstringLength))]
			int uniqueSubstringLength)
			: base(table)
		{
			_table = table;

			_maximumLength = maximumLength;
			_expectedCase = expectedCase;
			_uniqueSubstringLength = uniqueSubstringLength;
		}

		[InternallyUsedTest]
		public QaSchemaFieldNames([NotNull] QaSchemaFieldNamesDefinition definition)
			: this((IReadOnlyTable) definition.Table, definition.MaximumLength,
			       definition.ExpectedCase, definition.UniqueSubstringLength) { }

		#region Overrides of QaSchemaTestBase

		public override int Execute()
		{
			IList<IField> fields = DatasetUtils.GetFields(_table.Fields);

			int errorCount = fields.Sum(field => ValidateFieldName(field));

			errorCount += ValidateUniqueSubstrings(fields, _uniqueSubstringLength);

			return errorCount;
		}

		#endregion

		[CanBeNull]
		private static IssueCode GetIssueCode(TextLengthIssue lengthIssue)
		{
			switch (lengthIssue)
			{
				case TextLengthIssue.LessThanMinimum:
					return Codes[Code.TextLength_TooShort];

				case TextLengthIssue.GreaterThanMaximum:
					return Codes[Code.TextLength_TooLong];

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		[CanBeNull]
		private static IssueCode GetIssueCode(ExpectedCase expectedCase)
		{
			switch (expectedCase)
			{
				case ExpectedCase.Any:
					return null;

				case ExpectedCase.AllUpper:
					return Codes[Code.Case_NotAllUpper];

				case ExpectedCase.AllLower:
					return Codes[Code.Case_NotAllLower];

				case ExpectedCase.Mixed:
					return Codes[Code.Case_NotMixed];

				case ExpectedCase.NotAllUpper:
					return Codes[Code.Case_AllUpper];

				case ExpectedCase.NotAllLower:
					return Codes[Code.Case_AllLower];

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private int ValidateUniqueSubstrings([NotNull] ICollection<IField> fields,
		                                     int uniqueSubstringLength)
		{
			Assert.ArgumentNotNull(fields, nameof(fields));

			const int noError = 0;
			if (uniqueSubstringLength <= 0)
			{
				// disable check for unique substring
				return noError;
			}

			int errorCount = 0;

			foreach (KeyValuePair<string, List<IField>> pair in
			         GetFieldNameSubstrings(fields, uniqueSubstringLength))
			{
				List<IField> fieldsForSubstring = pair.Value;

				if (fieldsForSubstring.Count > 1)
				{
					errorCount += ReportSchemaError(
						Codes[Code.SubstringNotUnique],
						LocalizableStrings.QaSchemaFieldNames_FieldNamesSubstringNotUnique,
						uniqueSubstringLength,
						ConcatenateFieldNames(fieldsForSubstring));
				}
			}

			return errorCount;
		}

		[NotNull]
		private static string ConcatenateFieldNames([NotNull] IEnumerable<IField> fields)
		{
			Assert.ArgumentNotNull(fields, nameof(fields));

			var sb = new StringBuilder();

			foreach (IField field in fields)
			{
				if (sb.Length == 0)
				{
					sb.Append(field.Name);
				}
				else
				{
					sb.AppendFormat(", {0}", field.Name);
				}
			}

			return sb.ToString();
		}

		[NotNull]
		private static Dictionary<string, List<IField>> GetFieldNameSubstrings(
			[NotNull] ICollection<IField> fields, int substringLength)
		{
			Assert.ArgumentNotNull(fields, nameof(fields));

			var result = new Dictionary<string, List<IField>>(
				fields.Count, StringComparer.OrdinalIgnoreCase);

			foreach (IField field in fields)
			{
				string fieldName = field.Name;

				string fieldNameSubstring = fieldName.Length <= substringLength
					                            ? fieldName
					                            : fieldName.Substring(0, substringLength);

				List<IField> fieldsForSubstring;
				if (! result.TryGetValue(fieldNameSubstring, out fieldsForSubstring))
				{
					fieldsForSubstring = new List<IField>();
					result.Add(fieldNameSubstring, fieldsForSubstring);
				}

				fieldsForSubstring.Add(field);
			}

			return result;
		}

		private int ValidateFieldName([NotNull] IField field)
		{
			Assert.ArgumentNotNull(field, nameof(field));

			const int noError = 0;

			if (HasPredefinedName(field))
			{
				return noError;
			}

			string fieldName = field.Name;

			const string fieldMessageFormat = "Field '{0}': {1}";
			int errorCount = 0;
			string caseMessage;
			if (! SchemaTestUtils.HasExpectedCase(fieldName, _expectedCase, "name",
			                                      out caseMessage))
			{
				errorCount += ReportSchemaPropertyError(
					GetIssueCode(_expectedCase), fieldName,
					fieldMessageFormat, fieldName, caseMessage);
			}

			string lengthMessage;
			TextLengthIssue? lengthIssue = SchemaTestUtils.HasValidLength(
				fieldName, _maximumLength, "name", out lengthMessage);
			if (lengthIssue != null)
			{
				errorCount += ReportSchemaPropertyError(
					GetIssueCode(lengthIssue.Value), fieldName,
					fieldMessageFormat, fieldName,
					lengthMessage);
			}

			return errorCount;
		}

		private bool HasPredefinedName([NotNull] IField field)
		{
			Assert.ArgumentNotNull(field, nameof(field));

			esriFieldType fieldType = field.Type;

			if (fieldType == esriFieldType.esriFieldTypeOID)
			{
				return true;
			}

			if (fieldType == esriFieldType.esriFieldTypeGeometry)
			{
				return true;
			}

			var featureClass = _table as IReadOnlyFeatureClass;

			if (featureClass != null)
			{
				if (field == DatasetUtils.GetAreaField(featureClass))
				{
					return true;
				}

				if (field == DatasetUtils.GetLengthField(featureClass))
				{
					return true;
				}
			}

			return false;
		}
	}
}
