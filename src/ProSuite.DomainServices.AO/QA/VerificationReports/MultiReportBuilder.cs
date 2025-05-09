using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA.Exceptions;
using ProSuite.DomainServices.AO.QA.Issues;

namespace ProSuite.DomainServices.AO.QA.VerificationReports
{
	public class MultiReportBuilder : IVerificationReportBuilder
	{
		[NotNull] private readonly List<IVerificationReportBuilder> _reportBuilders;

		public MultiReportBuilder(
			[NotNull] params IVerificationReportBuilder[] reportBuilders)
			: this((IEnumerable<IVerificationReportBuilder>) reportBuilders) { }

		public MultiReportBuilder(
			[NotNull] IEnumerable<IVerificationReportBuilder> reportBuilders)
		{
			Assert.ArgumentNotNull(reportBuilders, nameof(reportBuilders));

			_reportBuilders = new List<IVerificationReportBuilder>(reportBuilders);
		}

		public void BeginVerification(AreaOfInterest areaOfInterest)
		{
			foreach (IVerificationReportBuilder builder in _reportBuilders)
			{
				builder.BeginVerification(areaOfInterest);
			}
		}

		public void AddVerifiedDataset(QualityVerificationDataset verificationDataset,
		                               string workspaceDisplayText,
		                               ISpatialReference spatialReference)
		{
			foreach (IVerificationReportBuilder builder in _reportBuilders)
			{
				builder.AddVerifiedDataset(verificationDataset, workspaceDisplayText,
				                           spatialReference);
			}
		}

		public void AddIssue(Issue issue, IGeometry errorGeometry)
		{
			foreach (IVerificationReportBuilder builder in _reportBuilders)
			{
				builder.AddIssue(issue, errorGeometry);
			}
		}

		public void AddRowsWithStopConditions(
			IEnumerable<RowWithStopCondition> rowsWithStopCondition)
		{
			ICollection<RowWithStopCondition> collection =
				CollectionUtils.GetCollection(rowsWithStopCondition);

			foreach (IVerificationReportBuilder builder in _reportBuilders)
			{
				builder.AddRowsWithStopConditions(collection);
			}
		}

		public void AddExceptionStatistics(IExceptionStatistics statistics)
		{
			foreach (IVerificationReportBuilder builder in _reportBuilders)
			{
				builder.AddExceptionStatistics(statistics);
			}
		}

		public void EndVerification(bool cancelled)
		{
			foreach (IVerificationReportBuilder builder in _reportBuilders)
			{
				builder.EndVerification(cancelled);
			}
		}

		public void AddVerifiedQualityCondition(
			QualitySpecificationElement qualitySpecificationElement)
		{
			foreach (IVerificationReportBuilder builder in _reportBuilders)
			{
				builder.AddVerifiedQualityCondition(qualitySpecificationElement);
			}
		}
	}
}
