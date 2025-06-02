using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Xml;
using ProSuite.DomainServices.AO.QA.VerifiedDataModel;

namespace ProSuite.DomainServices.AO.QA.Standalone.XmlBased
{
	public class XmlBasedQualitySpecificationFactory
	{
		private static int _currentModelId = -100;

		/// <summary>
		/// Initializes a new instance of the <see cref="XmlBasedQualitySpecificationFactory"/> class.
		/// </summary>
		/// <param name="modelFactory">The model builder.</param>
		public XmlBasedQualitySpecificationFactory([NotNull] IVerifiedModelFactory modelFactory)
		{
			Assert.ArgumentNotNull(modelFactory, nameof(modelFactory));

			ModelFactory = modelFactory;
		}

		private IVerifiedModelFactory ModelFactory { get; }

		/// <summary>
		/// Creates a full specification from the specified xml document with support for
		/// issue filters and transformers.
		/// </summary>
		/// <param name="document"></param>
		/// <param name="qualitySpecificationName"></param>
		/// <param name="dataSources"></param>
		/// <param name="factorySettings"></param>
		/// <returns></returns>
		[NotNull]
		public QualitySpecification CreateQualitySpecification(
			[NotNull] XmlDataQualityDocument document,
			[NotNull] string qualitySpecificationName,
			[NotNull] IEnumerable<DataSource> dataSources,
			[CanBeNull] FactorySettings factorySettings = null)
		{
			CultureInfo origCulture = Thread.CurrentThread.CurrentCulture;
			try
			{
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

				return CreateQualitySpecificationCore(
					document, qualitySpecificationName, dataSources,
					factorySettings ?? new FactorySettings());
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
		/// <param name="factorySettings"></param>
		/// <returns></returns>
		private QualitySpecification CreateQualitySpecificationCore(
			[NotNull] XmlDataQualityDocument document,
			[NotNull] string qualitySpecificationName,
			[NotNull] IEnumerable<DataSource> dataSources,
			[NotNull] FactorySettings factorySettings)
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
				XmlDataQualityUtils.GetDocumentCache(document, new[] { xmlQualitySpecification },
				                                     new TestParameterDatasetValidator());

			IList<XmlWorkspace> referencedXmlWorkspaces =
				XmlDataQualityUtils.GetReferencedWorkspaces(documentCache, out bool _);

			IDictionary<string, DdxModel> modelsByWorkspaceId = GetModelsByWorkspaceId(
				referencedXmlWorkspaces, dataSources,
				documentCache.EnumReferencedConfigurationInstances().ToList());

			documentCache.ReferencedModels = modelsByWorkspaceId;

			documentCache.CategoryMap = categoryMap;

			Dictionary<string, QualityCondition> qualityConditions =
				CreateQualityConditions(
					documentCache, modelsByWorkspaceId, factorySettings);

			DataQualityCategory specificationCategory =
				xmlSpecificationCategory != null
					? categoryMap[xmlSpecificationCategory]
					: null;

			return XmlDataQualityUtils.CreateQualitySpecification(
				xmlQualitySpecification, qualityConditions, specificationCategory,
				factorySettings.IgnoreConditionsForUnknownDatasets);
		}

		private static Dictionary<string, QualityCondition> CreateQualityConditions(
			XmlDataQualityDocumentCache xmlDataDocumentCache,
			IDictionary<string, DdxModel> modelsByWorkspaceId,
			[NotNull] FactorySettings factorySettings)
		{
			var qualityConditions = new Dictionary<string, QualityCondition>(
				StringComparer.OrdinalIgnoreCase);

			Func<string, IList<Dataset>> getDatasetsByName = name => new List<Dataset>();

			foreach (XmlQualityCondition xmlCondition in xmlDataDocumentCache
			                                             .QualityConditionsWithCategories
			                                             .Select(x => x.Key))
			{
				QualityCondition createdCondition = xmlDataDocumentCache.CreateQualityCondition(
					xmlCondition, getDatasetsByName, factorySettings,
					out ICollection<DatasetTestParameterRecord> unknownDatasetParameters);

				if (createdCondition == null)
				{
					InstanceConfigurationUtils.HandleNoConditionCreated(
						xmlCondition.Name, modelsByWorkspaceId,
						factorySettings.IgnoreConditionsForUnknownDatasets,
						unknownDatasetParameters);
				}
				else
				{
					qualityConditions.Add(createdCondition.Name, createdCondition);
				}
			}

			return qualityConditions;
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

		private IDictionary<string, DdxModel> GetModelsByWorkspaceId(
			[NotNull] IEnumerable<XmlWorkspace> xmlWorkspaces,
			[NotNull] IEnumerable<DataSource> dataSources,
			[NotNull] IList<XmlInstanceConfiguration> referencedConditions)
		{
			Dictionary<string, DataSource> dataSourcesByWorkspaceId =
				dataSources.ToDictionary(dataSource => dataSource.ID,
				                         StringComparer.OrdinalIgnoreCase);

			var result = new Dictionary<string, DdxModel>(StringComparer.OrdinalIgnoreCase);

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
		private DdxModel CreateModel(
			[NotNull] IWorkspace workspace,
			[NotNull] string modelName,
			[NotNull] string workspaceId,
			[CanBeNull] string databaseName,
			[CanBeNull] string schemaOwner,
			[NotNull] IList<XmlInstanceConfiguration> referencedConditions)
		{
			List<string> datasetNames =
				new List<string>(XmlDataQualityUtils.GetReferencedDatasetNames(
					                 workspaceId, referencedConditions));

			// Assign a unique, non-persistent model id in order to associate datasets with the model,
			// e.g. if the resulting specification is converted to a proto-based specification.
			int modelId = _currentModelId--;
			DdxModel result = ModelFactory.CreateModel(
				workspace, modelName, modelId, databaseName, schemaOwner, datasetNames);

			IEnumerable<Dataset> referencedDatasets = datasetNames.Select(datasetName =>
					XmlDataQualityUtils.GetDatasetByParameterValue(result, datasetName))
				.Where(dataset => dataset != null);

			ModelFactory.AssignMostFrequentlyUsedSpatialReference(result, referencedDatasets);

			return result;
		}
	}
}
