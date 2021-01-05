using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.Properties;
using ProSuite.DomainServices.AO.QA.Issues;

namespace ProSuite.DomainServices.AO.QA.HtmlReports
{
	public class HtmlReportIssueGroup
	{
		[NotNull] private static readonly string _issueTypeWarning =
			LocalizableStrings.HtmlReports_IssueType_Warning;

		[NotNull] private static readonly string _issueTypeError =
			LocalizableStrings.HtmlReports_IssueType_Error;

		[NotNull] private readonly IssueGroup _issueGroup;

		public HtmlReportIssueGroup(
			[NotNull] IssueGroup issueGroup,
			int maximumIssueCount,
			[NotNull] string testIdentifier,
			[NotNull] IEnumerable<HtmlExceptionCategoryCount> htmlExceptionCategoryCounts)
		{
			Assert.ArgumentNotNull(issueGroup, nameof(issueGroup));
			Assert.ArgumentNotNullOrEmpty(testIdentifier, nameof(testIdentifier));
			Assert.ArgumentNotNull(htmlExceptionCategoryCounts,
			                       nameof(htmlExceptionCategoryCounts));
			Assert.ArgumentCondition(issueGroup.IssueCount <= maximumIssueCount,
			                         "Issue count must be <= maximum issue count",
			                         nameof(issueGroup));

			_issueGroup = issueGroup;
			PercentageOfMaximumIssueCount = maximumIssueCount == 0
				                                ? 0 // zero-width bar will be drawn
				                                : issueGroup.IssueCount * 100 /
				                                  maximumIssueCount;
			ExceptionCategories = htmlExceptionCategoryCounts.ToList();
			ExceptionCount = issueGroup.ExceptionCount;

			string url = issueGroup.QualityCondition.Url;

			if (url != null && StringUtils.IsNotEmpty(url))
			{
				UrlText = url;
				UrlLink = HtmlReportUtils.GetCompleteUrl(url);
			}

			if (issueGroup.IssueCode != null)
			{
				IssueCode = issueGroup.IssueCode.ID;
				IssueCodeDescription = issueGroup.IssueCode.Description;
				HasIssueCode = true;
			}

			string issueDescription = issueGroup.IssueDescription;
			HasIssueDescription = StringUtils.IsNotEmpty(issueDescription);

			Description = HasIssueDescription
				              ? issueGroup.IssueDescription
				              : IssueCodeDescription;

			TestIdentifier = testIdentifier;
		}

		[UsedImplicitly]
		public int ExceptionCount { get; }

		[NotNull]
		[UsedImplicitly]
		public List<HtmlExceptionCategoryCount> ExceptionCategories { get; }

		[UsedImplicitly]
		public int ExceptionCategoryCount => ExceptionCategories.Count;

		[NotNull]
		internal QualityCondition QualityCondition => _issueGroup.QualityCondition;

		[NotNull]
		[UsedImplicitly]
		public string QualityConditionName => _issueGroup.QualityCondition.Name;

		[CanBeNull]
		[UsedImplicitly]
		public string QualityConditionDescription
			=> _issueGroup.QualityCondition.Description;

		[UsedImplicitly]
		public bool HasIssueDescription { get; }

		[UsedImplicitly]
		public bool HasIssueCode { get; }

		[CanBeNull]
		[UsedImplicitly]
		public string IssueCode { get; }

		[CanBeNull]
		[UsedImplicitly]
		public string Description { get; }

		[CanBeNull]
		[UsedImplicitly]
		public string IssueCodeDescription { get; }

		[CanBeNull]
		[UsedImplicitly]
		public string TestDescription
			=> _issueGroup.QualityCondition.TestDescriptor.Description;

		[CanBeNull]
		[UsedImplicitly]
		public string UrlText { get; }

		[CanBeNull]
		[UsedImplicitly]
		public string UrlLink { get; }

		[CanBeNull]
		[UsedImplicitly]
		public string AffectedComponent => _issueGroup.AffectedComponent;

		[CanBeNull]
		[UsedImplicitly]
		public string IssueDescription => _issueGroup.IssueDescription;

		[NotNull]
		[UsedImplicitly]
		public string TestIdentifier { get; }

		[NotNull]
		[UsedImplicitly]
		public string TestName => _issueGroup.QualityCondition.TestDescriptor.Name;

		[NotNull]
		[UsedImplicitly]
		public string IssueType => _issueGroup.Allowable
			                           ? _issueTypeWarning
			                           : _issueTypeError;

		[UsedImplicitly]
		public bool IsAllowable => _issueGroup.Allowable;

		[UsedImplicitly]
		public bool StopCondition => _issueGroup.StopCondition;

		[UsedImplicitly]
		public int PercentageOfMaximumIssueCount { get; }

		[UsedImplicitly]
		[NotNull]
		[Obsolete(
			"use Format filter in template. Example: category.IssueCount | Format:'N0' ")]
		public string IssueCountFormatted => string.Format("{0:N0}", IssueCount);

		public int IssueCount => _issueGroup.IssueCount;
	}
}
