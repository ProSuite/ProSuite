using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.QA.Xml
{
	public class XmlDataQualityCategory : IXmlEntityMetadata
	{
		[CanBeNull] private string _description;
		private const bool _defaultCanContainQualityConditions = true;
		private const bool _defaultCanContainQualitySpecifications = true;
		private const bool _defaultCanContainSubCategories = true;

		[XmlAttribute("name")]
		public string Name { get; set; }

		[CanBeNull]
		[XmlAttribute("abbreviation")]
		public string Abbreviation { get; set; }

		[CanBeNull]
		[XmlAttribute("uuid")]
		public string Uuid { get; set; }

		[XmlAttribute("listOrder")]
		[DefaultValue(0)]
		public int ListOrder { get; set; }

		[CanBeNull]
		[XmlAttribute("defaultModelName")]
		public string DefaultModelName { get; set; }

		[XmlAttribute("canContainQualityConditions")]
		[DefaultValue(_defaultCanContainQualityConditions)]
		public bool CanContainQualityConditions { get; set; } =
			_defaultCanContainQualityConditions;

		[XmlAttribute("canContainQualitySpecifications")]
		[DefaultValue(_defaultCanContainQualitySpecifications)]
		public bool CanContainQualitySpecifications { get; set; } =
			_defaultCanContainQualitySpecifications;

		[XmlAttribute("canContainSubCategories")]
		[DefaultValue(_defaultCanContainSubCategories)]
		public bool CanContainSubCategories { get; set; } =
			_defaultCanContainSubCategories;

		[CanBeNull]
		[XmlElement("Description")]
		[DefaultValue(null)]
		public string Description
		{
			get => string.IsNullOrEmpty(_description)
				       ? null
				       : _description;
			set => _description = value;
		}

		[CanBeNull]
		[XmlArray("SubCategories")]
		[XmlArrayItem("Category")]
		public List<XmlDataQualityCategory> SubCategories { get; set; }

		[CanBeNull]
		[XmlArray("QualitySpecifications")]
		[XmlArrayItem("QualitySpecification")]
		public List<XmlQualitySpecification> QualitySpecifications { get; set; }

		[CanBeNull]
		[XmlArray("QualityConditions")]
		[XmlArrayItem("QualityCondition")]
		public List<XmlQualityCondition> QualityConditions { get; set; }

		[CanBeNull]
		[XmlArray("Transformers")]
		[XmlArrayItem("Transformer")]
		public List<XmlTransformerConfiguration> Transformers { get; set; }

		[CanBeNull]
		[XmlArray("IssueFilters")]
		[XmlArrayItem("IssueFilter")]
		public List<XmlIssueFilterConfiguration> IssueFilters { get; set; }

		[XmlAttribute("createdDate")]
		public string CreatedDate { get; set; }

		[XmlAttribute("createdByUser")]
		public string CreatedByUser { get; set; }

		[XmlAttribute("lastChangedDate")]
		public string LastChangedDate { get; set; }

		[XmlAttribute("lastChangedByUser")]
		public string LastChangedByUser { get; set; }

		[XmlIgnore]
		public bool IsNotEmpty
		{
			get
			{
				if (QualitySpecifications != null && QualitySpecifications.Count > 0 ||
				    QualityConditions != null && QualityConditions.Count > 0 ||
				    Transformers != null && Transformers.Count > 0 ||
				    IssueFilters != null && IssueFilters.Count > 0)
				{
					return true;
				}

				return SubCategories != null && SubCategoriesAreNotEmpty(SubCategories);
			}
		}

		private static bool SubCategoriesAreNotEmpty(
			[NotNull] IEnumerable<XmlDataQualityCategory> categories)
		{
			foreach (XmlDataQualityCategory category in categories)
			{
				if (category.QualitySpecifications != null &&
				    category.QualitySpecifications.Count > 0 ||
				    category.QualityConditions != null && category.QualityConditions.Count > 0 ||
				    category.Transformers != null && category.Transformers.Count > 0 ||
				    category.IssueFilters != null && category.IssueFilters.Count > 0)
				{
					return true;
				}

				if (category.SubCategories != null)
				{
					return SubCategoriesAreNotEmpty(category.SubCategories);
				}
			}

			return false;
		}

		[NotNull]
		public IEnumerable<XmlInstanceConfiguration> GetInstanceConfigurations()
		{
			if (QualityConditions != null)
			{
				foreach (var instanceConfig in QualityConditions)
				{
					yield return instanceConfig;
				}
			}

			if (Transformers != null)
			{
				foreach (var instanceConfig in Transformers)
				{
					yield return instanceConfig;
				}
			}

			if (IssueFilters != null)
			{
				foreach (var instanceConfig in IssueFilters)
				{
					yield return instanceConfig;
				}
			}
		}

		public void AddSubCategory([NotNull] XmlDataQualityCategory xmlSubCategory)
		{
			Assert.ArgumentNotNull(xmlSubCategory, nameof(xmlSubCategory));
			SubCategories = SubCategories ?? new List<XmlDataQualityCategory>();
			SubCategories.Add(xmlSubCategory);
		}

		public void AddQualitySpecification(
			[NotNull] XmlQualitySpecification xmlQualitySpecification)
		{
			Assert.ArgumentNotNull(xmlQualitySpecification, nameof(xmlQualitySpecification));
			QualitySpecifications = QualitySpecifications ?? new List<XmlQualitySpecification>();
			QualitySpecifications.Add(xmlQualitySpecification);
		}

		public void AddQualityCondition([NotNull] XmlQualityCondition xmlQualityCondition)
		{
			Assert.ArgumentNotNull(xmlQualityCondition, nameof(xmlQualityCondition));
			QualityConditions = QualityConditions ?? new List<XmlQualityCondition>();
			QualityConditions.Add(xmlQualityCondition);
		}

		public void AddTransformer([NotNull] XmlTransformerConfiguration xmlTransformer)
		{
			Assert.ArgumentNotNull(xmlTransformer, nameof(xmlTransformer));
			Transformers = Transformers ?? new List<XmlTransformerConfiguration>();
			Transformers.Add(xmlTransformer);
		}

		public void AddIssueFilter([NotNull] XmlIssueFilterConfiguration xmlIssueFilter)
		{
			Assert.ArgumentNotNull(xmlIssueFilter, nameof(xmlIssueFilter));
			IssueFilters = IssueFilters ?? new List<XmlIssueFilterConfiguration>();
			IssueFilters.Add(xmlIssueFilter);
		}
	}
}
