using System;
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
using ProSuite.QA.Tests.Schema;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[SchemaTest]
	public class QaSchemaFieldDomainNames : QaSchemaTestBase
	{
		private readonly IReadOnlyTable _table;
		private readonly string _expectedPrefix;
		private readonly int _maximumLength;
		private readonly bool _mustContainFieldName;
		private readonly ExpectedCase _expectedCase;

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
			public const string TextLength_TooShort = "TextLength.TooShort";
			public const string TextLength_TooLong = "TextLength.TooLong";
			public const string DoesStartWithPrefix = "DoesNotStartWithPrefix";
			public const string DoesNotContainFieldName = "DoesNotContainFieldName";

			public Code() : base("DomainNames") { }
		}

		#endregion

		#region Constructors

		[Doc(nameof(DocStrings.QaSchemaFieldDomainNames_0))]
		public QaSchemaFieldDomainNames(
			[Doc(nameof(DocStrings.QaSchemaFieldDomainNames_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaSchemaFieldDomainNames_expectedPrefix))] [CanBeNull]
			string expectedPrefix,
			[Doc(nameof(DocStrings.QaSchemaFieldDomainNames_maximumLength))]
			int maximumLength,
			[Doc(nameof(DocStrings.QaSchemaFieldDomainNames_mustContainFieldName))]
			bool mustContainFieldName,
			[Doc(nameof(DocStrings.QaSchemaFieldDomainNames_expectedCase))]
			ExpectedCase expectedCase)
			: base(table)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			_table = table;
			_expectedPrefix = expectedPrefix;
			_maximumLength = maximumLength;
			_mustContainFieldName = mustContainFieldName;
			_expectedCase = expectedCase;
		}

		#endregion

		[InternallyUsedTest]
		public QaSchemaFieldDomainNames(
			[NotNull] QaSchemaFieldDomainNamesDefinition definition)
			: this((IReadOnlyTable) definition.Table, definition.ExpectedPrefix,
			       definition.MaximumLength, definition.MustContainFieldName,
			       definition.ExpectedCase) { }

		public override int Execute()
		{
			int errorCount = 0;

			foreach (DomainUsage domainUsage in SchemaTestUtils.GetDomainUsages(_table))
			{
				errorCount += CheckDomain(domainUsage);
			}

			return errorCount;
		}

		private int CheckDomain([NotNull] DomainUsage domainUsage)
		{
			Assert.ArgumentNotNull(domainUsage, nameof(domainUsage));

			string domainName = domainUsage.DomainName;

			int errorCount = 0;

			string caseMessage;
			if (! SchemaTestUtils.HasExpectedCase(domainName, _expectedCase, "name",
			                                      out caseMessage))
			{
				errorCount += ReportSchemaPropertyError(
					GetIssueCode(_expectedCase), domainName,
					"Domain name '{0}' has unexpected case: {1}",
					domainName, caseMessage);
			}

			if (StringUtils.IsNotEmpty(_expectedPrefix))
			{
				if (! domainName.StartsWith(_expectedPrefix))
				{
					errorCount +=
						ReportSchemaPropertyError(
							Codes[Code.DoesStartWithPrefix], domainName,
							"Domain name '{0}' does not start with prefix '{1}'",
							domainName, _expectedPrefix);
				}
			}

			if (_mustContainFieldName)
			{
				foreach (IField field in domainUsage.ReferencingFields)
				{
					string fieldName = field.Name;

					if (domainName.Contains(fieldName))
					{
						continue;
					}

					errorCount +=
						ReportSchemaPropertyError(
							Codes[Code.DoesNotContainFieldName], domainName,
							"Domain name '{0}' does not contain the field name '{1}'",
							domainName, fieldName);
				}
			}

			if (_maximumLength > 0)
			{
				string message;
				TextLengthIssue? lengthIssue = SchemaTestUtils.HasValidLength(
					domainName, _maximumLength, "name", out message);
				if (lengthIssue != null)
				{
					errorCount += ReportSchemaPropertyError(
						GetIssueCode(lengthIssue.Value), domainName,
						"Domain '{0}': '{1}'",
						domainName, message);
				}
			}

			return errorCount;
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
	}
}
