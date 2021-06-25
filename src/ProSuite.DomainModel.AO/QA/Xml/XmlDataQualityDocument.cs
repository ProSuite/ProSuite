using System.Collections.Generic;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.QA.Xml
{
	[XmlRoot("DataQuality"
	         , Namespace = "urn:EsriDE.ProSuite.QA.QualitySpecifications-3.0"
						 //TODO: allow 
//	         , Namespace = "urn:EsriDE.ProSuite.QA.QualitySpecifications-2.0"
	)]
	public class XmlDataQualityDocument
	{
		[XmlArrayItem("QualitySpecification")]
		[CanBeNull]
		public List<XmlQualitySpecification> QualitySpecifications { get; set; }

		[XmlArrayItem(ElementName = "QualityCondition")]
		public List<XmlQualityCondition> QualityConditions { get; set; }

		[XmlArrayItem(ElementName = "IssueFilter")]
		[CanBeNull]
		public List<XmlIssueFilterConfiguration> IssueFilters { get; set; }

		[XmlArrayItem(ElementName = "RowFilter")]
		[CanBeNull]
		public List<XmlRowFilterConfiguration> RowFilters { get; set; }

		[XmlArrayItem(ElementName = "Transformer")]
		[CanBeNull]
		public List<XmlTransformerConfiguration> Transformers { get; set; }

		[XmlArrayItem("Category")]
		[CanBeNull]
		public List<XmlDataQualityCategory> Categories { get; set; }

		[XmlArrayItem("TestDescriptor")]
		[CanBeNull]
		public List<XmlTestDescriptor> TestDescriptors { get; set; }

		[XmlArrayItem("Workspace")]
		[CanBeNull]
		public List<XmlWorkspace> Workspaces { get; set; }

		public void AddWorkspace([NotNull] XmlWorkspace xmlWorkspace)
		{
			Assert.ArgumentNotNull(xmlWorkspace, nameof(xmlWorkspace));

			if (Workspaces == null)
			{
				Workspaces = new List<XmlWorkspace>();
			}

			Workspaces.Add(xmlWorkspace);
		}

		public void AddCategory([NotNull] XmlDataQualityCategory xmlCategory)
		{
			Assert.ArgumentNotNull(xmlCategory, nameof(xmlCategory));

			if (Categories == null)
			{
				Categories = new List<XmlDataQualityCategory>();
			}

			Categories.Add(xmlCategory);
		}

		public void AddQualitySpecification(
			[NotNull] XmlQualitySpecification xmlQualitySpecification)
		{
			Assert.ArgumentNotNull(xmlQualitySpecification, nameof(xmlQualitySpecification));

			if (QualitySpecifications == null)
			{
				QualitySpecifications = new List<XmlQualitySpecification>();
			}

			QualitySpecifications.Add(xmlQualitySpecification);
		}

		public void AddQualityCondition([NotNull] XmlQualityCondition xmlQualityCondition)
		{
			Assert.ArgumentNotNull(xmlQualityCondition, nameof(xmlQualityCondition));
			QualityConditions = QualityConditions ?? new List<XmlQualityCondition>();
			QualityConditions.Add(xmlQualityCondition);
		}

		public void AddIssueFilter([NotNull] XmlIssueFilterConfiguration xmlIssueFilter)
		{
			Assert.ArgumentNotNull(xmlIssueFilter, nameof(xmlIssueFilter));
			IssueFilters = IssueFilters ?? new List<XmlIssueFilterConfiguration>();
			IssueFilters.Add(xmlIssueFilter);
		}

		public void AddRowFilter([NotNull] XmlRowFilterConfiguration xmlRowFilter)
		{
			Assert.ArgumentNotNull(xmlRowFilter, nameof(xmlRowFilter));
			RowFilters = RowFilters ?? new List<XmlRowFilterConfiguration>();
			RowFilters.Add(xmlRowFilter);
		}
		public void AddTransformer([NotNull] XmlTransformerConfiguration xmlTransformer)
		{
			Assert.ArgumentNotNull(xmlTransformer, nameof(xmlTransformer));
			Transformers = Transformers ?? new List<XmlTransformerConfiguration>();
			Transformers.Add(xmlTransformer);
		}

		public void AddTestDescriptor([NotNull] XmlTestDescriptor xmlTestDescriptor)
		{
			Assert.ArgumentNotNull(xmlTestDescriptor, nameof(xmlTestDescriptor));

			if (TestDescriptors == null)
			{
				TestDescriptors = new List<XmlTestDescriptor>();
			}

			TestDescriptors.Add(xmlTestDescriptor);
		}

		[NotNull]
		public IEnumerable<XmlDataQualityCategory> GetAllCategories()
		{
			if (Categories == null)
			{
				yield break;
			}

			foreach (XmlDataQualityCategory category in GetCategories(Categories))
			{
				yield return category;
			}
		}

		[NotNull]
		public IEnumerable<KeyValuePair<XmlQualitySpecification, XmlDataQualityCategory>>
			GetAllQualitySpecifications()
		{
			if (QualitySpecifications != null)
			{
				foreach (XmlQualitySpecification qs in QualitySpecifications)
				{
					yield return new KeyValuePair<XmlQualitySpecification, XmlDataQualityCategory>(
						qs, null);
				}
			}

			if (Categories != null)
			{
				foreach (XmlDataQualityCategory category in Categories)
				{
					foreach (
						KeyValuePair<XmlQualitySpecification, XmlDataQualityCategory> pair in
						GetQualitySpecifications(category))
					{
						yield return pair;
					}
				}
			}
		}

		[NotNull]
		public IEnumerable<KeyValuePair<XmlQualityCondition, XmlDataQualityCategory>>
			GetAllQualityConditions()
		{
			if (QualityConditions != null)
			{
				foreach (XmlQualityCondition qc in QualityConditions)
				{
					yield return new KeyValuePair<XmlQualityCondition, XmlDataQualityCategory>(
						qc, null);
				}
			}

			if (Categories != null)
			{
				foreach (XmlDataQualityCategory category in Categories)
				{
					foreach (
						KeyValuePair<XmlQualityCondition, XmlDataQualityCategory> pair in
						GetQualityConditions(category))
					{
						yield return pair;
					}
				}
			}
		}

		[NotNull]
		private IEnumerable<XmlDataQualityCategory> GetCategories(
			[NotNull] IEnumerable<XmlDataQualityCategory> categories)
		{
			foreach (XmlDataQualityCategory category in categories)
			{
				yield return category;

				if (category.SubCategories == null)
				{
					continue;
				}

				foreach (
					XmlDataQualityCategory subCategory in GetCategories(category.SubCategories))
				{
					yield return subCategory;
				}
			}
		}

		[NotNull]
		private IEnumerable<KeyValuePair<XmlQualitySpecification, XmlDataQualityCategory>>
			GetQualitySpecifications(
				[NotNull] XmlDataQualityCategory category)
		{
			if (category.QualitySpecifications != null)
			{
				foreach (XmlQualitySpecification qs in category.QualitySpecifications)
				{
					yield return new KeyValuePair<XmlQualitySpecification, XmlDataQualityCategory>(
						qs, category);
				}
			}

			if (category.SubCategories != null)
			{
				foreach (XmlDataQualityCategory subCategory in category.SubCategories)
				{
					foreach (
						KeyValuePair<XmlQualitySpecification, XmlDataQualityCategory> pair in
						GetQualitySpecifications(subCategory))
					{
						yield return pair;
					}
				}
			}
		}

		[NotNull]
		private IEnumerable<KeyValuePair<XmlQualityCondition, XmlDataQualityCategory>>
			GetQualityConditions(
				[NotNull] XmlDataQualityCategory category)
		{
			if (category.QualityConditions != null)
			{
				foreach (XmlQualityCondition qc in category.QualityConditions)
				{
					yield return new KeyValuePair<XmlQualityCondition, XmlDataQualityCategory>(
						qc, category);
				}
			}

			if (category.SubCategories != null)
			{
				foreach (XmlDataQualityCategory subCategory in category.SubCategories)
				{
					foreach (
						KeyValuePair<XmlQualityCondition, XmlDataQualityCategory> pair in
						GetQualityConditions(subCategory))
					{
						yield return pair;
					}
				}
			}
		}
	}
}
