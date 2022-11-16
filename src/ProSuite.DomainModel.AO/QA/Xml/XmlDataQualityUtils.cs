using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.Commons.Xml;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.Schemas;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.AO.QA.Xml
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

		[NotNull]
		public static XmlQualityCondition DeserializeCondition([NotNull] TextReader xml)
		{
			Assert.ArgumentNotNull(xml, nameof(xml));

			string schema = Schema.ProSuite_QA_QualitySpecifications_2_0;

			try
			{
				return XmlUtils.Deserialize<XmlQualityCondition>(xml, schema);
			}
			catch (Exception e)
			{
				throw new XmlDeserializationException($"Error deserializing xml: {e.Message}", e);
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

		public static void CreateXmlDataQualityDocument<T>(
			[NotNull] IEnumerable<QualitySpecification> qualitySpecifications,
			[NotNull] IList<InstanceDescriptor> descriptors,
			[NotNull] IEnumerable<DataQualityCategory> categories,
			bool exportMetadata,
			bool exportConnections,
			bool exportConnectionFilePaths,
			bool exportAllDescriptors,
			bool exportAllCategories,
			bool exportNotes,
			out T result)
			where T : XmlDataQualityDocument, new()
		{
			Assert.ArgumentNotNull(qualitySpecifications, nameof(qualitySpecifications));
			Assert.ArgumentNotNull(descriptors, nameof(descriptors));

			result = new T();

			Populate(result,
			         qualitySpecifications.ToList(),
			         descriptors,
			         categories,
			         exportMetadata,
			         exportConnections,
			         exportConnectionFilePaths,
			         exportAllDescriptors,
			         exportAllCategories,
			         exportNotes);
		}

		public static void ExportXmlDocument<T>(
			[NotNull] T document, [NotNull] string xmlFilePath)
			where T : XmlDataQualityDocument
		{
			Assert.ArgumentNotNull(document, nameof(document));
			Assert.ArgumentNotNullOrEmpty(xmlFilePath, nameof(xmlFilePath));

			using (XmlWriter xmlWriter = XmlWriter.Create(xmlFilePath, XmlUtils.GetWriterSettings()))
			{
				ExportXmlDocument(document, xmlWriter);
			}
		}


		public static void ExportXmlDocument<T>([NotNull] T document, [NotNull] XmlWriter xmlWriter)
			where T : XmlDataQualityDocument
		{
			Assert.ArgumentNotNull(document, nameof(document));
			Assert.ArgumentNotNull(xmlWriter, nameof(xmlWriter));

			// Sort entries
			SortQualitySpecifications(document.QualitySpecifications);
			SortQualityConditions(document.QualityConditions);

			StringComparison o = StringComparison.Ordinal;

			document.Transformers?.Sort((x, y) => string.Compare(x.Name, y.Name, o));
			document.IssueFilters?.Sort((x, y) => string.Compare(x.Name, y.Name, o));

			document.TestDescriptors?.Sort((x, y) => string.Compare(x.Name, y.Name, o));
			document.TransformerDescriptors?.Sort((x, y) => string.Compare(x.Name, y.Name, o));
			document.IssueFilterDescriptors?.Sort((x, y) => string.Compare(x.Name, y.Name, o));

			SortCategories(document.Categories);

			XmlUtils.Serialize(document, xmlWriter);
		}

		private static void SortQualitySpecifications(
			[CanBeNull] List<XmlQualitySpecification> specifications)
		{
			if (specifications == null)
			{
				return;
			}

			StringComparison o = StringComparison.Ordinal;
			specifications.Sort((x, y) => string.Compare(x.Name, y.Name, o));
			foreach (XmlQualitySpecification spec in specifications)
			{
				spec.Elements.Sort(
					(x, y) => string.Compare(
						x.QualityConditionName, y.QualityConditionName, o));
			}
		}

		private static void SortQualityConditions(
			[CanBeNull] List<XmlQualityCondition> qualityConditions)
		{
			qualityConditions?.Sort(
				(x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
		}

		private static void SortCategories([CanBeNull] List<XmlDataQualityCategory> categories)
		{
			if (categories == null)
			{
				return;
			}

			foreach (XmlDataQualityCategory category in categories)
			{
				SortQualitySpecifications(category.QualitySpecifications);
				SortQualityConditions(category.QualityConditions);

				SortCategories(category.SubCategories);
			}
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
				document.GetAllQualityConditions().Select(p => p.Key),
				"quality condition");
		}

		public static void AssertUniqueTransformerUuids(
			[NotNull] XmlDataQualityDocument document)
		{
			Assert.ArgumentNotNull(document, nameof(document));
			AssertUniqueInstanceConfigurationUuids(document.Transformers, "transformer");
		}

		public static void AssertUniqueIssueFilterUuids(
			[NotNull] XmlDataQualityDocument document)
		{
			Assert.ArgumentNotNull(document, nameof(document));
			AssertUniqueInstanceConfigurationUuids(document.IssueFilters, "issue filter");
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

			IEnumerable<XmlQualityCondition> xmlQualityConditions =
				document.GetAllQualityConditions().Select(p => p.Key);

			AssertUniqueInstanceConfigurationNames(xmlQualityConditions, "quality condition");
		}

		public static void AssertUniqueTransformerNames(
			[NotNull] XmlDataQualityDocument document)
		{
			Assert.ArgumentNotNull(document, nameof(document));
			AssertUniqueInstanceConfigurationNames(document.Transformers, "transformer");
		}

		public static void AssertUniqueIssueFilterNames(
			[NotNull] XmlDataQualityDocument document)
		{
			Assert.ArgumentNotNull(document, nameof(document));
			AssertUniqueInstanceConfigurationNames(document.IssueFilters, "issue filter");
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

			var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
			out XmlDataQualityCategory category)
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
			out XmlDataQualityCategory category)
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
			[NotNull] XmlDataQualityDocumentCache documentCache)
		{
			return GetReferencedWorkspaces(
				documentCache.QualityConditions,
				documentCache,
				out bool hasUndefinedWorkspaceReference);
		}

		[NotNull]
		public static IList<XmlWorkspace> GetReferencedWorkspaces<T>(
			[NotNull] IEnumerable<T> instanceConfigurations,
			[NotNull] XmlDataQualityDocumentCache documentCache,
			out bool hasUndefinedWorkspaceReference)
			where T : XmlInstanceConfiguration
		{
			Assert.ArgumentNotNull(documentCache, nameof(documentCache));

			var referencedWorkspaceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			hasUndefinedWorkspaceReference = false;

			CollectWorkspaceIds(referencedWorkspaceIds, instanceConfigurations, documentCache,
			                    ref hasUndefinedWorkspaceReference);

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

						CollectWorkspaceIds(workspaceIds, new[] {transformerConfiguration},
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
					IList<string> issueFilterNames =
						FilterUtils.GetFilterNames(
							xmlCondition.IssueFilterExpression?.Expression);

					// Handle issue filters
					var issueFilterConfigurations = new List<XmlInstanceConfiguration>();
					foreach (string name in issueFilterNames)
					{
						if (! documentCache.TryGetIssueFilter(
							    name, out XmlIssueFilterConfiguration issueFilterConfiguration))
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

		[NotNull]
		public static XmlDataQualityDocumentCache GetDocumentCache(
			[NotNull] XmlDataQualityDocument document,
			[NotNull] IEnumerable<XmlQualitySpecification> xmlQualitySpecifications)
		{
			Assert.ArgumentNotNull(document, nameof(document));
			Assert.ArgumentNotNull(xmlQualitySpecifications, nameof(xmlQualitySpecifications));

			ICollection<string> referencedConditionNames =
				GetReferencedQualityConditionNames(xmlQualitySpecifications);

			IEnumerable<KeyValuePair<XmlQualityCondition, XmlDataQualityCategory>>
				qualityConditions = document.GetAllQualityConditions()
				                            .Where(pair => referencedConditionNames.Contains(
					                                   pair.Key.Name));

			return new XmlDataQualityDocumentCache(document, qualityConditions);
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

		[NotNull]
		public static QualitySpecification CreateQualitySpecification(
			string name,
			[NotNull] IDictionary<string, QualityCondition> qualityConditionsByName,
			[NotNull] IEnumerable<QualitySpecificationElement> specificationElements)
		{
			Assert.ArgumentNotNull(qualityConditionsByName, nameof(qualityConditionsByName));

			var result = new QualitySpecification(name);

			foreach (QualitySpecificationElement element in specificationElements)
			{
				result.AddElement(element.QualityCondition, element.StopOnError,
				                  element.AllowErrors);
			}

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
						return;
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

		[NotNull]
		public static QualityCondition PrepareInstanceConfiguration(
			[NotNull] XmlQualityCondition xmlQualityCondition,
			[NotNull] IDictionary<string, TestDescriptor> testDescriptorsByName)
		{
			string testDescriptorName = xmlQualityCondition.TestDescriptorName;
			Assert.True(StringUtils.IsNotEmpty(testDescriptorName), "test descriptor name");

			TestDescriptor testDescriptor;
			if (! testDescriptorsByName.TryGetValue(testDescriptorName.Trim(),
			                                        out testDescriptor))
			{
				Assert.Fail(
					"Test descriptor '{0}' referenced in quality condition '{1}' does not exist", // TODO '... quality condition ...' correct?
					testDescriptorName, xmlQualityCondition.Name);
			}

			var result = new QualityCondition(xmlQualityCondition.Name, testDescriptor);

			return result;
		}

		[CanBeNull]
		public static QualityCondition CreateQualityCondition(
			[NotNull] XmlQualityCondition xmlQualityCondition,
			[NotNull] XmlDataQualityDocumentCache documentCache,
			[NotNull] Func<string, IList<Dataset>> getDatasetsByName,
			[CanBeNull] DataQualityCategory category,
			bool ignoreForUnknownDatasets,
			out ICollection<XmlDatasetTestParameterValue> unknownDatasetParameters)
		{
			QualityCondition result =
				documentCache.CreateQualityCondition(xmlQualityCondition, getDatasetsByName,
				                                     ignoreForUnknownDatasets,
				                                     out unknownDatasetParameters);

			if (result == null)
			{
				return null;
			}

			UpdateQualityCondition(result, xmlQualityCondition, category);

			return result;
		}

		[CanBeNull]
		public static QualityCondition CreateQualityConditionLegacy(
			[NotNull] XmlQualityCondition xmlQualityCondition,
			[NotNull] TestDescriptor testDescriptor,
			[NotNull] IDictionary<string, Model> modelsByWorkspaceId,
			[NotNull] Func<string, IList<Dataset>> getDatasetsByName,
			[CanBeNull] DataQualityCategory category,
			bool ignoreForUnknownDatasets,
			out ICollection<XmlDatasetTestParameterValue> unknownDatasetParameters)
		{
			Assert.ArgumentNotNull(xmlQualityCondition, nameof(xmlQualityCondition));
			Assert.ArgumentNotNull(testDescriptor, nameof(testDescriptor));
			Assert.ArgumentNotNull(modelsByWorkspaceId, nameof(modelsByWorkspaceId));
			Assert.ArgumentNotNull(getDatasetsByName, nameof(getDatasetsByName));

			unknownDatasetParameters = new List<XmlDatasetTestParameterValue>();

			TestFactory testFactory =
				Assert.NotNull(TestFactoryUtils.GetTestFactory(testDescriptor));

			Dictionary<string, TestParameter> testParametersByName =
				testFactory.Parameters.ToDictionary(
					parameter => parameter.Name,
					StringComparer.OrdinalIgnoreCase);

			var result = new QualityCondition(xmlQualityCondition.Name, testDescriptor);

			UpdateQualityCondition(result, xmlQualityCondition, category);

			foreach (XmlTestParameterValue xmlTestParameterValue in
			         xmlQualityCondition.EnumParameterValues(ignoreEmptyValues: true))
			{
				TestParameter testParameter;
				if (! testParametersByName.TryGetValue(xmlTestParameterValue.TestParameterName,
				                                       out testParameter))
				{
					throw new InvalidConfigurationException(
						string.Format(
							"The name '{0}' as a test parameter in quality condition '{1}' " +
							"does not match test descriptor.",
							xmlTestParameterValue.TestParameterName,
							xmlQualityCondition.Name));
				}

				TestParameterValue parameterValue;

				var datasetValue = xmlTestParameterValue as XmlDatasetTestParameterValue;
				if (datasetValue != null)
				{
					parameterValue = CreateDatasetTestParameterValue(
						testParameter, datasetValue,
						Assert.NotNullOrEmpty(xmlQualityCondition.Name),
						modelsByWorkspaceId, getDatasetsByName, ignoreForUnknownDatasets);

					if (parameterValue == null)
					{
						unknownDatasetParameters.Add(datasetValue);
					}
				}
				else
				{
					var scalarValue = xmlTestParameterValue as XmlScalarTestParameterValue;
					if (scalarValue != null)
					{
						parameterValue = CreateScalarTestParameterValue(testParameter, scalarValue);
					}
					else
					{
						throw new InvalidProgramException("Unhandled TestParameterValue " +
						                                  xmlTestParameterValue.TestParameterName);
					}
				}

				if (parameterValue != null)
				{
					result.AddParameterValue(parameterValue);
				}
			}

			if (unknownDatasetParameters.Count > 0)
			{
				Assert.True(ignoreForUnknownDatasets, "ignoreForUnknownDatasets");

				return null;
			}

			return result;
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

		public static void UpdateIssueFilters(
			[NotNull] QualityCondition qualityCondition,
			[NotNull] XmlQualityCondition xmlCondition,
			[NotNull] IDictionary<string, IssueFilterConfiguration> issueFiltersByName)
		{
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));
			Assert.ArgumentNotNull(xmlCondition, nameof(xmlCondition));
			Assert.ArgumentNotNull(issueFiltersByName, nameof(issueFiltersByName));

			qualityCondition.ClearIssueFilterConfigurations();

			string issueFilterExpression = xmlCondition.IssueFilterExpression?.Expression;
			if (! string.IsNullOrWhiteSpace(issueFilterExpression))
			{
				IList<string> issueFilterNames =
					FilterUtils.GetFilterNames(issueFilterExpression);

				Assert.NotNull(issueFilterNames,
				               "Unable to get issue filter names from IssueFilterExpression defined for '{0}'",
				               xmlCondition.Name);

				foreach (string issueFilterName in issueFilterNames)
				{
					var issueFilterConfig = issueFiltersByName[issueFilterName.Trim()];

					Assert.NotNull(issueFilterConfig,
					               "IssueFilter '{0}' defined in IssueFilterExpression for '{1}' does not exist",
					               issueFilterName.Trim(), xmlCondition.Name);

					qualityCondition.AddIssueFilterConfiguration(issueFilterConfig);
				}
			}

			qualityCondition.IssueFilterExpression = issueFilterExpression;
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

			instanceConfiguration.Description = xmlInstanceConfiguration.Description;
			instanceConfiguration.Notes = xmlInstanceConfiguration.Notes;
			instanceConfiguration.Url = xmlInstanceConfiguration.Url;

			string uuid = xmlInstanceConfiguration.Uuid;
			if (StringUtils.IsNotEmpty(uuid))
			{
				instanceConfiguration.Uuid = uuid;
			}

			ImportMetadata(instanceConfiguration, xmlInstanceConfiguration);

			instanceConfiguration.Category = category;
		}

		[NotNull]
		private static TestParameterValue CreateScalarTestParameterValue(
			[NotNull] TestParameter testParameter,
			[NotNull] XmlScalarTestParameterValue xmlScalarTestParameterValue)
		{
			return new ScalarTestParameterValue(testParameter, xmlScalarTestParameterValue.Value);
		}

		[CanBeNull]
		public static TestParameterValue CreateDatasetTestParameterValue(
			[NotNull] TestParameter testParameter,
			[NotNull] XmlDatasetTestParameterValue xmlDatasetTestParameterValue,
			[NotNull] string qualityConditionName,
			[NotNull] IDictionary<string, Model> modelsByWorkspaceId,
			[NotNull] Func<string, IList<Dataset>> getDatasetsByName,
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

			TestParameterTypeUtils.AssertValidDataset(testParameter, dataset);

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
			[NotNull] IDictionary<string, Model> modelsByWorkspaceId,
			[NotNull] Func<string, IList<Dataset>> getDatasetsByName,
			bool ignoreUnknownDataset)
		{
			string datasetName = xmlDatasetTestParameterValue.Value;
			if (string.IsNullOrWhiteSpace(datasetName))
			{
				if (testParameter.IsConstructorParameter)
				{
					Assert.NotNullOrEmpty(
						datasetName,
						"Dataset is not defined for constructor-parameter '{0}' in quality condition '{1}'",
						testParameter.Name, qualityConditionName);
				}

				return null;
			}

			string workspaceId = xmlDatasetTestParameterValue.WorkspaceId;

			if (StringUtils.IsNotEmpty(workspaceId))
			{
				Model model;
				Assert.True(modelsByWorkspaceId.TryGetValue(workspaceId, out model),
				            "No matching model found for workspace id '{0}'", workspaceId);

				return ModelElementUtils.GetDatasetFromStoredName(datasetName,
					model,
					ignoreUnknownDataset);
			}

			if (StringUtils.IsNullOrEmptyOrBlank(workspaceId))
			{
				const string defaultModelId = "";

				Model defaultModel;
				if (modelsByWorkspaceId.TryGetValue(defaultModelId, out defaultModel))
				{
					// there is a default model
					return ModelElementUtils.GetDatasetFromStoredName(datasetName,
						defaultModel,
						ignoreUnknownDataset);
				}
			}

			// no workspace id for dataset, and there is no default model

			IList<Dataset> datasets = getDatasetsByName(datasetName);

			Assert.False(datasets.Count > 1,
			             "More than one dataset found with name '{0}', for parameter '{1}' in quality condition '{2}'",
			             datasetName, testParameter.Name, qualityConditionName);

			if (datasets.Count == 0)
			{
				if (ignoreUnknownDataset)
				{
					return null;
				}

				Assert.False(datasets.Count == 0,
				             "Dataset '{0}' for parameter '{1}' in quality condition '{2}' not found",
				             datasetName, testParameter.Name, qualityConditionName);
			}

			return datasets[0];
		}

		[NotNull]
		public static DataQualityCategory CreateDataQualityCategory(
			[NotNull] XmlDataQualityCategory xmlCategory,
			[CanBeNull] DataQualityCategory parentCategory,
			[CanBeNull] Func<string, Model> getModelByName = null)
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
				Model model = getModelByName(xmlCategory.DefaultModelName);
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

		public static void TransferProperties([NotNull] QualityCondition from,
		                                      [NotNull] QualityCondition to)
		{
			Assert.ArgumentNotNull(from, nameof(from));
			Assert.ArgumentNotNull(to, nameof(to));

			to.Name = from.Name;
			to.TestDescriptor = from.TestDescriptor;

			to.AllowErrorsOverride = from.AllowErrorsOverride;
			to.StopOnErrorOverride = from.StopOnErrorOverride;

			to.NeverFilterTableRowsUsingRelatedGeometry =
				from.NeverFilterTableRowsUsingRelatedGeometry;
			to.NeverStoreRelatedGeometryForTableRowIssues =
				from.NeverStoreRelatedGeometryForTableRowIssues;

			to.Category = from.Category;

			if (! AreParameterValuesEqual(to.ParameterValues, from.ParameterValues))
			{
				to.ClearParameterValues();
				foreach (TestParameterValue value in from.ParameterValues)
				{
					to.AddParameterValue(value);
				}
			}

			// TODO consider adding option to not overwrite if description in xml is empty
			to.Description = from.Description;
			to.Url = from.Url;

			TransferMetadata(from, to);
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

		[NotNull]
		public static IWorkspace OpenWorkspace([NotNull] XmlWorkspace xmlWorkspace)
		{
			Assert.ArgumentNotNull(xmlWorkspace, nameof(xmlWorkspace));
			Assert.ArgumentCondition(
				StringUtils.IsNotEmpty(xmlWorkspace.CatalogPath) ||
				StringUtils.IsNotEmpty(xmlWorkspace.ConnectionString),
				"neither catalog path nor connection string are specified for xml workspace id '{0}'",
				xmlWorkspace.ID);

			if (StringUtils.IsNotEmpty(xmlWorkspace.CatalogPath))
			{
				try
				{
					return WorkspaceUtils.OpenWorkspace(xmlWorkspace.CatalogPath);
				}
				catch (Exception e)
				{
					throw new InvalidConfigurationException(
						string.Format(
							"Unable to open workspace for catalog path of xml workspace with id '{0}': {1}",
							xmlWorkspace.ID, e.Message));
				}
			}

			Assert.NotNullOrEmpty(xmlWorkspace.FactoryProgId,
			                      "no factory progId is specified for xml workspace id '{0}'",
			                      xmlWorkspace.ID);

			try
			{
				return WorkspaceUtils.OpenWorkspace(xmlWorkspace.ConnectionString,
				                                    xmlWorkspace.FactoryProgId);
			}
			catch (Exception e)
			{
				throw new InvalidConfigurationException(
					string.Format(
						"Unable to open workspace for connection string of xml workspace with id '{0}': {1}",
						xmlWorkspace.ID, e.Message), e);
			}
		}

		[NotNull]
		public static string ConcatenateUnknownDatasetNames(
			[NotNull] IEnumerable<XmlDatasetTestParameterValue> unknownDatasetParameters,
			[NotNull] IDictionary<string, Model> modelsByWorkspaceId,
			[NotNull] string anonymousWorkspaceId)
		{
			Assert.ArgumentNotNull(unknownDatasetParameters, nameof(unknownDatasetParameters));
			Assert.ArgumentNotNull(modelsByWorkspaceId, nameof(modelsByWorkspaceId));
			Assert.ArgumentNotNull(anonymousWorkspaceId, nameof(anonymousWorkspaceId));

			var sb = new StringBuilder();

			foreach (XmlDatasetTestParameterValue datasetParameter in unknownDatasetParameters)
			{
				if (sb.Length > 0)
				{
					sb.Append(", ");
				}

				string workspaceId = datasetParameter.WorkspaceId ?? anonymousWorkspaceId;
				Model model;
				if (modelsByWorkspaceId.TryGetValue(workspaceId, out model))
				{
					sb.AppendFormat("{0} ({1})", datasetParameter.Value, model.Name);
				}
				else
				{
					sb.Append(datasetParameter.Value);
				}
			}

			return sb.ToString();
		}

		[CanBeNull]
		public static Dataset GetDatasetByParameterValue(
			[NotNull] Model model,
			[NotNull] string datasetParameterValue)
		{
			const bool ignoreUnknownDataset = true;
			return ModelElementUtils.GetDatasetFromStoredName(datasetParameterValue,
			                                                  model,
			                                                  ignoreUnknownDataset);
		}

		[NotNull]
		public static IEnumerable<Dataset> GetReferencedDatasets(
			[NotNull] Model model,
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

		private static bool AreParameterValuesEqual(
			[CanBeNull] IList<TestParameterValue> list1,
			[CanBeNull] IList<TestParameterValue> list2)
		{
			if ((list1 == null) != (list2 == null))
			{
				return false;
			}

			if (list1 == null)
			{
				Assert.Null(list2, "list1 is null but list2 isn't");
				return true;
			}

			int list1Count = list1.Count;
			if (list1Count != list2.Count)
			{
				return false;
			}

			for (var i = 0; i < list1Count; i++)
			{
				TestParameterValue value1 = list1[i];
				TestParameterValue value2 = list2[i];

				if (value1.Equals(value2) == false)
				{
					return false;
				}
			}

			return true;
		}

		private static void Populate(
			[NotNull] XmlDataQualityDocument document,
			[NotNull] IList<QualitySpecification> qualitySpecifications,
			[NotNull] IList<InstanceDescriptor> descriptors,
			[NotNull] IEnumerable<DataQualityCategory> categories,
			bool exportMetadata,
			bool exportWorkspaceConnections,
			bool exportConnectionFilePaths,
			bool exportAllDescriptors,
			bool exportAllCategories,
			bool exportNotes)
		{
			Assert.ArgumentNotNull(document, nameof(document));
			Assert.ArgumentNotNull(qualitySpecifications, nameof(qualitySpecifications));
			Assert.ArgumentNotNull(descriptors, nameof(descriptors));
			Assert.ArgumentNotNull(categories, nameof(categories));

			IDictionary<Model, string> workspaceIdsByModel =
				AddWorkspaces(qualitySpecifications,
				              document,
				              exportWorkspaceConnections,
				              exportConnectionFilePaths);

			IReadOnlyList<QualityCondition> qualityConditions =
				GetQualityConditions(qualitySpecifications);

			// TODO: add any test descriptors that are referenced in the quality specifications but were not passed in
			// TODO: allow the test descriptors collection to be empty (or even null?)

			GetConfigurations(qualityConditions,
			                  out IReadOnlyList<TransformerConfiguration> transformerConfigurations,
			                  out IReadOnlyList<IssueFilterConfiguration>
				                      issueFilterConfigurations);

			IReadOnlyList<TestDescriptor> testDescriptors =
				exportAllDescriptors
					? descriptors.OfType<TestDescriptor>().ToList()
					: GetTestDescriptors(qualityConditions);

			IReadOnlyList<TransformerDescriptor> transformerDescriptors =
				exportAllDescriptors
					? descriptors.OfType<TransformerDescriptor>().ToList()
					: GetTransformerDescriptors(transformerConfigurations);

			IReadOnlyList<IssueFilterDescriptor> issueFilterDescriptors =
				exportAllDescriptors
					? descriptors.OfType<IssueFilterDescriptor>().ToList()
					: GetIssueFilterDescriptors(issueFilterConfigurations);

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

			AddCategories(document, categories,
			              c => qualityConditions.Where(qc => Equals(qc.Category, c))
			                                    .ToList(),
			              c => qualitySpecifications.Where(qs => Equals(qs.Category, c))
			                                        .ToList(),
			              workspaceIdsByModel,
			              exportMetadata,
			              exportAllCategories,
			              exportNotes);

			// export root level quality conditions
			foreach (QualityCondition qualityCondition in qualityConditions)
			{
				if (qualityCondition.Category == null)
				{
					document.AddQualityCondition(
						CreateXmlQualityCondition(qualityCondition,
						                          workspaceIdsByModel,
						                          exportMetadata,
						                          exportNotes));
				}
			}

			foreach (TransformerConfiguration transformer in
			         GetSortedConfigurations(transformerConfigurations))
			{
				document.AddTransformer(
					CreateXmlTransformerConfiguration(transformer, workspaceIdsByModel,
					                                  exportMetadata,
					                                  exportNotes));
			}

			foreach (IssueFilterConfiguration issueFilter in
			         GetSortedConfigurations(issueFilterConfigurations))
			{
				document.AddIssueFilter(
					CreateXmlIssueFilterConfiguration(issueFilter, workspaceIdsByModel,
					                                  exportMetadata,
					                                  exportNotes));
			}

			// export root level quality specifications
			foreach (QualitySpecification qualitySpecification in qualitySpecifications)
			{
				if (qualitySpecification.Category == null)
				{
					document.AddQualitySpecification(
						CreateXmlQualitySpecification(qualitySpecification,
						                              exportMetadata,
						                              exportNotes));
				}
			}
		}

		[NotNull]
		private static IDictionary<Model, string> AddWorkspaces(
			[NotNull] IEnumerable<QualitySpecification> qualitySpecifications,
			[NotNull] XmlDataQualityDocument document,
			bool exportWorkspaceConnections,
			bool exportConnectionFilePaths)
		{
			var result = new Dictionary<Model, string>();

			foreach (QualitySpecification qualitySpecification in qualitySpecifications)
			{
				foreach (QualitySpecificationElement element in qualitySpecification.Elements)
				{
					QualityCondition qualityCondition = element.QualityCondition;

					foreach (Dataset dataset in qualityCondition.GetDatasetParameterValues(
						         true, true))
					{
						Model model = (Model) dataset.Model;
						if (result.ContainsKey(model))
						{
							continue;
						}

						XmlWorkspace xmlWorkspace = CreateXmlWorkspace(
							model,
							exportWorkspaceConnections,
							exportConnectionFilePaths);

						document.AddWorkspace(xmlWorkspace);

						result.Add(model, xmlWorkspace.ID);
					}
				}
			}

			return result;
		}

		private static void AddCategories(
			[NotNull] XmlDataQualityDocument document,
			[NotNull] IEnumerable<DataQualityCategory> categories,
			[NotNull] Func<DataQualityCategory, IList<QualityCondition>> getQualityConditions,
			[NotNull] Func<DataQualityCategory, IList<QualitySpecification>>
				getQualitySpecifications,
			[NotNull] IDictionary<Model, string> workspaceIdsByModel,
			bool exportMetadata,
			bool exportAllCategories,
			bool exportNotes)
		{
			Assert.ArgumentNotNull(document, nameof(document));
			Assert.ArgumentNotNull(categories, nameof(categories));

			foreach (DataQualityCategory category in GetSorted(
				         categories.Where(c => c.ParentCategory == null)))
			{
				document.AddCategory(CreateXmlDataQualityCategory(category,
					                     getQualityConditions,
					                     getQualitySpecifications,
					                     workspaceIdsByModel,
					                     exportMetadata,
					                     exportAllCategories,
					                     exportNotes));
			}
		}

		[NotNull]
		private static IReadOnlyList<QualityCondition> GetQualityConditions(
			[NotNull] IEnumerable<QualitySpecification> qualitySpecifications)
		{
			var uniqueConditions = new HashSet<QualityCondition>();

			foreach (QualitySpecification qualitySpecification in qualitySpecifications)
			{
				foreach (QualitySpecificationElement element in qualitySpecification.Elements)
				{
					QualityCondition qualityCondition = element.QualityCondition;

					if (uniqueConditions.Contains(qualityCondition))
					{
						continue;
					}

					Assert.NotNull(
						TestFactoryUtils.CreateTestFactory(qualityCondition),
						$"Cannot create test factory for condition {qualityCondition.Name}");

					uniqueConditions.Add(qualityCondition);
				}
			}

			return uniqueConditions.ToList();
		}

		private static void GetConfigurations(
			[NotNull] IEnumerable<QualityCondition> qualityConditions,
			out IReadOnlyList<TransformerConfiguration> transformerConfigurations,
			out IReadOnlyList<IssueFilterConfiguration> issueFilterConfigurations)
		{
			HashSet<TransformerConfiguration>
				allTransformers = new HashSet<TransformerConfiguration>();

			HashSet<IssueFilterConfiguration>
				allIssueFilters = new HashSet<IssueFilterConfiguration>();
			foreach (QualityCondition qualityCondition in qualityConditions)
			{
				CollectTransformers(qualityCondition, allTransformers);
				foreach (var issueFilter in qualityCondition.IssueFilterConfigurations)
				{
					if (allIssueFilters.Add(issueFilter))
					{
						Assert.NotNull(
							InstanceFactoryUtils.CreateFactory(issueFilter),
							$"Cannot create factory for issue filter {issueFilter.Name}");
					}

					CollectTransformers(issueFilter, allTransformers);
				}
			}

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
						Assert.NotNull(
							InstanceFactoryUtils.CreateFactory(transformer),
							$"Cannot create factory for transformer {transformer.Name}");
					}

					CollectTransformers(transformer, allTransformers);
				}
			}
		}

		[NotNull]
		private static IReadOnlyList<TestDescriptor> GetTestDescriptors(
			[NotNull] IEnumerable<QualityCondition> qualityConditions)
		{
			var descriptors = new HashSet<TestDescriptor>();

			foreach (QualityCondition qualityCondition in qualityConditions)
			{
				TestDescriptor testDescriptor = qualityCondition.TestDescriptor;

				if (testDescriptor != null)
				{
					descriptors.Add(testDescriptor);
				}
			}

			return descriptors.ToList();
		}

		private static IReadOnlyList<TransformerDescriptor> GetTransformerDescriptors(
			[NotNull] IEnumerable<TransformerConfiguration> transformers)
		{
			HashSet<TransformerDescriptor> descriptors = new HashSet<TransformerDescriptor>();
			foreach (TransformerConfiguration configuration in transformers)
			{
				descriptors.Add(configuration.TransformerDescriptor);
			}

			return descriptors.ToList();
		}

		private static IReadOnlyList<IssueFilterDescriptor> GetIssueFilterDescriptors(
			[NotNull] IEnumerable<IssueFilterConfiguration> issueFilters)
		{
			HashSet<IssueFilterDescriptor> descriptors = new HashSet<IssueFilterDescriptor>();
			foreach (IssueFilterConfiguration configuration in issueFilters)
			{
				descriptors.Add(configuration.IssueFilterDescriptor);
			}

			return descriptors.ToList();
		}

		[NotNull]
		private static IEnumerable<QualitySpecification> GetSorted(
			[NotNull] IEnumerable<QualitySpecification> qualitySpecifications)
		{
			return qualitySpecifications.OrderBy(
				qs => string.Format("{0}#{1}", qs.ListOrder, qs.Name));
		}

		[NotNull]
		private static IEnumerable<DataQualityCategory> GetSorted(
			[NotNull] IEnumerable<DataQualityCategory> categories)
		{
			return categories.OrderBy(c => string.Format("{0}#{1}", c.ListOrder, c.Name));
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
			[NotNull] Func<DataQualityCategory, IList<QualityCondition>> getQualityConditions,
			[NotNull] Func<DataQualityCategory, IList<QualitySpecification>>
				getQualitySpecifications,
			[NotNull] IDictionary<Model, string> workspaceIdsByModel,
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

			foreach (DataQualityCategory subCategory in GetSorted(category.SubCategories))
			{
				XmlDataQualityCategory xmlSubCategory = CreateXmlDataQualityCategory(subCategory,
					getQualityConditions,
					getQualitySpecifications,
					workspaceIdsByModel,
					exportMetadata,
					exportAllCategories,
					exportNotes);

				if (! exportAllCategories &&
				    ! xmlSubCategory.ContainsQualityConditions &&
				    ! xmlSubCategory.ContainsQualitySpecifications)
				{
					continue;
				}

				result.AddSubCategory(xmlSubCategory);
			}

			foreach (QualityCondition condition in GetSortedConfigurations(
				         getQualityConditions(category)))
			{
				result.AddQualityCondition(CreateXmlQualityCondition(condition,
					                           workspaceIdsByModel,
					                           exportMetadata,
					                           exportNotes));
			}

			foreach (QualitySpecification specification in GetSorted(
				         getQualitySpecifications(category)))
			{
				result.AddQualitySpecification(CreateXmlQualitySpecification(specification,
					                               exportMetadata,
					                               exportNotes));
			}

			return result;
		}

		[NotNull]
		private static XmlQualitySpecification CreateXmlQualitySpecification(
			[NotNull] QualitySpecification qualitySpecification,
			bool exportMetadata,
			bool exportNotes)
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

			foreach (QualitySpecificationElement element in qualitySpecification.Elements)
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
				new XmlTestDescriptor
				{
					Name = Escape(testDescriptor.Name),
					Description = Escape(testDescriptor.Description),
					StopOnError = testDescriptor.StopOnError,
					AllowErrors = testDescriptor.AllowErrors,
					TestClass = CreateXmlClassDescriptor(testDescriptor.TestClass,
					                                     testDescriptor.TestConstructorId),
					TestFactoryDescriptor = CreateXmlClassDescriptor(
						testDescriptor.TestFactoryDescriptor),
					TestConfigurator = CreateXmlClassDescriptor(
						testDescriptor.TestConfigurator)
				};

			if (exportMetadata)
			{
				ExportMetadata(testDescriptor, xmlDescriptor);
			}

			xmlDescriptor.SetExecutionPriority(testDescriptor.ExecutionPriority);

			return xmlDescriptor;
		}

		[NotNull]
		public static XmlTransformerDescriptor CreateXmlTransformerDescriptor(
			[NotNull] TransformerDescriptor transformerDescriptor, bool exportMetadata)
		{
			Assert.ArgumentNotNull(transformerDescriptor, nameof(transformerDescriptor));

			var xmlDescriptor =
				new XmlTransformerDescriptor
				{
					Name = Escape(transformerDescriptor.Name),
					Description = Escape(transformerDescriptor.Description),
					TransformerClass = CreateXmlClassDescriptor(
						transformerDescriptor.Class, transformerDescriptor.ConstructorId),
				};

			if (exportMetadata)
			{
				ExportMetadata(transformerDescriptor, xmlDescriptor);
			}

			return xmlDescriptor;
		}

		[NotNull]
		public static XmlIssueFilterDescriptor CreateXmlIssueFilterDescriptor(
			[NotNull] IssueFilterDescriptor issueFilterDescriptor, bool exportMetadata)
		{
			Assert.ArgumentNotNull(issueFilterDescriptor, nameof(issueFilterDescriptor));

			var xmlDescriptor =
				new XmlIssueFilterDescriptor
				{
					Name = Escape(issueFilterDescriptor.Name),
					Description = Escape(issueFilterDescriptor.Description),
					IssueFilterClass = CreateXmlClassDescriptor(
						issueFilterDescriptor.Class, issueFilterDescriptor.ConstructorId),
				};

			if (exportMetadata)
			{
				ExportMetadata(issueFilterDescriptor, xmlDescriptor);
			}

			return xmlDescriptor;
		}

		[NotNull]
		private static XmlQualityCondition CreateXmlQualityCondition(
			[NotNull] QualityCondition qualityCondition,
			[NotNull] IDictionary<Model, string> workspaceIdsByModel,
			bool exportMetadata, bool exportNotes)
		{
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));
			Assert.ArgumentNotNull(workspaceIdsByModel, nameof(workspaceIdsByModel));

			var xmlCondition =
				new XmlQualityCondition
				{
					Name = Escape(qualityCondition.Name),
					Uuid = qualityCondition.Uuid,
					VersionUuid = qualityCondition.VersionUuid,
					Url = Escape(qualityCondition.Url),
					TestDescriptorName = Escape(qualityCondition.TestDescriptor.Name),
					Description = Escape(qualityCondition.Description),
					AllowErrors = EOverride(qualityCondition.AllowErrorsOverride),
					StopOnError = EOverride(qualityCondition.StopOnErrorOverride),
					NeverFilterTableRowsUsingRelatedGeometry =
						qualityCondition.NeverFilterTableRowsUsingRelatedGeometry,
					NeverStoreRelatedGeometryForTableRowIssues =
						qualityCondition.NeverStoreRelatedGeometryForTableRowIssues,

					IssueFilterExpression = CreateXmlFilterExpression(
						qualityCondition.IssueFilterExpression,
						qualityCondition.IssueFilterConfigurations)
				};

			if (exportMetadata)
			{
				ExportMetadata(qualityCondition, xmlCondition);
			}

			if (exportNotes)
			{
				xmlCondition.Notes = Escape(qualityCondition.Notes);
			}

			foreach (TestParameterValue parameterValue in qualityCondition.ParameterValues)
			{
				xmlCondition.ParameterValues.Add(
					CreateXmlTestParameterValue(parameterValue, qualityCondition,
					                            workspaceIdsByModel));
			}

			return xmlCondition;
		}

		[NotNull]
		private static XmlTransformerConfiguration CreateXmlTransformerConfiguration(
			[NotNull] TransformerConfiguration transformer,
			[NotNull] IDictionary<Model, string> workspaceIdsByModel,
			bool exportMetadata, bool exportNotes)
		{
			Assert.ArgumentNotNull(transformer, nameof(transformer));
			Assert.ArgumentNotNull(workspaceIdsByModel, nameof(workspaceIdsByModel));

			var xmlTransformer =
				new XmlTransformerConfiguration
				{
					Name = Escape(transformer.Name),
					Uuid = transformer.Uuid,
					// TODO: VersionUuid = transformer.VersionUuid,
					Url = Escape(transformer.Url),
					TransformerDescriptorName = Escape(transformer.TransformerDescriptor.Name),
					Description = Escape(transformer.Description),
				};

			if (exportMetadata)
			{
				ExportMetadata(transformer, xmlTransformer);
			}

			if (exportNotes)
			{
				xmlTransformer.Notes = Escape(transformer.Notes);
			}

			foreach (TestParameterValue parameterValue in transformer.ParameterValues)
			{
				xmlTransformer.ParameterValues.Add(
					CreateXmlTestParameterValue(parameterValue, transformer,
					                            workspaceIdsByModel));
			}

			return xmlTransformer;
		}

		[NotNull]
		private static XmlIssueFilterConfiguration CreateXmlIssueFilterConfiguration(
			[NotNull] IssueFilterConfiguration issueFilter,
			[NotNull] IDictionary<Model, string> workspaceIdsByModel,
			bool exportMetadata, bool exportNotes)
		{
			Assert.ArgumentNotNull(issueFilter, nameof(issueFilter));
			Assert.ArgumentNotNull(workspaceIdsByModel, nameof(workspaceIdsByModel));

			var xmlIssueFilter =
				new XmlIssueFilterConfiguration
				{
					Name = Escape(issueFilter.Name),
					Uuid = issueFilter.Uuid,
					// TODO: VersionUuid = issueFilter.VersionUuid,
					Url = Escape(issueFilter.Url),
					IssueFilterDescriptorName = Escape(issueFilter.IssueFilterDescriptor.Name),
					Description = Escape(issueFilter.Description),
				};

			if (exportMetadata)
			{
				ExportMetadata(issueFilter, xmlIssueFilter);
			}

			if (exportNotes)
			{
				xmlIssueFilter.Notes = Escape(issueFilter.Notes);
			}

			foreach (TestParameterValue parameterValue in issueFilter.ParameterValues)
			{
				xmlIssueFilter.ParameterValues.Add(
					CreateXmlTestParameterValue(parameterValue, issueFilter,
					                            workspaceIdsByModel));
			}

			return xmlIssueFilter;
		}

		[NotNull]
		private static XmlTestParameterValue CreateXmlTestParameterValue(
			[NotNull] TestParameterValue parameterValue,
			[NotNull] InstanceConfiguration instanceConfiguration,
			[NotNull] IDictionary<Model, string> workspaceIdsByModel)
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
			catch (Exception exception)
			{
				throw new InvalidConfigurationException(
					$"Parameter {parameterValue.TestParameterName} in quality condition {instanceConfiguration.Name} is invalid: {exception.Message}",
					exception);
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
			[NotNull] IDictionary<Model, string> workspaceIdsByModel)
		{
			Dataset dataset = datasetTestParameterValue.DatasetValue;

			string datasetName;
			string workspaceId;
			if (dataset != null)
			{
				datasetName = dataset.Name;
				Assert.NotNull(dataset.Model, "dataset model is null");

				if (! workspaceIdsByModel.TryGetValue((Model) dataset.Model, out workspaceId))
				{
					throw new ArgumentException(
						string.Format("model not found in dictionary: {0}", dataset.Model),
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

		[CanBeNull]
		private static XmlFilterExpression CreateXmlFilterExpression<T>(
			[CanBeNull] string expression, [NotNull] IList<T> filters)
			where T : InstanceConfiguration
		{
			if (string.IsNullOrWhiteSpace(expression))
			{
				if (filters.Count == 0)
				{
					return null;
				}

				if (filters.Count > 1)
				{
					throw new InvalidConfigurationException(
						$"A filter expression must be set for multiple filters {string.Concat(filters.Select(x => "{x},"))}");
				}

				expression = filters[0].Name;
			}

			return new XmlFilterExpression {Expression = expression};
		}

		[NotNull]
		private static XmlWorkspace CreateXmlWorkspace(
			[NotNull] Model model,
			bool exportWorkspaceConnections,
			bool exportConnectionFilePath)
		{
			var result = new XmlWorkspace
			             {
				             ID = Escape(model.Name),
				             ModelName = Escape(model.Name),
				             Database = model.DefaultDatabaseName,
				             SchemaOwner = model.DefaultDatabaseSchemaOwner
			             };

			if (exportWorkspaceConnections)
			{
				IWorkspace workspace = model.GetMasterDatabaseWorkspace();

				Assert.NotNull(workspace,
				               "Unable to determine workspace connection string for model {0} " +
				               "(cannot open model master database workspace)",
				               model.Name);

				string catalogPath = workspace.Type ==
				                     esriWorkspaceType.esriRemoteDatabaseWorkspace &&
				                     ! exportConnectionFilePath
					                     ? null // don't use catalog path even if defined
					                     : WorkspaceUtils.TryGetCatalogPath(workspace);

				if (! string.IsNullOrEmpty(catalogPath))
				{
					result.CatalogPath = catalogPath;
				}
				else
				{
					result.ConnectionString = WorkspaceUtils.GetConnectionString(workspace);
					result.FactoryProgId = WorkspaceUtils.GetFactoryProgId(workspace);
				}
			}

			return result;
		}

		public static void ExportMetadata([NotNull] IEntityMetadata entity,
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
