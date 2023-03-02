using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.Commons.Xml;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;
using ProSuite.DomainModel.Core.QA.Xml;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.Persistence.Core.QA.Xml
{
	// TODO add options to
	// - import categories (all or referenced), or ignore them
	// - target category (optional)
	// - import quality condition category assignment (category from xml file, or use target category/root)
	// - keep or update quality condition category assignment for *existing* conditions
	// - import quality specification category assignment (category from xml file, or use target category/root)
	// - keep or update quality specification category assignment for *existing* conditions

	// TODO unique index on qspec element table?
	[UsedImplicitly]
	public class XmlDataQualityImporter : XmlDataQualityExchangeBase,
	                                      IXmlDataQualityImporter
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="XmlDataQualityImporter"/> class.
		/// </summary>
		/// <param name="instanceConfigurations">The instance configurations repository.</param>
		/// <param name="instanceDescriptors">The instance descriptor repository.</param>
		/// <param name="qualitySpecifications">The quality specifications repository.</param>
		/// <param name="categories">The data quality category repository</param>
		/// <param name="datasets">The dataset repository.</param>
		/// <param name="models">The model repository.</param>
		/// <param name="unitOfWork">The unit of work.</param>
		/// <param name="workspaceConverter"></param>
		/// <param name="datasetValidator"></param>
		/// <param name="issueFilterExpressionParser"></param>
		public XmlDataQualityImporter(
			[NotNull] IInstanceConfigurationRepository instanceConfigurations,
			[NotNull] IInstanceDescriptorRepository instanceDescriptors,
			[NotNull] IQualitySpecificationRepository qualitySpecifications,
			[CanBeNull] IDataQualityCategoryRepository categories,
			[NotNull] IDatasetRepository datasets,
			[NotNull] IDdxModelRepository models,
			[NotNull] IUnitOfWork unitOfWork,
			[NotNull] IXmlWorkspaceConverter workspaceConverter,
			[NotNull] ITestParameterDatasetValidator datasetValidator,
			[NotNull] IIssueFilterExpressionParser issueFilterExpressionParser)
			: base(instanceConfigurations, instanceDescriptors, qualitySpecifications,
			       categories, datasets, unitOfWork, workspaceConverter)
		{
			Assert.ArgumentNotNull(models, nameof(models));

			Models = models;

			TestParameterDatasetValidator = datasetValidator;
			IssueFilterExpressionParser = issueFilterExpressionParser;
		}

		[NotNull]
		private IDdxModelRepository Models { get; }

		public IIssueFilterExpressionParser IssueFilterExpressionParser { get; set; }

		public ITestParameterDatasetValidator TestParameterDatasetValidator { get; set; }

		#endregion

		#region IXmlDataQualityImporter Members

		public IList<QualitySpecification> Import(
			string xmlFilePath,
			ICollection<QualitySpecification> qualitySpecificationsToUpdate,
			bool ignoreConditionsForUnknownDatasets,
			bool updateDescriptorNames,
			bool updateDescriptorProperties)
		{
			Assert.ArgumentNotNull(qualitySpecificationsToUpdate,
			                       nameof(qualitySpecificationsToUpdate));
			Assert.ArgumentNotNullOrEmpty(xmlFilePath, nameof(xmlFilePath));

			List<QualitySpecification> qspecList = qualitySpecificationsToUpdate.ToList();

			return UnitOfWork.UseTransaction(
				delegate
				{
					Reattach(qspecList);

					return ImportTx(xmlFilePath, qspecList,
					                QualitySpecificationImportType.UpdateOnly,
					                ignoreConditionsForUnknownDatasets,
					                updateDescriptorNames,
					                updateDescriptorProperties);
				});
		}

		public IList<QualitySpecification> Import(string xmlFilePath,
		                                          QualitySpecificationImportType importType,
		                                          bool ignoreConditionsForUnknownDatasets,
		                                          bool updateDescriptorNames,
		                                          bool updateDescriptorProperties)
		{
			return UnitOfWork.UseTransaction(
				() => ImportTx(xmlFilePath,
				               QualitySpecifications.GetAll(),
				               importType,
				               ignoreConditionsForUnknownDatasets,
				               updateDescriptorNames,
				               updateDescriptorProperties));
		}

		public void ImportInstanceDescriptors(
			string xmlFilePath,
			bool updateDescriptorNames,
			bool updateDescriptorProperties)
		{
			Assert.ArgumentNotNullOrEmpty(xmlFilePath, nameof(xmlFilePath));
			Assert.True(File.Exists(xmlFilePath), "File does not exist: {0}", xmlFilePath);

			CultureInfo origCulture = Thread.CurrentThread.CurrentCulture;
			try
			{
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
				XmlDataQualityDocument document;
				using (StreamReader xmlReader = new StreamReader(xmlFilePath))
				{
					document = XmlDataQualityUtils.Deserialize(xmlReader);
				}

				UnitOfWork.UseTransaction(
					delegate
					{
						ImportDescriptors(document,
						                  updateDescriptorNames,
						                  updateDescriptorProperties);

						UnitOfWork.Commit();
					});
			}
			finally
			{
				Thread.CurrentThread.CurrentCulture = origCulture;
			}
		}

		#endregion

		[NotNull]
		private IList<QualitySpecification> ImportTx(
			[NotNull] string xmlFilePath,
			[NotNull] IList<QualitySpecification> qualitySpecifications,
			QualitySpecificationImportType qualitySpecificationImportType,
			bool ignoreConditionsForUnknownDatasets,
			bool updateDescriptorNames,
			bool updateDescriptorProperties)
		{
			Assert.ArgumentNotNull(qualitySpecifications, nameof(qualitySpecifications));
			Assert.ArgumentNotNullOrEmpty(xmlFilePath, nameof(xmlFilePath));
			Assert.True(File.Exists(xmlFilePath), "File does not exist: {0}", xmlFilePath);

			CultureInfo origCulture = Thread.CurrentThread.CurrentCulture;
			try
			{
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
				XmlDataQualityDocument document;
				using (StreamReader xmlReader = new StreamReader(xmlFilePath))
				{
					document = XmlDataQualityUtils.Deserialize(xmlReader);
				}

				return UnitOfWork.UseTransaction(
					delegate
					{
						var result =
							ImportTx(qualitySpecifications,
							         document,
							         qualitySpecificationImportType,
							         ignoreConditionsForUnknownDatasets,
							         updateDescriptorNames,
							         updateDescriptorProperties);

						UnitOfWork.Commit();

						return result;
					});
			}
			finally
			{
				Thread.CurrentThread.CurrentCulture = origCulture;
			}
		}

		[NotNull]
		private IList<QualitySpecification> ImportTx(
			[NotNull] IList<QualitySpecification> existingSpecifications,
			[NotNull] XmlDataQualityDocument document,
			QualitySpecificationImportType qualitySpecificationImportType,
			bool ignoreConditionsForUnknownDatasets,
			bool updateDescriptorNames,
			bool updateDescriptorProperties)
		{
			Assert.ArgumentNotNull(existingSpecifications, nameof(existingSpecifications));
			Assert.ArgumentNotNull(document, nameof(document));

			XmlDataQualityUtils.AssertUniqueQualitySpecificationNames(document);
			XmlDataQualityUtils.AssertUniqueQualityConditionNames(document);
			XmlDataQualityUtils.AssertUniqueIssueFilterNames(document);
			XmlDataQualityUtils.AssertUniqueTransformerNames(document);
			XmlDataQualityUtils.AssertUniqueQualitySpecificationUuids(document);
			XmlDataQualityUtils.AssertUniqueQualityConditionsUuids(document);
			XmlDataQualityUtils.AssertUniqueTransformerUuids(document);
			XmlDataQualityUtils.AssertUniqueIssueFilterUuids(document);
			XmlDataQualityUtils.AssertUniqueCategoryUuids(document);
			XmlDataQualityUtils.AssertUniqueWorkspaceIds(document);
			XmlDataQualityUtils.AssertUniqueQualifiedCategoryNames(document);

			IList<DdxModel> models = Models.GetAllDdxModels();

			IDictionary<string, DdxModel> modelsByWorkspaceId =
				document.Workspaces == null
					? new Dictionary<string, DdxModel>()
					: GetModelsByWorkspaceId(document.Workspaces, models);

			IList<KeyValuePair<XmlQualitySpecification, XmlDataQualityCategory>>
				xmlSpecificationsToImport =
					GetXmlQualitySpecificationsToImport(document,
					                                    existingSpecifications,
					                                    qualitySpecificationImportType);

			XmlDataQualityDocumentCache documentCache =
				XmlDataQualityUtils.GetDocumentCache(
					document, xmlSpecificationsToImport.Select(p => p.Key),
					TestParameterDatasetValidator, IssueFilterExpressionParser);

			documentCache.ReferencedModels = modelsByWorkspaceId;

			IDictionary<string, InstanceDescriptor> availableDescriptorsByName =
				ImportDescriptors(document,
				                  updateDescriptorNames,
				                  updateDescriptorProperties);

			IDictionary<XmlDataQualityCategory, DataQualityCategory> availableCategories =
				ImportCategories(document.Categories,
				                 document.GetAllCategories().ToList(),
				                 models);

			IDictionary<string, InstanceConfiguration> availableConfigurationsByName =
				ImportConfigurations(documentCache,
				                     availableDescriptorsByName,
				                     modelsByWorkspaceId,
				                     availableCategories,
				                     ignoreConditionsForUnknownDatasets);

			IDictionary<string, QualityCondition> availableConditionsByName =
				new Dictionary<string, QualityCondition>();
			foreach (KeyValuePair<string, InstanceConfiguration> pair in
			         availableConfigurationsByName.Where(kvp => kvp.Value is QualityCondition))
			{
				availableConditionsByName.Add(pair.Key, pair.Value as QualityCondition);
			}

			IList<QualitySpecification> result =
				ImportQualitySpecifications(existingSpecifications,
				                            xmlSpecificationsToImport,
				                            availableCategories,
				                            availableConditionsByName,
				                            qualitySpecificationImportType,
				                            ignoreConditionsForUnknownDatasets);

			AllowImportedCategoryContent(availableCategories.Values);

			return result;
		}

		private void AllowImportedCategoryContent(
			[NotNull] IEnumerable<DataQualityCategory> categories)
		{
			foreach (DataQualityCategory category in categories)
			{
				if (! category.CanContainSubCategories && category.SubCategories.Count > 0)
				{
					_msg.WarnFormat(
						"Category {0} is changed to allow subcategories, since it contains subcategories after the import",
						category.GetQualifiedName());

					category.CanContainSubCategories = true;
				}

				if (! category.CanContainQualityConditions &&
				    InstanceConfigurations.Get<QualityCondition>(category).Count > 0)
				{
					_msg.WarnFormat(
						"Category {0} is changed to allow quality conditions, since it contains quality conditions after the import",
						category.GetQualifiedName());

					category.CanContainQualityConditions = true;
				}

				if (! category.CanContainQualitySpecifications &&
				    QualitySpecifications.Get(category).Count > 0)
				{
					_msg.WarnFormat(
						"Category {0} is changed to allow quality specifications, since it contains quality specifications after the import",
						category.GetQualifiedName());

					category.CanContainQualitySpecifications = true;
				}
			}
		}

		[NotNull]
		private IDictionary<string, DdxModel> GetModelsByWorkspaceId(
			[NotNull] IEnumerable<XmlWorkspace> workspaces,
			[NotNull] IList<DdxModel> models)
		{
			return workspaces.ToDictionary(
				xmlWorkspace => xmlWorkspace.ID,
				xmlWorkspace => WorkspaceConverter.SelectMatchingModel(xmlWorkspace, models),
				StringComparer.InvariantCultureIgnoreCase);
		}

		[NotNull]
		private static IList<KeyValuePair<XmlQualitySpecification, XmlDataQualityCategory>>
			GetXmlQualitySpecificationsToImport(
				[NotNull] XmlDataQualityDocument document,
				[NotNull] IEnumerable<QualitySpecification> existingSpecifications,
				QualitySpecificationImportType importType)
		{
			Assert.ArgumentNotNull(document, nameof(document));
			Assert.ArgumentNotNull(existingSpecifications, nameof(existingSpecifications));

			switch (importType)
			{
				case QualitySpecificationImportType.UpdateOrAdd:
					return document.GetAllQualitySpecifications().ToList();

				case QualitySpecificationImportType.UpdateOnly:
				{
					var namesToImport =
						new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
					foreach (QualitySpecification existingSpecification in existingSpecifications)
					{
						namesToImport.Add(existingSpecification.Name);
					}

					var result =
						new List<KeyValuePair<XmlQualitySpecification, XmlDataQualityCategory>>();

					foreach (
						KeyValuePair<XmlQualitySpecification, XmlDataQualityCategory> pair in
						document.GetAllQualitySpecifications())
					{
						if (namesToImport.Contains(pair.Key.Name))
						{
							result.Add(pair);
						}
					}

					return result;
				}

				default:
					throw new ArgumentOutOfRangeException(nameof(importType), importType,
					                                      @"Unsupported import type");
			}
		}

		#region <Descriptors>

		/// <summary>
		/// Imports the descriptors.
		/// </summary>
		/// <param name="document">The xml document.</param>
		/// <param name="updateDescriptorNames">if set to <c>true</c> the names of existing descriptors are updated.</param>
		/// <param name="updateDescriptorProperties">if set to <c>true</c> the other properties (except the name) of existing 
		/// descriptors are updated.</param>
		/// <returns>
		/// A dictionary of imported or updated descriptors, indexed by name
		/// </returns>
		[NotNull]
		private IDictionary<string, InstanceDescriptor> ImportDescriptors(
			[NotNull] XmlDataQualityDocument document,
			bool updateDescriptorNames,
			bool updateDescriptorProperties)
		{
			Assert.ArgumentNotNull(document, nameof(document));

			XmlDataQualityUtils.AssertUniqueInstanceDescriptorNames(document);

			// validate stored descriptors
			IList<InstanceDescriptor> existingDescriptors = InstanceDescriptors.GetAll();

			AssertUniqueDescriptorNames(existingDescriptors, "ddx");
			AssertUniqueInstanceDefinitions(existingDescriptors, "ddx");

			// validate document descriptors
			IList<InstanceDescriptor> documentDescriptors =
				GetInstanceDescriptors(document).ToList();

			AssertUniqueDescriptorNames(documentDescriptors, "xml");
			AssertUniqueInstanceDefinitions(documentDescriptors, "xml");

			// assert that existing descriptors with the same name as imported descriptors also have the same
			// definition - otherwise duplicate names may result by matching descriptors by definition
			AssertSameNameImpliesSameDefinition(existingDescriptors, documentDescriptors);

			IDictionary<InstanceDefinition, InstanceDescriptor> existingDescriptorsByDefinition =
				existingDescriptors.ToDictionary(InstanceDefinition.CreateFrom);

			var result =
				new Dictionary<string, InstanceDescriptor>(StringComparer.OrdinalIgnoreCase);

			foreach (InstanceDescriptor documentDescriptor in documentDescriptors)
			{
				var instanceDefinition = InstanceDefinition.CreateFrom(documentDescriptor);

				if (existingDescriptorsByDefinition.TryGetValue(
					    instanceDefinition, out InstanceDescriptor existingDescriptor))
				{
					XmlDataQualityUtils.TransferProperties(documentDescriptor,
					                                       existingDescriptor,
					                                       updateDescriptorNames,
					                                       updateDescriptorProperties);

					result.Add(documentDescriptor.Name, existingDescriptor);
				}
				else
				{
					_msg.InfoFormat("Adding new {0}: {1}", documentDescriptor.TypeDisplayName,
					                documentDescriptor.Name);

					InstanceDescriptors.Save(documentDescriptor);

					existingDescriptorsByDefinition.Add(instanceDefinition, documentDescriptor);

					result.Add(documentDescriptor.Name, documentDescriptor);
				}
			}

			return result;
		}

		[NotNull]
		private static IEnumerable<InstanceDescriptor> GetInstanceDescriptors(
			[NotNull] XmlDataQualityDocument document)
		{
			Assert.ArgumentNotNull(document, nameof(document));

			foreach (XmlInstanceDescriptor descriptor in document.GetAllInstanceDescriptors())
			{
				if (descriptor is XmlTestDescriptor testDescriptor)
					yield return XmlDataQualityUtils.CreateTestDescriptor(testDescriptor);

				if (descriptor is XmlTransformerDescriptor transformerDescriptor)
					yield return XmlDataQualityUtils.CreateTransformerDescriptor(
						transformerDescriptor);

				if (descriptor is XmlIssueFilterDescriptor issueFilterDescriptor)
					yield return XmlDataQualityUtils.CreateIssueFilterDescriptor(
						issueFilterDescriptor);
			}
		}

		private static void AssertSameNameImpliesSameDefinition(
			[NotNull] ICollection<InstanceDescriptor> existingDescriptors,
			[NotNull] IEnumerable<InstanceDescriptor> documentDescriptors)
		{
			Assert.ArgumentNotNull(existingDescriptors, nameof(existingDescriptors));
			Assert.ArgumentNotNull(documentDescriptors, nameof(documentDescriptors));

			var existingByName =
				new Dictionary<string, InstanceDescriptor>(existingDescriptors.Count,
				                                           StringComparer.OrdinalIgnoreCase);

			foreach (InstanceDescriptor descriptor in existingDescriptors)
			{
				existingByName.Add(descriptor.Name, descriptor);
			}

			var conflicts = new List<InstanceDescriptor>();

			foreach (InstanceDescriptor descriptor in documentDescriptors)
			{
				if (! existingByName.TryGetValue(descriptor.Name,
				                                 out InstanceDescriptor existingDescriptor))
				{
					continue;
				}

				var documentDefinition = InstanceDefinition.CreateFrom(descriptor);
				var existingDefinition = InstanceDefinition.CreateFrom(existingDescriptor);

				if (! Equals(documentDefinition, existingDefinition))
				{
					conflicts.Add(descriptor);
				}
			}

			if (conflicts.Count == 0)
			{
				return;
			}

			// there are conflicts
			var sb = new StringBuilder();
			foreach (InstanceDescriptor descriptor in conflicts)
			{
				sb.AppendFormat("- {0}", descriptor.Name);
				sb.AppendLine();
			}

			string message =
				string.Format(
					"For the following descriptor{0} in the import document, " +
					"there exists a saved descriptor with the same name but a different implementation." +
					"{1}{1}{2}{1}" +
					"This is not allowed because it would result in non-unique descriptor names. ",
					conflicts.Count == 1
						? string.Empty
						: "s",
					Environment.NewLine, sb);

			Assert.Fail(message);
		}

		private static void AssertUniqueDescriptorNames(
			[NotNull] ICollection<InstanceDescriptor> descriptors, [NotNull] string context)
		{
			var map = new Dictionary<string, InstanceDescriptor>(descriptors.Count,
			                                                     StringComparer.OrdinalIgnoreCase);

			foreach (InstanceDescriptor descriptor in descriptors)
			{
				string name = descriptor.Name;
				Assert.NotNullOrEmpty(name, "Descriptor without name encountered ({0})", context);
				Assert.False(map.ContainsKey(name), "Duplicate {0} name: {1} ({2})",
				             descriptor.TypeDisplayName, name, context);

				map.Add(name, descriptor);
			}
		}

		private static void AssertUniqueInstanceDefinitions(
			[NotNull] ICollection<InstanceDescriptor> descriptors, [NotNull] string context)
		{
			var map = new Dictionary<InstanceDefinition, InstanceDescriptor>(descriptors.Count);

			foreach (InstanceDescriptor descriptor in descriptors)
			{
				var instanceDefinition = InstanceDefinition.CreateFrom(descriptor);

				if (map.TryGetValue(instanceDefinition, out InstanceDescriptor existing))
				{
					Assert.Fail("Duplicate {0} definition: {1}, {2} ({3})",
					            descriptor.TypeDisplayName, descriptor.Name, existing.Name,
					            context);
				}

				map.Add(instanceDefinition, descriptor);
			}
		}

		#endregion

		#region <Categories>

		[NotNull]
		private IDictionary<XmlDataQualityCategory, DataQualityCategory> ImportCategories(
			[CanBeNull] IEnumerable<XmlDataQualityCategory> rootXmlCategories,
			[CanBeNull] IList<XmlDataQualityCategory> allXmlCategories,
			[NotNull] IEnumerable<DdxModel> models)
		{
			var result = new Dictionary<XmlDataQualityCategory, DataQualityCategory>();

			if (Categories == null || rootXmlCategories == null || allXmlCategories == null)
			{
				return result;
			}

			var unmappedXmlCategories = new HashSet<XmlDataQualityCategory>(allXmlCategories);

			// first, try to map by uuid
			IDictionary<string, DataQualityCategory> categoriesByUuid =
				Categories.GetAll()
				          .ToDictionary(c => c.Uuid, StringComparer.OrdinalIgnoreCase);

			IDictionary<string, DdxModel> modelsByName = models.ToDictionary(
				m => m.Name.Trim(),
				StringComparer.OrdinalIgnoreCase);

			Func<string, DdxModel> getModelByName =
				name =>
				{
					if (StringUtils.IsNullOrEmptyOrBlank(name))
					{
						return null;
					}

					DdxModel model;
					return modelsByName.TryGetValue(name.Trim(), out model)
						       ? model
						       : null;
				};

			foreach (XmlDataQualityCategory xmlCategory in allXmlCategories)
			{
				string xmlUuid = xmlCategory.Uuid;
				if (StringUtils.IsNullOrEmptyOrBlank(xmlUuid))
				{
					continue;
				}

				DataQualityCategory category;
				if (categoriesByUuid.TryGetValue(xmlUuid, out category))
				{
					result.Add(xmlCategory, category);

					DataQualityCategory importedCategory =
						XmlDataQualityUtils.CreateDataQualityCategory(xmlCategory,
							category.ParentCategory,
							getModelByName);

					_msg.InfoFormat("Updating existing category '{0}' (matched by Uuid)",
					                category.GetQualifiedName());
					XmlDataQualityUtils.TransferProperties(importedCategory, category);

					unmappedXmlCategories.Remove(xmlCategory);
				}
			}

			if (unmappedXmlCategories.Count > 0)
			{
				// there are unmapped xml categories - try to map them by name, along the hierarchy
				ImportCategoriesByName(rootXmlCategories,
				                       Categories.GetTopLevelCategories(),
				                       Categories,
				                       result,
				                       getModelByName);
			}

			return result;
		}

		private static void ImportCategoriesByName(
			[NotNull] IEnumerable<XmlDataQualityCategory> xmlCategoriesAtLevel,
			[NotNull] IEnumerable<DataQualityCategory> categoriesAtLevel,
			[NotNull] IDataQualityCategoryRepository categoryRepository,
			[NotNull] IDictionary<XmlDataQualityCategory, DataQualityCategory> mappedCategories,
			[NotNull] Func<string, DdxModel> getModelByName,
			[CanBeNull] DataQualityCategory parentCategory = null)
		{
			Assert.ArgumentNotNull(xmlCategoriesAtLevel, nameof(xmlCategoriesAtLevel));
			Assert.ArgumentNotNull(categoriesAtLevel, nameof(categoriesAtLevel));
			Assert.ArgumentNotNull(categoryRepository, nameof(categoryRepository));
			Assert.ArgumentNotNull(mappedCategories, nameof(mappedCategories));
			Assert.ArgumentNotNull(getModelByName, nameof(getModelByName));

			IDictionary<string, DataQualityCategory> categoriesAtLevelByName =
				categoriesAtLevel.ToDictionary(c => c.Name,
				                               StringComparer.OrdinalIgnoreCase);

			foreach (XmlDataQualityCategory xmlCategory in xmlCategoriesAtLevel)
			{
				DataQualityCategory category;
				mappedCategories.TryGetValue(xmlCategory, out category);

				if (category != null)
				{
					XmlDataQualityUtils.AssignParentCategory(category, parentCategory);
				}
				else
				{
					if (categoriesAtLevelByName.TryGetValue(xmlCategory.Name, out category))
					{
						DataQualityCategory importedCategory =
							XmlDataQualityUtils.CreateDataQualityCategory(xmlCategory,
								parentCategory,
								getModelByName);

						_msg.InfoFormat("Updating existing category '{0}' (matched by name)",
						                category.GetQualifiedName());

						XmlDataQualityUtils.TransferProperties(importedCategory, category);

						mappedCategories.Add(xmlCategory, category);
					}
					else
					{
						// no category with same name found - create a new one if it is not already mapped by Uuid

						// no corresponding category - create one
						category = XmlDataQualityUtils.CreateDataQualityCategory(
							xmlCategory, parentCategory, getModelByName);

						_msg.InfoFormat("Adding new category '{0}'", category.GetQualifiedName());
						categoryRepository.Save(category);

						mappedCategories.Add(xmlCategory, category);
					}
				}

				if (xmlCategory.SubCategories != null)
				{
					// recurse to map sub categories
					ImportCategoriesByName(xmlCategory.SubCategories,
					                       category.SubCategories,
					                       categoryRepository,
					                       mappedCategories,
					                       getModelByName,
					                       category);
				}
			}
		}

		#endregion

		#region <Configurations>

		[NotNull]
		private IDictionary<string, InstanceConfiguration> ImportConfigurations(
			[NotNull] XmlDataQualityDocumentCache documentCache,
			[NotNull] IDictionary<string, InstanceDescriptor> descriptorsByName,
			[NotNull] IDictionary<string, DdxModel> modelsByWorkspaceId,
			[NotNull] IDictionary<XmlDataQualityCategory, DataQualityCategory> categories,
			bool ignoreConditionsForUnknownDatasets)
		{
			IList<InstanceConfiguration> configurations = InstanceConfigurations.GetAll();

			IDictionary<string, InstanceConfiguration> configsByName =
				GetExistingConfigurationsByEscapedName(configurations);
			IDictionary<string, InstanceConfiguration> configsByUuid =
				configurations.ToDictionary(c => c.Uuid, StringComparer.OrdinalIgnoreCase);

			foreach (XmlInstanceConfiguration xmlInstanceConfiguration in documentCache
				         .EnumReferencedConfigurationInstances())
			{
				InstanceConfiguration existing = GetMatchingConfiguration(
					xmlInstanceConfiguration, configsByName, configsByUuid);

				if (existing == null)
				{
					InstanceConfiguration imported;
					if (xmlInstanceConfiguration is XmlTransformerConfiguration xmlTransformer)
					{
						var descriptor =
							GetDescriptor<TransformerDescriptor>(
								descriptorsByName, xmlTransformer.TransformerDescriptorName);

						imported = new TransformerConfiguration(xmlTransformer.Name, descriptor);
					}
					else if (xmlInstanceConfiguration is XmlIssueFilterConfiguration xmlIssueFilter)
					{
						var descriptor =
							GetDescriptor<IssueFilterDescriptor>(
								descriptorsByName, xmlIssueFilter.IssueFilterDescriptorName);

						imported = new IssueFilterConfiguration(xmlIssueFilter.Name, descriptor);
					}
					else if (xmlInstanceConfiguration is XmlQualityCondition xmlQualityCondition)
					{
						var descriptor =
							GetDescriptor<TestDescriptor>(
								descriptorsByName, xmlQualityCondition.TestDescriptorName);

						imported = new QualityCondition(xmlQualityCondition.Name, descriptor);
					}
					else
					{
						throw new NotSupportedException(
							$"Unsupported XmlInstanceConfiguration type: {xmlInstanceConfiguration.GetType()}");
					}

					_msg.InfoFormat("Adding new {0} '{1}'", imported.TypeDisplayName,
					                imported.Name);
					InstanceConfigurations.Save(imported);
					_msg.VerboseDebug(() => $"Saved {imported.TypeDisplayName} {imported.Name} " +
					                        $"(id {imported.Id}, uuid {imported.Uuid})");

					configsByName.Add(imported.Name, imported);
				}
			}

			foreach (XmlInstanceConfiguration xmlInstanceConfiguration in documentCache
				         .EnumReferencedConfigurationInstances())
			{
				InstanceConfiguration config = GetMatchingConfiguration(
					xmlInstanceConfiguration, configsByName, configsByUuid);

				DataQualityCategory category = GetCategory(
					xmlInstanceConfiguration, documentCache, categories);

				if (xmlInstanceConfiguration is XmlTransformerConfiguration xmlTransformer)
				{
					XmlDataQualityUtils.UpdateTransformerConfiguration(
						(TransformerConfiguration) config, xmlTransformer, category);
				}
				else if (xmlInstanceConfiguration is XmlIssueFilterConfiguration xmlIssueFilter)
				{
					XmlDataQualityUtils.UpdateIssueFilterConfiguration(
						(IssueFilterConfiguration) config, xmlIssueFilter, category);
				}
				else if (xmlInstanceConfiguration is XmlQualityCondition xmlCondition)
				{
					QualityCondition qualityCondition = (QualityCondition) config;

					XmlDataQualityUtils.UpdateQualityCondition(
						qualityCondition, xmlCondition, category);

					IDictionary<string, IssueFilterConfiguration> issueFiltersByName =
						configsByName.Values
						             .OfType<IssueFilterConfiguration>()
						             .ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

					XmlDataQualityUtils.UpdateIssueFilters(
						qualityCondition, xmlCondition, issueFiltersByName,
						documentCache.IssueFilterExpressionParser);
				}
				else
				{
					throw new NotSupportedException(
						$"Unsupported XmlInstanceConfiguration type: {xmlInstanceConfiguration.GetType()}");
				}

				if (configsByName.TryGetValue(xmlInstanceConfiguration.Name,
				                              out InstanceConfiguration existingForName))
				{
					// There exists an existing configuration for the new name 

					// Fail if it is not the same instance as the matching existing configuration
					// (this can be the case when the match is made by the Uuid, and the xml configuration
					// has the same name as an existing configuration with a different Uuid)
					Assert.True(ReferenceEquals(existingForName, config),
					            "Instance configuration name '{0}' for UUID {1} in the xml document is equal " +
					            "to the name of an existing instance configuration with a different UUID ({2}). " +
					            "Unable to import document (duplicate instance configuration names would result)",
					            xmlInstanceConfiguration.Name, config.Uuid, existingForName.Uuid);
				}
				else
				{
					// there exists no existing configuration with the name from the xml document
					configsByName.Add(xmlInstanceConfiguration.Name, config);
				}

				config.ClearParameterValues();

				IInstanceInfo instanceInfo = null;
				try
				{
					instanceInfo =
						InstanceDescriptorUtils.GetInstanceInfo(config.InstanceDescriptor);
				}
				catch (Exception e)
				{
					// TODO: Handle the case where the xmlInstanceConfiguration references a different, valid descriptor.
					_msg.Warn(
						$"Error loading instance descriptor {config.InstanceDescriptor.Name}. " +
						"It will be ignored.", e);
				}

				Assert.NotNull(instanceInfo, "Cannot create instance info for {0}", config);

				Dictionary<string, TestParameter> testParametersByName =
					instanceInfo.Parameters.ToDictionary(parameter => parameter.Name,
					                                     StringComparer.OrdinalIgnoreCase);

				foreach (XmlTestParameterValue xmlParamValue in xmlInstanceConfiguration
					         .ParameterValues)
				{
					TestParameter testParameter;
					if (! testParametersByName.TryGetValue(xmlParamValue.TestParameterName,
					                                       out testParameter))
					{
						Assert.Fail(
							"The name '{0}' as a test parameter for {1} '{2}' " +
							"defined in import document does not match descriptor.",
							xmlParamValue.TestParameterName, config.TypeDisplayName, config.Name);
					}

					TestParameterValue parameterValue;

					if (! string.IsNullOrWhiteSpace(xmlParamValue.TransformerName))
					{
						var transformerConfig =
							(TransformerConfiguration) configsByName[xmlParamValue.TransformerName];

						Assert.NotNull(transformerConfig,
						               "Transformer '{0}' defined in dataset parameter for '{1}' does not exist",
						               xmlParamValue.TransformerName,
						               xmlInstanceConfiguration.Name);

						if (xmlParamValue is XmlDatasetTestParameterValue datasetValue)
						{
							parameterValue = new DatasetTestParameterValue(testParameter, null,
								datasetValue.WhereClause, datasetValue.UsedAsReferenceData);
						}
						else if (xmlParamValue is XmlScalarTestParameterValue scalarValue)
						{
							parameterValue =
								new ScalarTestParameterValue(testParameter, scalarValue.Value);
						}
						else
						{
							throw new InvalidProgramException("Unhandled TestParameterValue " +
							                                  xmlParamValue.TestParameterName);
						}

						parameterValue.ValueSource = transformerConfig;
					}
					else if (xmlParamValue is XmlDatasetTestParameterValue datasetValue)
					{
						parameterValue = XmlDataQualityUtils.CreateDatasetTestParameterValue(
							testParameter, datasetValue,
							Assert.NotNull(xmlInstanceConfiguration.Name), modelsByWorkspaceId,
							Datasets.Get, TestParameterDatasetValidator, true);

						if (parameterValue == null)
						{
							// TODO handle the ignoreConditionsForUnknownDatasets bool
							//datasetSettings.UnknownDatasetParameters.Add(datasetValue);
						}
					}
					else if (xmlParamValue is XmlScalarTestParameterValue scalarValue)
					{
						parameterValue =
							new ScalarTestParameterValue(testParameter, scalarValue.Value);
					}
					else
					{
						throw new InvalidProgramException("Unhandled TestParameterValue " +
						                                  xmlParamValue.TestParameterName);
					}

					if (parameterValue != null)
					{
						config.AddParameterValue(parameterValue);
					}
				}
			}

			WarnForNonUniqueConfigurationsUuid(configsByName);

			return configsByName;
		}

		private static void WarnForNonUniqueConfigurationsUuid(
			[NotNull] IDictionary<string, InstanceConfiguration> availableConfigurationsByName)
		{
			// Provide good error message in case of non-unique UUID (rather than DB unique constraint violation)

			var uuidSet = new HashSet<string>();
			foreach (InstanceConfiguration instanceConfiguration in availableConfigurationsByName
				         .Values)
			{
				// Log all problems rather than failing at the first possible opportunity:
				if (uuidSet.Contains(instanceConfiguration.Uuid))
				{
					var configurations = availableConfigurationsByName.Values
						.Where(c => c.Uuid == instanceConfiguration.Uuid).ToList();

					_msg.Warn(
						$"Non-unique UUID in instance configurations (the process will fail!): " +
						$"{StringUtils.Concatenate(configurations, c => $"{c.Name} ({c.Uuid})", ", ")}");
				}

				uuidSet.Add(instanceConfiguration.Uuid);
			}
		}

		[NotNull]
		private static T GetDescriptor<T>(
			[NotNull] IDictionary<string, InstanceDescriptor> descriptorsByName, string name)
			where T : InstanceDescriptor
		{
			Assert.True(StringUtils.IsNotEmpty(name), "name");

			// NOTE: The previously persisted InstanceDescriptor must be referenced to avoid TransientObjectException
			// ("object references an unsaved transient instance")
			if (! descriptorsByName.TryGetValue(name.Trim(),
			                                    out InstanceDescriptor instanceDescriptor))
			{
				Assert.Fail("Descriptor '{0}' does not exist", name);
			}

			var result = instanceDescriptor as T;
			if (result == null)
			{
				Assert.Fail("Descriptor '{0}' is not of type {1}", name, typeof(T).Name);
			}

			return result;
		}

		[CanBeNull]
		private static DataQualityCategory GetCategory(
			[NotNull] XmlInstanceConfiguration xmlInstanceConfiguration,
			[NotNull] XmlDataQualityDocumentCache documentCache,
			IDictionary<XmlDataQualityCategory, DataQualityCategory> categories)
		{
			DataQualityCategory result = null;

			if (xmlInstanceConfiguration is XmlTransformerConfiguration)
			{
				foreach (var pair in documentCache.TransformersWithCategories)
				{
					if (pair.Key.Equals(xmlInstanceConfiguration))
					{
						XmlDataQualityCategory xmlCategory = pair.Value;
						result = xmlCategory == null ? null : categories[xmlCategory];
					}
				}
			}
			else if (xmlInstanceConfiguration is XmlIssueFilterConfiguration)
			{
				foreach (var pair in documentCache.IssueFiltersWithCategories)
				{
					if (pair.Key.Equals(xmlInstanceConfiguration))
					{
						XmlDataQualityCategory xmlCategory = pair.Value;
						result = xmlCategory == null ? null : categories[xmlCategory];
					}
				}
			}
			else if (xmlInstanceConfiguration is XmlQualityCondition)
			{
				foreach (var pair in documentCache.QualityConditionsWithCategories)
				{
					if (pair.Key.Equals(xmlInstanceConfiguration))
					{
						XmlDataQualityCategory xmlCategory = pair.Value;
						result = xmlCategory == null ? null : categories[xmlCategory];
					}
				}
			}

			return result;
		}

		[NotNull]
		private static IDictionary<string, T>
			GetExistingConfigurationsByEscapedName<T>(
				[NotNull] IEnumerable<T> instanceConfigurations) where T : InstanceConfiguration
		{
			var result = new Dictionary<string, T>();

			foreach (T instanceConfiguration in instanceConfigurations)
			{
				string name = instanceConfiguration.Name;
				Assert.True(StringUtils.IsNotEmpty(name),
				            "Name not defined for {0} id={1} in data dictionary",
				            instanceConfiguration.TypeDisplayName, instanceConfiguration.Id);

				string escapedName = XmlUtils.EscapeInvalidCharacters(name.Trim());

				if (result.ContainsKey(escapedName))
				{
					Assert.Fail(
						"Duplicate {0} name in data dictionary: '{1}'",
						instanceConfiguration.TypeDisplayName, instanceConfiguration.Name);
				}

				result.Add(escapedName, instanceConfiguration);
			}

			return result;
		}

		[CanBeNull]
		private static T GetMatchingConfiguration<T>(
			[NotNull] XmlInstanceConfiguration xmlInstanceConfiguration,
			[NotNull] IDictionary<string, T> existingConfigsByName,
			[NotNull] IDictionary<string, T> existingConfigsByUuid) where T : InstanceConfiguration
		{
			T result;

			string uuid = xmlInstanceConfiguration.Uuid;
			if (StringUtils.IsNotEmpty(uuid) &&
			    existingConfigsByUuid.TryGetValue(uuid.Trim(), out result))
			{
				return result;
			}

			string name = xmlInstanceConfiguration.Name;
			Assert.True(StringUtils.IsNotEmpty(name), "undefined name");

			// no uuid, or no matching uuid - match by (trimmed) name
			return existingConfigsByName.TryGetValue(name.Trim(), out result)
				       ? result
				       : null;
		}

		#endregion

		#region <QualitySpecification>

		[NotNull]
		private IList<QualitySpecification> ImportQualitySpecifications(
			[NotNull] IList<QualitySpecification> existingQualitySpecifications,
			[NotNull] IEnumerable<KeyValuePair<XmlQualitySpecification, XmlDataQualityCategory>>
				xmlSpecificationsToImport,
			[CanBeNull] IDictionary<XmlDataQualityCategory, DataQualityCategory> categories,
			[NotNull] IDictionary<string, QualityCondition> qualityConditionsByName,
			QualitySpecificationImportType importType,
			bool ignoreMissingConditions)
		{
			Assert.ArgumentNotNull(existingQualitySpecifications,
			                       nameof(existingQualitySpecifications));
			Assert.ArgumentNotNull(xmlSpecificationsToImport,
			                       nameof(xmlSpecificationsToImport));
			Assert.ArgumentNotNull(qualityConditionsByName, nameof(qualityConditionsByName));

			var result = new List<QualitySpecification>();

			IDictionary<string, QualitySpecification> specificationsByUuid =
				existingQualitySpecifications.ToDictionary(qs => qs.Uuid,
				                                           StringComparer.OrdinalIgnoreCase);
			IDictionary<string, QualitySpecification> specificationsByName =
				GetExistingSpecificationsByEscapedName(existingQualitySpecifications);

			foreach (
				KeyValuePair<XmlQualitySpecification, XmlDataQualityCategory> pair in
				xmlSpecificationsToImport)
			{
				XmlQualitySpecification xmlSpecification = pair.Key;
				XmlDataQualityCategory xmlCategory = pair.Value;

				XmlDataQualityUtils.AssertUniqueQualitySpecificationElementNames(xmlSpecification);

				DataQualityCategory category = xmlCategory == null || categories == null
					                               ? null
					                               : categories[xmlCategory];

				QualitySpecification qualitySpecification =
					GetMatchingQualitySpecification(xmlSpecification,
					                                specificationsByName,
					                                specificationsByUuid);

				if (qualitySpecification != null)
				{
					qualitySpecification.Clear();

					XmlDataQualityUtils.UpdateQualitySpecification(qualitySpecification,
						xmlSpecification,
						qualityConditionsByName,
						category,
						ignoreMissingConditions);

					result.Add(qualitySpecification);
				}
				else
				{
					if (importType == QualitySpecificationImportType.UpdateOrAdd)
					{
						QualitySpecification newSpecification =
							XmlDataQualityUtils.CreateQualitySpecification(xmlSpecification,
								qualityConditionsByName,
								category, ignoreMissingConditions);

						QualitySpecifications.Save(newSpecification);

						result.Add(newSpecification);

						_msg.InfoFormat("Quality specification '{0}' created",
						                xmlSpecification.Name);
					}
				}
			}

			if (importType == QualitySpecificationImportType.UpdateOnly)
			{
				foreach (
					QualitySpecification existingQualitySpecification in
					existingQualitySpecifications)
				{
					if (! result.Contains(existingQualitySpecification))
					{
						_msg.Warn(string.Format(
							          "Quality specification '{0}' does not exist in document",
							          existingQualitySpecification.Name));
					}
				}
			}

			return result;
		}

		[NotNull]
		private static IDictionary<string, QualitySpecification>
			GetExistingSpecificationsByEscapedName(
				[NotNull] IEnumerable<QualitySpecification> qualitySpecifications)
		{
			var result = new Dictionary<string, QualitySpecification>();

			foreach (QualitySpecification qualitySpecification in qualitySpecifications)
			{
				string name = qualitySpecification.Name;
				Assert.True(StringUtils.IsNotEmpty(name),
				            "Name not defined for quality specification id={0} in data dictionary",
				            qualitySpecification.Id);

				string escapedName = XmlUtils.EscapeInvalidCharacters(name.Trim());

				if (result.ContainsKey(escapedName))
				{
					Assert.Fail(
						"Duplicate quality specification name in data dictionary: '{0}' ",
						qualitySpecification.Name);
				}

				result.Add(escapedName, qualitySpecification);
			}

			return result;
		}

		[CanBeNull]
		private static QualitySpecification GetMatchingQualitySpecification(
			[NotNull] XmlQualitySpecification xmlQualitySpecification,
			[NotNull] IDictionary<string, QualitySpecification> existingSpecificationsByName,
			[NotNull] IDictionary<string, QualitySpecification> existingSpecificationsByUuid)
		{
			QualitySpecification result;

			string uuid = xmlQualitySpecification.Uuid;
			if (StringUtils.IsNotEmpty(uuid) &&
			    existingSpecificationsByUuid.TryGetValue(uuid.Trim(), out result))
			{
				return result;
			}

			string name = xmlQualitySpecification.Name;
			Assert.True(StringUtils.IsNotEmpty(name), "undefined name");

			// no uuid, or no matching uuid - match by (trimmed) name
			return existingSpecificationsByName.TryGetValue(name.Trim(), out result)
				       ? result
				       : null;
		}

		#endregion
	}
}
