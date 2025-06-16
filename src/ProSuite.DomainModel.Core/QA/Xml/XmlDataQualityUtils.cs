using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.Commons.Xml;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.Schemas;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.Core.QA.Xml
{
	public static class XmlDataQualityUtils
	{
		private enum QaSpecVersion
		{
			v2_0,
			v3_0
		}

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull]
		public static XmlDataQualityDocument ReadXmlDocument(
			[NotNull] StreamReader xml,
			[NotNull] out IList<XmlQualitySpecification> qualitySpecifications)
		{
			Assert.ArgumentNotNull(xml, nameof(xml));

			XmlDataQualityDocument document = Deserialize(xml);

			Assert.ArgumentCondition(document.GetAllQualitySpecifications().Any(),
			                         "The document does not contain any quality specifications");

			AssertUniqueQualitySpecificationNames(document);
			AssertUniqueQualityConditionNames(document);
			AssertUniqueInstanceDescriptorNames(document);

			qualitySpecifications = document.GetAllQualitySpecifications()
			                                .Select(p => p.Key)
			                                .Where(qs => qs.Elements.Count > 0)
			                                .ToList();

			return document;
		}

		[NotNull]
		public static XmlDataQualityDocument Deserialize([NotNull] StreamReader xml)
		{
			Assert.ArgumentNotNull(xml, nameof(xml));

			// TODO: allow different schemas
			string schema = GetSchema(xml, out QaSpecVersion version);
			try
			{
				switch (version)
				{
					case QaSpecVersion.v2_0:
						return XmlUtils.Deserialize<XmlDataQualityDocument20>(xml, schema);
					case QaSpecVersion.v3_0:
						return XmlUtils.Deserialize<XmlDataQualityDocument30>(xml, schema);
					default:
						throw new InvalidOperationException("Unknown schema");
				}
			}
			catch (Exception e)
			{
				throw new XmlDeserializationException($"Error deserializing file: {e.Message}", e);
			}
		}

		private static string GetSchema(StreamReader xml, out QaSpecVersion version)
		{
			string schema = null;
			version = QaSpecVersion.v3_0;

			if (xml is StreamReader r)
			{
				using (var reader = XmlReader.Create(r.BaseStream))
				{
					while (reader.Read())
					{
						if (reader.NodeType == XmlNodeType.Element)
						{
							for (int iAttr = 0; iAttr < reader.AttributeCount; iAttr++)
							{
								reader.MoveToAttribute(iAttr);
								if (reader.Name == "xmlns" && reader.Value ==
								    "urn:EsriDE.ProSuite.QA.QualitySpecifications-2.0")
								{
									schema = Schema.ProSuite_QA_QualitySpecifications_2_0;
									version = QaSpecVersion.v2_0;
								}
							}

							break;
						}
					}
				}

				r.BaseStream.Seek(0, SeekOrigin.Begin);
			}

			if (schema == null)
			{
				schema = Schema.ProSuite_QA_QualitySpecifications_3_0;
			}

			return schema;
		}

		public static void ExportXmlDocument<T>([NotNull] T document, [NotNull] XmlWriter xmlWriter)
			where T : XmlDataQualityDocument
		{
			Assert.ArgumentNotNull(document, nameof(document));
			Assert.ArgumentNotNull(xmlWriter, nameof(xmlWriter));

			XmlUtils.Serialize(document, xmlWriter);
		}

		public static void AssertUniqueWorkspaceIds(
			[NotNull] XmlDataQualityDocument document)
		{
			Assert.ArgumentNotNull(document, nameof(document));

			if (document.Workspaces == null)
			{
				return;
			}

			var ids = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

			foreach (XmlWorkspace workspace in document.Workspaces)
			{
				string id = workspace.ID;

				Assert.True(StringUtils.IsNotEmpty(id), "Missing workspace ID in document");

				string trimmedId = id.Trim();

				if (ids.Contains(trimmedId))
				{
					Assert.Fail("Duplicate workspace ID in document: {0}", trimmedId);
				}

				ids.Add(trimmedId);
			}
		}

		public static void AssertUniqueQualitySpecificationUuids(
			[NotNull] XmlDataQualityDocument document)
		{
			Assert.ArgumentNotNull(document, nameof(document));

			if (document.QualitySpecifications == null)
			{
				return;
			}

			var uuids = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

			foreach (XmlQualitySpecification xmlQualitySpecification in
			         document.QualitySpecifications)
			{
				string uuid = xmlQualitySpecification.Uuid;

				if (StringUtils.IsNullOrEmptyOrBlank(uuid))
				{
					continue;
				}

				string trimmedUuid = uuid.Trim();

				if (uuids.Contains(trimmedUuid))
				{
					Assert.Fail("Duplicate UUID in document: {0} (quality specification {1})",
					            trimmedUuid, xmlQualitySpecification.Name);
				}

				uuids.Add(trimmedUuid);
			}
		}

		public static void AssertUniqueQualityConditionsUuids(
			[NotNull] XmlDataQualityDocument document)
		{
			Assert.ArgumentNotNull(document, nameof(document));

			AssertUniqueInstanceConfigurationUuids(
				document.GetAllQualityConditions().Select(p => p.Key), "quality condition");
		}

		public static void AssertUniqueTransformerUuids(
			[NotNull] XmlDataQualityDocument document)
		{
			Assert.ArgumentNotNull(document, nameof(document));
			AssertUniqueInstanceConfigurationUuids(
				document.GetAllTransformers().Select(p => p.Key), "transformer");
		}

		public static void AssertUniqueIssueFilterUuids(
			[NotNull] XmlDataQualityDocument document)
		{
			Assert.ArgumentNotNull(document, nameof(document));
			AssertUniqueInstanceConfigurationUuids(
				document.GetAllIssueFilters().Select(p => p.Key), "issue filter");
		}

		public static void AssertUniqueInstanceConfigurationUuids<T>(
			[CanBeNull] IEnumerable<T> instanceConfigurations, [NotNull] string type)
			where T : XmlInstanceConfiguration
		{
			if (instanceConfigurations == null)
			{
				return;
			}

			var uuids = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

			foreach (T xmlInstanceConfiguration in instanceConfigurations)
			{
				foreach (string uuid in new[]
				                        {
					                        xmlInstanceConfiguration.Uuid,
					                        xmlInstanceConfiguration.VersionUuid
				                        })
				{
					if (StringUtils.IsNullOrEmptyOrBlank(uuid))
					{
						continue;
					}

					string trimmedUuid = uuid.Trim();

					if (uuids.Contains(trimmedUuid))
					{
						Assert.Fail(
							$"Duplicate UUID in document: {trimmedUuid} ({type} {xmlInstanceConfiguration.Name})");
					}

					uuids.Add(trimmedUuid);
				}
			}
		}

		public static void AssertUniqueCategoryUuids(
			[NotNull] XmlDataQualityDocument document)
		{
			Assert.ArgumentNotNull(document, nameof(document));

			var uuids = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

			foreach (XmlDataQualityCategory xmlCategory in document.GetAllCategories())
			{
				string uuid = xmlCategory.Uuid;

				if (StringUtils.IsNullOrEmptyOrBlank(uuid))
				{
					continue;
				}

				string trimmedUuid = uuid.Trim();

				if (uuids.Contains(trimmedUuid))
				{
					Assert.Fail("Duplicate UUID in document: {0} (category {1})",
					            trimmedUuid, xmlCategory.Name);
				}

				uuids.Add(trimmedUuid);
			}
		}

		public static void AssertUniqueQualitySpecificationNames(
			[NotNull] XmlDataQualityDocument document)
		{
			Assert.ArgumentNotNull(document, nameof(document));

			var names = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

			foreach (XmlQualitySpecification xmlQualitySpecification in
			         document.GetAllQualitySpecifications().Select(p => p.Key))
			{
				string name = xmlQualitySpecification.Name;

				Assert.True(StringUtils.IsNotEmpty(name),
				            "Missing quality specification name in document");

				string trimmedName = name.Trim();

				if (names.Contains(trimmedName))
				{
					Assert.Fail("Duplicate quality specification name in document: {0}",
					            trimmedName);
				}

				names.Add(trimmedName);
			}
		}

		public static void AssertUniqueQualitySpecificationElementNames(
			[NotNull] XmlQualitySpecification qualitySpecification)
		{
			Assert.ArgumentNotNull(qualitySpecification, nameof(qualitySpecification));

			var names = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

			foreach (XmlQualitySpecificationElement xmlQualitySpecificationElement in
			         qualitySpecification.Elements)
			{
				string name = xmlQualitySpecificationElement.QualityConditionName;

				Assert.True(StringUtils.IsNotEmpty(name),
				            "Missing quality condition reference name in document");

				string trimmedName = name.Trim();

				if (names.Contains(trimmedName))
				{
					Assert.Fail("Duplicate quality condition reference in document: {0}",
					            trimmedName);
				}

				names.Add(trimmedName);
			}
		}

		public static void AssertUniqueQualityConditionNames(
			[NotNull] XmlDataQualityDocument document)
		{
			Assert.ArgumentNotNull(document, nameof(document));
			AssertUniqueInstanceConfigurationNames(
				document.GetAllQualityConditions().Select(p => p.Key), "quality condition");
		}

		public static void AssertUniqueTransformerNames(
			[NotNull] XmlDataQualityDocument document)
		{
			Assert.ArgumentNotNull(document, nameof(document));
			AssertUniqueInstanceConfigurationNames(
				document.GetAllTransformers().Select(p => p.Key), "transformer");
		}

		public static void AssertUniqueIssueFilterNames(
			[NotNull] XmlDataQualityDocument document)
		{
			Assert.ArgumentNotNull(document, nameof(document));
			AssertUniqueInstanceConfigurationNames(
				document.GetAllIssueFilters().Select(p => p.Key), "issue filter");
		}

		public static void AssertUniqueInstanceConfigurationNames<T>(
			[CanBeNull] IEnumerable<T> instanceConfigurations, [NotNull] string type)
			where T : XmlInstanceConfiguration
		{
			if (instanceConfigurations == null)
			{
				return;
			}

			var names = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

			foreach (T xmlInstanceConfiguration in instanceConfigurations)
			{
				string name = xmlInstanceConfiguration.Name;

				Assert.True(StringUtils.IsNotEmpty(name), $"Missing {type} name in document");

				string trimmedName = name.Trim();

				if (names.Contains(trimmedName))
				{
					Assert.Fail($"Duplicate {type} name in document: {trimmedName}");
				}

				names.Add(trimmedName);
			}
		}

		public static void AssertUniqueQualifiedCategoryNames(
			[NotNull] XmlDataQualityDocument document)
		{
			Assert.ArgumentNotNull(document, nameof(document));

			if (document.Categories != null)
			{
				AssertUniqueCategoryNames(document.Categories);
			}
		}

		private static void AssertUniqueCategoryNames(
			[NotNull] IEnumerable<XmlDataQualityCategory> categories)
		{
			var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (XmlDataQualityCategory category in categories)
			{
				if (category.SubCategories != null)
				{
					AssertUniqueCategoryNames(category.SubCategories);
				}

				string name = category.Name;

				Assert.True(StringUtils.IsNotEmpty(name), "Missing category name in document");

				string trimmedName = name.Trim();

				if (names.Contains(trimmedName))
				{
					Assert.Fail("Duplicate category name in document: {0}", trimmedName);
				}

				names.Add(trimmedName);
			}
		}

		public static void AssertUniqueInstanceDescriptorNames(
			[NotNull] XmlDataQualityDocument document)
		{
			Assert.ArgumentNotNull(document, nameof(document));

			IEnumerable<XmlInstanceDescriptor> xmlInstanceDescriptors =
				document.GetAllInstanceDescriptors();
			AssertUniqueInstanceDescriptorNames(xmlInstanceDescriptors,
			                                    "instance descriptor");
		}

		public static void AssertUniqueInstanceDescriptorNames<T>(
			[CanBeNull] IEnumerable<T> instanceDescriptors, [NotNull] string type)
			where T : XmlInstanceDescriptor
		{
			if (instanceDescriptors == null)
			{
				return;
			}

			// NOTE: In the database the names are case sensitive. Hence it is possible to have
			// qaNoGaps(0) and QaNoGaps(0) as different names.
			StringComparer compareCaseSensitive = StringComparer.Ordinal;
			var names = new HashSet<string>(compareCaseSensitive);

			foreach (T xmlInstanceDescriptor in instanceDescriptors)
			{
				string name = xmlInstanceDescriptor.Name;
				Assert.True(StringUtils.IsNotEmpty(name), $"Missing {type} name in document");

				string trimmedName = name.Trim();

				if (names.Contains(trimmedName))
				{
					Assert.Fail($"Duplicate {type} name in document: {trimmedName}");
				}

				names.Add(trimmedName);
			}
		}

		[CanBeNull]
		public static XmlQualitySpecification FindXmlQualitySpecification(
			[NotNull] XmlDataQualityDocument document,
			[NotNull] string qualitySpecificationName,
			[CanBeNull] out XmlDataQualityCategory category)
		{
			Assert.ArgumentNotNull(document, nameof(document));

			return FindXmlQualitySpecification(document.GetAllQualitySpecifications(),
			                                   qualitySpecificationName,
			                                   out category);
		}

		[CanBeNull]
		public static XmlQualitySpecification FindXmlQualitySpecification(
			[NotNull] IEnumerable<XmlQualitySpecification> qualitySpecifications,
			[NotNull] string qualitySpecificationName)
		{
			Assert.ArgumentNotNull(qualitySpecifications, nameof(qualitySpecifications));
			Assert.ArgumentNotNullOrEmpty(qualitySpecificationName,
			                              nameof(qualitySpecificationName));

			return qualitySpecifications.FirstOrDefault(
				qs => string.Equals(qs.Name, qualitySpecificationName,
				                    StringComparison.OrdinalIgnoreCase));
		}

		[CanBeNull]
		public static XmlQualitySpecification FindXmlQualitySpecification(
			[NotNull] IEnumerable<KeyValuePair<XmlQualitySpecification, XmlDataQualityCategory>>
				qualitySpecifications,
			[NotNull] string qualitySpecificationName,
			[CanBeNull] out XmlDataQualityCategory category)
		{
			Assert.ArgumentNotNull(qualitySpecifications, nameof(qualitySpecifications));
			Assert.ArgumentNotNullOrEmpty(qualitySpecificationName,
			                              nameof(qualitySpecificationName));

			string trimmedName = qualitySpecificationName.Trim();

			var availableNames = new List<string>();

			foreach (
				KeyValuePair<XmlQualitySpecification, XmlDataQualityCategory> pair in
				qualitySpecifications)
			{
				availableNames.Add(pair.Key.Name);
				if (string.Equals(pair.Key.Name, trimmedName,
				                  StringComparison.OrdinalIgnoreCase))
				{
					category = pair.Value;
					return pair.Key;
				}
			}

			_msg.InfoFormat(
				"No match found for desired specification {0}. Available specifications are: {1}",
				qualitySpecificationName, StringUtils.Concatenate(availableNames, ", "));

			category = null;
			return null;
		}

		[NotNull]
		public static IList<XmlWorkspace> GetReferencedWorkspaces(
			[NotNull] XmlDataQualityDocumentCache documentCache,
			out bool hasUndefinedWorkspaceReference)
		{
			Assert.ArgumentNotNull(documentCache, nameof(documentCache));

			var referencedWorkspaceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			hasUndefinedWorkspaceReference = false;
			CollectWorkspaceIds(referencedWorkspaceIds,
			                    documentCache.QualityConditionsWithCategories.Select(x => x.Key),
			                    documentCache, ref hasUndefinedWorkspaceReference);

			return documentCache.Workspaces?.Where(
				       workspace => referencedWorkspaceIds.Contains(workspace.ID)).ToList()
			       ?? new List<XmlWorkspace>();
		}

		private static void CollectWorkspaceIds<T>(
			[NotNull] HashSet<string> workspaceIds,
			[NotNull] IEnumerable<T> instanceConfigurations,
			[NotNull] XmlDataQualityDocumentCache documentCache,
			ref bool hasUndefinedWorkspaceReference)
			where T : XmlInstanceConfiguration
		{
			foreach (T xmlInstanceConfig in instanceConfigurations)
			{
				foreach (XmlTestParameterValue xmlTestParameterValue in
				         xmlInstanceConfig.EnumParameterValues(ignoreEmptyValues: true))
				{
					if (! string.IsNullOrWhiteSpace(xmlTestParameterValue.TransformerName))
					{
						if (! documentCache.TryGetTransformer(
							    xmlTestParameterValue.TransformerName,
							    out XmlTransformerConfiguration transformerConfiguration))
						{
							hasUndefinedWorkspaceReference = true;
							// TODO: handle missing
							continue;
						}

						CollectWorkspaceIds(workspaceIds, new[] { transformerConfiguration },
						                    documentCache,
						                    ref hasUndefinedWorkspaceReference);
					}

					var datasetTestParameterValue =
						xmlTestParameterValue as XmlDatasetTestParameterValue;
					if (datasetTestParameterValue == null)
					{
						continue;
					}

					if (string.IsNullOrEmpty(datasetTestParameterValue.WorkspaceId))
					{
						if (string.IsNullOrWhiteSpace(xmlTestParameterValue.TransformerName))
						{
							hasUndefinedWorkspaceReference = true;
						}
					}
					else
					{
						workspaceIds.Add(datasetTestParameterValue.WorkspaceId);
					}
				}

				if (xmlInstanceConfig is XmlQualityCondition xmlCondition)
				{
					// Handle issue filters
					if (xmlCondition.Filters != null && xmlCondition.Filters.Count > 0)
					{
						var issueFilterConfigurations = new List<XmlInstanceConfiguration>();
						foreach (string filterName in xmlCondition.Filters.Select(
							         f => f.IssueFilterName))
						{
							if (! documentCache.TryGetIssueFilter(
								    filterName.Trim(),
								    out XmlIssueFilterConfiguration issueFilterConfiguration))
							{
								hasUndefinedWorkspaceReference = true;
								// TODO: handle missing
								continue;
							}

							issueFilterConfigurations.Add(issueFilterConfiguration);
						}

						CollectWorkspaceIds(workspaceIds, issueFilterConfigurations,
						                    documentCache,
						                    ref hasUndefinedWorkspaceReference);
					}
				}
			}
		}

		[NotNull]
		public static XmlDataQualityDocumentCache GetDocumentCache(
			[NotNull] XmlDataQualityDocument document,
			[NotNull] IEnumerable<XmlQualitySpecification> xmlQualitySpecifications,
			ITestParameterDatasetValidator testParameterDatasetValidator)
		{
			Assert.ArgumentNotNull(document, nameof(document));
			Assert.ArgumentNotNull(xmlQualitySpecifications, nameof(xmlQualitySpecifications));

			ICollection<string> referencedConditionNames =
				GetReferencedQualityConditionNames(xmlQualitySpecifications);

			IEnumerable<KeyValuePair<XmlQualityCondition, XmlDataQualityCategory>>
				qualityConditions = document.GetAllQualityConditions()
				                            .Where(pair => referencedConditionNames.Contains(
					                                   pair.Key.Name));

			return new XmlDataQualityDocumentCache(document, qualityConditions)
			       {
				       ParameterDatasetValidator = testParameterDatasetValidator
			       };
		}

		[NotNull]
		public static QualitySpecification CreateQualitySpecification(
			[NotNull] XmlQualitySpecification xmlSpecification,
			[NotNull] IDictionary<string, QualityCondition> qualityConditionsByName,
			[CanBeNull] DataQualityCategory category,
			bool ignoreMissingConditions = false)
		{
			Assert.ArgumentNotNull(qualityConditionsByName, nameof(qualityConditionsByName));
			Assert.ArgumentNotNull(xmlSpecification, nameof(xmlSpecification));

			var result = new QualitySpecification(xmlSpecification.Name)
			             {
				             Category = category
			             };

			string uuid = xmlSpecification.Uuid;
			if (StringUtils.IsNotEmpty(uuid))
			{
				result.Uuid = uuid;
			}

			UpdateQualitySpecification(result,
			                           xmlSpecification,
			                           qualityConditionsByName,
			                           category,
			                           ignoreMissingConditions);

			return result;
		}

		public static void UpdateQualitySpecification(
			[NotNull] QualitySpecification qualitySpecification,
			[NotNull] XmlQualitySpecification xmlSpecification,
			[NotNull] IDictionary<string, QualityCondition> qualityConditionsByName,
			[CanBeNull] DataQualityCategory category,
			bool ignoreMissingConditions = false)
		{
			Assert.ArgumentNotNull(qualitySpecification, nameof(qualitySpecification));
			Assert.ArgumentNotNull(xmlSpecification, nameof(xmlSpecification));
			Assert.ArgumentNotNull(qualityConditionsByName, nameof(qualityConditionsByName));

			qualitySpecification.Name = Assert.NotNull(xmlSpecification.Name).Trim();
			qualitySpecification.Description = xmlSpecification.Description;
			qualitySpecification.Notes = xmlSpecification.Notes;
			qualitySpecification.TileSize = xmlSpecification.TileSize <= 0
				                                ? (double?) null
				                                : xmlSpecification.TileSize;
			qualitySpecification.Url = xmlSpecification.Url;
			qualitySpecification.Hidden = xmlSpecification.Hidden;
			qualitySpecification.Category = category;

			ImportMetadata(qualitySpecification, xmlSpecification);

			if (xmlSpecification.ListOrder >= 0)
			{
				qualitySpecification.ListOrder = xmlSpecification.ListOrder;
			}

			foreach (XmlQualitySpecificationElement xmlElement in xmlSpecification.Elements)
			{
				string conditionName = xmlElement.QualityConditionName;

				QualityCondition qualityCondition;
				if (! qualityConditionsByName.TryGetValue(conditionName, out qualityCondition))
				{
					if (ignoreMissingConditions)
					{
						continue;
					}

					Assert.Fail(
						"The quality condition reference '{0}' defined in import document " +
						"is based on an unknown quality condition.", conditionName);
				}

				bool? stopOnError = TranslateOverride(xmlElement.StopOnError);
				bool? allowErrors = TranslateOverride(xmlElement.AllowErrors);

				qualitySpecification.AddElement(qualityCondition, stopOnError, allowErrors);
			}
		}

		public static void UpdateQualityCondition([NotNull] QualityCondition qualityCondition,
		                                          [NotNull] XmlQualityCondition xmlCondition,
		                                          [CanBeNull] DataQualityCategory category)
		{
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));
			Assert.ArgumentNotNull(xmlCondition, nameof(xmlCondition));

			UpdateInstanceConfiguration(qualityCondition, xmlCondition, category);

			qualityCondition.AllowErrorsOverride =
				TranslateOverride(xmlCondition.AllowErrors);
			qualityCondition.StopOnErrorOverride =
				TranslateOverride(xmlCondition.StopOnError);
			qualityCondition.NeverFilterTableRowsUsingRelatedGeometry =
				xmlCondition.NeverFilterTableRowsUsingRelatedGeometry;
			qualityCondition.NeverStoreRelatedGeometryForTableRowIssues =
				xmlCondition.NeverStoreRelatedGeometryForTableRowIssues;

			string versionUuid = xmlCondition.VersionUuid;
			if (StringUtils.IsNotEmpty(versionUuid))
			{
				qualityCondition.VersionUuid = versionUuid;
			}
		}

		public static void UpdateTransformerConfiguration(
			[NotNull] TransformerConfiguration transformer,
			[NotNull] XmlTransformerConfiguration xmlTransformer,
			[CanBeNull] DataQualityCategory category)
		{
			Assert.ArgumentNotNull(transformer, nameof(transformer));
			Assert.ArgumentNotNull(xmlTransformer, nameof(xmlTransformer));

			UpdateInstanceConfiguration(transformer, xmlTransformer, category);
		}

		public static void UpdateIssueFilterConfiguration(
			[NotNull] IssueFilterConfiguration issueFilter,
			[NotNull] XmlIssueFilterConfiguration xmlIssueFilter,
			[CanBeNull] DataQualityCategory category)
		{
			Assert.ArgumentNotNull(issueFilter, nameof(issueFilter));
			Assert.ArgumentNotNull(xmlIssueFilter, nameof(xmlIssueFilter));

			UpdateInstanceConfiguration(issueFilter, xmlIssueFilter, category);
		}

		private static void UpdateInstanceConfiguration<T>(
			[NotNull] T instanceConfiguration,
			[NotNull] XmlInstanceConfiguration xmlInstanceConfiguration,
			[CanBeNull] DataQualityCategory category)
			where T : InstanceConfiguration
		{
			Assert.ArgumentNotNull(instanceConfiguration, nameof(instanceConfiguration));
			Assert.ArgumentNotNull(xmlInstanceConfiguration, nameof(xmlInstanceConfiguration));

			if (StringUtils.IsNotEmpty(xmlInstanceConfiguration.Uuid))
			{
				Assert.AreEqual(instanceConfiguration.Uuid, xmlInstanceConfiguration.Uuid,
				                "{0}: Non-matching UUIDs in update.", instanceConfiguration.Name);
			}

			instanceConfiguration.Name = xmlInstanceConfiguration.Name;
			instanceConfiguration.Description = xmlInstanceConfiguration.Description;
			instanceConfiguration.Notes = xmlInstanceConfiguration.Notes;
			instanceConfiguration.Url = xmlInstanceConfiguration.Url;

			ImportMetadata(instanceConfiguration, xmlInstanceConfiguration);

			instanceConfiguration.Category = category;
		}

		[CanBeNull]
		public static TestParameterValue CreateDatasetTestParameterValue(
			[NotNull] TestParameter testParameter,
			[NotNull] XmlDatasetTestParameterValue xmlDatasetTestParameterValue,
			[NotNull] string qualityConditionName,
			[NotNull] IDictionary<string, DdxModel> modelsByWorkspaceId,
			[NotNull] Func<string, IList<Dataset>> getDatasetsByName,
			[CanBeNull] ITestParameterDatasetValidator parameterDatasetValidator,
			bool ignoreForUnknownDatasets)
		{
			Dataset dataset = GetDataset(xmlDatasetTestParameterValue,
			                             testParameter,
			                             qualityConditionName,
			                             modelsByWorkspaceId,
			                             getDatasetsByName,
			                             ignoreForUnknownDatasets);

			if (dataset == null)
			{
				if (! testParameter.IsConstructorParameter)
				{
					return new DatasetTestParameterValue(testParameter, dataset,
					                                     xmlDatasetTestParameterValue.WhereClause,
					                                     xmlDatasetTestParameterValue
						                                     .UsedAsReferenceData);
				}

				// Exception must already be thrown in GetDataset()
				Assert.True(ignoreForUnknownDatasets, "ignoreForUnknownDatasets");

				return null;
			}

			if (parameterDatasetValidator != null &&
			    ! parameterDatasetValidator.IsValidForParameter(
				    dataset, testParameter, out string message))
			{
				throw new InvalidOperationException(message);
			}

			return CreateDatasetTestParameterValue(testParameter, xmlDatasetTestParameterValue,
			                                       dataset);
		}

		[NotNull]
		private static TestParameterValue CreateDatasetTestParameterValue(
			[NotNull] TestParameter testParameter,
			[NotNull] XmlDatasetTestParameterValue xmlValue,
			[CanBeNull] Dataset dataset)
		{
			var paramValue = new DatasetTestParameterValue(
				testParameter, dataset,
				xmlValue.WhereClause,
				xmlValue.UsedAsReferenceData);

			return paramValue;
		}

		[CanBeNull]
		private static Dataset GetDataset(
			[NotNull] XmlDatasetTestParameterValue xmlDatasetTestParameterValue,
			// ReSharper disable once UnusedParameter.Local
			[NotNull] TestParameter testParameter,
			// ReSharper disable once UnusedParameter.Local
			[NotNull] string qualityConditionName,
			[NotNull] IDictionary<string, DdxModel> modelsByWorkspaceId,
			[NotNull] Func<string, IList<Dataset>> getDatasetsByName,
			bool ignoreUnknownDataset)
		{
			string datasetName = xmlDatasetTestParameterValue.Value;
			string workspaceId = xmlDatasetTestParameterValue.WorkspaceId;

			return GetDataset(datasetName, workspaceId, testParameter,
			                  qualityConditionName, modelsByWorkspaceId,
			                  getDatasetsByName, ignoreUnknownDataset);
		}

		[CanBeNull]
		public static Dataset GetDataset(
			[CanBeNull] string datasetName,
			[CanBeNull] string workspaceId,
			[NotNull] TestParameter testParameter,
			[NotNull] string instanceConfigurationName,
			[NotNull] IDictionary<string, DdxModel> modelsByWorkspaceId,
			[NotNull] Func<string, IList<Dataset>> getDatasetsByName,
			bool ignoreUnknownDataset)
		{
			if (string.IsNullOrWhiteSpace(datasetName))
			{
				if (testParameter.IsConstructorParameter)
				{
					Assert.NotNullOrEmpty(
						datasetName,
						"Dataset is not defined for constructor-parameter '{0}' in configuration '{1}'",
						testParameter.Name, instanceConfigurationName);
				}

				return null;
			}

			if (StringUtils.IsNotEmpty(workspaceId))
			{
				Assert.True(modelsByWorkspaceId.TryGetValue(workspaceId, out DdxModel model),
				            "No matching model found for workspace id '{0}'", workspaceId);

				return DdxModelElementUtils.GetDatasetFromStoredName(
					datasetName, model, ignoreUnknownDataset);
			}

			if (StringUtils.IsNullOrEmptyOrBlank(workspaceId))
			{
				const string defaultModelId = "";

				if (modelsByWorkspaceId.TryGetValue(defaultModelId, out DdxModel defaultModel))
				{
					// there is a default model
					return DdxModelElementUtils.GetDatasetFromStoredName(
						datasetName, defaultModel, ignoreUnknownDataset);
				}
			}

			// no workspace id for dataset, and there is no default model

			IList<Dataset> datasets = getDatasetsByName(datasetName);

			Assert.False(datasets.Count > 1,
			             "More than one dataset found with name '{0}', for parameter '{1}' in configuration '{2}'",
			             datasetName, testParameter.Name, instanceConfigurationName);

			if (datasets.Count == 0)
			{
				if (ignoreUnknownDataset)
				{
					return null;
				}

				Assert.False(datasets.Count == 0,
				             "Dataset '{0}' for parameter '{1}' in configuration '{2}' not found",
				             datasetName, testParameter.Name, instanceConfigurationName);
			}

			return datasets[0];
		}

		[NotNull]
		public static DataQualityCategory CreateDataQualityCategory(
			[NotNull] XmlDataQualityCategory xmlCategory,
			[CanBeNull] DataQualityCategory parentCategory,
			[CanBeNull] Func<string, DdxModel> getModelByName = null)
		{
			var category = new DataQualityCategory(assignUuid: true)
			               {
				               Name = xmlCategory.Name,
				               Abbreviation = xmlCategory.Abbreviation,
				               Description = xmlCategory.Description,
				               ListOrder = xmlCategory.ListOrder,
				               CanContainQualityConditions =
					               xmlCategory.CanContainQualityConditions,
				               CanContainQualitySpecifications =
					               xmlCategory.CanContainQualitySpecifications,
				               CanContainSubCategories = xmlCategory.CanContainSubCategories
			               };

			ImportMetadata(category, xmlCategory);

			string uuid = xmlCategory.Uuid;
			if (StringUtils.IsNotEmpty(uuid))
			{
				category.Uuid = uuid;
			}

			if (parentCategory != null)
			{
				parentCategory.AddSubCategory(category);
			}

			if (getModelByName != null && StringUtils.IsNotEmpty(xmlCategory.DefaultModelName))
			{
				DdxModel model = getModelByName(xmlCategory.DefaultModelName);
				if (model != null)
				{
					category.DefaultModel = model;
				}
				else
				{
					_msg.WarnFormat("Default model not found for category {0}: {1}",
					                category.Name, xmlCategory.DefaultModelName);
				}
			}

			return category;
		}

		public static void TransferProperties([NotNull] DataQualityCategory from,
		                                      [NotNull] DataQualityCategory to)
		{
			Assert.ArgumentNotNull(from, nameof(from));
			Assert.ArgumentNotNull(to, nameof(to));

			to.Name = from.Name;
			to.Abbreviation = from.Abbreviation;
			to.Description = from.Description;
			to.CanContainQualityConditions = from.CanContainQualityConditions;
			to.CanContainQualitySpecifications = from.CanContainQualitySpecifications;
			to.CanContainSubCategories = from.CanContainSubCategories;
			to.DefaultModel = from.DefaultModel;

			AssignParentCategory(to, from.ParentCategory);

			if (from.ListOrder != 0)
			{
				to.ListOrder = from.ListOrder;
			}

			TransferMetadata(from, to);
		}

		public static void AssignParentCategory(
			[NotNull] DataQualityCategory category,
			[CanBeNull] DataQualityCategory parentCategory)
		{
			Assert.ArgumentNotNull(category, nameof(category));

			if (Equals(parentCategory, category.ParentCategory))
			{
				return;
			}

			if (category.ParentCategory != null)
			{
				category.ParentCategory.RemoveSubCategory(category);
			}

			if (parentCategory != null)
			{
				parentCategory.AddSubCategory(category);
			}
		}

		[NotNull]
		public static TestDescriptor CreateTestDescriptor(
			[NotNull] XmlTestDescriptor xmlTestDescriptor)
		{
			Assert.ArgumentNotNull(xmlTestDescriptor, nameof(xmlTestDescriptor));

			TestDescriptor result;

			if (xmlTestDescriptor.TestClass != null)
			{
				result = new TestDescriptor(
					xmlTestDescriptor.Name,
					CreateClassDescriptor(xmlTestDescriptor.TestClass),
					xmlTestDescriptor.TestClass.ConstructorId);
			}
			else if (xmlTestDescriptor.TestFactoryDescriptor != null)
			{
				result = new TestDescriptor(
					xmlTestDescriptor.Name,
					CreateClassDescriptor(xmlTestDescriptor.TestFactoryDescriptor));
			}
			else
			{
				throw new InvalidOperationException(
					string.Format("Invalid xml test descriptor '{0}': " +
					              "neither test class nor test factory is defined",
					              xmlTestDescriptor.Name));
			}

			result.Description = xmlTestDescriptor.Description;
			result.AllowErrors = xmlTestDescriptor.AllowErrors;
			result.StopOnError = xmlTestDescriptor.StopOnError;
			result.ExecutionPriority = xmlTestDescriptor.GetExecutionPriority();

			ImportMetadata(result, xmlTestDescriptor);

			if (xmlTestDescriptor.TestConfigurator != null)
			{
				ClassDescriptor testConfigDescriptor =
					CreateClassDescriptor(xmlTestDescriptor.TestConfigurator);

				try
				{
					var configurator = testConfigDescriptor.CreateInstance<ITestConfigurator>();

					Type testClassType = configurator.GetTestClassType();
					Type factoryType = configurator.GetFactoryType();

					Assert.True(testClassType == null != (factoryType == null),
					            "Invalid TestConfigurator {0}: Combination testClass = {1} and factory = {2} not allowed",
					            testConfigDescriptor, testClassType, factoryType);

					if (testClassType != null)
					{
						int constructorId = configurator.GetTestConstructorId();
						Assert.True(constructorId >= 0, "Invalid constructorId {0}", constructorId);

						result.TestClass = new ClassDescriptor(testClassType);
						result.TestConstructorId = constructorId;
						result.TestFactoryDescriptor = null;
					}
					else
					{
						result.TestFactoryDescriptor = new ClassDescriptor(factoryType);
						result.TestClass = null;
						result.TestConstructorId = 0;
					}
				}
				catch (Exception e)
				{
					_msg.Debug($"Unable to load TestConfigurator for {xmlTestDescriptor.Name}.", e);
				}

				result.TestConfigurator = testConfigDescriptor;
			}

			return result;
		}

		[NotNull]
		public static TransformerDescriptor CreateTransformerDescriptor(
			[NotNull] XmlTransformerDescriptor xmlTransformerDescriptor)
		{
			Assert.ArgumentNotNull(xmlTransformerDescriptor, nameof(xmlTransformerDescriptor));

			return CreateInstanceDescriptor<TransformerDescriptor>(xmlTransformerDescriptor);
		}

		[NotNull]
		public static IssueFilterDescriptor CreateIssueFilterDescriptor(
			[NotNull] XmlIssueFilterDescriptor xmlIssueFilterDescriptor)
		{
			Assert.ArgumentNotNull(xmlIssueFilterDescriptor, nameof(xmlIssueFilterDescriptor));

			return CreateInstanceDescriptor<IssueFilterDescriptor>(xmlIssueFilterDescriptor);
		}

		[NotNull]
		private static T CreateInstanceDescriptor<T>(
			[NotNull] XmlInstanceDescriptor xmlInstanceDescriptor)
			where T : InstanceDescriptor, new()
		{
			Assert.ArgumentNotNull(xmlInstanceDescriptor, nameof(xmlInstanceDescriptor));

			Assert.NotNull(xmlInstanceDescriptor.ClassDescriptor);
			T result = new T();
			result.Name = xmlInstanceDescriptor.Name;
			result.Class = CreateClassDescriptor(xmlInstanceDescriptor.ClassDescriptor);
			result.ConstructorId = xmlInstanceDescriptor.ClassDescriptor.ConstructorId;

			result.Description = xmlInstanceDescriptor.Description;

			ImportMetadata(result, xmlInstanceDescriptor);

			return result;
		}

		[NotNull]
		private static ClassDescriptor CreateClassDescriptor(
			[NotNull] XmlClassDescriptor xmlClassDescriptor)
		{
			Assert.ArgumentNotNull(xmlClassDescriptor, nameof(xmlClassDescriptor));

			return new ClassDescriptor(xmlClassDescriptor.TypeName,
			                           xmlClassDescriptor.AssemblyName,
			                           xmlClassDescriptor.Description);
		}

		public static void TransferProperties([NotNull] InstanceDescriptor from,
		                                      [NotNull] InstanceDescriptor to,
		                                      bool updateName,
		                                      bool updateProperties)
		{
			Assert.ArgumentNotNull(from, nameof(from));
			Assert.ArgumentNotNull(to, nameof(to));
			Assert.AreEqual(from.GetType(), to.GetType(), "Type mismatch");

			if (updateName)
			{
				if (! string.Equals(to.Name, from.Name))
				{
					_msg.InfoFormat("Updating name of {0} {1} -> {2}", to.TypeDisplayName, to.Name,
					                from.Name);

					to.Name = from.Name;
				}
			}

			if (updateProperties)
			{
				_msg.InfoFormat("Updating properties of {0} {1}", to.TypeDisplayName, to.Name);

				to.Description = from.Description;

				if (from is TestDescriptor fromTd && to is TestDescriptor toTd)
				{
					toTd.AllowErrors = fromTd.AllowErrors;
					toTd.StopOnError = fromTd.StopOnError;
					toTd.ExecutionPriority = fromTd.ExecutionPriority;

					if (fromTd.TestConfigurator != null)
					{
						toTd.TestConfigurator = fromTd.TestConfigurator;
					}
				}

				TransferMetadata(from, to);
			}
		}

		[CanBeNull]
		public static Dataset GetDatasetByParameterValue(
			[NotNull] DdxModel model,
			[NotNull] string datasetParameterValue)
		{
			const bool ignoreUnknownDataset = true;
			return DdxModelElementUtils.GetDatasetFromStoredName(datasetParameterValue,
			                                                     model,
			                                                     ignoreUnknownDataset);
		}

		[NotNull]
		public static IEnumerable<Dataset> GetReferencedDatasets(
			[NotNull] DdxModel model,
			[NotNull] string workspaceId,
			[NotNull] IEnumerable<XmlInstanceConfiguration> referencedConditions)
		{
			Assert.ArgumentNotNull(model, nameof(model));
			Assert.ArgumentNotNull(referencedConditions, nameof(referencedConditions));
			Assert.ArgumentNotNullOrEmpty(workspaceId, nameof(workspaceId));

			var datasetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (XmlInstanceConfiguration xmlQualityCondition in referencedConditions)
			{
				foreach (XmlTestParameterValue paramterValue in
				         xmlQualityCondition.EnumParameterValues(ignoreEmptyValues: true))
				{
					var datasetParameterValue = paramterValue as XmlDatasetTestParameterValue;
					if (datasetParameterValue == null)
					{
						continue;
					}

					string datasetWorkspaceId = datasetParameterValue.WorkspaceId ?? string.Empty;

					if (! string.Equals(datasetWorkspaceId, workspaceId,
					                    StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}

					string datasetName = datasetParameterValue.Value;
					if (datasetName == null || datasetNames.Contains(datasetName))
					{
						continue;
					}

					datasetNames.Add(datasetName);

					Dataset dataset = GetDatasetByParameterValue(model, datasetName);
					if (dataset != null)
					{
						yield return dataset;
					}
				}
			}
		}

		[NotNull]
		public static IEnumerable<string> GetReferencedDatasetNames(
			[NotNull] string workspaceId,
			[NotNull] IEnumerable<XmlInstanceConfiguration> referencedConditions)
		{
			Assert.ArgumentNotNull(referencedConditions, nameof(referencedConditions));
			Assert.ArgumentNotNullOrEmpty(workspaceId, nameof(workspaceId));

			var datasetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (XmlInstanceConfiguration xmlQualityCondition in referencedConditions)
			{
				foreach (XmlTestParameterValue paramterValue in
				         xmlQualityCondition.EnumParameterValues(ignoreEmptyValues: true))
				{
					var datasetParameterValue = paramterValue as XmlDatasetTestParameterValue;
					if (datasetParameterValue == null)
					{
						continue;
					}

					string datasetWorkspaceId = datasetParameterValue.WorkspaceId ?? string.Empty;

					if (! string.Equals(datasetWorkspaceId, workspaceId,
					                    StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}

					string datasetName = datasetParameterValue.Value;
					if (datasetName == null || datasetNames.Contains(datasetName))
					{
						continue;
					}

					datasetNames.Add(datasetName);
					yield return datasetName;
				}
			}
		}

		private static void ImportMetadata([NotNull] IEntityMetadata entity,
		                                   [NotNull] IXmlEntityMetadata fromXml)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));
			Assert.ArgumentNotNull(fromXml, nameof(fromXml));

			// created

			if (! string.IsNullOrEmpty(fromXml.CreatedByUser))
			{
				entity.CreatedByUser = fromXml.CreatedByUser;
			}

			DateTime? xmlCreatedDate = ParseDateTime(fromXml.CreatedDate);

			if (xmlCreatedDate != null &&
			    ! AreEqual(entity.CreatedDate, xmlCreatedDate))
			{
				entity.CreatedDate = xmlCreatedDate;
			}

			// last changed 

			if (! string.IsNullOrEmpty(fromXml.LastChangedByUser))
			{
				entity.LastChangedByUser = fromXml.LastChangedByUser;
			}

			DateTime? xmlLastChangedDate = ParseDateTime(fromXml.LastChangedDate);
			if (xmlLastChangedDate != null &&
			    ! AreEqual(entity.LastChangedDate, xmlLastChangedDate))
			{
				entity.LastChangedDate = xmlLastChangedDate;
			}
		}

		private static void TransferMetadata([NotNull] IEntityMetadata from,
		                                     [NotNull] IEntityMetadata to)
		{
			Assert.ArgumentNotNull(from, nameof(from));
			Assert.ArgumentNotNull(to, nameof(to));

			// created

			if (! string.IsNullOrEmpty(from.CreatedByUser))
			{
				to.CreatedByUser = from.CreatedByUser;
			}

			if (from.CreatedDate != null &&
			    ! AreEqual(to.CreatedDate, from.CreatedDate))
			{
				to.CreatedDate = from.CreatedDate;
			}

			// last changed

			if (! string.IsNullOrEmpty(from.LastChangedByUser))
			{
				to.LastChangedByUser = from.LastChangedByUser;
			}

			if (from.LastChangedDate != null &&
			    ! AreEqual(to.LastChangedDate, from.LastChangedDate))
			{
				to.LastChangedDate = from.LastChangedDate;
			}
		}

		[NotNull]
		private static ICollection<string> GetReferencedQualityConditionNames(
			[NotNull] IEnumerable<XmlQualitySpecification> xmlQualitySpecifications)
		{
			Assert.ArgumentNotNull(xmlQualitySpecifications, nameof(xmlQualitySpecifications));

			var result = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

			foreach (
				XmlQualitySpecification xmlQualitySpecification in xmlQualitySpecifications)
			{
				foreach (
					XmlQualitySpecificationElement element in xmlQualitySpecification.Elements)
				{
					result.Add(element.QualityConditionName);
				}
			}

			return result;
		}

		/// <summary>
		/// Populates the specified document with the provided entities. The XML Workspaces must have been
		/// populated previously.
		/// </summary>
		/// <param name="document"></param>
		/// <param name="workspaceIdsByModel">The workspace IDs per model.</param>
		/// <param name="qualitySpecifications"></param>
		/// <param name="descriptors"></param>
		/// <param name="categories"></param>
		/// <param name="exportMetadata"></param>
		/// <param name="exportAllDescriptors"></param>
		/// <param name="exportAllCategories"></param>
		/// <param name="exportNotes"></param>
		public static void Populate(
			[NotNull] XmlDataQualityDocument document,
			[NotNull] IDictionary<DdxModel, string> workspaceIdsByModel,
			[NotNull] IList<QualitySpecification> qualitySpecifications,
			[CanBeNull] IList<InstanceDescriptor> descriptors,
			[CanBeNull] IList<DataQualityCategory> categories,
			bool exportMetadata,
			bool exportAllDescriptors,
			bool exportAllCategories,
			bool exportNotes)
		{
			Assert.ArgumentNotNull(document, nameof(document));
			Assert.ArgumentNotNull(workspaceIdsByModel, nameof(workspaceIdsByModel));
			Assert.ArgumentNotNull(qualitySpecifications, nameof(qualitySpecifications));

			GetReferencedConfigurations(qualitySpecifications,
			                            out IReadOnlyList<QualityCondition> qualityConditions,
			                            out IReadOnlyList<TransformerConfiguration> transformers,
			                            out IReadOnlyList<IssueFilterConfiguration> issueFilters);

			// TODO: add any descriptors that are referenced in the quality specifications but were not passed in

			IReadOnlyList<TestDescriptor> testDescriptors =
				exportAllDescriptors && descriptors != null
					? descriptors.OfType<TestDescriptor>().ToList()
					: GetTestDescriptors(qualityConditions);

			IReadOnlyList<TransformerDescriptor> transformerDescriptors =
				exportAllDescriptors && descriptors != null
					? descriptors.OfType<TransformerDescriptor>().ToList()
					: GetTransformerDescriptors(transformers);

			IReadOnlyList<IssueFilterDescriptor> issueFilterDescriptors =
				exportAllDescriptors && descriptors != null
					? descriptors.OfType<IssueFilterDescriptor>().ToList()
					: GetIssueFilterDescriptors(issueFilters);

			foreach (var testDescriptor in GetSortedDescriptors(testDescriptors))
			{
				document.AddTestDescriptor(
					CreateXmlTestDescriptor(testDescriptor, exportMetadata));
			}

			foreach (var transformerDescriptor in GetSortedDescriptors(transformerDescriptors))
			{
				document.AddTransformerDescriptor(
					CreateXmlTransformerDescriptor(transformerDescriptor, exportMetadata));
			}

			foreach (var issueFilterDescriptor in GetSortedDescriptors(issueFilterDescriptors))
			{
				document.AddIssueFilterDescriptor(
					CreateXmlIssueFilterDescriptor(issueFilterDescriptor, exportMetadata));
			}

			IEnumerable<DataQualityCategory> cats =
				exportAllCategories && categories != null
					? categories
					: GetReferencedCategories(qualityConditions.Cast<InstanceConfiguration>()
					                                           .Concat(transformers)
					                                           .Concat(issueFilters));

			AddCategories(document, cats,
			              c => qualityConditions.Cast<InstanceConfiguration>()
			                                    .Concat(transformers)
			                                    .Concat(issueFilters)
			                                    .Where(inst => Equals(inst.Category, c)),
			              c => qualitySpecifications.Where(qs => Equals(qs.Category, c)),
			              workspaceIdsByModel,
			              exportMetadata,
			              exportAllCategories,
			              exportNotes);

			// export root level (= without category) 
			foreach (QualityCondition condition in GetSortedConfigurations(
				         qualityConditions.Where(inst => inst.Category == null)))
			{
				document.AddQualityCondition(
					CreateXmlQualityCondition(condition, workspaceIdsByModel,
					                          exportMetadata, exportNotes));
			}

			foreach (TransformerConfiguration transformer in GetSortedConfigurations(
				         transformers.Where(inst => inst.Category == null)))
			{
				document.AddTransformer(
					CreateXmlTransformerConfiguration(transformer, workspaceIdsByModel,
					                                  exportMetadata, exportNotes));
			}

			foreach (IssueFilterConfiguration issueFilter in GetSortedConfigurations(
				         issueFilters.Where(inst => inst.Category == null)))
			{
				document.AddIssueFilter(
					CreateXmlIssueFilterConfiguration(issueFilter, workspaceIdsByModel,
					                                  exportMetadata, exportNotes));
			}

			foreach (QualitySpecification qualitySpecification in GetSortedQualitySpecifications(
				         qualitySpecifications.Where(qs => qs.Category == null)))
			{
				document.AddQualitySpecification(
					CreateXmlQualitySpecification(qualitySpecification,
					                              exportMetadata, exportNotes));
			}
		}

		private static void AddCategories(
			[NotNull] XmlDataQualityDocument document,
			[NotNull] IEnumerable<DataQualityCategory> categories,
			[NotNull] Func<DataQualityCategory, IEnumerable<InstanceConfiguration>>
				getInstanceConfigurations,
			[NotNull] Func<DataQualityCategory, IEnumerable<QualitySpecification>>
				getQualitySpecifications,
			[NotNull] IDictionary<DdxModel, string> workspaceIdsByModel,
			bool exportMetadata,
			bool exportAllCategories,
			bool exportNotes)
		{
			Assert.ArgumentNotNull(document, nameof(document));
			Assert.ArgumentNotNull(categories, nameof(categories));

			foreach (DataQualityCategory category in GetSortedCategories(
				         categories.Where(c => c.ParentCategory == null)))
			{
				document.AddCategory(CreateXmlDataQualityCategory(category,
					                     getInstanceConfigurations,
					                     getQualitySpecifications,
					                     workspaceIdsByModel,
					                     exportMetadata,
					                     exportAllCategories,
					                     exportNotes));
			}
		}

		[NotNull]
		private static IEnumerable<DataQualityCategory> GetReferencedCategories(
			[NotNull] IEnumerable<InstanceConfiguration> instanceConfigurations)
		{
			var categories = new HashSet<DataQualityCategory>();
			foreach (InstanceConfiguration instanceConfiguration in instanceConfigurations)
			{
				CollectCategories(instanceConfiguration.Category, categories);
			}

			return categories;
		}

		private static void CollectCategories([CanBeNull] DataQualityCategory category,
		                                      [NotNull] HashSet<DataQualityCategory> set)
		{
			if (category == null)
			{
				return;
			}

			set.Add(category);

			CollectCategories(category.ParentCategory, set);
		}

		private static void GetReferencedConfigurations(
			[NotNull] IEnumerable<QualitySpecification> qualitySpecifications,
			out IReadOnlyList<QualityCondition> qualityConditions,
			out IReadOnlyList<TransformerConfiguration> transformerConfigurations,
			out IReadOnlyList<IssueFilterConfiguration> issueFilterConfigurations)
		{
			var uniqueConditions = new HashSet<QualityCondition>();
			foreach (QualitySpecification qualitySpecification in qualitySpecifications)
			{
				foreach (QualityCondition qualityCondition in qualitySpecification.Elements.Select(
					         element => element.QualityCondition))
				{
					if (uniqueConditions.Contains(qualityCondition))
					{
						continue;
					}

					InitializeParameterValues(qualityCondition);

					uniqueConditions.Add(qualityCondition);
				}
			}

			var allTransformers = new HashSet<TransformerConfiguration>();
			var allIssueFilters = new HashSet<IssueFilterConfiguration>();
			foreach (QualityCondition qualityCondition in uniqueConditions)
			{
				CollectTransformers(qualityCondition, allTransformers);
				foreach (var issueFilter in qualityCondition.IssueFilterConfigurations)
				{
					if (allIssueFilters.Add(issueFilter))
					{
						InitializeParameterValues(issueFilter);
					}

					CollectTransformers(issueFilter, allTransformers);
				}
			}

			qualityConditions = uniqueConditions.ToList();
			transformerConfigurations = allTransformers.ToList();
			issueFilterConfigurations = allIssueFilters.ToList();
		}

		private static void CollectTransformers(
			[NotNull] InstanceConfiguration configuration,
			[NotNull] HashSet<TransformerConfiguration> allTransformers)
		{
			foreach (TestParameterValue parameterValue in configuration.ParameterValues)
			{
				TransformerConfiguration transformer = parameterValue.ValueSource;
				if (transformer != null)
				{
					if (allTransformers.Add(transformer))
					{
						InitializeParameterValues(transformer);
					}

					CollectTransformers(transformer, allTransformers);
				}
			}
		}

		private static void InitializeParameterValues(
			[NotNull] InstanceConfiguration instanceConfiguration)
		{
			InstanceDescriptor descriptor = instanceConfiguration.InstanceDescriptor;

			IInstanceInfo instanceInfo = Assert.NotNull(
				InstanceDescriptorUtils.GetInstanceInfo(descriptor),
				$"Cannot create factory for {descriptor}");

			InstanceConfigurationUtils.InitializeParameterValues(
				instanceInfo, instanceConfiguration);
		}

		[NotNull]
		private static IReadOnlyList<TestDescriptor> GetTestDescriptors(
			[NotNull] IEnumerable<QualityCondition> qualityConditions)
		{
			var descriptors = new HashSet<TestDescriptor>();
			foreach (QualityCondition qualityCondition in qualityConditions)
			{
				descriptors.Add(qualityCondition.TestDescriptor);
			}

			return descriptors.ToList();
		}

		[NotNull]
		private static IReadOnlyList<TransformerDescriptor> GetTransformerDescriptors(
			[NotNull] IEnumerable<TransformerConfiguration> transformers)
		{
			var descriptors = new HashSet<TransformerDescriptor>();
			foreach (TransformerConfiguration configuration in transformers)
			{
				descriptors.Add(configuration.TransformerDescriptor);
			}

			return descriptors.ToList();
		}

		[NotNull]
		private static IReadOnlyList<IssueFilterDescriptor> GetIssueFilterDescriptors(
			[NotNull] IEnumerable<IssueFilterConfiguration> issueFilters)
		{
			var descriptors = new HashSet<IssueFilterDescriptor>();
			foreach (IssueFilterConfiguration configuration in issueFilters)
			{
				descriptors.Add(configuration.IssueFilterDescriptor);
			}

			return descriptors.ToList();
		}

		[NotNull]
		private static IEnumerable<QualitySpecification> GetSortedQualitySpecifications(
			[NotNull] IEnumerable<QualitySpecification> qualitySpecifications)
		{
			return qualitySpecifications.OrderBy(qs => qs.ListOrder).ThenBy(qs => qs.Name);
		}

		[NotNull]
		private static IEnumerable<QualitySpecificationElement> GetSortedElements(
			[NotNull] IEnumerable<QualitySpecificationElement> qualitySpecificationElements)
		{
			return qualitySpecificationElements.OrderBy(e => e.QualityCondition.Name);
		}

		[NotNull]
		private static IEnumerable<DataQualityCategory> GetSortedCategories(
			[NotNull] IEnumerable<DataQualityCategory> categories)
		{
			return categories.OrderBy(c => c.ListOrder).ThenBy(c => c.Name);
		}

		[NotNull]
		private static IEnumerable<T> GetSortedConfigurations<T>(
			[NotNull] IEnumerable<T> configurations)
			where T : InstanceConfiguration
		{
			Assert.ArgumentNotNull(configurations, nameof(configurations));

			return configurations.OrderBy(t => t.Name);
		}

		[NotNull]
		private static IEnumerable<T> GetSortedDescriptors<T>(
			[NotNull] IEnumerable<T> descriptors)
			where T : InstanceDescriptor
		{
			Assert.ArgumentNotNull(descriptors, nameof(descriptors));

			return descriptors.OrderBy(t => t.Name);
		}

		#region CreateXml

		[NotNull]
		private static XmlDataQualityCategory CreateXmlDataQualityCategory(
			[NotNull] DataQualityCategory category,
			[NotNull] Func<DataQualityCategory, IEnumerable<InstanceConfiguration>>
				getInstanceConfigurations,
			[NotNull] Func<DataQualityCategory, IEnumerable<QualitySpecification>>
				getQualitySpecifications,
			[NotNull] IDictionary<DdxModel, string> workspaceIdsByModel,
			bool exportMetadata,
			bool exportAllCategories,
			bool exportNotes)
		{
			var result =
				new XmlDataQualityCategory
				{
					Name = Escape(category.Name),
					Abbreviation = Escape(category.Abbreviation),
					Uuid = category.Uuid,
					Description = Escape(category.Description),
					ListOrder = category.ListOrder,
					CanContainQualityConditions = category.CanContainQualityConditions,
					CanContainQualitySpecifications = category.CanContainQualitySpecifications,
					CanContainSubCategories = category.CanContainSubCategories,
					DefaultModelName = category.DefaultModel == null
						                   ? null
						                   : Escape(category.DefaultModel.Name)
				};

			if (exportMetadata)
			{
				ExportMetadata(category, result);
			}

			foreach (DataQualityCategory subCategory in GetSortedCategories(category.SubCategories))
			{
				XmlDataQualityCategory xmlSubCategory = CreateXmlDataQualityCategory(subCategory,
					getInstanceConfigurations,
					getQualitySpecifications,
					workspaceIdsByModel,
					exportMetadata,
					exportAllCategories,
					exportNotes);

				if (! exportAllCategories && ! xmlSubCategory.IsNotEmpty)
				{
					continue;
				}

				result.AddSubCategory(xmlSubCategory);
			}

			foreach (QualityCondition condition in GetSortedConfigurations(
				         getInstanceConfigurations(category).OfType<QualityCondition>()))
			{
				result.AddQualityCondition(
					CreateXmlQualityCondition(condition, workspaceIdsByModel,
					                          exportMetadata, exportNotes));
			}

			foreach (TransformerConfiguration transformer in GetSortedConfigurations(
				         getInstanceConfigurations(category).OfType<TransformerConfiguration>()))
			{
				result.AddTransformer(
					CreateXmlTransformerConfiguration(transformer, workspaceIdsByModel,
					                                  exportMetadata, exportNotes));
			}

			foreach (IssueFilterConfiguration issueFilter in GetSortedConfigurations(
				         getInstanceConfigurations(category).OfType<IssueFilterConfiguration>()))
			{
				result.AddIssueFilter(
					CreateXmlIssueFilterConfiguration(issueFilter, workspaceIdsByModel,
					                                  exportMetadata, exportNotes));
			}

			foreach (QualitySpecification qualitySpecification in GetSortedQualitySpecifications(
				         getQualitySpecifications(category)))
			{
				result.AddQualitySpecification(
					CreateXmlQualitySpecification(qualitySpecification,
					                              exportMetadata, exportNotes));
			}

			return result;
		}

		[NotNull]
		private static XmlQualitySpecification CreateXmlQualitySpecification(
			[NotNull] QualitySpecification qualitySpecification,
			bool exportMetadata, bool exportNotes)
		{
			Assert.ArgumentNotNull(qualitySpecification, nameof(qualitySpecification));

			var result = new XmlQualitySpecification
			             {
				             Name = Escape(qualitySpecification.Name),
				             Description = Escape(qualitySpecification.Description),
				             Uuid = qualitySpecification.Uuid,
				             ListOrder = qualitySpecification.ListOrder,
				             TileSize = qualitySpecification.TileSize ?? 0,
				             Url = Escape(qualitySpecification.Url),
				             Hidden = qualitySpecification.Hidden
			             };

			if (exportMetadata)
			{
				ExportMetadata(qualitySpecification, result);
			}

			if (exportNotes)
			{
				result.Notes = Escape(qualitySpecification.Notes);
			}

			foreach (QualitySpecificationElement element in
			         GetSortedElements(qualitySpecification.Elements))
			{
				result.Elements.Add(CreateXmlQualitySpecificationElement(element));
			}

			return result;
		}

		[NotNull]
		private static XmlQualitySpecificationElement CreateXmlQualitySpecificationElement
			([NotNull] QualitySpecificationElement element)
		{
			Assert.ArgumentNotNull(element, nameof(element));

			var xmlElement =
				new XmlQualitySpecificationElement
				{
					AllowErrors = EOverride(element.AllowErrorsOverride),
					StopOnError = EOverride(element.StopOnErrorOverride),
					QualityConditionName = Escape(element.QualityCondition.Name)
				};

			return xmlElement;
		}

		[CanBeNull]
		private static XmlClassDescriptor CreateXmlClassDescriptor(
			[CanBeNull] ClassDescriptor descriptor, int constructorIndex)
		{
			XmlClassDescriptor result = CreateXmlClassDescriptor(descriptor);

			if (result == null)
			{
				return null;
			}

			result.ConstructorId = constructorIndex;
			return result;
		}

		[CanBeNull]
		private static XmlClassDescriptor CreateXmlClassDescriptor(
			[CanBeNull] ClassDescriptor descriptor)
		{
			if (descriptor == null)
			{
				return null;
			}

			var xmlDescriptor =
				new XmlClassDescriptor
				{
					AssemblyName = descriptor.AssemblyName,
					TypeName = descriptor.TypeName,
					Description = descriptor.Description
				};

			return xmlDescriptor;
		}

		[NotNull]
		public static XmlTestDescriptor CreateXmlTestDescriptor(
			[NotNull] TestDescriptor testDescriptor, bool exportMetadata)
		{
			Assert.ArgumentNotNull(testDescriptor, nameof(testDescriptor));

			var xmlDescriptor =
				CreateXmlInstanceDescriptor<XmlTestDescriptor>(
					testDescriptor, exportMetadata);

			xmlDescriptor.StopOnError = testDescriptor.StopOnError;
			xmlDescriptor.AllowErrors = testDescriptor.AllowErrors;
			xmlDescriptor.TestClass = CreateXmlClassDescriptor(testDescriptor.TestClass,
			                                                   testDescriptor.TestConstructorId);
			xmlDescriptor.TestFactoryDescriptor = CreateXmlClassDescriptor(
				testDescriptor.TestFactoryDescriptor);
			xmlDescriptor.TestConfigurator = CreateXmlClassDescriptor(
				testDescriptor.TestConfigurator);

			xmlDescriptor.SetExecutionPriority(testDescriptor.ExecutionPriority);

			return xmlDescriptor;
		}

		[NotNull]
		private static XmlTransformerDescriptor CreateXmlTransformerDescriptor(
			[NotNull] TransformerDescriptor transformerDescriptor, bool exportMetadata)
		{
			Assert.ArgumentNotNull(transformerDescriptor, nameof(transformerDescriptor));

			var xmlDescriptor =
				CreateXmlInstanceDescriptor<XmlTransformerDescriptor>(
					transformerDescriptor, exportMetadata);

			xmlDescriptor.TransformerClass = CreateXmlClassDescriptor(
				transformerDescriptor.Class, transformerDescriptor.ConstructorId);

			return xmlDescriptor;
		}

		[NotNull]
		private static XmlIssueFilterDescriptor CreateXmlIssueFilterDescriptor(
			[NotNull] IssueFilterDescriptor issueFilterDescriptor, bool exportMetadata)
		{
			Assert.ArgumentNotNull(issueFilterDescriptor, nameof(issueFilterDescriptor));

			var xmlDescriptor =
				CreateXmlInstanceDescriptor<XmlIssueFilterDescriptor>(
					issueFilterDescriptor, exportMetadata);

			xmlDescriptor.IssueFilterClass = CreateXmlClassDescriptor(
				issueFilterDescriptor.Class, issueFilterDescriptor.ConstructorId);

			return xmlDescriptor;
		}

		[NotNull]
		private static T CreateXmlInstanceDescriptor<T>(
			[NotNull] InstanceDescriptor descriptor, bool exportMetadata)
			where T : XmlInstanceDescriptor, new()
		{
			T result = new T
			           {
				           Name = Escape(descriptor.Name),
				           Description = Escape(descriptor.Description)
			           };

			if (exportMetadata)
			{
				ExportMetadata(descriptor, result);
			}

			return result;
		}

		[NotNull]
		private static XmlQualityCondition CreateXmlQualityCondition(
			[NotNull] QualityCondition qualityCondition,
			[NotNull] IDictionary<DdxModel, string> workspaceIdsByModel,
			bool exportMetadata, bool exportNotes)
		{
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));
			Assert.ArgumentNotNull(workspaceIdsByModel, nameof(workspaceIdsByModel));

			var xmlConfiguration =
				CreateXmlInstanceConfiguration<XmlQualityCondition>(
					qualityCondition, workspaceIdsByModel, exportMetadata, exportNotes);

			xmlConfiguration.VersionUuid = qualityCondition.VersionUuid;
			xmlConfiguration.TestDescriptorName = Escape(qualityCondition.TestDescriptor.Name);
			xmlConfiguration.AllowErrors = EOverride(qualityCondition.AllowErrorsOverride);
			xmlConfiguration.StopOnError = EOverride(qualityCondition.StopOnErrorOverride);
			xmlConfiguration.NeverFilterTableRowsUsingRelatedGeometry =
				qualityCondition.NeverFilterTableRowsUsingRelatedGeometry;
			xmlConfiguration.NeverStoreRelatedGeometryForTableRowIssues =
				qualityCondition.NeverStoreRelatedGeometryForTableRowIssues;

			if (qualityCondition.IssueFilterConfigurations.Count > 0)
			{
				xmlConfiguration.Filters = qualityCondition.IssueFilterConfigurations
				                                           .OrderBy(f => f.Name)
				                                           .Select(CreateXmlFilter).ToList();
			}

			xmlConfiguration.FilterExpression =
				CreateXmlFilterExpression(qualityCondition.IssueFilterExpression);

			return xmlConfiguration;
		}

		[NotNull]
		private static XmlTransformerConfiguration CreateXmlTransformerConfiguration(
			[NotNull] TransformerConfiguration transformer,
			[NotNull] IDictionary<DdxModel, string> workspaceIdsByModel,
			bool exportMetadata, bool exportNotes)
		{
			Assert.ArgumentNotNull(transformer, nameof(transformer));
			Assert.ArgumentNotNull(workspaceIdsByModel, nameof(workspaceIdsByModel));

			var xmlConfiguration =
				CreateXmlInstanceConfiguration<XmlTransformerConfiguration>(
					transformer, workspaceIdsByModel, exportMetadata, exportNotes);

			// TODO: VersionUuid = transformer.VersionUuid,
			xmlConfiguration.TransformerDescriptorName =
				Escape(transformer.TransformerDescriptor.Name);

			return xmlConfiguration;
		}

		[NotNull]
		private static XmlIssueFilterConfiguration CreateXmlIssueFilterConfiguration(
			[NotNull] IssueFilterConfiguration issueFilter,
			[NotNull] IDictionary<DdxModel, string> workspaceIdsByModel,
			bool exportMetadata, bool exportNotes)
		{
			Assert.ArgumentNotNull(issueFilter, nameof(issueFilter));
			Assert.ArgumentNotNull(workspaceIdsByModel, nameof(workspaceIdsByModel));

			var xmlConfiguration =
				CreateXmlInstanceConfiguration<XmlIssueFilterConfiguration>(
					issueFilter, workspaceIdsByModel, exportMetadata, exportNotes);

			// TODO: VersionUuid = issueFilter.VersionUuid,
			xmlConfiguration.IssueFilterDescriptorName =
				Escape(issueFilter.IssueFilterDescriptor.Name);

			return xmlConfiguration;
		}

		[NotNull]
		private static T CreateXmlInstanceConfiguration<T>(
			[NotNull] InstanceConfiguration instanceConfiguration,
			[NotNull] IDictionary<DdxModel, string> workspaceIdsByModel,
			bool exportMetadata, bool exportNotes)
			where T : XmlInstanceConfiguration, new()
		{
			T result = new T
			           {
				           Name = Escape(instanceConfiguration.Name),
				           Uuid = instanceConfiguration.Uuid,
				           // TODO: VersionUuid = instanceConfiguration.VersionUuid,
				           Url = Escape(instanceConfiguration.Url),
				           Description = Escape(instanceConfiguration.Description),
			           };

			if (exportMetadata)
			{
				ExportMetadata(instanceConfiguration, result);
			}

			if (exportNotes)
			{
				result.Notes = Escape(instanceConfiguration.Notes);
			}

			foreach (TestParameterValue parameterValue in instanceConfiguration.ParameterValues)
			{
				result.ParameterValues.Add(
					CreateXmlTestParameterValue(parameterValue, instanceConfiguration,
					                            workspaceIdsByModel));
			}

			return result;
		}

		[NotNull]
		private static XmlTestParameterValue CreateXmlTestParameterValue(
			[NotNull] TestParameterValue parameterValue,
			[NotNull] InstanceConfiguration instanceConfiguration,
			[NotNull] IDictionary<DdxModel, string> workspaceIdsByModel)
		{
			Assert.ArgumentNotNull(parameterValue, nameof(parameterValue));
			Assert.ArgumentNotNull(instanceConfiguration, nameof(instanceConfiguration));
			Assert.ArgumentNotNull(workspaceIdsByModel, nameof(workspaceIdsByModel));

			XmlTestParameterValue xmlValue;
			try
			{
				if (parameterValue is ScalarTestParameterValue scValue)
				{
					xmlValue = CreateXmlScalarTestParameterValue(scValue);
				}
				else if (parameterValue is DatasetTestParameterValue dsValue)
				{
					xmlValue = CreateXmlDatasetTestParameterValue(dsValue, workspaceIdsByModel);
				}
				else
				{
					throw new InvalidConfigurationException(
						$"Parameter has an unhandled type {parameterValue.GetType()}");
				}
			}
			catch (Exception ex)
			{
				throw new InvalidConfigurationException(
					$"Parameter {parameterValue.TestParameterName} in {instanceConfiguration.Name} is invalid: {ex.Message}",
					ex);
			}

			if (parameterValue.ValueSource != null)
			{
				xmlValue.TransformerName = parameterValue.ValueSource.Name;
			}

			return xmlValue;
		}

		[NotNull]
		private static XmlScalarTestParameterValue CreateXmlScalarTestParameterValue(
			[NotNull] ScalarTestParameterValue scValue)
		{
			return new XmlScalarTestParameterValue
			       {
				       TestParameterName = scValue.TestParameterName,
				       Value = scValue.StringValue
			       };
		}

		[NotNull]
		private static XmlDatasetTestParameterValue CreateXmlDatasetTestParameterValue(
			[NotNull] DatasetTestParameterValue datasetTestParameterValue,
			[NotNull] IDictionary<DdxModel, string> workspaceIdsByModel)
		{
			Dataset dataset = datasetTestParameterValue.DatasetValue;

			string datasetName;
			string workspaceId;
			if (dataset != null)
			{
				datasetName = dataset.Name;
				Assert.NotNull(dataset.Model, "dataset model is null");

				if (! workspaceIdsByModel.TryGetValue(dataset.Model, out workspaceId))
				{
					throw new ArgumentException(
						$@"Model not found in dictionary: {dataset.Model}",
						nameof(workspaceIdsByModel));
				}
			}
			else
			{
				datasetName = null;
				workspaceId = null;
			}

			return new XmlDatasetTestParameterValue
			       {
				       TestParameterName = datasetTestParameterValue.TestParameterName,
				       Value = datasetName,
				       WhereClause = Escape(datasetTestParameterValue.FilterExpression),
				       UsedAsReferenceData = datasetTestParameterValue.UsedAsReferenceData,
				       WorkspaceId = Escape(workspaceId)
			       };
		}

		[NotNull]
		private static XmlFilter CreateXmlFilter(
			[NotNull] IssueFilterConfiguration filterConfiguration)
		{
			return new XmlFilter { IssueFilterName = filterConfiguration.Name };
		}

		[CanBeNull]
		private static XmlFilterExpression CreateXmlFilterExpression([CanBeNull] string expression)
		{
			return string.IsNullOrWhiteSpace(expression)
				       ? null
				       : new XmlFilterExpression { Expression = expression };
		}

		private static void ExportMetadata([NotNull] IEntityMetadata entity,
		                                   [NotNull] IXmlEntityMetadata xml)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));
			Assert.ArgumentNotNull(xml, nameof(xml));

			xml.LastChangedByUser = Escape(entity.LastChangedByUser);
			xml.CreatedByUser = Escape(entity.CreatedByUser);
			xml.LastChangedDate = Format(entity.LastChangedDate);
			xml.CreatedDate = Format(entity.CreatedDate);
		}

		#endregion

		#region Utils

		[CanBeNull]
		public static string Format(DateTime? dateTime)
		{
			return dateTime?.ToString("s", CultureInfo.InvariantCulture);
		}

		[CanBeNull]
		public static DateTime? ParseDateTime([CanBeNull] string dateTimeString)
		{
			return string.IsNullOrEmpty(dateTimeString)
				       ? (DateTime?) null
				       : DateTime.Parse(dateTimeString,
				                        CultureInfo.InvariantCulture,
				                        DateTimeStyles.AssumeLocal);
		}

		public static bool AreEqual(DateTime? dateTime1, DateTime? dateTime2)
		{
			return Equals(Format(dateTime1), Format(dateTime2));
		}

		[ContractAnnotation("text:null => null")]
		private static string Escape([CanBeNull] string text)
		{
			return XmlUtils.EscapeInvalidCharacters(text);
		}

		private static bool? TranslateOverride(Override value)
		{
			switch (value)
			{
				case Override.Null:
					return null;

				case Override.False:
					return false;

				case Override.True:
					return true;

				default:
					throw new InvalidOperationException("Unhandled Override " + value);
			}
		}

		private static Override EOverride(bool? value)
		{
			if (! value.HasValue)
			{
				return Override.Null;
			}

			return value.Value
				       ? Override.True
				       : Override.False;
		}

		#endregion
	}
}
