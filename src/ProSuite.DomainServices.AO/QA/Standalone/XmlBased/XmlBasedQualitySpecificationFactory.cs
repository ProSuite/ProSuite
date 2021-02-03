using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
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

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Initializes a new instance of the <see cref="XmlBasedQualitySpecificationFactory"/> class.
		/// </summary>
		/// <param name="modelFactory">The model builder.</param>
		/// <param name="datasetOpener"></param>
		[CLSCompliant(false)]
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
			[NotNull] string qualitySpecificationName,
			[NotNull] IEnumerable<DataSource> dataSources)
		{
			const bool ignoreConditionsForUnknownDatasets = false;
			return CreateQualitySpecification(document, qualitySpecificationName,
			                                  dataSources,
			                                  ignoreConditionsForUnknownDatasets);
		}

		[NotNull]
		public QualitySpecification CreateQualitySpecification(
			[NotNull] XmlDataQualityDocument document,
			[NotNull] string qualitySpecificationName,
			[NotNull] IEnumerable<DataSource> dataSources,
			bool ignoreConditionsForUnknownDatasets)
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
			XmlDataQualityUtils.AssertUniqueTestDescriptorNames(document);
			XmlDataQualityUtils.AssertUniqueQualityConditionNames(document);
			XmlDataQualityUtils.AssertUniqueQualitySpecificationNames(document);
			XmlDataQualityUtils.AssertUniqueQualifiedCategoryNames(document);

			XmlDataQualityCategory xmlSpecificationCategory;
			XmlQualitySpecification xmlQualitySpecification =
				XmlDataQualityUtils.FindXmlQualitySpecification(document,
				                                                qualitySpecificationName,
				                                                out xmlSpecificationCategory);
			Assert.NotNull(xmlQualitySpecification,
			               "Specification '{0} not found in document",
			               qualitySpecificationName);

			XmlDataQualityUtils.AssertUniqueElementNames(xmlQualitySpecification);

			IDictionary<XmlDataQualityCategory, DataQualityCategory> categoryMap =
				GetCategoryMap(document);

			IList<KeyValuePair<XmlQualityCondition, XmlDataQualityCategory>>
				referencedXmlConditionPairs =
					XmlDataQualityUtils.GetReferencedXmlQualityConditions(
						                   document, new[] {xmlQualitySpecification})
					                   .ToList();

			IList<XmlQualityCondition> referencedXmlConditions =
				referencedXmlConditionPairs.Select(p => p.Key)
				                           .ToList();

			IList<XmlWorkspace> referencedXmlWorkspaces =
				XmlDataQualityUtils.GetReferencedWorkspaces(document,
				                                            referencedXmlConditions);

			IDictionary<string, Model> modelsByWorkspaceId = GetModelsByWorkspaceId(
				referencedXmlWorkspaces, dataSources, referencedXmlConditions);

			IDictionary<string, TestDescriptor> testDescriptorsByName =
				GetReferencedTestDescriptorsByName(referencedXmlConditions, document);

			var qualityConditions = new Dictionary<string, QualityCondition>(
				StringComparer.OrdinalIgnoreCase);

			Func<string, IList<Dataset>> getDatasetsByName = name => new List<Dataset>();

			foreach (
				KeyValuePair<XmlQualityCondition, XmlDataQualityCategory> pair in
				referencedXmlConditionPairs)
			{
				XmlQualityCondition xmlCondition = pair.Key;
				XmlDataQualityCategory xmlConditionCategory = pair.Value;

				DataQualityCategory conditionCategory = xmlConditionCategory != null
					                                        ? categoryMap[xmlConditionCategory]
					                                        : null;

				ICollection<XmlDatasetTestParameterValue> unknownDatasetParameters;
				QualityCondition qualityCondition = XmlDataQualityUtils.CreateQualityCondition(
					xmlCondition,
					testDescriptorsByName[xmlCondition.TestDescriptorName],
					modelsByWorkspaceId,
					getDatasetsByName,
					conditionCategory,
					ignoreConditionsForUnknownDatasets,
					out unknownDatasetParameters);

				if (qualityCondition == null)
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
				else
				{
					qualityConditions.Add(qualityCondition.Name, qualityCondition);
				}
			}

			DataQualityCategory specificationCategory =
				xmlSpecificationCategory != null
					? categoryMap[xmlSpecificationCategory]
					: null;

			return XmlDataQualityUtils.CreateQualitySpecification(
				qualityConditions, xmlQualitySpecification, specificationCategory,
				ignoreConditionsForUnknownDatasets);
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

		[NotNull]
		private static IDictionary<string, TestDescriptor>
			GetReferencedTestDescriptorsByName(
				[NotNull] IEnumerable<XmlQualityCondition> referencedConditions,
				[NotNull] XmlDataQualityDocument document)
		{
			IDictionary<string, XmlTestDescriptor> xmlTestDescriptorsByName =
				GetXmlTestDescriptorsByName(document);

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

		[NotNull]
		private static IDictionary<string, XmlTestDescriptor> GetXmlTestDescriptorsByName(
			[NotNull] XmlDataQualityDocument document)
		{
			return document.TestDescriptors?.ToDictionary(
				       descriptor => descriptor.Name,
				       StringComparer.OrdinalIgnoreCase)
			       ?? new Dictionary<string, XmlTestDescriptor>();
		}

		[NotNull]
		private IDictionary<string, Model> GetModelsByWorkspaceId(
			[NotNull] IEnumerable<XmlWorkspace> xmlWorkspaces,
			[NotNull] IEnumerable<DataSource> dataSources,
			[NotNull] IList<XmlQualityCondition> referencedConditions)
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
		private Model CreateModel(
			[NotNull] IWorkspace workspace,
			[NotNull] string modelName,
			[NotNull] string workspaceId,
			[CanBeNull] string databaseName,
			[CanBeNull] string schemaOwner,
			[NotNull] IEnumerable<XmlQualityCondition> referencedConditions)
		{
			Model result = _modelFactory.CreateModel(workspace, modelName, null,
			                                         databaseName, schemaOwner);

			ISpatialReference spatialReference = GetMainSpatialReference(result,
			                                                             workspaceId,
			                                                             referencedConditions);

			if (spatialReference != null)
			{
				result.SpatialReferenceDescriptor =
					new SpatialReferenceDescriptor(spatialReference);
			}

			return result;
		}

		[CanBeNull]
		private ISpatialReference GetMainSpatialReference(
			[NotNull] Model model,
			[NotNull] string workspaceId,
			[NotNull] IEnumerable<XmlQualityCondition> referencedConditions)
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

			int maxReferenceCount = int.MinValue;
			string maxDatasetName = null;
			foreach (KeyValuePair<string, int> pair in spatialDatasetReferenceCount)
			{
				if (pair.Value <= maxReferenceCount)
				{
					continue;
				}

				maxReferenceCount = pair.Value;
				maxDatasetName = pair.Key;
			}

			if (maxDatasetName == null)
			{
				return null;
			}

			Dataset maxDataset = model.GetDatasetByModelName(maxDatasetName);

			return maxDataset == null
				       ? null
				       : GetSpatialReference(maxDataset);
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
