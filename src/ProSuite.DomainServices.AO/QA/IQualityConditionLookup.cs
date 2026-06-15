using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA
{
	public interface IQualityConditionLookup
	{
		[NotNull]
		QualityConditionVerification GetQualityConditionVerification([NotNull] ITest test);

		[NotNull]
		QualityCondition GetQualityCondition([NotNull] ITest test);
	}
}
