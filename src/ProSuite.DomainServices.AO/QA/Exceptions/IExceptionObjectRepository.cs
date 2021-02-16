using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Exceptions
{
	public interface IExceptionObjectRepository
	{
		[NotNull]
		IExceptionStatistics ExceptionStatistics { get; }

		[CanBeNull]
		IExceptionObjectEvaluator ExceptionObjectEvaluator { get; }
	}
}
