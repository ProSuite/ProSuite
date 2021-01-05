using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA.Exceptions;
using ProSuite.DomainServices.AO.QA.Issues;

namespace ProSuite.DomainServices.AO.QA.VerificationReports
{
	public class MultiReportBuilder : IVerificationReportBuilder
	{
		[NotNull] private readonly List<IVerificationReportBuilder> _reportBuilders;

		[CLSCompliant(false)]
		public MultiReportBuilder(
			[NotNull] params IVerificationReportBuilder[] reportBuilders)
			: this((IEnumerable<IVerificationReportBuilder>) reportBuilders) { }

		[CLSCompliant(false)]
		public MultiReportBuilder(
			[NotNull] IEnumerable<IVerificationReportBuilder> reportBuilders)
		{
			Assert.ArgumentNotNull(reportBuilders, nameof(reportBuilders));

			_reportBuilders = new List<IVerificationReportBuilder>(reportBuilders);
		}

		[CLSCompliant(false)]
		public void BeginVerification(AreaOfInterest areaOfInterest)
		{
			foreach (IVerificationReportBuilder builder in _reportBuilders)
			{
				builder.BeginVerification(areaOfInterest);
			}
		}

		public void AddVerifiedDataset(Dataset dataset)
		{
			foreach (IVerificationReportBuilder builder in _reportBuilders)
			{
				builder.AddVerifiedDataset(dataset);
			}
		}

		[CLSCompliant(false)]
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

		[CLSCompliant(false)]
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
