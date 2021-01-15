using ProSuite.QA.Container;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.IssueCodes
{
	public interface IFieldSpecificationsIssueCodes : IFieldSpecificationIssueCodes
	{
		[CanBeNull]
		IssueCode UnexpectedFieldNameForAlias { get; }

		[CanBeNull]
		IssueCode MissingField { get; }
	}
}
