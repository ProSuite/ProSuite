using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA.Exceptions;
using ProSuite.DomainServices.AO.QA.VerificationReports;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public class IssueStatisticsBuilder : IVerificationReportBuilder
	{
		public void BeginVerification(AreaOfInterest areaOfInterest) { }

		public void AddVerifiedDataset(QualityVerificationDataset verificationDataset,
		                               string workspaceDisplayText,
		                               ISpatialReference spatialReference) { }

		public void AddIssue(Issue issue, IGeometry errorGeometry)
		{
			IssueStatistics.AddIssue(issue);
		}

		public void AddRowsWithStopConditions(
			IEnumerable<RowWithStopCondition> rowsWithStopCondition) { }

		public void AddExceptionStatistics(IExceptionStatistics statistics)
		{
			IssueStatistics.IncludeUsedExceptions(statistics.GetUsedExceptions());
		}

		public void EndVerification(bool cancelled) { }

		public void AddVerifiedQualityCondition(
			QualitySpecificationElement qualitySpecificationElement) { }

		[NotNull]
		public IssueStatistics IssueStatistics { get; } = new IssueStatistics();
	}
}
