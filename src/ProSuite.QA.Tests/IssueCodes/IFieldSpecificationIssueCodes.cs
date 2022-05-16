using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core.IssueCodes;

namespace ProSuite.QA.Tests.IssueCodes
{
	public interface IFieldSpecificationIssueCodes
	{
		[CanBeNull]
		IssueCode UnexpectedFieldLength { get; }

		[CanBeNull]
		IssueCode UnexpectedAlias { get; }

		[CanBeNull]
		IssueCode NoDomain { get; }

		[CanBeNull]
		IssueCode UnexpectedDomain { get; }

		[CanBeNull]
		IssueCode UnexpectedFieldType { get; }
	}
}
