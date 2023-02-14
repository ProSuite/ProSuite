using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Core.IssueCodes
{
	public class AggregatedTestIssueCodes : ITestIssueCodes
	{
		[NotNull] private readonly List<TestIssueCodes> _testIssueCodeses;
		[NotNull] private readonly IList<IssueCode> _issueCodes;

		/// <summary>
		/// Initializes a new instance of the <see cref="AggregatedTestIssueCodes"/> class.
		/// </summary>
		/// <param name="testIssueCodes">The test issue codes.</param>
		public AggregatedTestIssueCodes(params TestIssueCodes[] testIssueCodes)
		{
			_testIssueCodeses = new List<TestIssueCodes>(testIssueCodes);
			_issueCodes = GetAllIssueCodes(_testIssueCodeses);
		}

		public IssueCode GetIssueCode(string code)
		{
			foreach (TestIssueCodes testIssueCodes in _testIssueCodeses)
			{
				IssueCode issueCode = testIssueCodes[code];

				if (issueCode != null)
				{
					return issueCode;
				}
			}

			return null;
		}

		public IList<IssueCode> GetIssueCodes()
		{
			return _issueCodes;
		}

		[NotNull]
		private static IList<IssueCode> GetAllIssueCodes(
			[NotNull] IEnumerable<TestIssueCodes> testIssueCodeses)
		{
			var result = new List<IssueCode>();

			foreach (TestIssueCodes testIssueCodes in testIssueCodeses)
			{
				result.AddRange(testIssueCodes.GetIssueCodes());
			}

			return result.AsReadOnly();
		}
	}
}
