using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
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
	public class QaSchemaFieldAliases : QaSchemaTestBase
	{
		private readonly IReadOnlyTable _table;
		private readonly int _maximumLength;
		private readonly ExpectedCase _expectedCase;
		private readonly bool _requireUniqueAliasNames;
		private readonly bool _allowCustomSystemFieldAlias;
		private readonly ExpectedStringDifference _expectedDifference;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string NotUnique = "NotUnique";
			public const string Case_NotAllUpper = "Case.NotAllUpper";
			public const string Case_NotAllLower = "Case.NotAllLower";
			public const string Case_NotMixed = "Case.NotMixed";
			public const string Case_AllLower = "Case.AllLower";
			public const string Case_AllUpper = "Case.AllUpper";
			public const string NoAliasName = "NoAliasName";
			public const string EqualsFieldName = "EqualsFieldName";
			public const string EqualsFieldName_ExceptCase = "EqualsFieldName.ExceptCase";
			public const string TextLength_TooShort = "TextLength.TooShort";
			public const string TextLength_TooLong = "TextLength.TooLong";

			public const string SystemFieldAliasDiffersFromName =
				"SystemFieldAliasDiffersFromName";

			public Code() : base("FieldAliases") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaSchemaFieldAliases_0))]
		public QaSchemaFieldAliases(
			[Doc(nameof(DocStrings.QaSchemaFieldAliases_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaSchemaFieldAliases_maximumLength))]
			int maximumLength,
			[Doc(nameof(DocStrings.QaSchemaFieldAliases_expectedCase))]
			ExpectedCase expectedCase,
			[Doc(nameof(DocStrings.QaSchemaFieldAliases_requireUniqueAliasNames))]
			bool requireUniqueAliasNames,
			[Doc(nameof(DocStrings.QaSchemaFieldAliases_allowCustomSystemFieldAlias))]
			bool allowCustomSystemFieldAlias)
			: this(table, maximumLength, expectedCase, requireUniqueAliasNames,
			       allowCustomSystemFieldAlias,
			       // ReSharper disable once IntroduceOptionalParameters.Global
			       ExpectedStringDifference.Any) { }

		[Doc(nameof(DocStrings.QaSchemaFieldAliases_0))]
		public QaSchemaFieldAliases(
			[Doc(nameof(DocStrings.QaSchemaFieldAliases_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaSchemaFieldAliases_maximumLength))]
			int maximumLength,
			[Doc(nameof(DocStrings.QaSchemaFieldAliases_expectedCase))]
			ExpectedCase expectedCase,
			[Doc(nameof(DocStrings.QaSchemaFieldAliases_requireUniqueAliasNames))]
			bool requireUniqueAliasNames,
			[Doc(nameof(DocStrings.QaSchemaFieldAliases_allowCustomSystemFieldAlias))]
			bool allowCustomSystemFieldAlias,
			[Doc(nameof(DocStrings.QaSchemaFieldAliases_expectedDifference))]
			ExpectedStringDifference expectedDifference)
			: base(table)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			_table = table;
			_maximumLength = maximumLength;
			_expectedCase = expectedCase;
			_requireUniqueAliasNames = requireUniqueAliasNames;
			_allowCustomSystemFieldAlias = allowCustomSystemFieldAlias;
			_expectedDifference = expectedDifference;
		}

		[InternallyUsedTest]
		public QaSchemaFieldAliases(
			[NotNull] QaSchemaFieldAliasesDefinition definition)
			: this((IReadOnlyTable) definition.Table, definition.MaximumLength,
			       definition.ExpectedCase,
			       definition.RequireUniqueAliasNames, definition.AllowCustomSystemFieldAlias,
			       definition.ExpectedDifference) { }

		public override int Execute()
		{
			IList<IField> fields = DatasetUtils.GetFields(_table.Fields);

			int errorCount = fields.Sum(field => CheckField(field));

			if (_requireUniqueAliasNames)
			{
				errorCount += CheckDuplicateAliases(fields);
			}

			return errorCount;
		}

		private int CheckField([NotNull] IField field)
		{
			string aliasName = field.AliasName;
			string fieldName = field.Name;

			if (! StringUtils.IsNotEmpty(aliasName))
			{
				return ReportSchemaPropertyError(
					Codes[Code.NoAliasName], fieldName,
					LocalizableStrings.QaSchemaFieldAliases_NoAliasName,
					fieldName);
			}

			if (! IsSystemField(field, _table))
			{
				IssueCode issueCode;
				string message = GetDifferenceError(fieldName, aliasName, _expectedDifference,
				                                    out issueCode);

				if (! string.IsNullOrEmpty(message))
				{
					return ReportSchemaPropertyError(issueCode, fieldName, message);
				}
			}

			int errorCount = 0;

			if (IsSystemField(field, _table))
			{
				if (_allowCustomSystemFieldAlias)
				{
					// system field alias name *may* be different from field name

					// the maximum length should in any case not be exceeded
					errorCount += CheckMaximumLength(field, aliasName);

					if (! aliasName.Equals(fieldName))
					{
						// the alias name is different from the field name - 
						// in this case it must also conform to the case rules
						errorCount += CheckExpectedCase(field, aliasName);
					}

					// else: 
					//   the alias name is exactly equal to the field name. This is always allowed 
					//   (there's no obligation to change all system field alias names), regardless of case
				}
				else
				{
					// alias name must be equal to field name (case differences are ok)

					if (! aliasName.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase))
					{
						errorCount +=
							ReportSchemaPropertyError(
								Codes[Code.SystemFieldAliasDiffersFromName], fieldName,
								LocalizableStrings.QaSchemaFieldAliases_MustBeEqualToFieldName,
								aliasName, fieldName);
					}
				}
			}
			else
			{
				// not a system field
				errorCount += CheckExpectedCase(field, aliasName);
				errorCount += CheckMaximumLength(field, aliasName);
			}

			return errorCount;
		}

		private int CheckMaximumLength([NotNull] IField field,
		                               [NotNull] string aliasName)
		{
			string message;
			TextLengthIssue? lengthIssue = SchemaTestUtils.HasValidLength(
				aliasName, _maximumLength, "alias name", out message);

			if (lengthIssue == null)
			{
				return NoError;
			}

			return ReportSchemaPropertyError(
				GetIssueCode(lengthIssue.Value), field.Name,
				LocalizableStrings.QaSchemaFieldAliases_FieldNameCase,
				aliasName, field.Name, message);
		}

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

		private int CheckExpectedCase([NotNull] IField field,
		                              [NotNull] string aliasName)
		{
			string message;
			if (SchemaTestUtils.HasExpectedCase(aliasName, _expectedCase, "alias name",
			                                    out message))
			{
				return NoError;
			}

			return ReportSchemaPropertyError(
				GetIssueCode(_expectedCase),
				field.Name,
				LocalizableStrings.QaSchemaFieldAliases_FieldNameCase,
				aliasName, field.Name, message);
		}

		[CanBeNull]
		private static string GetDifferenceError([NotNull] string fieldName,
		                                         [NotNull] string aliasName,
		                                         ExpectedStringDifference expectedDifference,
		                                         [CanBeNull] out IssueCode issueCode)
		{
			switch (expectedDifference)
			{
				case ExpectedStringDifference.Any:
					issueCode = null;
					return null;

				case ExpectedStringDifference.CaseSensitiveDifference:
					if (! string.Equals(fieldName, aliasName))
					{
						// difference in case only is valid
						issueCode = null;
						return null;
					}

					issueCode = Codes[Code.EqualsFieldName];
					return string.Format(
						LocalizableStrings.QaSchemaFieldAliases_EqualsFieldName,
						aliasName, fieldName);

				case ExpectedStringDifference.CaseInsensitiveDifference:
					if (string.Equals(fieldName, aliasName,
					                  StringComparison.InvariantCultureIgnoreCase))
					{
						string description;
						if (string.Equals(fieldName, aliasName))
						{
							// equal also in case
							description = LocalizableStrings.QaSchemaFieldAliases_EqualsFieldName;
							issueCode = Codes[Code.EqualsFieldName];
						}
						else
						{
							// differs in case only (still not correct, but different description/code)
							description = LocalizableStrings
								.QaSchemaFieldAliases_EqualsFieldNameExceptCase;
							issueCode = Codes[Code.EqualsFieldName_ExceptCase];
						}

						return string.Format(description, aliasName, fieldName);
					}

					issueCode = null;
					return null;

				default:
					throw new ArgumentOutOfRangeException(nameof(expectedDifference),
					                                      expectedDifference,
					                                      @"Unsupported expected difference");
			}
		}

		private int CheckDuplicateAliases([NotNull] IEnumerable<IField> fields)
		{
			Assert.ArgumentNotNull(fields, nameof(fields));

			var fieldsByAlias = new Dictionary<string, List<IField>>(
				StringComparer.InvariantCultureIgnoreCase);

			foreach (IField field in fields)
			{
				string aliasName = field.AliasName;

				List<IField> fieldsWithSameAlias;
				if (! fieldsByAlias.TryGetValue(aliasName,
				                                out fieldsWithSameAlias))
				{
					fieldsWithSameAlias = new List<IField>();
					fieldsByAlias.Add(aliasName, fieldsWithSameAlias);
				}

				fieldsWithSameAlias.Add(field);
			}

			int errorCount = 0;

			foreach (KeyValuePair<string, List<IField>> pair in fieldsByAlias)
			{
				List<IField> fieldsForAlias = pair.Value;

				if (fieldsForAlias.Count <= 1)
				{
					continue;
				}

				string aliasName = pair.Key;
				string description =
					string.Format(
						LocalizableStrings.QaSchemaFieldAliases_NotUnique,
						aliasName, _table.Name,
						StringUtils.Concatenate(GetFieldNames(fieldsForAlias), ", "));

				errorCount += ReportSchemaError(Codes[Code.NotUnique], description);
			}

			return errorCount;
		}

		[NotNull]
		private static IEnumerable<string> GetFieldNames(
			[NotNull] IEnumerable<IField> fields)
		{
			return fields.Select(field => field.Name);
		}

		private static bool IsSystemField([NotNull] IField field,
		                                  [NotNull] IReadOnlyTable table)
		{
			switch (field.Type)
			{
				case esriFieldType.esriFieldTypeOID:
				case esriFieldType.esriFieldTypeGeometry:
					return true;
			}

			var featureClass = table as IReadOnlyFeatureClass;
			if (featureClass != null)
			{
				if (field == DatasetUtils.GetLengthField(featureClass) ||
				    field == DatasetUtils.GetAreaField(featureClass))
				{
					return true;
				}
			}

			return false;
		}
	}
}
