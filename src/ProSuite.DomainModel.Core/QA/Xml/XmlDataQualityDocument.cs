using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.QA.Xml
{
	[XmlRoot("DataQuality", Namespace = "urn:ProSuite.QA.QualitySpecifications-3.0")]
	public class XmlDataQualityDocument30 : XmlDataQualityDocument { }

	[XmlRoot("DataQuality", Namespace = "urn:EsriDE.ProSuite.QA.QualitySpecifications-2.0")]
	public class XmlDataQualityDocument20 : XmlDataQualityDocument { }

	public class XmlDataQualityDocument
	{
		[XmlArrayItem("QualitySpecification")]
		[CanBeNull]
		public List<XmlQualitySpecification> QualitySpecifications { get; set; }

		[XmlArrayItem("QualityCondition")]
		[CanBeNull]
		public List<XmlQualityCondition> QualityConditions { get; set; }

		[XmlArrayItem("Transformer")]
		[CanBeNull]
		public List<XmlTransformerConfiguration> Transformers { get; set; }

		[XmlArrayItem("IssueFilter")]
		[CanBeNull]
		public List<XmlIssueFilterConfiguration> IssueFilters { get; set; }

		[XmlArrayItem("Category")]
		[CanBeNull]
		public List<XmlDataQualityCategory> Categories { get; set; }

		[XmlArrayItem("TestDescriptor")]
		[CanBeNull]
		public List<XmlTestDescriptor> TestDescriptors { get; set; }

		[XmlArrayItem("TransformerDescriptor")]
		[CanBeNull]
		public List<XmlTransformerDescriptor> TransformerDescriptors { get; set; }

		[XmlArrayItem("IssueFilterDescriptor")]
		[CanBeNull]
		public List<XmlIssueFilterDescriptor> IssueFilterDescriptors { get; set; }

		[XmlArrayItem("Workspace")]
		[CanBeNull]
		public List<XmlWorkspace> Workspaces { get; set; }

		public void AddWorkspace([NotNull] XmlWorkspace xmlWorkspace)
		{
			Assert.ArgumentNotNull(xmlWorkspace, nameof(xmlWorkspace));
			Workspaces = Workspaces ?? new List<XmlWorkspace>();
			Workspaces.Add(xmlWorkspace);
		}

		public void AddCategory([NotNull] XmlDataQualityCategory xmlCategory)
		{
			Assert.ArgumentNotNull(xmlCategory, nameof(xmlCategory));
			Categories = Categories ?? new List<XmlDataQualityCategory>();
			Categories.Add(xmlCategory);
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

		public void AddTestDescriptor([NotNull] XmlTestDescriptor xmlTestDescriptor)
		{
			Assert.ArgumentNotNull(xmlTestDescriptor, nameof(xmlTestDescriptor));
			TestDescriptors = TestDescriptors ?? new List<XmlTestDescriptor>();
			TestDescriptors.Add(xmlTestDescriptor);
		}

		public void AddTransformerDescriptor(
			[NotNull] XmlTransformerDescriptor xmlTransformerDescriptor)
		{
			Assert.ArgumentNotNull(xmlTransformerDescriptor, nameof(xmlTransformerDescriptor));
			TransformerDescriptors = TransformerDescriptors ?? new List<XmlTransformerDescriptor>();
			TransformerDescriptors.Add(xmlTransformerDescriptor);
		}

		public void AddIssueFilterDescriptor(
			[NotNull] XmlIssueFilterDescriptor xmlIssueFilterDescriptor)
		{
			Assert.ArgumentNotNull(xmlIssueFilterDescriptor, nameof(xmlIssueFilterDescriptor));
			IssueFilterDescriptors = IssueFilterDescriptors ?? new List<XmlIssueFilterDescriptor>();
			IssueFilterDescriptors.Add(xmlIssueFilterDescriptor);
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

			if (Categories == null)
			{
				yield break;
			}

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

			if (Categories == null)
			{
				yield break;
			}

			foreach (XmlDataQualityCategory category in Categories)
			{
				foreach (
					KeyValuePair<XmlQualityCondition, XmlDataQualityCategory> pair in
					GetInstanceConfigurations<XmlQualityCondition>(category))
				{
					yield return pair;
				}
			}
		}

		[NotNull]
		public IEnumerable<KeyValuePair<XmlTransformerConfiguration, XmlDataQualityCategory>>
			GetAllTransformers()
		{
			if (Transformers != null)
			{
				foreach (XmlTransformerConfiguration tr in Transformers)
				{
					yield return
						new KeyValuePair<XmlTransformerConfiguration, XmlDataQualityCategory>(
							tr, null);
				}
			}

			if (Categories == null)
			{
				yield break;
			}

			foreach (XmlDataQualityCategory category in Categories)
			{
				foreach (
					KeyValuePair<XmlTransformerConfiguration, XmlDataQualityCategory> pair in
					GetInstanceConfigurations<XmlTransformerConfiguration>(category))
				{
					yield return pair;
				}
			}
		}

		[NotNull]
		public IEnumerable<KeyValuePair<XmlIssueFilterConfiguration, XmlDataQualityCategory>>
			GetAllIssueFilters()
		{
			if (IssueFilters != null)
			{
				foreach (XmlIssueFilterConfiguration iF in IssueFilters)
				{
					yield return new KeyValuePair<XmlIssueFilterConfiguration,
						XmlDataQualityCategory>(iF, null);
				}
			}

			if (Categories == null)
			{
				yield break;
			}

			foreach (XmlDataQualityCategory category in Categories)
			{
				foreach (
					KeyValuePair<XmlIssueFilterConfiguration, XmlDataQualityCategory> pair in
					GetInstanceConfigurations<XmlIssueFilterConfiguration>(category))
				{
					yield return pair;
				}
			}
		}

		[NotNull]
		public IEnumerable<XmlInstanceDescriptor> GetAllInstanceDescriptors()
		{
			if (TestDescriptors != null)
			{
				foreach (var descriptor in TestDescriptors)
				{
					yield return descriptor;
				}
			}

			if (TransformerDescriptors != null)
			{
				foreach (var descriptor in TransformerDescriptors)
				{
					yield return descriptor;
				}
			}

			if (IssueFilterDescriptors != null)
			{
				foreach (var descriptor in IssueFilterDescriptors)
				{
					yield return descriptor;
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
			GetQualitySpecifications([NotNull] XmlDataQualityCategory category)
		{
			if (category.QualitySpecifications != null)
			{
				foreach (XmlQualitySpecification qs in category.QualitySpecifications)
				{
					yield return new KeyValuePair<XmlQualitySpecification, XmlDataQualityCategory>(
						qs, category);
				}
			}

			if (category.SubCategories == null)
			{
				yield break;
			}

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

		[NotNull]
		private IEnumerable<KeyValuePair<T, XmlDataQualityCategory>>
			GetInstanceConfigurations<T>([NotNull] XmlDataQualityCategory category)
			where T : XmlInstanceConfiguration
		{
			foreach (T config in category.GetInstanceConfigurations().OfType<T>())
			{
				yield return new KeyValuePair<T, XmlDataQualityCategory>(config, category);
			}

			if (category.SubCategories == null)
			{
				yield break;
			}

			foreach (XmlDataQualityCategory subCategory in category.SubCategories)
			{
				foreach (KeyValuePair<T, XmlDataQualityCategory> pair in
				         GetInstanceConfigurations<T>(subCategory))
				{
					yield return pair;
				}
			}
		}
	}
}
