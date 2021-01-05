using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.Properties;
using ProSuite.DomainServices.AO.QA.Exceptions;

namespace ProSuite.DomainServices.AO.QA.HtmlReports
{
	public class HtmlReportQualityCondition
	{
		[NotNull] private static readonly string _issueTypeWarning =
			LocalizableStrings.HtmlReports_IssueType_Warning;

		[NotNull] private static readonly string _issueTypeError =
			LocalizableStrings.HtmlReports_IssueType_Error;

		[NotNull] private readonly QualityCondition _qualityCondition;
		[NotNull] private readonly IList<ExceptionCategory> _exceptionCategories;

		[NotNull]
		private readonly List<HtmlExceptionCategoryCount> _issueGroupExceptionCategoryCounts
			= new List<HtmlExceptionCategoryCount>();

		[CanBeNull] private List<HtmlExceptionCategoryCount> _htmlExceptionCategoryCounts;

		public HtmlReportQualityCondition([NotNull] QualityCondition qualityCondition,
		                                  [NotNull] IList<ExceptionCategory> exceptionCategories,
		                                  bool isAllowable, bool stopCondtion)
		{
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));
			Assert.ArgumentNotNull(exceptionCategories, nameof(exceptionCategories));

			_qualityCondition = qualityCondition;
			_exceptionCategories = exceptionCategories;
			IsAllowable = isAllowable;
			StopCondtion = stopCondtion;

			string url = qualityCondition.Url;

			if (url != null && StringUtils.IsNotEmpty(url))
			{
				UrlText = url;
				UrlLink = HtmlReportUtils.GetCompleteUrl(url);
			}
		}

		[NotNull]
		[UsedImplicitly]
		public string QualityConditionName => _qualityCondition.Name;

		[CanBeNull]
		[UsedImplicitly]
		public string QualityConditionDescription => _qualityCondition.Description;

		[NotNull]
		[UsedImplicitly]
		public string IssueType => _qualityCondition.AllowErrors
			                           ? _issueTypeWarning
			                           : _issueTypeError;

		[UsedImplicitly]
		public bool IsAllowable { get; }

		[UsedImplicitly]
		public bool StopCondtion { get; }

		[CanBeNull]
		[UsedImplicitly]
		public string UrlText { get; }

		[CanBeNull]
		[UsedImplicitly]
		public string UrlLink { get; }

		[UsedImplicitly]
		public int IssueCount { get; set; }

		[UsedImplicitly]
		public int ExceptionCount { get; private set; }

		[NotNull]
		[UsedImplicitly]
		public List<HtmlExceptionCategoryCount> ExceptionCategories =>
			_htmlExceptionCategoryCounts ??
			(_htmlExceptionCategoryCounts = HtmlReportUtils.AggregateExceptionCategoryCounts(
				 _issueGroupExceptionCategoryCounts,
				 _exceptionCategories));

		[UsedImplicitly]
		public int ExceptionCategoryCount => ExceptionCategories.Count;

		public void IncludeExceptions(
			[NotNull] IEnumerable<HtmlExceptionCategoryCount> exceptionCategoryCounts)
		{
			_htmlExceptionCategoryCounts = null;

			foreach (HtmlExceptionCategoryCount category in exceptionCategoryCounts)
			{
				_issueGroupExceptionCategoryCounts.Add(category);

				ExceptionCount += category.ExceptionCount;
			}
		}
	}
}
