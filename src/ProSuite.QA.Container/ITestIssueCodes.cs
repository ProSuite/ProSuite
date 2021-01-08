using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	public interface ITestIssueCodes
	{
		[CanBeNull]
		IssueCode GetIssueCode([NotNull] string code);

		[NotNull]
		IList<IssueCode> GetIssueCodes();
	}
}
