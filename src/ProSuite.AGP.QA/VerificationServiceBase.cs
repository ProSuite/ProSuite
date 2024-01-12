using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Progress;
using ProSuite.DomainModel.AGP.Workflow;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;

namespace ProSuite.AGP.QA
{
	/// <summary>
	/// Base class of the application service that performs the quality verification using the
	/// appropriate back-end.
	/// </summary>
	public abstract class VerificationServiceBase
	{
		public string VerificationReportName { get; set; }

		public string HtmlReportName { get; set; }

		[CanBeNull]
		public ISupportedInstanceDescriptors SupportedInstanceDescriptors { get; set; }

		public abstract Task<ServiceCallStatus> Verify(
			[NotNull] IQualitySpecificationReference qualitySpecificationRef,
			[CanBeNull] Geometry perimeter,
			ProjectWorkspace projectWorkspace,
			[NotNull] QualityVerificationProgressTracker progress,
			[CanBeNull] string resultsPath);

		public abstract Task<ServiceCallStatus> Verify(
			[NotNull] QualitySpecification qualitySpecification,
			[CanBeNull] Geometry perimeter,
			ProjectWorkspace projectWorkspace,
			[NotNull] QualityVerificationProgressTracker progress,
			[CanBeNull] string resultsPath);

		public abstract Task<ServiceCallStatus> VerifySelection(
			[NotNull] IQualitySpecificationReference qualitySpecificationRef,
			[NotNull] IList<Row> objectsToVerify,
			[CanBeNull] Geometry perimeter,
			ProjectWorkspace projectWorkspace,
			[NotNull] QualityVerificationProgressTracker progress,
			[CanBeNull] string resultsPath);

		public abstract Task<ServiceCallStatus> VerifySelection(
			[NotNull] QualitySpecification qualitySpecification,
			[NotNull] IList<Row> objectsToVerify,
			[CanBeNull] Geometry perimeter,
			ProjectWorkspace projectWorkspace,
			[NotNull] QualityVerificationProgressTracker progress,
			[CanBeNull] string resultsPath);
	}
}
