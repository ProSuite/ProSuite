using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA.Exceptions;
using ProSuite.DomainServices.AO.QA.Issues;

namespace ProSuite.DomainServices.AO.QA.VerificationReports
{
	public interface IVerificationReportBuilder
	{
		void BeginVerification([CanBeNull] AreaOfInterest areaOfInterest);

		void AddVerifiedDataset([NotNull] QualityVerificationDataset verificationDataset,
		                        [CanBeNull] string workspaceDisplayText,
		                        [CanBeNull] ISpatialReference spatialReference);

		void AddIssue([NotNull] Issue issue, [CanBeNull] IGeometry errorGeometry);

		void AddRowsWithStopConditions(
			[NotNull] IEnumerable<RowWithStopCondition> rowsWithStopCondition);

		void EndVerification(bool cancelled);

		void AddVerifiedQualityCondition(
			[NotNull] QualitySpecificationElement qualitySpecificationElement);

		void AddExceptionStatistics([NotNull] IExceptionStatistics statistics);
	}
}
