using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Tests.ParameterTypes;
using ProSuite.QA.Tests.Properties;

namespace ProSuite.QA.Tests.Schema
{
	public enum TextLengthIssue
	{
		LessThanMinimum,
		GreaterThanMaximum
	}

	/// <summary>
	/// Utility methods for testing schemas
	/// </summary>
	public static class SchemaTestUtils
	{
		public static TextLengthIssue? HasValidLength([CanBeNull] string text,
		                                              int maximumLength,
		                                              [NotNull] string propertyName,
		                                              [NotNull] out string message)
		{
			const int minimumLength = 0;
			return HasValidLength(text, minimumLength, maximumLength, propertyName,
			                      out message);
		}

		public static TextLengthIssue? HasValidLength([CanBeNull] string text,
		                                              int minimumLength,
		                                              int maximumLength,
		                                              [NotNull] string propertyName,
		                                              [NotNull] out string message)
		{
			int actualLength = text?.Length ?? 0;

			if (actualLength < minimumLength)
			{
				message = string.Format(
					LocalizableStrings.SchemaTestUtils_LengthLessThanMinimum,
					propertyName, minimumLength, actualLength);
				return TextLengthIssue.LessThanMinimum;
			}

			if (maximumLength > 0 && actualLength > maximumLength)
			{
				message =
					string.Format(
						LocalizableStrings.SchemaTestUtils_LengthGreaterThanMaximum,
						propertyName, maximumLength, actualLength);
				return TextLengthIssue.GreaterThanMaximum;
			}

			message = string.Empty;
			return null;
		}

		public static bool HasExpectedCase([NotNull] string text,
		                                   ExpectedCase expectedCase,
		                                   [NotNull] string propertyName,
		                                   [NotNull] out string message)
		{
			Assert.ArgumentNotNull(text, nameof(text));
			Assert.ArgumentNotNull(propertyName, nameof(propertyName));

			message = string.Empty;
			switch (expectedCase)
			{
				case ExpectedCase.Any:
					return true;

				case ExpectedCase.AllUpper:
					if (! Equals(text, text.ToUpper()))
					{
						message = string.Format(
							LocalizableStrings.SchemaTestUtils_CaseAllUppercase,
							StringUtils.ToProperCase(propertyName), text);
						return false;
					}

					return true;

				case ExpectedCase.AllLower:
					if (! Equals(text, text.ToLower()))
					{
						message = string.Format(
							LocalizableStrings.SchemaTestUtils_CaseAllLowercase,
							StringUtils.ToProperCase(propertyName), text);
						return false;
					}

					return true;

				case ExpectedCase.Mixed:
					if (Equals(text, text.ToUpper()) || Equals(text, text.ToLower()))
					{
						message = string.Format(
							LocalizableStrings.SchemaTestUtils_CaseMixedCase,
							StringUtils.ToProperCase(propertyName), text);
						return false;
					}

					return true;

				case ExpectedCase.NotAllUpper:
					if (Equals(text, text.ToUpper()))
					{
						message = string.Format(
							LocalizableStrings.SchemaTestUtils_CaseNotAllUppercase,
							StringUtils.ToProperCase(propertyName), text);
						return false;
					}

					return true;

				case ExpectedCase.NotAllLower:
					if (Equals(text, text.ToLower()))
					{
						message = string.Format(
							LocalizableStrings.SchemaTestUtils_CaseNotAllLowercase,
							StringUtils.ToProperCase(propertyName), text);
						return false;
					}

					return true;

				default:
					throw new ArgumentOutOfRangeException(nameof(expectedCase),
					                                      expectedCase,
					                                      @"Illegal expected case value");
			}
		}

		[NotNull]
		internal static IList<DomainUsage> GetDomainUsages([NotNull] IReadOnlyTable table)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			var domains = new Dictionary<IDomain, DomainUsage>();

			var subtypes = table as ISubtypes;

			IList<Subtype> subtypeValues = DatasetUtils.GetSubtypes(subtypes);

			foreach (IField field in DatasetUtils.GetFields(table.Fields))
			{
				IDomain domain = field.Domain;

				if (domain != null)
				{
					DomainUsage domainUsage;
					if (! domains.TryGetValue(domain, out domainUsage))
					{
						domainUsage = new DomainUsage(domain);
						domains.Add(domain, domainUsage);
					}

					domainUsage.AddReferenceFrom(field);
				}

				string fieldName = field.Name;

				if (subtypes == null)
				{
					continue;
				}

				foreach (Subtype subtype in subtypeValues)
				{
					IDomain subtypeDomain = subtypes.Domain[subtype.Code, fieldName];

					if (subtypeDomain == null)
					{
						continue;
					}

					DomainUsage domainUsage;
					if (! domains.TryGetValue(subtypeDomain, out domainUsage))
					{
						domainUsage = new DomainUsage(subtypeDomain);
						domains.Add(subtypeDomain, domainUsage);
					}

					domainUsage.AddReferenceFrom(field);
				}
			}

			return new List<DomainUsage>(domains.Values);
		}
	}
}
