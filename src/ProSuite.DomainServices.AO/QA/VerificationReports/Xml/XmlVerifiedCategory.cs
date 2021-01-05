using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.VerificationReports.Xml
{
	public class XmlVerifiedCategory : IComparable<XmlVerifiedCategory>
	{
		[NotNull] private readonly List<XmlVerifiedQualityCondition> _conditions =
			new List<XmlVerifiedQualityCondition>();

		[CanBeNull] private List<XmlVerifiedCategory> _subCategories;
		[CanBeNull] private XmlVerifiedCategory _parentCategory;
		private bool _conditionSortOrderDirty;
		private bool _subCategorySortOrderDirty;
		private readonly int _listOrder;

		[UsedImplicitly]
		public XmlVerifiedCategory() { }

		public XmlVerifiedCategory([NotNull] string name,
		                           [CanBeNull] string abbreviation = null,
		                           [CanBeNull] string uuid = null,
		                           [CanBeNull] string description = null,
		                           int listOrder = int.MaxValue)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			Name = name;
			Description = description;
			Abbreviation = abbreviation;
			Uuid = uuid;

			_listOrder = listOrder;
		}

		[XmlAttribute("abbreviation")]
		[DefaultValue(null)]
		public string Abbreviation { get; set; }

		[XmlAttribute("name")]
		public string Name { get; set; }

		[XmlAttribute("uuid")]
		public string Uuid { get; set; }

		[XmlAttribute("conditionCount")]
		public int ConditionCount { get; set; }

		[XmlAttribute("errorCount")]
		public int ErrorCount { get; set; }

		[XmlAttribute("warningCount")]
		public int WarningCount { get; set; }

		[XmlAttribute("exceptionCount")]
		[DefaultValue(0)]
		public int ExceptionCount { get; set; }

		[XmlAttribute("conditionCountWithChildren")]
		public int ConditionCountWithChildren { get; set; }

		[XmlAttribute("errorCountWithChildren")]
		public int ErrorCountWithChildren { get; set; }

		[XmlAttribute("warningCountWithChildren")]
		public int WarningCountWithChildren { get; set; }

		[XmlAttribute("exceptionCountWithChildren")]
		[DefaultValue(0)]
		public int ExceptionCountWithChildren { get; set; }

		[XmlElement("Description")]
		public string Description { get; set; }

		[XmlArray("QualityConditions")]
		[XmlArrayItem("Condition")]
		[NotNull]
		public List<XmlVerifiedQualityCondition> Conditions
		{
			get
			{
				if (_conditionSortOrderDirty)
				{
					_conditions.Sort((c1, c2) => string.CompareOrdinal(c1.Name, c2.Name));
					_conditionSortOrderDirty = false;
				}

				return _conditions;
			}
		}

		[XmlArray("SubCategories")]
		[XmlArrayItem("Category")]
		[CanBeNull]
		public List<XmlVerifiedCategory> SubCategories
		{
			get
			{
				if (_subCategories != null && _subCategorySortOrderDirty)
				{
					_subCategories.Sort();
					_subCategorySortOrderDirty = false;
				}

				return _subCategories;
			}
		}

		public void AddCondition([NotNull] XmlVerifiedQualityCondition condition)
		{
			Assert.ArgumentNotNull(condition, nameof(condition));

			_conditions.Add(condition);
			_conditionSortOrderDirty = true;

			ConditionCount = _conditions.Count;
			ExceptionCount += condition.ExceptionCount;

			switch (condition.Type)
			{
				case XmlQualityConditionType.Soft:
					WarningCount += condition.IssueCount;
					break;

				case XmlQualityConditionType.Hard:
					ErrorCount += condition.IssueCount;
					break;

				default:
					throw new ArgumentOutOfRangeException(
						string.Format("Condition '{0}' has an unsupported type: {1}",
						              condition.Name, condition.Type));
			}

			UpdateCountsWithChildren();
		}

		private void UpdateCountsWithChildren()
		{
			List<XmlVerifiedCategory> subCategories = SubCategories;
			if (subCategories == null)
			{
				ConditionCountWithChildren = ConditionCount;
				ExceptionCountWithChildren = ExceptionCount;
				ErrorCountWithChildren = ErrorCount;
				WarningCountWithChildren = WarningCount;
			}
			else
			{
				ConditionCountWithChildren = ConditionCount +
				                             subCategories.Sum(c => c.ConditionCountWithChildren);
				ExceptionCountWithChildren = ExceptionCount +
				                             subCategories.Sum(c => c.ExceptionCountWithChildren);
				ErrorCountWithChildren = ErrorCount +
				                         subCategories.Sum(c => c.ErrorCountWithChildren);
				WarningCountWithChildren = WarningCount +
				                           subCategories.Sum(c => c.WarningCountWithChildren);
			}

			if (_parentCategory != null)
			{
				_parentCategory.UpdateCountsWithChildren();
			}
		}

		public void AddSubCategory([NotNull] XmlVerifiedCategory xmlCategory)
		{
			if (_subCategories == null)
			{
				_subCategories = new List<XmlVerifiedCategory>();
			}

			xmlCategory._parentCategory = this;

			_subCategories.Add(xmlCategory);
			_subCategorySortOrderDirty = true;
		}

		int IComparable<XmlVerifiedCategory>.CompareTo(XmlVerifiedCategory other)
		{
			int listOrderComparison = _listOrder.CompareTo(other._listOrder);

			return listOrderComparison != 0
				       ? listOrderComparison
				       : string.Compare(Name, other.Name,
				                        StringComparison.InvariantCulture);
		}
	}
}
