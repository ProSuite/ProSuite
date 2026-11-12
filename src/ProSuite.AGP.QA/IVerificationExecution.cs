using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Progress;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;

namespace ProSuite.AGP.QA
{
	/// <summary>
	/// Executes verification with an application context and results path bound for one run.
	/// </summary>
	public interface IVerificationExecution
	{
		[CanBeNull]
		string ContextDisplayName { get; }

		[CanBeNull]
		string ResultsPath { get; }

		Task<ServiceCallStatus> Verify(
			[NotNull] IQualitySpecificationReference specification,
			[CanBeNull] Geometry perimeter,
			[NotNull] QualityVerificationProgressTracker progress);

		Task<ServiceCallStatus> Verify(
			[NotNull] QualitySpecification specification,
			[CanBeNull] Geometry perimeter,
			[NotNull] QualityVerificationProgressTracker progress);

		Task<ServiceCallStatus> VerifySelection(
			[NotNull] IQualitySpecificationReference specification,
			[NotNull] IList<Row> objectsToVerify,
			[CanBeNull] Geometry perimeter,
			[NotNull] QualityVerificationProgressTracker progress);

		Task<ServiceCallStatus> VerifySelection(
			[NotNull] QualitySpecification specification,
			[NotNull] IList<Row> objectsToVerify,
			[CanBeNull] Geometry perimeter,
			[NotNull] QualityVerificationProgressTracker progress);
	}
}
