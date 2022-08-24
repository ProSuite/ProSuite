using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.QA.Xml
{
	public class XmlDataQualityCategory : IXmlEntityMetadata
	{
		[CanBeNull] private string _description;

		public XmlDataQualityCategory()
		{
			CanContainQualityConditions = true;
			CanContainQualitySpecifications = true;
			CanContainSubCategories = true;
		}

		[XmlAttribute("name")]
		public string Name { get; set; }

		[XmlAttribute("abbreviation")]
		[CanBeNull]
		public string Abbreviation { get; set; }

		[CanBeNull]
		[XmlAttribute("uuid")]
		public string Uuid { get; set; }

		[XmlAttribute("listOrder")]
		[DefaultValue(0)]
		public int ListOrder { get; set; }

		[XmlAttribute("defaultModelName")]
		[CanBeNull]
		public string DefaultModelName { get; set; }

		[XmlAttribute("canContainQualityConditions")]
		[DefaultValue(true)]
		public bool CanContainQualityConditions { get; set; }

		[XmlAttribute("canContainQualitySpecifications")]
		[DefaultValue(true)]
		public bool CanContainQualitySpecifications { get; set; }

		[XmlAttribute("canContainSubCategories")]
		[DefaultValue(true)]
		public bool CanContainSubCategories { get; set; }

		[XmlElement("Description")]
		[DefaultValue(null)]
		public string Description
		{
			get => string.IsNullOrEmpty(_description)
				       ? null
				       : _description;
			set => _description = value;
		}

		[XmlArray("SubCategories")]
		[XmlArrayItem("Category")]
		[CanBeNull]
		public List<XmlDataQualityCategory> SubCategories { get; set; }

		[XmlArray("QualitySpecifications")]
		[XmlArrayItem("QualitySpecification")]
		[CanBeNull]
		public List<XmlQualitySpecification> QualitySpecifications { get; set; }

		[XmlArray("QualityConditions")]
		[XmlArrayItem("QualityCondition")]
		[CanBeNull]
		public List<XmlQualityCondition> QualityConditions { get; set; }

		[XmlAttribute("createdDate")]
		public string CreatedDate { get; set; }

		[XmlAttribute("createdByUser")]
		public string CreatedByUser { get; set; }

		[XmlAttribute("lastChangedDate")]
		public string LastChangedDate { get; set; }

		[XmlAttribute("lastChangedByUser")]
		public string LastChangedByUser { get; set; }

		[XmlIgnore]
		public bool ContainsQualityConditions
		{
			get
			{
				if (QualityConditions != null && QualityConditions.Count > 0)
				{
					return true;
				}

				if (SubCategories != null)
				{
					foreach (XmlDataQualityCategory subCategory in SubCategories)
					{
						if (subCategory.ContainsQualityConditions)
						{
							return true;
						}
					}
				}

				return false;
			}
		}

		[XmlIgnore]
		public bool ContainsQualitySpecifications
		{
			get
			{
				if (QualitySpecifications != null && QualitySpecifications.Count > 0)
				{
					return true;
				}

				if (SubCategories != null)
				{
					foreach (XmlDataQualityCategory subCategory in SubCategories)
					{
						if (subCategory.ContainsQualitySpecifications)
						{
							return true;
						}
					}
				}

				return false;
			}
		}

		public void AddSubCategory([NotNull] XmlDataQualityCategory xmlSubCategory)
		{
			if (SubCategories == null)
			{
				SubCategories = new List<XmlDataQualityCategory>();
			}

			SubCategories.Add(xmlSubCategory);
		}

		public void AddQualitySpecification(XmlQualitySpecification xmlQualitySpecification)
		{
			if (QualitySpecifications == null)
			{
				QualitySpecifications = new List<XmlQualitySpecification>();
			}

			QualitySpecifications.Add(xmlQualitySpecification);
		}

		public void AddQualityCondition(XmlQualityCondition xmlQualityCondition)
		{
			if (QualityConditions == null)
			{
				QualityConditions = new List<XmlQualityCondition>();
			}

			QualityConditions.Add(xmlQualityCondition);
		}
	}
}
