using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.AO.QA.Xml
{
	public static class XmlDataQualityUtils
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		[NotNull]
		public static XmlDataQualityDocument ReadXmlDocument(
			[NotNull] TextReader xml,
			[NotNull] out IList<XmlQualitySpecification> qualitySpecifications)
		{
			Assert.ArgumentNotNull(xml, nameof(xml));

			XmlDataQualityDocument document = Deserialize(xml);

			Assert.ArgumentCondition(document.GetAllQualitySpecifications().Any(),
			                         "The document does not contain any quality specifications");

			AssertUniqueQualitySpecificationNames(document);
			AssertUniqueQualityConditionNames(document);
			AssertUniqueTestDescriptorNames(document);

			qualitySpecifications = document.GetAllQualitySpecifications()
			                                .Select(p => p.Key)
			                                .Where(qs => qs.Elements.Count > 0)
			                                .ToList();

			return document;
		}

		[NotNull]
		public static XmlDataQualityDocument Deserialize([NotNull] TextReader xml)
		{
			Assert.ArgumentNotNull(xml, nameof(xml));

			string schema = Schema.ProSuite_QA_QualitySpecifications_2_0;

			try
			{
				return XmlUtils.Deserialize<XmlDataQualityDocument>(xml, schema);
			}
			catch (Exception e)
			{
				throw new XmlDeserializationException(
					string.Format("Error deserializing xml: {0}", e.Message), e);
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

		public static void AssertUniqueQualityConditionUuids(
			[NotNull] XmlDataQualityDocument document)
		{
			Assert.ArgumentNotNull(document, nameof(document));

			var uuids = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

			foreach (XmlQualityCondition xmlQualityCondition in
				document.GetAllQualityConditions().Select(p => p.Key))
			{
				foreach (string uuid in new[]
				                        {
					                        xmlQualityCondition.Uuid,
					                        xmlQualityCondition.VersionUuid
				                        })
				{
					if (StringUtils.IsNullOrEmptyOrBlank(uuid))
					{
						continue;
					}

					string trimmedUuid = uuid.Trim();

					if (uuids.Contains(trimmedUuid))
					{
						Assert.Fail("Duplicate UUID in document: {0} (quality condition {1})",
						            trimmedUuid, xmlQualityCondition.Name);
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

		public static void AssertUniqueQualityConditionNames(
			[NotNull] XmlDataQualityDocument document)
		{
			Assert.ArgumentNotNull(document, nameof(document));

			var names = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

			foreach (XmlQualityCondition xmlQualityCondition in
				document.GetAllQualityConditions().Select(p => p.Key))
			{
				string name = xmlQualityCondition.Name;

				Assert.True(StringUtils.IsNotEmpty(name),
				            "Missing quality condition name in document");

				string trimmedName = name.Trim();

				if (names.Contains(trimmedName))
				{
					Assert.Fail("Duplicate quality condition name in document: {0}", trimmedName);
				}

				names.Add(trimmedName);
			}
		}

		public static void AssertUniqueQualifiedCategoryNames(
			[NotNull] XmlDataQualityDocument document)
		{
			if (document.Categories != null)
			{
				AssertUniqueCategoryNames(document.Categories);
			}
		}

		public static void AssertUniqueTestDescriptorNames(
			[NotNull] XmlDataQualityDocument document)
		{
			Assert.ArgumentNotNull(document, nameof(document));

			if (document.TestDescriptors == null)
			{
				return;
			}

			var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (XmlTestDescriptor testDescriptor in document.TestDescriptors)
			{
				string name = testDescriptor.Name;
				Assert.True(StringUtils.IsNotEmpty(name),
				            "Test descriptor with undefined name encountered");

				string trimmedName = name.Trim();

				Assert.False(names.Contains(trimmedName),
				             "Duplicate test descriptor name: {0}",
				             trimmedName);

				names.Add(trimmedName);
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

		public static void AssertUniqueElementNames(
			[NotNull] XmlQualitySpecification xmlSpecification)
		{
			Assert.ArgumentNotNull(xmlSpecification, nameof(xmlSpecification));

			var names = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

			foreach (
				XmlQualitySpecificationElement xmlQualitySpecificationElement in
				xmlSpecification.Elements)
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
			[NotNull] XmlDataQualityDocument document,
			[NotNull] IEnumerable<XmlQualityCondition> referencedConditions)
		{
			bool hasUndefinedWorkspaceReference;
			return GetReferencedWorkspaces(document,
			                               referencedConditions,
			                               out hasUndefinedWorkspaceReference);
		}

		[NotNull]
		public static IList<XmlWorkspace> GetReferencedWorkspaces(
			[NotNull] XmlDataQualityDocument document,
			[NotNull] IEnumerable<XmlQualityCondition> referencedConditions,
			out bool hasUndefinedWorkspaceReference)
		{
			Assert.ArgumentNotNull(document, nameof(document));
			Assert.ArgumentNotNull(referencedConditions, nameof(referencedConditions));

			var referencedWorkspaceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			hasUndefinedWorkspaceReference = false;

			foreach (XmlQualityCondition xmlQualityCondition in referencedConditions)
			{
				foreach (XmlTestParameterValue xmlTestParameterValue in
					xmlQualityCondition.EnumParameterValues(ignoreEmptyValues: true))
				{
					var datasetTestParameterValue =
						xmlTestParameterValue as XmlDatasetTestParameterValue;
					if (datasetTestParameterValue == null)
					{
						continue;
					}

					if (string.IsNullOrEmpty(datasetTestParameterValue.WorkspaceId))
					{
						hasUndefinedWorkspaceReference = true;
					}
					else
					{
						referencedWorkspaceIds.Add(datasetTestParameterValue.WorkspaceId);
					}
				}
			}

			return document.Workspaces?.Where(
				               workspace => referencedWorkspaceIds.Contains(workspace.ID))
			               .ToList()
			       ?? new List<XmlWorkspace>();
		}

		[NotNull]
		public static IEnumerable<KeyValuePair<XmlQualityCondition, XmlDataQualityCategory>>
			GetReferencedXmlQualityConditions(
				[NotNull] XmlDataQualityDocument document,
				[NotNull] IEnumerable<XmlQualitySpecification> xmlQualitySpecifications)
		{
			Assert.ArgumentNotNull(document, nameof(document));
			Assert.ArgumentNotNull(xmlQualitySpecifications, nameof(xmlQualitySpecifications));

			ICollection<string> referencedConditionNames =
				GetReferencedQualityConditionNames(xmlQualitySpecifications);

			return document.GetAllQualityConditions()
			               .Where(pair => referencedConditionNames.Contains(
				                      pair.Key.Name?.Trim()));
		}

		[NotNull]
		public static QualitySpecification CreateQualitySpecification(
			[NotNull] IDictionary<string, QualityCondition> qualityConditionsByName,
			[NotNull] XmlQualitySpecification xmlSpecification,
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

			UpdateSpecification(result,
			                    xmlSpecification,
			                    qualityConditionsByName,
			                    category,
			                    ignoreMissingConditions);

			return result;
		}

		public static void UpdateSpecification(
			[NotNull] QualitySpecification qualitySpecification,
			[NotNull] XmlQualitySpecification xmlSpecification,
			[NotNull] IDictionary<string, QualityCondition> conditions,
			[CanBeNull] DataQualityCategory category,
			bool ignoreMissingConditions = false)
		{
			Assert.ArgumentNotNull(qualitySpecification, nameof(qualitySpecification));
			Assert.ArgumentNotNull(xmlSpecification, nameof(xmlSpecification));
			Assert.ArgumentNotNull(conditions, nameof(conditions));

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

			foreach (XmlQualitySpecificationElement xmlElement in
				xmlSpecification.Elements)
			{
				AddQualitySpecificationElement(qualitySpecification, conditions,
				                               xmlElement, ignoreMissingConditions);
			}
		}

		[CanBeNull]
		public static QualityCondition CreateQualityCondition(
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

			var result = new QualityCondition(xmlQualityCondition.Name, testDescriptor)
			             {
				             Description = xmlQualityCondition.Description,
				             Notes = xmlQualityCondition.Notes,
				             Url = xmlQualityCondition.Url,
				             AllowErrorsOverride =
					             TranslateOverride(xmlQualityCondition.AllowErrors),
				             StopOnErrorOverride =
					             TranslateOverride(xmlQualityCondition.StopOnError),
				             NeverFilterTableRowsUsingRelatedGeometry =
					             xmlQualityCondition.NeverFilterTableRowsUsingRelatedGeometry,
				             NeverStoreRelatedGeometryForTableRowIssues =
					             xmlQualityCondition.NeverStoreRelatedGeometryForTableRowIssues
			             };

			string uuid = xmlQualityCondition.Uuid;
			if (StringUtils.IsNotEmpty(uuid))
			{
				result.Uuid = uuid;
			}

			string versionUuid = xmlQualityCondition.VersionUuid;
			if (StringUtils.IsNotEmpty(versionUuid))
			{
				result.VersionUuid = versionUuid;
			}

			ImportMetadata(result, xmlQualityCondition);

			result.Category = category;

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
							"defined in import document does not match test descriptor.",
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

				result.TestConfigurator = testConfigDescriptor;
			}

			Assert.NotNull(TestFactoryUtils.GetTestFactory(result),
			               "Error in xml test descriptor '{0}'", result.Name);

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

		public static void TransferProperties([NotNull] TestDescriptor from,
		                                      [NotNull] TestDescriptor to,
		                                      bool updateName,
		                                      bool updateProperties)
		{
			Assert.ArgumentNotNull(from, nameof(from));
			Assert.ArgumentNotNull(to, nameof(to));

			if (updateName)
			{
				if (! string.Equals(to.Name, from.Name))
				{
					_msg.InfoFormat("Updating name of test descriptor {0} -> {1}", to.Name,
					                from.Name);

					to.Name = from.Name;
				}
			}

			if (updateProperties)
			{
				_msg.InfoFormat("Updating properties of test descriptor {0}", to.Name);

				to.Description = from.Description;

				to.AllowErrors = from.AllowErrors;
				to.StopOnError = from.StopOnError;
				to.ExecutionPriority = from.ExecutionPriority;

				TransferMetadata(from, to);

				if (from.TestConfigurator != null)
				{
					to.TestConfigurator = from.TestConfigurator;
				}
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

		[NotNull]
		public static XmlDataQualityDocument CreateXmlDataQualityDocument(
			[NotNull] IEnumerable<QualitySpecification> qualitySpecifications,
			[NotNull] IEnumerable<TestDescriptor> testDescriptors,
			[NotNull] IEnumerable<DataQualityCategory> categories,
			bool exportMetadata,
			bool exportConnections,
			bool exportConnectionFilePaths,
			bool exportAllCategories,
			bool exportNotes)
		{
			Assert.ArgumentNotNull(qualitySpecifications, nameof(qualitySpecifications));
			Assert.ArgumentNotNull(testDescriptors, nameof(testDescriptors));

			var result = new XmlDataQualityDocument();

			Populate(result,
			         qualitySpecifications.ToList(),
			         testDescriptors,
			         categories,
			         exportMetadata,
			         exportConnections,
			         exportConnectionFilePaths,
			         exportAllCategories,
			         exportNotes);

			return result;
		}

		public static void ExportDocument([NotNull] XmlDataQualityDocument document,
		                                  [NotNull] string xmlFilePath)
		{
			Assert.ArgumentNotNull(document, nameof(document));
			Assert.ArgumentNotNullOrEmpty(xmlFilePath, nameof(xmlFilePath));

			XmlUtils.Serialize(document, xmlFilePath);
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
			[NotNull] IEnumerable<XmlQualityCondition> referencedConditions)
		{
			Assert.ArgumentNotNull(model, nameof(model));
			Assert.ArgumentNotNull(referencedConditions, nameof(referencedConditions));
			Assert.ArgumentNotNullOrEmpty(workspaceId, nameof(workspaceId));

			var datasetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (XmlQualityCondition xmlQualityCondition in referencedConditions)
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

					foreach (Dataset dataset in qualityCondition.GetDatasetParameterValues())
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

		[ContractAnnotation("text:null => null")]
		private static string Escape([CanBeNull] string text)
		{
			return XmlUtils.EscapeInvalidCharacters(text);
		}

		private static void Populate(
			[NotNull] XmlDataQualityDocument document,
			[NotNull] IList<QualitySpecification> qualitySpecifications,
			[NotNull] IEnumerable<TestDescriptor> testDescriptors,
			[NotNull] IEnumerable<DataQualityCategory> categories,
			bool exportMetadata,
			bool exportWorkspaceConnections,
			bool exportConnectionFilePaths,
			bool exportAllCategories,
			bool exportNotes)
		{
			Assert.ArgumentNotNull(document, nameof(document));
			Assert.ArgumentNotNull(qualitySpecifications, nameof(qualitySpecifications));
			Assert.ArgumentNotNull(testDescriptors, nameof(testDescriptors));
			Assert.ArgumentNotNull(categories, nameof(categories));

			IDictionary<Model, string> workspaceIdsByModel =
				AddWorkspaces(qualitySpecifications,
				              document,
				              exportWorkspaceConnections,
				              exportConnectionFilePaths);

			foreach (TestDescriptor testDescriptor in GetSorted(testDescriptors))
			{
				document.AddTestDescriptor(CreateXmlTestDescriptor(testDescriptor,
					                           exportMetadata));
			}

			IEnumerable<QualityCondition> qualityConditions =
				GetQualityConditions(qualitySpecifications);

			AddCategories(document, categories,
			              c => qualityConditions.Where(qc => Equals(qc.Category, c))
			                                    .ToList(),
			              c => qualitySpecifications.Where(qs => Equals(qs.Category, c))
			                                        .ToList(),
			              workspaceIdsByModel,
			              exportMetadata,
			              exportAllCategories,
			              exportNotes);

			// TODO: add any test descriptors that are referenced in the quality specifications but were not passed in
			// TODO: allow the test descriptors collection to be empty (or even null?)

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
		private static IEnumerable<QualityCondition> GetQualityConditions(
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

			foreach (DataQualityCategory category in GetSorted(categories.Where(
				                                                   c => c.ParentCategory == null))
			)
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

			foreach (QualityCondition condition in GetSorted(getQualityConditions(category)))
			{
				result.AddQualityCondition(CreateXmlQualityCondition(condition,
					                           workspaceIdsByModel,
					                           exportMetadata,
					                           exportNotes));
			}

			foreach (
				QualitySpecification specification in
				GetSorted(getQualitySpecifications(category)))
			{
				result.AddQualitySpecification(CreateXmlQualitySpecification(specification,
					                               exportMetadata,
					                               exportNotes));
			}

			return result;
		}

		[NotNull]
		private static IEnumerable<QualitySpecification> GetSorted(
			[NotNull] IEnumerable<QualitySpecification> qualitySpecifications)
		{
			return qualitySpecifications.OrderBy(
				qs => string.Format("{0}#{1}", qs.ListOrder, qs.Name));
		}

		[NotNull]
		private static IEnumerable<QualityCondition> GetSorted(
			[NotNull] IEnumerable<QualityCondition> qualityConditions)
		{
			return qualityConditions.OrderBy(qc => qc.Name);
		}

		[NotNull]
		private static IEnumerable<DataQualityCategory> GetSorted(
			[NotNull] IEnumerable<DataQualityCategory> categories)
		{
			return categories.OrderBy((c => string.Format("{0}#{1}", c.ListOrder, c.Name)));
		}

		[NotNull]
		private static IEnumerable<TestDescriptor> GetSorted(
			[NotNull] IEnumerable<TestDescriptor> testDescriptors)
		{
			Assert.ArgumentNotNull(testDescriptors, nameof(testDescriptors));

			return testDescriptors.OrderBy(t => t.Name);
		}

		[NotNull]
		private static XmlTestParameterValue CreateXmlTestParameterValue(
			[NotNull] ScalarTestParameterValue scValue)
		{
			return new XmlScalarTestParameterValue
			       {
				       TestParameterName = scValue.TestParameterName,
				       Value = scValue.StringValue
			       };
		}

		private static void ImportMetadata([NotNull] IEntityMetadata entity,
		                                   [NotNull] IXmlEntityMetadata xml)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));
			Assert.ArgumentNotNull(xml, nameof(xml));

			// created

			if (! string.IsNullOrEmpty(xml.CreatedByUser))
			{
				entity.CreatedByUser = xml.CreatedByUser;
			}

			DateTime? xmlCreatedDate = ParseDateTime(xml.CreatedDate);

			if (xmlCreatedDate != null &&
			    ! AreEqual(entity.CreatedDate, xmlCreatedDate))
			{
				entity.CreatedDate = xmlCreatedDate;
			}

			// last changed 

			if (! string.IsNullOrEmpty(xml.LastChangedByUser))
			{
				entity.LastChangedByUser = xml.LastChangedByUser;
			}

			DateTime? xmlLastChangedDate = ParseDateTime(xml.LastChangedDate);
			if (xmlLastChangedDate != null &&
			    ! AreEqual(entity.LastChangedDate, xmlLastChangedDate))
			{
				entity.LastChangedDate = xmlLastChangedDate;
			}
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

		[NotNull]
		private static ClassDescriptor CreateClassDescriptor(
			[NotNull] XmlClassDescriptor xmlClassDescriptor)
		{
			Assert.ArgumentNotNull(xmlClassDescriptor, nameof(xmlClassDescriptor));

			return new ClassDescriptor(xmlClassDescriptor.TypeName,
			                           xmlClassDescriptor.AssemblyName,
			                           xmlClassDescriptor.Description);
		}

		private static void AddQualitySpecificationElement(
			[NotNull] QualitySpecification qualitySpecification,
			[NotNull] IDictionary<string, QualityCondition> qualityConditions,
			[NotNull] XmlQualitySpecificationElement xmlElement,
			bool ignoreMissingConditions)
		{
			string conditionName = xmlElement.QualityConditionName;

			QualityCondition qualityCondition;
			if (! qualityConditions.TryGetValue(conditionName, out qualityCondition))
			{
				if (ignoreMissingConditions)
				{
					return;
				}

				Assert.Fail("The quality condition reference '{0}' defined in import document " +
				            "is based on an unknown quality condition.", conditionName);
			}

			bool? stopOnError = TranslateOverride(xmlElement.StopOnError);
			bool? allowErrors = TranslateOverride(xmlElement.AllowErrors);

			qualitySpecification.AddElement(qualityCondition, stopOnError, allowErrors);
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

		[NotNull]
		private static TestParameterValue CreateScalarTestParameterValue(
			[NotNull] TestParameter testParameter,
			[NotNull] XmlScalarTestParameterValue xmlScalarTestParameterValue)
		{
			return new ScalarTestParameterValue(testParameter, xmlScalarTestParameterValue.Value);
		}

		[CanBeNull]
		private static TestParameterValue CreateDatasetTestParameterValue(
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
			return new DatasetTestParameterValue(testParameter, dataset,
			                                     xmlDatasetTestParameterValue.WhereClause,
			                                     xmlDatasetTestParameterValue.UsedAsReferenceData);
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
		private static XmlTestDescriptor CreateXmlTestDescriptor(
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
		private static XmlQualityCondition CreateXmlQualityCondition(
			[NotNull] QualityCondition qualityCondition,
			[NotNull] IDictionary<Model, string> workspaceIdsByModel,
			bool exportMetadata,
			bool exportNotes)
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
						qualityCondition.NeverStoreRelatedGeometryForTableRowIssues
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

		[NotNull]
		private static XmlTestParameterValue CreateXmlTestParameterValue(
			[NotNull] TestParameterValue parameterValue,
			[NotNull] QualityCondition qualityCondition,
			[NotNull] IDictionary<Model, string> workspaceIdsByModel)
		{
			Assert.ArgumentNotNull(parameterValue, nameof(parameterValue));
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));
			Assert.ArgumentNotNull(workspaceIdsByModel, nameof(workspaceIdsByModel));

			var scValue = parameterValue as ScalarTestParameterValue;
			if (scValue != null)
			{
				return CreateXmlTestParameterValue(scValue);
			}

			var dsValue = parameterValue as DatasetTestParameterValue;
			if (dsValue != null)
			{
				return CreateXmlTestParameterValue(dsValue, qualityCondition, workspaceIdsByModel);
			}

			throw new InvalidConfigurationException(
				string.Format(
					"Parameter {0} in quality condition {1} has an unhandled type {2}",
					parameterValue.TestParameterName, qualityCondition.Name,
					parameterValue.GetType()));
		}

		[NotNull]
		private static XmlTestParameterValue CreateXmlTestParameterValue(
			[NotNull] DatasetTestParameterValue datasetTestParameterValue,
			[NotNull] QualityCondition qualityCondition,
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
	}
}
