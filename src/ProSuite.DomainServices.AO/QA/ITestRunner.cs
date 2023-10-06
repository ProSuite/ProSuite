using System;
using System.Collections.Generic;
using System.Threading;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA
{
	/// <summary>
	/// Encapsulates the execution of tests in a specific area of interest.
	/// </summary>
	public interface ITestRunner
	{
		event EventHandler<QaErrorEventArgs> QaError;

		event EventHandler<VerificationProgressEventArgs> Progress;

		TestAssembler TestAssembler { set; }

		/// <summary>
		/// The quality verification to be updated in the Execute method.
		/// </summary>
		[CanBeNull]
		QualityVerification QualityVerification { get; set; }

		[NotNull]
		RowsWithStopConditions RowsWithStopConditions { get; }

		string CancellationMessage { get; }

		bool Cancelled { get; }

		ISubverificationObserver SubverificationObserver { get; set; }

		/// <summary>
		/// Executes the specified tests in the provided area of interest.
		/// </summary>
		/// <param name="tests"></param>
		/// <param name="areaOfInterest"></param>
		/// <param name="cancellationTokenSource"></param>
		void Execute([NotNull] IEnumerable<ITest> tests,
		             [CanBeNull] AreaOfInterest areaOfInterest,
		             [NotNull] CancellationTokenSource cancellationTokenSource);
	}
}
