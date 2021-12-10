using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Html;

namespace ProSuite.DomainModel.AO.QA.SpecificationReport
{
	public class HtmlDataQualityCategory :
		IEquatable<HtmlDataQualityCategory>
	{
		[CanBeNull] private readonly DataQualityCategory _category;

		[NotNull] private readonly HtmlDataQualityCategoryComparer _categoryComparer;
		[NotNull] private readonly HtmlQualitySpecificationElementComparer _elementComparer;

		[NotNull] private readonly string _name;
		[NotNull] private readonly string _abbreviation;
		[NotNull] private readonly string _uniqueName;
		private readonly bool _isUndefinedCategory;

		[NotNull] private readonly HashSet<HtmlDataQualityCategory> _subCategories =
			new HashSet<HtmlDataQualityCategory>();

		[CanBeNull] private HtmlDataQualityCategory _parentCategory;

		[NotNull] private readonly List<HtmlQualitySpecificationElement>
			_elements = new List<HtmlQualitySpecificationElement>();

		internal HtmlDataQualityCategory(
			[CanBeNull] DataQualityCategory category,
			[CanBeNull] HtmlDataQualityCategoryOptions options,
			[NotNull] HtmlDataQualityCategoryComparer categoryComparer,
			[NotNull] HtmlQualitySpecificationElementComparer elementComparer)
		{
			Assert.ArgumentNotNull(categoryComparer, nameof(categoryComparer));
			Assert.ArgumentNotNull(elementComparer, nameof(elementComparer));

			_category = category;
			_categoryComparer = categoryComparer;
			_elementComparer = elementComparer;

			if (category == null)
			{
				_isUndefinedCategory = true;
				_uniqueName = "<nocategory>";

				_name = string.Empty;
				_abbreviation = string.Empty;
			}
			else
			{
				_isUndefinedCategory = false;
				_uniqueName = category.GetQualifiedName("||");

				_name = GetDisplayName(category, options);
				_abbreviation = category.Abbreviation ?? string.Empty;
			}
		}

		public bool IsRoot
		{
			get { return _parentCategory == null; }
		}

		[CanBeNull]
		public HtmlDataQualityCategory ParentCategory
		{
			get { return _parentCategory; }
			set { _parentCategory = value; }
		}

		[UsedImplicitly]
		public int Level
		{
			get
			{
				var count = 0;

				if (_parentCategory != null)
				{
					count = _parentCategory.Level + 1;
				}

				return count;
			}
		}

		[NotNull]
		public string Name
		{
			get { return _name; }
		}

		[NotNull]
		[UsedImplicitly]
		public string Abbreviation
		{
			get { return _abbreviation; }
		}

		[NotNull]
		[UsedImplicitly]
		public string QualifiedName
		{
			get
			{
				return _isUndefinedCategory
					       ? string.Empty
					       : SpecificationReportUtils.GetQualifiedText(this, c => c.Name);
			}
		}

		[NotNull]
		[UsedImplicitly]
		public string QualifiedAbbreviation
		{
			get
			{
				return _isUndefinedCategory
					       ? string.Empty
					       : SpecificationReportUtils.GetQualifiedText(
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
				return _isUndefinedCategory
					       ? string.Empty
					       : SpecificationReportUtils.GetQualifiedText(
						       this,
						       c => StringUtils.IsNullOrEmptyOrBlank(c.Abbreviation)
							            ? c.Name
							            : c.Abbreviation);
			}
		}

		public bool IsUndefinedCategory
		{
			get { return _isUndefinedCategory; }
		}

		public int ListOrder
		{
			get { return _category?.ListOrder ?? 0; }
		}

		[UsedImplicitly]
		public bool HasSubCategories
		{
			get { return _subCategories.Count > 0; }
		}

		[NotNull]
		[UsedImplicitly]
		public List<HtmlDataQualityCategory> SubCategories
		{
			get { return _subCategories.OrderBy(c => c, _categoryComparer).ToList(); }
		}

		[UsedImplicitly]
		public bool HasQualitySpecificationElements
		{
			get { return _elements.Count > 0; }
		}

		[NotNull]
		[UsedImplicitly]
		public List<HtmlQualitySpecificationElement> QualitySpecificationElements
		{
			get { return _elements.OrderBy(q => q, _elementComparer).ToList(); }
		}

		internal void IncludeSubCategory(
			[NotNull] HtmlDataQualityCategory reportCategory)
		{
			Assert.ArgumentNotNull(reportCategory, nameof(reportCategory));

			_subCategories.Add(reportCategory);
		}

		internal void AddQualitySpecificationElement(
			[NotNull] HtmlQualitySpecificationElement element)
		{
			Assert.ArgumentNotNull(element, nameof(element));

			_elements.Add(element);
		}

		public bool Equals(HtmlDataQualityCategory other)
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

			if (obj.GetType() != typeof(HtmlDataQualityCategory))
			{
				return false;
			}

			return Equals((HtmlDataQualityCategory) obj);
		}

		public override int GetHashCode()
		{
			return _uniqueName.GetHashCode();
		}

		public override string ToString()
		{
			return string.Format("QualifiedName: {0}", QualifiedName);
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
