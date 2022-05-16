using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core.IssueCodes;

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
