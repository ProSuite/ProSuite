using System;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Exceptions
{
	[CLSCompliant(false)]
	public interface IExceptionObjectRepository
	{
		[NotNull]
		IExceptionStatistics ExceptionStatistics { get; }

		[CanBeNull]
		IExceptionObjectEvaluator ExceptionObjectEvaluator { get; }
	}
}
