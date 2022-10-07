using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProSuite.Commons.DotLiquid;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.IO;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Html;

namespace ProSuite.DomainModel.AO.QA.SpecificationReport
{
	public static class SpecificationReportUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull]
		public static string RenderHtmlQualitySpecification(
			[NotNull] HtmlQualitySpecification qualitySpecification,
			[NotNull] string templateFilePath,
			[NotNull] string reportFilePath,
			bool throwTemplateErrors = false)
		{
			Assert.ArgumentNotNull(qualitySpecification, nameof(qualitySpecification));
			Assert.ArgumentNotNullOrEmpty(templateFilePath, nameof(templateFilePath));
			Assert.ArgumentNotNullOrEmpty(reportFilePath, nameof(reportFilePath));
			Assert.ArgumentCondition(File.Exists(templateFilePath),
			                         "template file does not exist: {0}",
			                         (object) templateFilePath);

			_msg.DebugFormat(
				"Rendering quality specification documentation based on template {0}",
				templateFilePath);

			LiquidUtils.RegisterSafeType<HtmlQualitySpecification>();
			LiquidUtils.RegisterSafeType<HtmlTexts>();

			const string rootName = "specification";

			string output = LiquidUtils.Render(
				templateFilePath,
				throwTemplateErrors,
				new KeyValuePair<string, object>(rootName, qualitySpecification),
				new KeyValuePair<string, object>("text", new HtmlTexts()));

			_msg.DebugFormat("Writing quality specification report to {0}", reportFilePath);

			FileSystemUtils.WriteTextFile(output, reportFilePath);

			_msg.InfoFormat("Quality specification report written to {0}", reportFilePath);
			return reportFilePath;
		}

		[NotNull]
		public static HtmlQualitySpecification CreateHtmlQualitySpecification(
			[NotNull] QualitySpecification qualitySpecification,
			[CanBeNull] IHtmlDataQualityCategoryOptionsProvider optionsProvider,
			bool showQualityConditionUuids = true)
		{
			Assert.ArgumentNotNull(qualitySpecification, nameof(qualitySpecification));

			IDictionary<TestDescriptor, HtmlTestDescriptor> htmlTestDescriptors =
				GetHtmlTestDescriptors(qualitySpecification);

			List<HtmlQualitySpecificationElement> htmlElements;
			IEnumerable<HtmlDataQualityCategory> htmlCategories =
				GroupByCategories(qualitySpecification.Elements,
				                  htmlTestDescriptors,
				                  new HtmlDataQualityCategoryComparer(),
				                  new HtmlQualitySpecificationElementComparer(),
				                  optionsProvider, out htmlElements);

			IEnumerable<HtmlDataModel> dataModels = GetInvolvedDataModels(htmlElements);

			return new HtmlQualitySpecification(qualitySpecification,
			                                    htmlCategories,
			                                    htmlElements,
			                                    htmlTestDescriptors.Values
				                                    .OrderBy(t => t.Name),
			                                    dataModels,
			                                    DateTime.Now)
			       {
				       ShowQualityConditionUuids = showQualityConditionUuids
			       };
		}

		[NotNull]
		private static IEnumerable<HtmlDataModel> GetInvolvedDataModels(
			[NotNull] IEnumerable<HtmlQualitySpecificationElement> htmlElements)
		{
			var models = new Dictionary<DdxModel, HtmlDataModel>();

			foreach (HtmlQualitySpecificationElement element in htmlElements)
			{
				foreach (HtmlTestParameterValue parameterValue in
				         element.QualityCondition.ParameterValues)
				{
					if (! parameterValue.IsDatasetParameter)
					{
						continue;
					}

					Dataset dataset = parameterValue.DatasetValue;

					if (dataset != null)
					{
						DdxModel model = dataset.Model;
						if (! models.TryGetValue(model, out HtmlDataModel htmlDataModel))
						{
							htmlDataModel = new HtmlDataModel(model);
							models.Add(model, htmlDataModel);
						}

						HtmlDataset htmlDataset = htmlDataModel.GetHtmlDataset(dataset);

						htmlDataset.AddReference(new HtmlDatasetReference(element, parameterValue));
					}
				}
			}

			return models.Values.OrderBy(m => m.Name);
		}

		[NotNull]
		private static IDictionary<TestDescriptor, HtmlTestDescriptor>
			GetHtmlTestDescriptors([NotNull] QualitySpecification qualitySpecification)
		{
			Assert.ArgumentNotNull(qualitySpecification, nameof(qualitySpecification));

			var result = new Dictionary<TestDescriptor, HtmlTestDescriptor>();

			foreach (QualitySpecificationElement element in qualitySpecification.Elements)
			{
				TestDescriptor testDescriptor = element.QualityCondition.TestDescriptor;
				HtmlTestDescriptor htmlTestDescriptor;
				if (! result.TryGetValue(testDescriptor, out htmlTestDescriptor))
				{
					htmlTestDescriptor = new HtmlTestDescriptor(testDescriptor);
					result.Add(testDescriptor, htmlTestDescriptor);
				}
			}

			return result;
		}

		[NotNull]
		private static IEnumerable<HtmlDataQualityCategory> GroupByCategories(
			[NotNull] IEnumerable<QualitySpecificationElement> elements,
			[NotNull] IDictionary<TestDescriptor, HtmlTestDescriptor> testDescriptors,
			[NotNull] HtmlDataQualityCategoryComparer categoryComparer,
			[NotNull] HtmlQualitySpecificationElementComparer elementComparer,
			[CanBeNull] IHtmlDataQualityCategoryOptionsProvider optionsProvider,
			[NotNull] out List<HtmlQualitySpecificationElement>
				htmlQualitySpecificationElements)
		{
			List<QualitySpecificationElement> elementsList = elements.ToList();

			IDictionary<string, HtmlDataQualityCategory> reportCategories =
				MapReportCategories(elementsList,
				                    categoryComparer,
				                    elementComparer,
				                    optionsProvider);

			htmlQualitySpecificationElements = new List<HtmlQualitySpecificationElement>();

			foreach (QualitySpecificationElement element in elementsList)
			{
				HtmlDataQualityCategory reportCategory =
					reportCategories[GetCategoryKey(element.QualityCondition.Category)];

				HtmlTestDescriptor htmlTestDescriptor =
					testDescriptors[element.QualityCondition.TestDescriptor];

				var htmlQualityCondition = new HtmlQualityCondition(
					element.QualityCondition, htmlTestDescriptor, reportCategory);

				var htmlElement = new HtmlQualitySpecificationElement(htmlQualityCondition,
					element);

				reportCategory.AddQualitySpecificationElement(htmlElement);
				htmlQualitySpecificationElements.Add(htmlElement);

				htmlTestDescriptor.AddReferencingElement(htmlElement);
			}

			htmlQualitySpecificationElements.Sort(elementComparer);

			// exclude undefined root category if it does not contain any quality conditions

			return reportCategories.Values
			                       .Where(cat => ! cat.IsRoot ||
			                                     ! cat.IsUndefinedCategory ||
			                                     cat.QualitySpecificationElements.Count > 0)
			                       .Distinct()
			                       .OrderBy(c => c, categoryComparer)
			                       .ToList();
		}

		[NotNull]
		public static string GetCompleteUrl([NotNull] string url)
		{
			Assert.ArgumentNotNullOrEmpty(url, nameof(url));

			return url.IndexOf("://", StringComparison.Ordinal) > 0
				       ? url
				       : string.Format("http://{0}", url);
		}

		[NotNull]
		public static string GetQualifiedText(
			[NotNull] HtmlDataQualityCategory category,
			[NotNull] Func<HtmlDataQualityCategory, string> getText,
			[CanBeNull] string separator = "/",
			bool skipNullOrEmpty = false)
		{
			Assert.ArgumentNotNull(category, nameof(category));

			var strings = new List<string>();

			CollectStrings(strings, category, getText, skipNullOrEmpty);

			return string.Join(separator, strings.ToArray());
		}

		[NotNull]
		private static Dictionary<string, HtmlDataQualityCategory> MapReportCategories
		([NotNull] IEnumerable<QualitySpecificationElement> issueGroups,
		 [NotNull] HtmlDataQualityCategoryComparer categoryComparer,
		 [NotNull] HtmlQualitySpecificationElementComparer qualityConditionComparer,
		 [CanBeNull] IHtmlDataQualityCategoryOptionsProvider optionsProvider = null)
		{
			var result = new Dictionary<string, HtmlDataQualityCategory>();

			foreach (QualitySpecificationElement issueGroup in issueGroups)
			{
				// add the next non-ignored category
				AddDataQualityCategory(issueGroup.QualityCondition.Category,
				                       categoryComparer,
				                       qualityConditionComparer,
				                       result,
				                       optionsProvider);
			}

			return result;
		}

		[NotNull]
		private static HtmlDataQualityCategory AddDataQualityCategory(
			[CanBeNull] DataQualityCategory category,
			[NotNull] HtmlDataQualityCategoryComparer categoryComparer,
			[NotNull] HtmlQualitySpecificationElementComparer elementComparer,
			[NotNull] IDictionary<string, HtmlDataQualityCategory> reportCategories,
			[CanBeNull] IHtmlDataQualityCategoryOptionsProvider optionsProvider = null)
		{
			string key = GetCategoryKey(category);

			HtmlDataQualityCategory result;
			if (reportCategories.TryGetValue(key, out result))
			{
				// already added (including parents)
				return result;
			}

			HtmlDataQualityCategoryOptions options =
				GetReportCategoryOptions(optionsProvider, category);

			if (category != null && options != null && options.IgnoreCategoryLevel)
			{
				// skip this category level
				result = AddDataQualityCategory(category.ParentCategory,
				                                categoryComparer,
				                                elementComparer,
				                                reportCategories,
				                                optionsProvider);

				reportCategories.Add(key, result);
				return result;
			}

			result = new HtmlDataQualityCategory(category,
			                                     options,
			                                     categoryComparer,
			                                     elementComparer);
			reportCategories.Add(key, result);

			if (category?.ParentCategory != null)
			{
				HtmlDataQualityCategory parent = AddDataQualityCategory(category.ParentCategory,
					categoryComparer,
					elementComparer,
					reportCategories,
					optionsProvider);
				if (! parent.IsUndefinedCategory)
				{
					result.ParentCategory = parent;
					result.ParentCategory.IncludeSubCategory(result);
				}
			}

			return result;
		}

		private static void CollectStrings(
			[NotNull] ICollection<string> strings,
			[NotNull] HtmlDataQualityCategory category,
			[NotNull] Func<HtmlDataQualityCategory, string> getString,
			bool skipNullOrEmpty)
		{
			if (category.ParentCategory != null)
			{
				CollectStrings(strings, category.ParentCategory, getString, skipNullOrEmpty);
			}

			string value = getString(category);
			if (! skipNullOrEmpty || ! string.IsNullOrEmpty(value))
			{
				strings.Add(getString(category));
			}
		}

		[NotNull]
		private static string GetCategoryKey([CanBeNull] DataQualityCategory category)
		{
			return category == null
				       ? string.Empty
				       : category.Uuid;
		}

		[CanBeNull]
		private static HtmlDataQualityCategoryOptions GetReportCategoryOptions(
			[CanBeNull] IHtmlDataQualityCategoryOptionsProvider categoryOptionsProvider,
			[CanBeNull] DataQualityCategory category)
		{
			if (category == null)
			{
				return null;
			}

			return categoryOptionsProvider?.GetCategoryOptions(category.Uuid);
		}
	}
}
