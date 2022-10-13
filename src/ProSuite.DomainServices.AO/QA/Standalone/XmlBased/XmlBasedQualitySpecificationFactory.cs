using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.AO.QA.Xml;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA.VerifiedDataModel;

namespace ProSuite.DomainServices.AO.QA.Standalone.XmlBased
{
	public class XmlBasedQualitySpecificationFactory
	{
		private readonly IVerifiedModelFactory _modelFactory;
		[NotNull] private readonly IOpenDataset _datasetOpener;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Initializes a new instance of the <see cref="XmlBasedQualitySpecificationFactory"/> class.
		/// </summary>
		/// <param name="modelFactory">The model builder.</param>
		/// <param name="datasetOpener"></param>
		public XmlBasedQualitySpecificationFactory(
			[NotNull] IVerifiedModelFactory modelFactory,
			[NotNull] IOpenDataset datasetOpener)
		{
			Assert.ArgumentNotNull(modelFactory, nameof(modelFactory));
			Assert.ArgumentNotNull(datasetOpener, nameof(datasetOpener));

			_modelFactory = modelFactory;
			_datasetOpener = datasetOpener;
		}

		[NotNull]
		public QualitySpecification CreateQualitySpecification(
			[NotNull] XmlDataQualityDocument document,
			[NotNull] IEnumerable<DataSource> dataSources,
			bool ignoreConditionsForUnknownDatasets)
		{
			KeyValuePair<XmlQualitySpecification, XmlDataQualityCategory> keyValuePair =
				document.GetAllQualitySpecifications().Single();

			string singleSpecificationName = Assert.NotNullOrEmpty(keyValuePair.Key.Name);

			return CreateQualitySpecification(document, singleSpecificationName, dataSources,
			                                  ignoreConditionsForUnknownDatasets);
		}

		[NotNull]
		public QualitySpecification CreateQualitySpecification(
			[NotNull] XmlDataQualityDocument document,
			[NotNull] string qualitySpecificationName,
			[NotNull] IEnumerable<DataSource> dataSources,
			bool ignoreConditionsForUnknownDatasets = false)
		{
			CultureInfo origCulture = Thread.CurrentThread.CurrentCulture;
			try
			{
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

				return CreateQualitySpecificationCore(document,
				                                      qualitySpecificationName,
				                                      dataSources,
				                                      ignoreConditionsForUnknownDatasets);
			}
			finally
			{
				Thread.CurrentThread.CurrentCulture = origCulture;
			}
		}

		[NotNull]
		public QualitySpecification CreateQualitySpecification(
			[NotNull] string name,
			[NotNull] IList<XmlTestDescriptor> xmlDescriptors,
			[NotNull] IList<SpecificationElement> specificationElements,
			[NotNull] IEnumerable<DataSource> dataSources,
			bool ignoreConditionsForUnknownDatasets = false)
		{
			CultureInfo origCulture = Thread.CurrentThread.CurrentCulture;
			try
			{
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

				return CreateQualitySpecificationCore(
					name, xmlDescriptors,
					specificationElements,
					dataSources,
					ignoreConditionsForUnknownDatasets);
			}
			finally
			{
				Thread.CurrentThread.CurrentCulture = origCulture;
			}
		}

		/// <summary>
		/// Creates a full specification from the specified xml document with support for row
		/// filters, issue filters and transformers.
		/// </summary>
		/// <param name="document"></param>
		/// <param name="qualitySpecificationName"></param>
		/// <param name="dataSources"></param>
		/// <param name="ignoreConditionsForUnknownDatasets"></param>
		/// <returns></returns>
		private QualitySpecification CreateQualitySpecificationCore(
			[NotNull] XmlDataQualityDocument document,
			[NotNull] string qualitySpecificationName,
			[NotNull] IEnumerable<DataSource> dataSources,
			bool ignoreConditionsForUnknownDatasets)
		{
			Assert.ArgumentNotNull(document, nameof(document));
			Assert.ArgumentNotNull(dataSources, nameof(dataSources));
			Assert.ArgumentCondition(document.GetAllQualitySpecifications().Any(),
			                         "The document must contain at least one quality specification");

			XmlDataQualityUtils.AssertUniqueWorkspaceIds(document);
			XmlDataQualityUtils.AssertUniqueInstanceDescriptorNames(document);
			XmlDataQualityUtils.AssertUniqueQualityConditionNames(document);
			XmlDataQualityUtils.AssertUniqueIssueFilterNames(document);
			XmlDataQualityUtils.AssertUniqueTransformerNames(document);
			XmlDataQualityUtils.AssertUniqueQualitySpecificationNames(document);
			XmlDataQualityUtils.AssertUniqueQualifiedCategoryNames(document);

			XmlDataQualityCategory xmlSpecificationCategory;
			XmlQualitySpecification xmlQualitySpecification =
				XmlDataQualityUtils.FindXmlQualitySpecification(document,
				                                                qualitySpecificationName,
				                                                out xmlSpecificationCategory);

			Assert.ArgumentCondition(xmlQualitySpecification != null,
			                         $"Specification '{qualitySpecificationName}' not found in document",
			                         nameof(qualitySpecificationName));

			XmlDataQualityUtils.AssertUniqueQualitySpecificationElementNames(
				xmlQualitySpecification);

			IDictionary<XmlDataQualityCategory, DataQualityCategory> categoryMap =
				GetCategoryMap(document);

			XmlDataQualityDocumentCache documentCache =
				XmlDataQualityUtils.GetDocumentCache(document, new[] {xmlQualitySpecification});

			IList<XmlWorkspace> referencedXmlWorkspaces =
				XmlDataQualityUtils.GetReferencedWorkspaces(documentCache);

			IDictionary<string, Model> modelsByWorkspaceId = GetModelsByWorkspaceId(
				referencedXmlWorkspaces, dataSources,
				documentCache.EnumReferencedConfigurationInstances().ToList());

			documentCache.ReferencedModels = modelsByWorkspaceId;

			Dictionary<string, QualityCondition> qualityConditions =
				CreateQualityConditions(documentCache,
				                        categoryMap,
				                        modelsByWorkspaceId,
				                        ignoreConditionsForUnknownDatasets);

			DataQualityCategory specificationCategory =
				xmlSpecificationCategory != null
					? categoryMap[xmlSpecificationCategory]
					: null;

			return XmlDataQualityUtils.CreateQualitySpecification(
				xmlQualitySpecification, qualityConditions, specificationCategory,
				ignoreConditionsForUnknownDatasets);
		}

		/// <summary>
		/// Creates a simple specification from the provided xml test descriptors and specification
		/// elements. Currently no special features (row filters, issue filters, transformers) are
		/// supported.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="xmlDescriptors"></param>
		/// <param name="specificationElements"></param>
		/// <param name="dataSources"></param>
		/// <param name="ignoreConditionsForUnknownDatasets"></param>
		/// <returns></returns>
		private QualitySpecification CreateQualitySpecificationCore(
			[NotNull] string name,
			[NotNull] IList<XmlTestDescriptor> xmlDescriptors,
			[NotNull] IList<SpecificationElement> specificationElements,
			[NotNull] IEnumerable<DataSource> dataSources,
			bool ignoreConditionsForUnknownDatasets)
		{
			Assert.ArgumentNotNull(xmlDescriptors, nameof(xmlDescriptors));
			Assert.ArgumentNotNull(specificationElements, nameof(specificationElements));
			Assert.ArgumentNotNull(dataSources, nameof(dataSources));
			Assert.ArgumentCondition(xmlDescriptors.Any(),
			                         "At least one xml test descriptor must be provided");

			IList<XmlQualityCondition> xmlConditions =
				specificationElements.Select(x => x.XmlCondition).ToList();

			XmlDataQualityUtils.AssertUniqueInstanceDescriptorNames(
				xmlDescriptors, "test descriptor");
			XmlDataQualityUtils.AssertUniqueInstanceConfigurationNames(
				xmlConditions, "quality condition");

			IDictionary<string, Model> modelsByWorkspaceId = GetModelsByWorkspaceId(
				dataSources, xmlConditions);

			IDictionary<string, XmlTestDescriptor> xmlTestDescriptorsByName =
				xmlDescriptors.ToDictionary(descriptor => descriptor.Name,
				                            StringComparer.OrdinalIgnoreCase);

			IDictionary<string, TestDescriptor> testDescriptorsByName =
				GetReferencedTestDescriptorsByName(xmlConditions, xmlTestDescriptorsByName);

			// TODO: GetReferencedRowFiltersByName, GetReferencedTransformers, etc.

			List<KeyValuePair<XmlQualityCondition, DataQualityCategory>> conditionsWithCategory =
				GetConditionsWithCategory(specificationElements);

			Dictionary<string, QualityCondition> qualityConditions =
				CreateQualityConditions(conditionsWithCategory,
				                        testDescriptorsByName,
				                        modelsByWorkspaceId,
				                        ignoreConditionsForUnknownDatasets);

			IEnumerable<QualitySpecificationElement> qualitySpecificationElements =
				GetQualitySpecificationElements(specificationElements, qualityConditions);

			return XmlDataQualityUtils.CreateQualitySpecification(
				name, qualityConditions, qualitySpecificationElements);
		}

		private static Dictionary<string, QualityCondition> CreateQualityConditions(
			[NotNull] IList<KeyValuePair<XmlQualityCondition, XmlDataQualityCategory>>
				referencedXmlConditionPairs,
			[NotNull] IDictionary<string, TestDescriptor> testDescriptorsByName,
			IDictionary<XmlDataQualityCategory, DataQualityCategory> categoryMap,
			IDictionary<string, Model> modelsByWorkspaceId,
			bool ignoreConditionsForUnknownDatasets)
		{
			var conditionsWithCategory =
				new List<KeyValuePair<XmlQualityCondition, DataQualityCategory>>();

			foreach (var kvp in referencedXmlConditionPairs)
			{
				XmlQualityCondition xmlQualityCondition = kvp.Key;
				XmlDataQualityCategory xmlCategory = kvp.Value;

				DataQualityCategory category = xmlCategory != null
					                               ? categoryMap[xmlCategory]
					                               : null;

				conditionsWithCategory.Add(
					new KeyValuePair<XmlQualityCondition, DataQualityCategory>(
						xmlQualityCondition, category));
			}

			return CreateQualityConditions(conditionsWithCategory, testDescriptorsByName,
			                               modelsByWorkspaceId, ignoreConditionsForUnknownDatasets);
		}

		private static Dictionary<string, QualityCondition> CreateQualityConditions(
			XmlDataQualityDocumentCache xmlDataDocumentCache,
			IDictionary<XmlDataQualityCategory, DataQualityCategory> categoryMap,
			IDictionary<string, Model> modelsByWorkspaceId,
			bool ignoreConditionsForUnknownDatasets)
		{
			var qualityConditions = new Dictionary<string, QualityCondition>(
				StringComparer.OrdinalIgnoreCase);

			Func<string, IList<Dataset>> getDatasetsByName = name => new List<Dataset>();

			foreach (KeyValuePair<XmlQualityCondition, XmlDataQualityCategory> pair in
			         xmlDataDocumentCache.QualityConditionsWithCategories)
			{
				XmlQualityCondition xmlCondition = pair.Key;
				XmlDataQualityCategory xmlCategory = pair.Value;

				DataQualityCategory category =
					xmlCategory != null
						? categoryMap[xmlCategory]
						: null;

				QualityCondition createdCondition = XmlDataQualityUtils.CreateQualityCondition(
					xmlCondition, xmlDataDocumentCache, getDatasetsByName, category,
					ignoreConditionsForUnknownDatasets,
					out ICollection<XmlDatasetTestParameterValue> unknownDatasetParameters);

				if (createdCondition == null)
				{
					HandleNoConditionCreated(xmlCondition, modelsByWorkspaceId,
					                         ignoreConditionsForUnknownDatasets,
					                         unknownDatasetParameters);
				}
				else
				{
					qualityConditions.Add(createdCondition.Name, createdCondition);
				}
			}

			return qualityConditions;
		}

		private static Dictionary<string, QualityCondition> CreateQualityConditions(
			[NotNull] IList<KeyValuePair<XmlQualityCondition, DataQualityCategory>> conditions,
			[NotNull] IDictionary<string, TestDescriptor> testDescriptorsByName,
			IDictionary<string, Model> modelsByWorkspaceId,
			bool ignoreConditionsForUnknownDatasets)
		{
			var qualityConditions = new Dictionary<string, QualityCondition>(
				StringComparer.OrdinalIgnoreCase);

			Func<string, IList<Dataset>> getDatasetsByName = name => new List<Dataset>();

			foreach (KeyValuePair<XmlQualityCondition, DataQualityCategory> pair in conditions)
			{
				XmlQualityCondition xmlCondition = pair.Key;
				DataQualityCategory conditionCategory = pair.Value;

				ICollection<XmlDatasetTestParameterValue> unknownDatasetParameters;
				QualityCondition qualityCondition =
					XmlDataQualityUtils.CreateQualityConditionLegacy(
						xmlCondition,
						testDescriptorsByName[xmlCondition.TestDescriptorName],
						modelsByWorkspaceId,
						getDatasetsByName,
						conditionCategory,
						ignoreConditionsForUnknownDatasets,
						out unknownDatasetParameters);

				if (qualityCondition == null)
				{
					HandleNoConditionCreated(xmlCondition, modelsByWorkspaceId,
					                         ignoreConditionsForUnknownDatasets,
					                         unknownDatasetParameters);
				}
				else
				{
					qualityConditions.Add(qualityCondition.Name, qualityCondition);
				}
			}

			return qualityConditions;
		}

		private static void HandleNoConditionCreated(XmlQualityCondition xmlCondition,
		                                             IDictionary<string, Model> modelsByWorkspaceId,
		                                             bool ignoreConditionsForUnknownDatasets,
		                                             ICollection<XmlDatasetTestParameterValue>
			                                             unknownDatasetParameters)
		{
			Assert.True(ignoreConditionsForUnknownDatasets,
			            "ignoreConditionsForUnknownDatasets");
			Assert.True(unknownDatasetParameters.Count > 0,
			            "Unexpected number of unknown datasets");

			_msg.WarnFormat(
				unknownDatasetParameters.Count == 1
					? "Quality condition '{0}' is ignored because the following dataset is not found: {1}"
					: "Quality condition '{0}' is ignored because the following datasets are not found: {1}",
				xmlCondition.Name,
				XmlDataQualityUtils.ConcatenateUnknownDatasetNames(
					unknownDatasetParameters,
					modelsByWorkspaceId,
					DataSource.AnonymousId));
		}

		[NotNull]
		private static IDictionary<XmlDataQualityCategory, DataQualityCategory>
			GetCategoryMap([NotNull] XmlDataQualityDocument document)
		{
			var result = new Dictionary<XmlDataQualityCategory, DataQualityCategory>();

			if (document.Categories == null)
			{
				return result;
			}

			foreach (XmlDataQualityCategory xmlCategory in document.Categories)
			{
				DataQualityCategory category =
					XmlDataQualityUtils.CreateDataQualityCategory(xmlCategory, null);

				result.Add(xmlCategory, category);

				if (xmlCategory.SubCategories != null)
				{
					AddSubCategories(xmlCategory.SubCategories, category, result);
				}
			}

			return result;
		}

		[NotNull]
		private static IDictionary<string, DataQualityCategory> GetCategoryMap(
			[NotNull] IEnumerable<string> categoryNames)
		{
			var result = new Dictionary<string, DataQualityCategory>();

			foreach (string categoryName in categoryNames)
			{
				if (! result.ContainsKey(categoryName))
				{
					result.Add(categoryName, new DataQualityCategory(categoryName));
				}
			}

			return result;
		}

		private static void AddSubCategories(
			[NotNull] IEnumerable<XmlDataQualityCategory> xmlCategories,
			[NotNull] DataQualityCategory parentCategory,
			[NotNull] IDictionary<XmlDataQualityCategory, DataQualityCategory> result)
		{
			foreach (XmlDataQualityCategory xmlCategory in xmlCategories)
			{
				DataQualityCategory category =
					XmlDataQualityUtils.CreateDataQualityCategory(xmlCategory, parentCategory);

				result.Add(xmlCategory, category);

				if (xmlCategory.SubCategories != null)
				{
					AddSubCategories(xmlCategory.SubCategories, category, result);
				}
			}
		}

		private static IDictionary<string, TestDescriptor> GetReferencedTestDescriptorsByName(
			[NotNull] IEnumerable<XmlQualityCondition> referencedConditions,
			[NotNull] IDictionary<string, XmlTestDescriptor> xmlTestDescriptorsByName)
		{
			var result = new Dictionary<string, TestDescriptor>(
				StringComparer.OrdinalIgnoreCase);

			foreach (XmlQualityCondition condition in referencedConditions)
			{
				string testDescriptorName = condition.TestDescriptorName;
				if (testDescriptorName == null || result.ContainsKey(testDescriptorName))
				{
					continue;
				}

				XmlTestDescriptor xmlTestDescriptor;
				if (! xmlTestDescriptorsByName.TryGetValue(testDescriptorName,
				                                           out xmlTestDescriptor))
				{
					throw new InvalidConfigurationException(
						string.Format(
							"Test descriptor {0}, referenced in quality condition {1}, not found",
							testDescriptorName, condition.Name));
				}

				result.Add(testDescriptorName,
				           XmlDataQualityUtils.CreateTestDescriptor(xmlTestDescriptor));
			}

			return result;
		}

		private IDictionary<string, Model> GetModelsByWorkspaceId(
			[NotNull] IEnumerable<XmlWorkspace> xmlWorkspaces,
			[NotNull] IEnumerable<DataSource> dataSources,
			[NotNull] IList<XmlInstanceConfiguration> referencedConditions)
		{
			Dictionary<string, DataSource> dataSourcesByWorkspaceId =
				dataSources.ToDictionary(dataSource => dataSource.ID,
				                         StringComparer.OrdinalIgnoreCase);

			var result = new Dictionary<string, Model>(StringComparer.OrdinalIgnoreCase);

			foreach (XmlWorkspace xmlWorkspace in xmlWorkspaces)
			{
				DataSource dataSource;
				if (! dataSourcesByWorkspaceId.TryGetValue(xmlWorkspace.ID, out dataSource))
				{
					throw new InvalidConfigurationException(
						string.Format("Data source not found for xml workspace {0}",
						              xmlWorkspace.ModelName));
				}

				result.Add(xmlWorkspace.ID,
				           CreateModel(dataSource.OpenWorkspace(),
				                       xmlWorkspace.ModelName,
				                       xmlWorkspace.ID,
				                       xmlWorkspace.Database,
				                       xmlWorkspace.SchemaOwner,
				                       referencedConditions));
			}

			DataSource anonymousDataSource;
			if (dataSourcesByWorkspaceId.TryGetValue(DataSource.AnonymousId,
			                                         out anonymousDataSource))
			{
				result.Add(anonymousDataSource.ID,
				           CreateModel(anonymousDataSource.OpenWorkspace(),
				                       anonymousDataSource.DisplayName,
				                       anonymousDataSource.ID,
				                       anonymousDataSource.DatabaseName,
				                       anonymousDataSource.SchemaOwner,
				                       referencedConditions));
			}

			return result;
		}

		[NotNull]
		private IDictionary<string, Model> GetModelsByWorkspaceId(
			[NotNull] IEnumerable<DataSource> allDataSources,
			[NotNull] IList<XmlQualityCondition> referencedConditions)
		{
			var result = new Dictionary<string, Model>(StringComparer.OrdinalIgnoreCase);

			foreach (DataSource dataSource in allDataSources)
			{
				result.Add(dataSource.ID,
				           CreateModel(dataSource.OpenWorkspace(),
				                       dataSource.DisplayName,
				                       dataSource.ID,
				                       dataSource.DatabaseName,
				                       dataSource.SchemaOwner,
				                       referencedConditions));
			}

			return result;
		}

		[NotNull]
		private Model CreateModel(
			[NotNull] IWorkspace workspace,
			[NotNull] string modelName,
			[NotNull] string workspaceId,
			[CanBeNull] string databaseName,
			[CanBeNull] string schemaOwner,
			[NotNull] IEnumerable<XmlInstanceConfiguration> referencedConditions)
		{
			Model result = _modelFactory.CreateModel(workspace, modelName, null,
			                                         databaseName, schemaOwner);

			ISpatialReference spatialReference = GetMainSpatialReference(
				result, workspaceId, referencedConditions);

			if (spatialReference != null)
			{
				result.SpatialReferenceDescriptor =
					new SpatialReferenceDescriptor(spatialReference);
			}

			return result;
		}

		private static List<KeyValuePair<XmlQualityCondition, DataQualityCategory>>
			GetConditionsWithCategory(
				[NotNull] IList<SpecificationElement> specificationElements)
		{
			var categoryMap = GetCategoryMap(
				specificationElements.Select(e => e.CategoryName));

			var conditionsWithCategory =
				new List<KeyValuePair<XmlQualityCondition, DataQualityCategory>>();

			foreach (var element in specificationElements)
			{
				string category = element.CategoryName;

				DataQualityCategory dataQualityCategory =
					category == null ? null : categoryMap[category];

				conditionsWithCategory.Add(
					new KeyValuePair<XmlQualityCondition, DataQualityCategory>(
						element.XmlCondition, dataQualityCategory));
			}

			return conditionsWithCategory;
		}

		private static IList<QualitySpecificationElement> GetQualitySpecificationElements(
			[NotNull] IEnumerable<SpecificationElement> specificationElements,
			[NotNull] IReadOnlyDictionary<string, QualityCondition> qualityConditions)
		{
			var qualitySpecificationElements = new List<QualitySpecificationElement>();

			foreach (SpecificationElement element in specificationElements)
			{
				string conditionName = Assert.NotNullOrEmpty(
					element.XmlCondition.Name, "Empty or null condition name.");

				QualityCondition qualityCondition = qualityConditions[conditionName];

				QualitySpecificationElement qualitySpecificationElement =
					new QualitySpecificationElement(qualityCondition, element.StopOnError,
					                                element.AllowErrors);

				qualitySpecificationElements.Add(qualitySpecificationElement);
			}

			return qualitySpecificationElements;
		}

		[CanBeNull]
		private ISpatialReference GetMainSpatialReference(
			[NotNull] Model model,
			[NotNull] string workspaceId,
			[NotNull] IEnumerable<XmlInstanceConfiguration> referencedConditions)
		{
			var spatialDatasetReferenceCount =
				new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

			foreach (Dataset dataset in XmlDataQualityUtils.GetReferencedDatasets(
				         model, workspaceId, referencedConditions))
			{
				if (! (dataset is ISpatialDataset))
				{
					continue;
				}

				if (! spatialDatasetReferenceCount.ContainsKey(dataset.Name))
				{
					spatialDatasetReferenceCount.Add(dataset.Name, 1);
				}
				else
				{
					spatialDatasetReferenceCount[dataset.Name]++;
				}
			}

			foreach (KeyValuePair<string, int> pair in
			         spatialDatasetReferenceCount.OrderByDescending(kvp => kvp.Value))
			{
				string datasetName = pair.Key;

				Dataset maxDataset = model.GetDatasetByModelName(datasetName);

				if (maxDataset == null)
				{
					continue;
				}

				ISpatialReference spatialReference = GetSpatialReference(maxDataset);

				if (spatialReference != null)
				{
					return spatialReference;
				}
			}

			return null;
		}

		[CanBeNull]
		private ISpatialReference GetSpatialReference([NotNull] Dataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			IGeoDataset geoDataset = _datasetOpener.OpenDataset(dataset) as IGeoDataset;

			return geoDataset?.SpatialReference;
		}
	}
}
