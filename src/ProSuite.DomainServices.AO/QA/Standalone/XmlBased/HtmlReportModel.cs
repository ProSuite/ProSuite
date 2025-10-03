using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProSuite.Commons.DotLiquid;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Html;
using ProSuite.DomainServices.AO.QA.HtmlReports;
using ProSuite.DomainServices.AO.QA.Issues;
using ProSuite.DomainServices.AO.QA.VerificationReports.Xml;
using ProSuite.DomainServices.AO.Xml;

namespace ProSuite.DomainServices.AO.QA.Standalone.XmlBased
{
	public class HtmlReportModel
	{
		[NotNull] private readonly XmlVerificationReport _verificationReport;
		[NotNull] private readonly List<HtmlReportIssueGroup> _issueGroups;

		public HtmlReportModel(
			[NotNull] string specificationName,
			[NotNull] IIssueStatistics statistics,
			[NotNull] XmlVerificationReport verificationReport,
			[NotNull] string outputDirectoryPath,
			[NotNull] string verificationReportName,
			[CanBeNull] string issueGeodatabasePath,
			[CanBeNull] IEnumerable<string> issueMapFilePaths,
			[NotNull] IEnumerable<string> htmlReportFileNames,
			[CanBeNull] IEnumerable<string> qualitySpecificationReportFilePaths,
			[NotNull] IHtmlDataQualityCategoryOptionsProvider categoryOptionsProvider)
		{
			Assert.ArgumentNotNullOrEmpty(specificationName, nameof(specificationName));
			Assert.ArgumentNotNull(statistics, nameof(statistics));
			Assert.ArgumentNotNull(verificationReport, nameof(verificationReport));
			Assert.ArgumentNotNullOrEmpty(outputDirectoryPath, nameof(outputDirectoryPath));
			Assert.ArgumentNotNullOrEmpty(verificationReportName,
			                              nameof(verificationReportName));
			Assert.ArgumentNotNull(categoryOptionsProvider, nameof(categoryOptionsProvider));

			_verificationReport = verificationReport;
			HtmlReportFiles =
				htmlReportFileNames.Select(fileName =>
					                           new OutputFile(
						                           Path.Combine(outputDirectoryPath, fileName)))
				                   .ToList();
			IssueMapFiles = issueMapFilePaths?.Select(path => new OutputFile(path))
			                                 .ToList() ?? new List<OutputFile>();
			QualitySpecificationReportFiles =
				qualitySpecificationReportFilePaths?.Select(path => new OutputFile(path))
				                                   .ToList() ?? new List<OutputFile>();

			Properties = new NameValuePairs(GetProperties(verificationReport.Properties));
			QualitySpecification = specificationName;
			VerificationWasCancelled = verificationReport.Cancelled;
			HasVerificationExtent = verificationReport.TestExtent != null;

			if (verificationReport.TestExtent != null)
			{
				VerificationExtentString = HtmlReportUtils.FormatExtent(
					verificationReport.TestExtent);
			}

			if (verificationReport.AreaOfInterest != null)
			{
				AreaOfInterest = HtmlReportUtils.GetAreaOfInterest(
					verificationReport.AreaOfInterest);
			}

			OutputDirectoryPath = outputDirectoryPath;
			OutputDirectoryName = Assert.NotNull(Path.GetFileName(outputDirectoryPath));
			OutputDirectoryRelativeUrl = HtmlReportUtils.GetRelativeUrl(string.Empty);
			OutputDirectoryAbsoluteUrl = outputDirectoryPath;

			VerificationReportName = verificationReportName;
			VerificationReportUrl = HtmlReportUtils.GetRelativeUrl(verificationReportName);

			if (IssueMapFiles.Count > 0)
			{
				OutputFile issueMapFile = IssueMapFiles[0];

				MapDocumentName = issueMapFile.FileName;
				MapDocumentUrl = issueMapFile.Url;
			}

			IssueGeodatabaseName = Path.GetFileName(issueGeodatabasePath);

			List<HtmlReportDataQualityCategory> categories =
				HtmlReportUtils.GroupByCategories(
					statistics,
					new HtmlReportDataQualityCategoryComparer(),
					new HtmlReportQualityConditionComparer(),
					new HtmlReportIssueGroupComparer(),
					GetTestIdentifier,
					categoryOptionsProvider,
					out _issueGroups);

			CategoriesWithIssues = categories.Where(c => c.IssueGroups.Count > 0).ToList();
			RootCategories = categories.Where(c => c.IsRoot).ToList();

			VerifiedDatasets = new List<HtmlVerifiedDataset>(
				_verificationReport.VerifiedDatasets.Select(xmld => new HtmlVerifiedDataset(xmld)));

			WorkspaceDescriptions = new List<HtmlWorkspaceDescription>(
				_verificationReport.DataSourceDescriptions.Select(xmlw =>
					new HtmlWorkspaceDescription(xmlw)));

			HasWarnings = statistics.WarningCount > 0;
			HasErrors = statistics.ErrorCount > 0;
			HasIssues = ! HasWarnings && ! HasErrors;

			IssueCount = HtmlReportUtils.Format(statistics.WarningCount +
			                                    statistics.ErrorCount);
			WarningCount = HtmlReportUtils.Format(statistics.WarningCount);
			ErrorCount = HtmlReportUtils.Format(statistics.ErrorCount);
			ExceptionCount = HtmlReportUtils.Format(statistics.ExceptionCount);

			TimeSpan t = TimeSpan.FromSeconds(verificationReport.ProcessingTimeSeconds);

			ProcessingTime = HtmlReportUtils.FormatTimeSpan(t);
		}

		[NotNull]
		[UsedImplicitly]
		public NameValuePairs Properties { get; private set; }

		[NotNull]
		[UsedImplicitly]
		public string QualitySpecification { get; private set; }

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

		[UsedImplicitly]
		[NotNull]
		public List<OutputFile> HtmlReportFiles { get; }

		[UsedImplicitly]
		[NotNull]
		public List<OutputFile> IssueMapFiles { get; }

		[UsedImplicitly]
		[NotNull]
		public List<OutputFile> QualitySpecificationReportFiles { get; }

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
		public string IssueCount { get; private set; }

		[UsedImplicitly]
		[NotNull]
		public string WarningCount { get; private set; }

		[UsedImplicitly]
		[NotNull]
		public string ErrorCount { get; private set; }

		[UsedImplicitly]
		[NotNull]
		public string ExceptionCount { get; private set; }

		[UsedImplicitly]
		public bool HasVerificationExtent { get; private set; }

		[CanBeNull]
		[UsedImplicitly]
		public string VerificationExtentString { get; private set; }

		[CanBeNull]
		[UsedImplicitly]
		public HtmlReportAreaOfInterest AreaOfInterest { get; private set; }

		[UsedImplicitly]
		public bool VerificationWasCancelled { get; private set; }

		[CanBeNull]
		[UsedImplicitly]
		public List<HtmlVerifiedDataset> VerifiedDatasets { get; private set; }

		[CanBeNull]
		[UsedImplicitly]
		public List<HtmlWorkspaceDescription> WorkspaceDescriptions { get; private set; }

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

		[UsedImplicitly]
		public bool DatasetsHaveKnownSpatialReference =>
			VerifiedDatasets?.Any(d => d.CoordinateSystem != null) ?? false;

		[NotNull]
		private static IEnumerable<KeyValuePair<string, string>> GetProperties(
			[CanBeNull] IEnumerable<XmlNameValuePair> properties)
		{
			if (properties == null)
			{
				yield break;
			}

			foreach (XmlNameValuePair property in properties)
			{
				yield return new KeyValuePair<string, string>(property.Name, property.Value);
			}
		}

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
