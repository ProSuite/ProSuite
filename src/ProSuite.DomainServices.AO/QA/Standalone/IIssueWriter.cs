using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA.Standalone
{
	public interface IIssueWriter
	{
		void WriteIssue([NotNull] QaError qaError,
		                [NotNull] QualitySpecificationElement element);
	}
}
