using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA.Exceptions
{
	public interface IExceptionEvaluationStatistics
	{
		void AddUsedException([NotNull] ExceptionObject exceptionObject,
		                      [NotNull] QualitySpecificationElement element,
		                      [NotNull] QaError qaError);

		void AddExceptionObject([NotNull] ExceptionObject exceptionObject,
		                        [NotNull] QualityCondition qualityCondition);

		int GetUsageCount([NotNull] ExceptionObject exceptionObject,
		                  [NotNull] QualityCondition qualityCondition);
	}
}
