using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainServices.AO.QA.Exceptions;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public interface IIssueStatistics
	{
		[NotNull]
		IList<ExceptionCategory> ExceptionCategories { get; }

		int IssueCount { get; }

		int WarningCount { get; }

		int ErrorCount { get; }

		int ExceptionCount { get; }

		[NotNull]
		IEnumerable<IssueGroup> GetIssueGroups();
	}
}
