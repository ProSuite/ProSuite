using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Collections;
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
	public class QaSchemaFieldDomainCodedValues : QaSchemaTestBase
	{
		private readonly IReadOnlyTable _table;
		private readonly int _maximumNameLength;
		private readonly UniqueStringsConstraint _uniqueNamesConstraint;
		private readonly int _minimumValueCount;
		private readonly int _minimumNonEqualNameValueCount;
		private readonly bool _allowEmptyName;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string NoName = "NoName";
			public const string TooFewCodedValues = "TooFewCodedValues";

			public const string NotEnoughNamesDifferentFromValue =
				"NotEnoughNamesDifferentFromValue";

			public const string NamesNotUnique = "NamesNotUnique";
			public const string TextLength_TooShort = "TextLength.TooShort";
			public const string TextLength_TooLong = "TextLength.TooLong";

			public Code() : base("CodedValues") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaSchemaFieldDomainCodedValues_0))]
		public QaSchemaFieldDomainCodedValues(
			[Doc(nameof(DocStrings.QaSchemaFieldDomainCodedValues_table))] [NotNull]
			IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaSchemaFieldDomainCodedValues_maximumNameLength))]
			int maximumNameLength,
			[Doc(nameof(DocStrings.QaSchemaFieldDomainCodedValues_uniqueNamesConstraint))]
			UniqueStringsConstraint uniqueNamesConstraint,
			[Doc(nameof(DocStrings.QaSchemaFieldDomainCodedValues_minimumValueCount))]
			int minimumValueCount,
			[Doc(nameof(DocStrings.QaSchemaFieldDomainCodedValues_minimumNonEqualNameValueCount))]
			int minimumNonEqualNameValueCount,
			[Doc(nameof(DocStrings.QaSchemaFieldDomainCodedValues_allowEmptyName))]
			bool allowEmptyName)
			: base(table)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			_table = table;
			_maximumNameLength = maximumNameLength;
			_uniqueNamesConstraint = uniqueNamesConstraint;
			_minimumValueCount = minimumValueCount;
			_minimumNonEqualNameValueCount = minimumNonEqualNameValueCount;
			_allowEmptyName = allowEmptyName;
		}

		[InternallyUsedTest]
		public QaSchemaFieldDomainCodedValues(
			[NotNull] QaSchemaFieldDomainCodedValuesDefinition definition)
			: this((IReadOnlyTable) definition.Table, definition.MaximumNameLength,
			       definition.UniqueNamesConstraint, definition.MinimumValueCount,
			       definition.MinimumNonEqualNameValueCount, definition.AllowEmptyName) { }

		public override int Execute()
		{
			return
				SchemaTestUtils.GetDomainUsages(_table).Sum(
					domainUsage => CheckDomain(domainUsage));
		}

		private int CheckDomain([NotNull] DomainUsage domainUsage)
		{
			IDomain domain = domainUsage.Domain;

			var codedValueDomain = domain as ICodedValueDomain;
			if (codedValueDomain == null)
			{
				return NoError;
			}

			List<CodedValue> codedValues = DomainUtils.GetCodedValueList(codedValueDomain);

			int nonEqualNameValueCount = 0;

			int errorCount = 0;
			foreach (CodedValue codedValue in codedValues)
			{
				if (! Equals(codedValue.Value.ToString().Trim(), codedValue.Name.Trim()))
				{
					nonEqualNameValueCount++;
				}

				if (! StringUtils.IsNotEmpty(codedValue.Name))
				{
					if (! _allowEmptyName)
					{
						errorCount +=
							ReportSchemaPropertyError(
								Codes[Code.NoName], domain.Name,
								new[] {codedValue.Value},
								"Value [{0}] in coded value domain '{1}' does not have an associated name",
								codedValue.Value, domain.Name);
					}

					continue;
				}

				string message;
				TextLengthIssue? lengthIssue = SchemaTestUtils.HasValidLength(
					codedValue.Name, _maximumNameLength, "name", out message);

				if (lengthIssue != null)
				{
					errorCount +=
						ReportSchemaPropertyError(GetIssueCode(lengthIssue.Value), domain.Name,
						                          new[] {codedValue.Value},
						                          "Value [{0}] in coded value domain '{1}': {2} ('{3}')",
						                          codedValue.Value, domain.Name, message,
						                          codedValue.Name);
				}
			}

			// report non-unique names
			errorCount += CheckUniqueNames(domainUsage, codedValues);

			// report error if the number of coded values is less than the minimum count
			if (codedValues.Count < _minimumValueCount)
			{
				string format = codedValues.Count == 1
					                ? "Domain '{0}' has {1} coded value. Minimum: {2}"
					                : "Domain '{0}' has {1} coded values. Minimum: {2}";

				string description = string.Format(format,
				                                   domainUsage.DomainName, codedValues.Count,
				                                   _minimumValueCount);

				errorCount += ReportSchemaPropertyError(Codes[Code.TooFewCodedValues],
				                                        domainUsage.DomainName, description);
			}

			// report error if the number of coded values with non-equal name/value pair is
			// less than the minimum count for non-equal values (and the total coded value
			// count exceeds that minimum value; otherwise an error would always be reported)
			if (nonEqualNameValueCount < _minimumNonEqualNameValueCount &&
			    codedValues.Count >= _minimumNonEqualNameValueCount)
			{
				string description = string.Format(
					"Domain '{0}' has {1} coded values with a name that is different from the value. Minimum: {2}",
					domainUsage.DomainName, nonEqualNameValueCount,
					_minimumNonEqualNameValueCount);

				errorCount +=
					ReportSchemaPropertyError(Codes[Code.NotEnoughNamesDifferentFromValue],
					                          domainUsage.DomainName, description);
			}

			return errorCount;
		}

		private int CheckUniqueNames([NotNull] DomainUsage domainUsage,
		                             [NotNull] ICollection<CodedValue> codedValues)
		{
			SimpleSet<string> uniqueNames = CreateUniqueNamesSet(_uniqueNamesConstraint);

			if (uniqueNames == null)
			{
				return NoError;
			}

			var nonUniqueNames = new List<string>();

			foreach (CodedValue codedValue in codedValues)
			{
				// gather non-unique names, uniqueness check will be done at end
				string name = codedValue.Name.Trim();

				if (uniqueNames.Contains(name))
				{
					nonUniqueNames.Add(name);
				}
				else
				{
					uniqueNames.Add(name);
				}
			}

			bool caseSensitive = _uniqueNamesConstraint ==
			                     UniqueStringsConstraint.UniqueExactCase;

			int errorCount = 0;

			foreach (string nonUniqueName in nonUniqueNames)
			{
				string description =
					string.Format(
						"Name '{0}' in coded value domain '{1}' is non-unique. The following values have the same name: {2}",
						nonUniqueName, domainUsage.DomainName,
						StringUtils.Concatenate(
							GetValuesForName(nonUniqueName, codedValues, caseSensitive), ", "));

				errorCount += ReportSchemaPropertyError(Codes[Code.NamesNotUnique],
				                                        domainUsage.DomainName,
				                                        new object[] {nonUniqueName},
				                                        description);
			}

			return errorCount;
		}

		[NotNull]
		private static IEnumerable<object> GetValuesForName(
			[NotNull] string nonUniqueName,
			[NotNull] IEnumerable<CodedValue> codedValues,
			bool caseSensitive)
		{
			Assert.ArgumentNotNull(nonUniqueName, nameof(nonUniqueName));
			Assert.ArgumentNotNull(codedValues, nameof(codedValues));

			StringComparison comparison = caseSensitive
				                              ? StringComparison.InvariantCulture
				                              : StringComparison.InvariantCultureIgnoreCase;

			foreach (CodedValue codedValue in codedValues)
			{
				if (nonUniqueName.Equals(codedValue.Name.Trim(), comparison))
				{
					yield return codedValue.Value;
				}
			}
		}

		[CanBeNull]
		private static SimpleSet<string> CreateUniqueNamesSet(
			UniqueStringsConstraint uniqueStringsConstraint)
		{
			switch (uniqueStringsConstraint)
			{
				case UniqueStringsConstraint.None:
					return null;

				case UniqueStringsConstraint.UniqueExactCase:
					return new SimpleSet<string>(StringComparer.InvariantCulture);

				case UniqueStringsConstraint.UniqueAnyCase:
					return new SimpleSet<string>(StringComparer.InvariantCultureIgnoreCase);

				default:
					throw new ArgumentOutOfRangeException(nameof(uniqueStringsConstraint),
					                                      uniqueStringsConstraint,
					                                      @"Unsupported unique constraint");
			}
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
	}
}
