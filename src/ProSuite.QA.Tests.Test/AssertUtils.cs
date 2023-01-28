using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Tests.Test.TestRunners;

namespace ProSuite.QA.Tests.Test
{
	public static class AssertUtils
	{
		[NotNull]
		public static QaError OneError([NotNull] QaTestRunnerBase runner,
		                               [NotNull] string issueCodeId,
		                               int expectedInvolvedRowsCount = 1)
		{
			Assert.AreEqual(1, runner.Errors.Count);

			QaError error = runner.Errors[0];
			Assert.AreEqual(error.InvolvedRows.Count, expectedInvolvedRowsCount);

			IssueCode issueCode = error.IssueCode;
			Assert.IsNotNull(issueCode);
			Assert.AreEqual(issueCodeId, issueCode.ID);

			return error;
		}

		public static void NoError([NotNull] QaTestRunnerBase runner)
		{
			Assert.AreEqual(0, runner.Errors.Count);
		}

		public static void ExpectedErrors(int expectedErrorCount,
		                                  [NotNull] ICollection<QaError> errors,
		                                  [NotNull] params string[] expectedIssueCodes)
		{
			ExpectedErrors(expectedErrorCount, null, errors, expectedIssueCodes);
		}

		public static void ExpectedErrors(int expectedErrorCount,
		                                  string issueCodePrefix,
		                                  [NotNull] ICollection<QaError> errors,
		                                  [NotNull] params string[] expectedIssueCodes)
		{
			Assert.AreEqual(expectedErrorCount, errors.Count);

			HashSet<string> actualIds = GetIssueCodes(errors, issueCodePrefix);

			var missingIssueCodes = new List<string>();

			foreach (string expectedIssueCode in expectedIssueCodes)
			{
				string expectedId = StripPrefix(expectedIssueCode, issueCodePrefix);

				if (! actualIds.Contains(expectedId))
				{
					missingIssueCodes.Add(expectedId);
				}
			}

			if (missingIssueCodes.Count > 0)
			{
				Assert.Fail("Missing issue codes: {0}",
				            StringUtils.Concatenate(missingIssueCodes, ", "));
			}
		}

		public static void ExpectedErrors(
			int expectedErrorCount,
			[NotNull] ICollection<QaError> errors)
		{
			Assert.AreEqual(expectedErrorCount, errors.Count);
		}

		public static void ExpectedErrors(
			int expectedErrorCount,
			[NotNull] ICollection<QaError> errors,
			[NotNull] params Predicate<QaError>[] expectedErrorPredicates)
		{
			Assert.AreEqual(expectedErrorCount, errors.Count);

			var unmatched = new List<int>();

			for (var i = 0; i < expectedErrorPredicates.Length; i++)
			{
				Predicate<QaError> predicate = expectedErrorPredicates[i];

				bool matched = errors.Any(error => predicate(error));

				if (! matched)
				{
					unmatched.Add(i);
				}
			}

			if (unmatched.Count > 0)
			{
				Assert.Fail("Unmatched predicate index(es): {0}",
				            StringUtils.Concatenate(unmatched, "; "));
			}
		}

		public static IDisposable UseInvariantCulture()
		{
			return new TempCulture();
		}

		[NotNull]
		private static HashSet<string> GetIssueCodes([NotNull] IEnumerable<QaError> errors,
		                                             string prefix)
		{
			var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (QaError error in errors)
			{
				IssueCode issueCode = error.IssueCode;
				if (issueCode != null)
				{
					result.Add(StripPrefix(issueCode.ID, prefix));
				}
			}

			return result;
		}

		[NotNull]
		private static string StripPrefix([NotNull] string issueCodeId,
		                                  [CanBeNull] string prefix)
		{
			if (prefix == null)
			{
				return issueCodeId;
			}

			return issueCodeId.StartsWith(prefix)
				       ? issueCodeId.Substring(prefix.Length)
				       : issueCodeId;
		}

		private class TempCulture : IDisposable
		{
			private readonly CultureInfo _origCulture;
			private readonly CultureInfo _origUiCulture;

			public TempCulture(CultureInfo culture = null, CultureInfo uiCulture = null)
			{
				_origCulture = CultureInfo.CurrentCulture;
				_origUiCulture = CultureInfo.CurrentUICulture;

				CultureInfo.CurrentCulture = culture ?? CultureInfo.InvariantCulture;
				CultureInfo.CurrentUICulture = uiCulture ?? CultureInfo.CurrentCulture;
			}

			public void Dispose()
			{
				CultureInfo.CurrentUICulture = _origUiCulture;
				CultureInfo.CurrentCulture = _origCulture;
			}
		}
	}
}
