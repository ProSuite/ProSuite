using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA.Exceptions
{
	public interface IExceptionObjectEvaluator
	{
		[ContractAnnotation(
			"=>true, exceptionObject:notnull; =>false, exceptionObject:canbenull")]
		bool ExistsExceptionFor([NotNull] QaError qaError,
		                        [NotNull] QualitySpecificationElement element,
		                        out ExceptionObject exceptionObject);
	}
}
