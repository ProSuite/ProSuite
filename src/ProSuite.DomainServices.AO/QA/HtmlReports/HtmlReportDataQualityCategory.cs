using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Html;
using ProSuite.DomainServices.AO.QA.Exceptions;

namespace ProSuite.DomainServices.AO.QA.HtmlReports
{
	public class HtmlReportDataQualityCategory :
		IEquatable<HtmlReportDataQualityCategory>
	{
		[CanBeNull] private readonly DataQualityCategory _category;
		[NotNull] private readonly IList<ExceptionCategory> _exceptionCategories;
		[NotNull] private readonly HtmlReportDataQualityCategoryComparer _categoryComparer;

		[NotNull] private readonly HtmlReportQualityConditionComparer
			_qualityConditionComparer;

		[NotNull] private readonly HtmlReportIssueGroupComparer _issueGroupComparer;
		[NotNull] private readonly string _uniqueName;

		[NotNull] private readonly HashSet<HtmlReportDataQualityCategory> _subCategories =
			new HashSet<HtmlReportDataQualityCategory>();

		[NotNull] private readonly Dictionary<QualityCondition, HtmlReportQualityCondition>
			_qualityConditions =
				new Dictionary<QualityCondition, HtmlReportQualityCondition>();

		[NotNull] private readonly List<HtmlReportIssueGroup> _issueGroups =
			new List<HtmlReportIssueGroup>();

		[CanBeNull] private List<HtmlExceptionCategoryCount> _htmlExceptionCategoryCounts;

		private bool _issueGroupsRequireSorting;

		public HtmlReportDataQualityCategory(
			[CanBeNull] DataQualityCategory category,
			[NotNull] IList<ExceptionCategory> exceptionCategories,
			[CanBeNull] HtmlDataQualityCategoryOptions options,
			[NotNull] HtmlReportDataQualityCategoryComparer categoryComparer,
			[NotNull] HtmlReportQualityConditionComparer qualityConditionComparer,
			[NotNull] HtmlReportIssueGroupComparer issueGroupComparer)
		{
			Assert.ArgumentNotNull(categoryComparer, nameof(categoryComparer));
			Assert.ArgumentNotNull(exceptionCategories, nameof(exceptionCategories));
			Assert.ArgumentNotNull(qualityConditionComparer, nameof(qualityConditionComparer));
			Assert.ArgumentNotNull(issueGroupComparer, nameof(issueGroupComparer));

			_category = category;
			_exceptionCategories = exceptionCategories;
			_categoryComparer = categoryComparer;
			_qualityConditionComparer = qualityConditionComparer;
			_issueGroupComparer = issueGroupComparer;

			if (category == null)
			{
				IsUndefinedCategory = true;
				_uniqueName = "<nocategory>";

				Name = string.Empty;
				Abbreviation = string.Empty;
				Description = string.Empty;
			}
			else
			{
				IsUndefinedCategory = false;
				_uniqueName = category.GetQualifiedName("||");

				Name = GetDisplayName(category, options);
				Abbreviation = category.Abbreviation ?? string.Empty;
				Description = category.Description ?? string.Empty;
			}
		}

		public bool IsRoot => ParentCategory == null;

		[CanBeNull]
		public HtmlReportDataQualityCategory ParentCategory { get; set; }

		[UsedImplicitly]
		public int Level
		{
			get
			{
				var count = 0;

				if (ParentCategory != null)
				{
					count = ParentCategory.Level + 1;
				}

				return count;
			}
		}

		[NotNull]
		public string Name { get; }

		[NotNull]
		[UsedImplicitly]
		public string Abbreviation { get; }

		[NotNull]
		[UsedImplicitly]
		public string Description { get; }

		[NotNull]
		[UsedImplicitly]
		public string QualifiedName
		{
			get
			{
				return IsUndefinedCategory
					       ? string.Empty
					       : HtmlReportUtils.GetQualifiedText(this, c => c.Name);
			}
		}

		[NotNull]
		[UsedImplicitly]
		public string QualifiedAbbreviation
		{
			get
			{
				return IsUndefinedCategory
					       ? string.Empty
					       : HtmlReportUtils.GetQualifiedText(
						       this,
						       c => StringUtils.IsNullOrEmptyOrBlank(c.Abbreviation)
							            ? null
							            : c.Abbreviation,
						       skipNullOrEmpty: true);
			}
		}

		[NotNull]
		[UsedImplicitly]
		public string QualifiedAbbreviationOrName
		{
			get
			{
				return IsUndefinedCategory
					       ? string.Empty
					       : HtmlReportUtils.GetQualifiedText(
						       this,
						       c => StringUtils.IsNullOrEmptyOrBlank(c.Abbreviation)
							            ? c.Name
							            : c.Abbreviation);
			}
		}

		public bool IsUndefinedCategory { get; }

		public int ListOrder => _category?.ListOrder ?? 0;

		[UsedImplicitly]
		public bool HasSubCategories => _subCategories.Count > 0;

		[NotNull]
		[UsedImplicitly]
		public List<HtmlReportDataQualityCategory> SubCategories
		{
			get { return _subCategories.OrderBy(c => c, _categoryComparer).ToList(); }
		}

		[NotNull]
		[UsedImplicitly]
		[Obsolete(
			"use Format filter in template. Example: category.IssueCount | Format:'N0' ")]
		public string IssueCountFormatted => $"{IssueCount:N0}";

		[UsedImplicitly]
		public int ExceptionCount
		{
			get { return _qualityConditions.Values.Sum(q => q.ExceptionCount); }
		}

		[UsedImplicitly]
		public int ExceptionCountWithChildren
		{
			get { return ExceptionCount + _subCategories.Sum(c => c.ExceptionCountWithChildren); }
		}

		[UsedImplicitly]
		public List<HtmlExceptionCategoryCount> ExceptionCategories
			=> _htmlExceptionCategoryCounts ??
			   (_htmlExceptionCategoryCounts = AggregateExceptionCategories());

		[UsedImplicitly]
		public int ExceptionCategoryCount => ExceptionCategories.Count;

		[UsedImplicitly]
		public int IssueCount
		{
			get { return _qualityConditions.Values.Sum(q => q.IssueCount); }
		}

		[UsedImplicitly]
		public int IssueCountWithChildren
		{
			get { return IssueCount + _subCategories.Sum(c => c.IssueCountWithChildren); }
		}

		[NotNull]
		[UsedImplicitly]
		public List<HtmlReportIssueGroup> IssueGroups
		{
			get
			{
				if (_issueGroupsRequireSorting)
				{
					_issueGroups.Sort(_issueGroupComparer);
				}

				return _issueGroups;
			}
		}

		[NotNull]
		[UsedImplicitly]
		public List<HtmlReportQualityCondition> QualityConditions
		{
			get
			{
				return _qualityConditions.Values
				                         .OrderBy(q => q, _qualityConditionComparer)
				                         .ToList();
			}
		}

		public void IncludeSubCategory(
			[NotNull] HtmlReportDataQualityCategory reportCategory)
		{
			Assert.ArgumentNotNull(reportCategory, nameof(reportCategory));

			_htmlExceptionCategoryCounts = null;

			_subCategories.Add(reportCategory);
		}

		public void AddIssueGroup([NotNull] HtmlReportIssueGroup issueGroup, int issueCount)
		{
			Assert.ArgumentNotNull(issueGroup, nameof(issueGroup));

			_htmlExceptionCategoryCounts = null;

			_issueGroups.Add(issueGroup);
			_issueGroupsRequireSorting = true;

			Include(issueGroup.QualityCondition,
			        issueGroup.IsAllowable,
			        issueGroup.StopCondition,
			        issueCount,
			        issueGroup.ExceptionCategories);
		}

		public bool Equals(HtmlReportDataQualityCategory other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return Equals(other._uniqueName, _uniqueName);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != typeof(HtmlReportDataQualityCategory))
			{
				return false;
			}

			return Equals((HtmlReportDataQualityCategory) obj);
		}

		public override int GetHashCode()
		{
			return _uniqueName.GetHashCode();
		}

		public override string ToString()
		{
			return $"QualifiedName: {QualifiedName}";
		}

		[NotNull]
		private List<HtmlExceptionCategoryCount> AggregateExceptionCategories()
		{
			var all = new List<HtmlExceptionCategoryCount>();

			all.AddRange(_issueGroups.SelectMany(ig => ig.ExceptionCategories));
			all.AddRange(_subCategories.SelectMany(sc => sc.ExceptionCategories));

			return HtmlReportUtils.AggregateExceptionCategoryCounts(all, _exceptionCategories);
		}

		private void Include(
			[NotNull] QualityCondition qualityCondition,
			bool isAllowable,
			bool stopCondition,
			int issueCount,
			[NotNull] IEnumerable<HtmlExceptionCategoryCount> exceptionCategoryCounts)
		{
			HtmlReportQualityCondition reportCondition;
			if (! _qualityConditions.TryGetValue(qualityCondition, out reportCondition))
			{
				reportCondition = new HtmlReportQualityCondition(qualityCondition,
				                                                 _exceptionCategories,
				                                                 isAllowable,
				                                                 stopCondition);
				_qualityConditions.Add(qualityCondition, reportCondition);
			}

			reportCondition.IssueCount += issueCount;
			reportCondition.IncludeExceptions(exceptionCategoryCounts);
		}

		[NotNull]
		private static string GetDisplayName(
			[NotNull] DataQualityCategory category,
			[CanBeNull] HtmlDataQualityCategoryOptions options)
		{
			if (options == null)
			{
				return category.Name;
			}

			string aliasName = options.AliasName;

			return StringUtils.IsNotEmpty(aliasName)
				       ? aliasName
				       : category.Name;
		}
	}
}
