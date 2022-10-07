using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA.DatasetReports.Xml;
using ProSuite.DomainServices.AO.QA.HtmlReports;
using ProSuite.DomainServices.AO.QA.Issues;
using ProSuite.DomainServices.AO.QA.VerificationReports.Xml;

namespace ProSuite.DomainServices.AO.QA.Standalone.RuleBased
{
	public class HtmlReportModel
	{
		[NotNull] private readonly XmlVerificationReport _verificationReport;
		[NotNull] private readonly List<HtmlReportIssueGroup> _issueGroups;

		public HtmlReportModel([NotNull] IssueStatistics statistics,
		                       [NotNull] ObjectClassReport objectClassReport,
		                       [NotNull] XmlVerificationReport verificationReport,
		                       [NotNull] string outputDirectoryPath,
		                       [NotNull] string objectClassReportName,
		                       [NotNull] string verificationReportName,
		                       [CanBeNull] string issueGeodatabasePath,
		                       [CanBeNull] string mapDocumentName)
		{
			Assert.ArgumentNotNull(statistics, nameof(statistics));
			Assert.ArgumentNotNull(objectClassReport, nameof(objectClassReport));
			Assert.ArgumentNotNull(verificationReport, nameof(verificationReport));
			Assert.ArgumentNotNullOrEmpty(outputDirectoryPath, nameof(outputDirectoryPath));
			Assert.ArgumentNotNullOrEmpty(objectClassReportName,
			                              nameof(objectClassReportName));
			Assert.ArgumentNotNullOrEmpty(verificationReportName,
			                              nameof(verificationReportName));

			_verificationReport = verificationReport;

			Name = objectClassReport.Name;
			AliasName = objectClassReport.AliasName;
			IsWorkspaceVersioned = objectClassReport.IsWorkspaceVersioned;
			IsRegisteredAsVersioned = objectClassReport.IsRegisteredAsVersioned;
			VersionName = objectClassReport.VersionName;
			GeodatabaseRelease = objectClassReport.GeodatabaseRelease;
			VerificationWasCancelled = verificationReport.Cancelled;
			HasVerificationExtent = verificationReport.TestExtent != null;
			if (verificationReport.TestExtent != null)
			{
				VerificationExtentString = HtmlReportUtils.FormatExtent(
					verificationReport.TestExtent);
			}

			Source = StringUtils.IsNotEmpty(objectClassReport.CatalogName)
				         ? objectClassReport.CatalogName
				         : objectClassReport.Name;

			RowCount = HtmlReportUtils.Format(objectClassReport.RowCount);

			var featureClassReport = objectClassReport as FeatureClassReport;
			if (featureClassReport != null)
			{
				IsFeatureClass = true;

				XmlSpatialReferenceDescriptor spatialReference =
					featureClassReport.SpatialReference;

				XYCoordinateSystem = spatialReference.XyCoordinateSystem;
				XYResolution = spatialReference.XyResolutionFormatted;
				XYTolerance = spatialReference.XyToleranceFormatted;

				MultipartFeatureCount =
					HtmlReportUtils.Format(featureClassReport.MultipartFeatureCount);
				EmptyGeometryFeatureCount =
					HtmlReportUtils.Format(featureClassReport.EmptyGeometryFeatureCount +
					                       featureClassReport.NullGeometryFeatureCount);
				NonLinearSegmentFeatureCount =
					HtmlReportUtils.Format(featureClassReport.NonLinearSegmentFeatureCount);

				GeometryType = HtmlReportUtils.FormatGeometryType(featureClassReport.ShapeType,
					featureClassReport.HasZ,
					featureClassReport.HasM);
			}

			OutputDirectoryPath = outputDirectoryPath;
			OutputDirectoryName = Assert.NotNull(Path.GetFileName(outputDirectoryPath));
			OutputDirectoryRelativeUrl = HtmlReportUtils.GetRelativeUrl(string.Empty);
			OutputDirectoryAbsoluteUrl = outputDirectoryPath;

			VerificationReportName = verificationReportName;
			VerificationReportUrl = HtmlReportUtils.GetRelativeUrl(verificationReportName);

			ObjectClassReportName = objectClassReportName;
			ObjectClassReportUrl = HtmlReportUtils.GetRelativeUrl(objectClassReportName);

			MapDocumentName = mapDocumentName;
			if (mapDocumentName != null && StringUtils.IsNotEmpty(mapDocumentName))
			{
				MapDocumentUrl = HtmlReportUtils.GetRelativeUrl(mapDocumentName);
			}

			IssueGeodatabaseName = Path.GetFileName(issueGeodatabasePath);

			List<HtmlReportDataQualityCategory> categories =
				HtmlReportUtils.GroupByCategories(
					statistics,
					new HtmlReportDataQualityCategoryComparer(),
					new HtmlReportQualityConditionComparer(),
					new HtmlReportIssueGroupComparer(),
					GetTestIdentifier, null,
					out _issueGroups);

			CategoriesWithIssues = categories.Where(c => c.IssueGroups.Count > 0).ToList();
			RootCategories = categories.Where(c => c.IsRoot).ToList();

			HasWarnings = statistics.WarningCount > 0;
			HasErrors = statistics.ErrorCount > 0;
			HasIssues = ! HasWarnings && ! HasErrors;

			WarningCount = HtmlReportUtils.Format(statistics.WarningCount);
			ErrorCount = HtmlReportUtils.Format(statistics.ErrorCount);

			TimeSpan t = TimeSpan.FromSeconds(verificationReport.ProcessingTimeSeconds);

			ProcessingTime = HtmlReportUtils.FormatTimeSpan(t);
		}

		[NotNull]
		[UsedImplicitly]
		public string OutputDirectoryName { get; private set; }

		[NotNull]
		[UsedImplicitly]
		public string OutputDirectoryPath { get; private set; }

		[NotNull]
		[UsedImplicitly]
		public string OutputDirectoryRelativeUrl { get; private set; }

		[NotNull]
		[UsedImplicitly]
		public string OutputDirectoryAbsoluteUrl { get; private set; }

		[CanBeNull]
		[UsedImplicitly]
		public string IssueGeodatabaseName { get; private set; }

		[NotNull]
		[UsedImplicitly]
		public string VerificationReportName { get; private set; }

		[NotNull]
		[UsedImplicitly]
		public string VerificationReportUrl { get; private set; }

		[CanBeNull]
		[UsedImplicitly]
		public string MapDocumentName { get; private set; }

		[CanBeNull]
		[UsedImplicitly]
		public string MapDocumentUrl { get; private set; }

		[NotNull]
		[UsedImplicitly]
		public string ObjectClassReportName { get; private set; }

		[NotNull]
		[UsedImplicitly]
		public string ObjectClassReportUrl { get; private set; }

		[UsedImplicitly]
		public string Name { get; private set; }

		[UsedImplicitly]
		public string AliasName { get; private set; }

		[UsedImplicitly]
		public bool IsWorkspaceVersioned { get; private set; }

		[UsedImplicitly]
		public bool IsRegisteredAsVersioned { get; private set; }

		[UsedImplicitly]
		public string VersionName { get; private set; }

		[UsedImplicitly]
		public string GeodatabaseRelease { get; private set; }

		[UsedImplicitly]
		public string Source { get; private set; }

		[UsedImplicitly]
		public DateTime VerificationDate => _verificationReport.EndTime;

		[UsedImplicitly]
		[NotNull]
		public string ProcessingTime { get; private set; }

		[UsedImplicitly]
		public bool HasWarnings { get; private set; }

		[UsedImplicitly]
		public bool HasIssues { get; private set; }

		[UsedImplicitly]
		public bool HasErrors { get; private set; }

		[UsedImplicitly]
		[NotNull]
		public string WarningCount { get; private set; }

		[UsedImplicitly]
		[NotNull]
		public string ErrorCount { get; private set; }

		[UsedImplicitly]
		public bool HasVerificationExtent { get; private set; }

		[UsedImplicitly]
		public string VerificationExtentString { get; private set; }

		[UsedImplicitly]
		public bool VerificationWasCancelled { get; private set; }

		[UsedImplicitly]
		public bool IsFeatureClass { get; private set; }

		[CanBeNull]
		[UsedImplicitly]
		public string XYCoordinateSystem { get; private set; }

		[CanBeNull]
		[UsedImplicitly]
		public string XYResolution { get; private set; }

		[CanBeNull]
		[UsedImplicitly]
		public string XYTolerance { get; private set; }

		[CanBeNull]
		[UsedImplicitly]
		public string GeometryType { get; private set; }

		[UsedImplicitly]
		public string RowCount { get; private set; }

		[UsedImplicitly]
		public string MultipartFeatureCount { get; private set; }

		[UsedImplicitly]
		public string EmptyGeometryFeatureCount { get; private set; }

		[UsedImplicitly]
		public string NonLinearSegmentFeatureCount { get; private set; }

		[NotNull]
		[UsedImplicitly]
		public List<HtmlReportIssueGroup> IssueGroups => _issueGroups;

		[NotNull]
		[UsedImplicitly]
		[Obsolete("Use 'CategoriesWithIssues'")]
		public List<HtmlReportDataQualityCategory> CategoryIssueGroups
			=> CategoriesWithIssues;

		[NotNull]
		[UsedImplicitly]
		public List<HtmlReportDataQualityCategory> CategoriesWithIssues { get; }

		[NotNull]
		[UsedImplicitly]
		public List<HtmlReportDataQualityCategory> RootCategories { get; }

		private static string GetTestIdentifier([NotNull] IssueGroup issueGroup)
		{
			QualityCondition qualityCondition = issueGroup.QualityCondition;

			return qualityCondition.TestDescriptor.Name;

			//return qualityCondition.Description != null &&
			//       StringUtils.IsNotEmpty(qualityCondition.Description)
			//        ? qualityCondition.Description
			//        : qualityCondition.TestDescriptor.Name;
		}
	}
}
