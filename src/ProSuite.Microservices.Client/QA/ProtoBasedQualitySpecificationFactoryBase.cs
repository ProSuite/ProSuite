using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Xml;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.QA.Core;

namespace ProSuite.Microservices.Client.QA
{
	public abstract class ProtoBasedQualitySpecificationFactoryBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected readonly ISupportedInstanceDescriptors InstanceDescriptors;

		protected IDictionary<string, DdxModel> ModelsByWorkspaceId { get; set; }

		protected ProtoBasedQualitySpecificationFactoryBase(
			[NotNull] ISupportedInstanceDescriptors instanceDescriptors)
		{
			InstanceDescriptors = instanceDescriptors;
		}

		[NotNull]
		public QualitySpecification CreateQualitySpecification(
			[NotNull] ConditionListSpecificationMsg conditionListSpecificationMsg)
		{
			// Prepare models (if stand-alone, the models must be harvested):
			if (ModelsByWorkspaceId == null)
			{
				ModelsByWorkspaceId = GetModelsByWorkspaceId(conditionListSpecificationMsg);
			}

			CultureInfo origCulture = Thread.CurrentThread.CurrentCulture;
			try
			{
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

				return CreateQualitySpecificationCore(conditionListSpecificationMsg);
			}
			finally
			{
				Thread.CurrentThread.CurrentCulture = origCulture;
			}
		}

		protected abstract IDictionary<string, DdxModel> GetModelsByWorkspaceId(
			[NotNull] ConditionListSpecificationMsg conditionListSpecificationMsg);

		protected abstract IInstanceInfo CreateInstanceFactory<T>(T created)
			where T : InstanceConfiguration;

		protected abstract TestParameterValue CreateEmptyTestParameterValue<T>(
			TestParameter testParameter)
			where T : InstanceConfiguration;

		protected abstract void AssertValidDataset(TestParameter testParameter, Dataset dataset);

		private QualitySpecification CreateQualitySpecificationCore(
			ConditionListSpecificationMsg conditionListSpecificationMsg)
		{
			const bool ignoreConditionsForUnknownDatasets = true;
			Dictionary<string, QualityCondition> qualityConditions =
				CreateQualityConditions(
					conditionListSpecificationMsg.Elements.Select(e => e.Condition),
					ignoreConditionsForUnknownDatasets);

			var result = new QualitySpecification(conditionListSpecificationMsg.Name)
			             {
				             Description = conditionListSpecificationMsg.Description
			             };

			AddElements(result, qualityConditions,
			            conditionListSpecificationMsg.Elements);

			// TODO: TileSize, Url, Category, Notes? They are not used by the verification.
			// The IsCustom property is not used, SaveVerification is a separate explicitly set parameter)

			_msg.DebugFormat("Created specification from protos with {0} conditions.",
			                 result.Elements.Count);

			return result;
		}

		private static void AddElements(
			[NotNull] QualitySpecification toQualitySpecification,
			[NotNull] IDictionary<string, QualityCondition> qualityConditionsByName,
			[NotNull] IEnumerable<QualitySpecificationElementMsg> specificationElementMsgs)
		{
			Assert.ArgumentNotNull(qualityConditionsByName, nameof(qualityConditionsByName));

			var categoriesByName = new Dictionary<string, DataQualityCategory>();

			foreach (QualitySpecificationElementMsg element in specificationElementMsgs)
			{
				string conditionName = Assert.NotNullOrEmpty(
					element.Condition.Name, "Empty or null condition name.");

				QualityCondition qualityCondition = qualityConditionsByName[conditionName];

				if (! string.IsNullOrEmpty(element.CategoryName))
				{
					string categoryName = element.CategoryName;

					if (! categoriesByName.TryGetValue(categoryName,
					                                   out DataQualityCategory category))
					{
						category = new DataQualityCategory(categoryName);
						categoriesByName.Add(categoryName, category);
					}

					qualityCondition.Category = category;
				}

				toQualitySpecification.AddElement(qualityCondition, element.StopOnError,
				                                  element.AllowErrors);
			}
		}

		#region Instance configurations

		[NotNull]
		protected Dictionary<string, QualityCondition> CreateQualityConditions(
			[NotNull] IEnumerable<QualityConditionMsg> conditionMessages,
			bool ignoreConditionsForUnknownDatasets)
		{
			var qualityConditions = new Dictionary<string, QualityCondition>(
				StringComparer.OrdinalIgnoreCase);

			Func<string, IList<Dataset>> getDatasetsByName = name => new List<Dataset>();

			foreach (QualityConditionMsg conditionMsg in conditionMessages)
			{
				DatasetSettings datasetSettings =
					new DatasetSettings(getDatasetsByName, ignoreConditionsForUnknownDatasets);

				QualityCondition qualityCondition =
					CreateQualityCondition(conditionMsg, datasetSettings);

				if (qualityCondition == null)
				{
					InstanceConfigurationUtils.HandleNoConditionCreated(
						conditionMsg.Name, ModelsByWorkspaceId, ignoreConditionsForUnknownDatasets,
						datasetSettings.UnknownDatasetParameters);
				}
				else
				{
					qualityConditions.Add(qualityCondition.Name, qualityCondition);
				}
			}

			return qualityConditions;
		}

		[CanBeNull]
		private QualityCondition CreateQualityCondition(
			[NotNull] QualityConditionMsg conditionMsg,
			[NotNull] DatasetSettings datasetSettings)
		{
			TestDescriptor testDescriptor = GetTestDescriptor(conditionMsg);

			var result = new QualityCondition(conditionMsg.Name, testDescriptor,
			                                  conditionMsg.Description);
			result.Url = conditionMsg.Url;

			result.SetCloneId(conditionMsg.ConditionId);

			AddIssueFilters(result, conditionMsg, datasetSettings);

			// The result will be set to null, if there are missing datasets:
			try
			{
				result = ConfigureParameters(result, conditionMsg.Parameters, datasetSettings);
			}
			catch (Exception e)
			{
				_msg.Warn($"Error creating configuration for condition {conditionMsg.Name}", e);
				throw;
			}

			return result;
		}

		[CanBeNull]
		private TransformerConfiguration CreateTransformerConfiguration(
			[NotNull] InstanceConfigurationMsg transformerConfigurationMsg,
			[NotNull] DatasetSettings datasetSettings)
		{
			TransformerDescriptor transformerDescriptor =
				GetInstanceDescriptor<TransformerDescriptor>(transformerConfigurationMsg);

			var result = new TransformerConfiguration(transformerConfigurationMsg.Name,
			                                          transformerDescriptor);

			// The result will be set to null, if there are missing datasets:
			result = ConfigureParameters(result, transformerConfigurationMsg.Parameters,
			                             datasetSettings);

			return result;
		}

		[CanBeNull]
		private IssueFilterConfiguration CreateIssueFilterConfiguration(
			[NotNull] InstanceConfigurationMsg issueFilterConfigurationMsg,
			[NotNull] DatasetSettings datasetSettings)
		{
			IssueFilterDescriptor issueFilterDescriptor =
				GetInstanceDescriptor<IssueFilterDescriptor>(issueFilterConfigurationMsg);

			var result =
				new IssueFilterConfiguration(issueFilterConfigurationMsg.Name,
				                             issueFilterDescriptor);

			// The result will be set to null, if there are missing datasets:
			result = ConfigureParameters(result, issueFilterConfigurationMsg.Parameters,
			                             datasetSettings);

			return result;
		}

		private void AddIssueFilters(
			[NotNull] QualityCondition qualityCondition,
			[NotNull] QualityConditionMsg conditionMsg,
			[NotNull] DatasetSettings datasetSettings)
		{
			foreach (InstanceConfigurationMsg issueFilterMsg in conditionMsg.ConditionIssueFilters)
			{
				IssueFilterConfiguration issueFilterConfig =
					CreateIssueFilterConfiguration(issueFilterMsg, datasetSettings);

				// TODO: Allow for missing datasets! Add to datasetSettings as below in ConfigureDatasetParameterValue?
				if (issueFilterConfig == null)
				{
					Assert.Fail(
						$"missing issue filter {issueFilterMsg} for condition {conditionMsg}");
				}
				else
				{
					qualityCondition.AddIssueFilterConfiguration(issueFilterConfig);
				}
			}

			qualityCondition.IssueFilterExpression = conditionMsg.IssueFilterExpression;
		}

		#endregion

		#region Parameter setup

		[CanBeNull]
		private T ConfigureParameters<T>(
			[NotNull] T created,
			[NotNull] IEnumerable<ParameterMsg> parameterMessages,
			[NotNull] DatasetSettings datasetSettings)
			where T : InstanceConfiguration
		{
			if (created.ParameterValues.Count > 0)
			{
				return created;
			}

			IInstanceInfo instanceFactory = CreateInstanceFactory(created);

			Dictionary<string, TestParameter> testParametersByName =
				instanceFactory.Parameters.ToDictionary(
					parameter => parameter.Name, StringComparer.OrdinalIgnoreCase);

			foreach (ParameterMsg parameterMsg in parameterMessages)
			{
				TestParameter testParameter;
				if (! testParametersByName.TryGetValue(parameterMsg.Name, out testParameter))
				{
					throw new InvalidConfigurationException(
						$"The name '{parameterMsg.Name}' as a test parameter in '{created.Name}' as defined in the " +
						"configuration message does not match instance descriptor.");
				}

				TestParameterValue parameterValue = CreateEmptyTestParameterValue<T>(testParameter);

				if (parameterMsg.Transformer != null)
				{
					ConfigureTransformedDatasetParameterValue(
						parameterValue, parameterMsg, datasetSettings);
				}
				else if (! string.IsNullOrEmpty(parameterMsg.WorkspaceId))
				{
					if (! ConfigureDatasetParameterValue(created, parameterValue, testParameter,
					                                     parameterMsg, datasetSettings))
					{
						parameterValue = null;
					}
				}
				else
				{
					parameterValue.StringValue = ProtobufGeomUtils.EmptyToNull(parameterMsg.Value);
				}

				// Or better add with null-dataset?
				if (parameterValue != null)
				{
					created.AddParameterValue(parameterValue);
				}
			}

			// TODO: Handle missing datasets in transformers and issue filters!
			if (datasetSettings.UnknownDatasetParameters.Count > 0)
			{
				Assert.True(datasetSettings.IgnoreUnknownDatasets,
				            nameof(datasetSettings.IgnoreUnknownDatasets));

				return null;
			}

			return created;
		}

		private void ConfigureTransformedDatasetParameterValue(
			[NotNull] TestParameterValue parameterValue,
			[NotNull] ParameterMsg parameterMsg,
			[NotNull] DatasetSettings datasetSettings)
		{
			// Must be dataset parameter
			if (! (parameterValue is DatasetTestParameterValue datasetParameterValue))
			{
				throw new AssertionException($"Type of {parameterMsg.Name} and " +
				                             $"{parameterValue.TestParameterName} do not match");
			}

			TransformerConfiguration transformerConfig =
				CreateTransformerConfiguration(parameterMsg.Transformer, datasetSettings);

			// TODO: Allow for missing datasets! Add to datasetSettings as below in ConfigureDatasetParameterValue?
			if (transformerConfig == null)
			{
				Assert.Fail(
					$"missing transformer {parameterMsg.Transformer.Name} for parameter value {parameterMsg}");
			}

			datasetParameterValue.ValueSource = transformerConfig;
			datasetParameterValue.FilterExpression = parameterMsg.WhereClause;
			datasetParameterValue.UsedAsReferenceData = parameterMsg.UsedAsReferenceData;
		}

		private bool ConfigureDatasetParameterValue([NotNull] InstanceConfiguration instanceConfig,
		                                            [NotNull] TestParameterValue parameterValue,
		                                            [NotNull] TestParameter testParameter,
		                                            [NotNull] ParameterMsg parameterMsg,
		                                            [NotNull] DatasetSettings datasetSettings)
		{
			// Must be dataset parameter
			if (! (parameterValue is DatasetTestParameterValue datasetParameterValue))
			{
				throw new AssertionException($"Type of {parameterMsg.Name} and " +
				                             $"{parameterValue.TestParameterName} do not match");
			}

			Dataset datasetValue =
				GetDataset(instanceConfig, parameterMsg, testParameter, datasetSettings);

			datasetParameterValue.FilterExpression = parameterMsg.WhereClause;
			datasetParameterValue.UsedAsReferenceData = parameterMsg.UsedAsReferenceData;

			if (datasetValue == null)
			{
				datasetSettings.UnknownDatasetParameters.Add(
					new DatasetTestParameterRecord(parameterMsg.Value,
					                               parameterMsg.WorkspaceId));
			}
			else
			{
				datasetParameterValue.DatasetValue = datasetValue;
			}

			return datasetValue != null;
		}

		[CanBeNull]
		private Dataset GetDataset([NotNull] InstanceConfiguration createdConfiguration,
		                           [NotNull] ParameterMsg parameterMsg,
		                           [NotNull] TestParameter testParameter,
		                           [NotNull] DatasetSettings datasetSettings)
		{
			Dataset dataset = XmlDataQualityUtils.GetDataset(
				parameterMsg.Value, parameterMsg.WorkspaceId,
				testParameter, createdConfiguration.Name, ModelsByWorkspaceId,
				datasetSettings.GetDatasetsByName, datasetSettings.IgnoreUnknownDatasets);

			if (dataset == null)
			{
				if (! testParameter.IsConstructorParameter)
				{
					// Null is always allowed for non-constructor parameters
					return null;
				}

				// Exception must already be thrown in GetDataset()
				Assert.True(datasetSettings.IgnoreUnknownDatasets,
				            $"{createdConfiguration.Name}: No dataset found for " +
				            $"{parameterMsg.Value} and IgnoreUnknownDatasets is false.");

				return null;
			}

			AssertValidDataset(testParameter, dataset);

			return dataset;
		}

		#endregion

		[NotNull]
		private TestDescriptor GetTestDescriptor(
			[NotNull] QualityConditionMsg conditionMsg)
		{
			string descriptorName = conditionMsg.TestDescriptorName;
			Assert.True(StringUtils.IsNotEmpty(descriptorName),
			            $"Test descriptor name is missing in condition: {conditionMsg}");

			TestDescriptor testDescriptor = InstanceDescriptors.GetTestDescriptor(descriptorName);

			if (testDescriptor == null &&
			    InstanceDescriptorUtils.TryExtractClassInfo(descriptorName,
			                                                out Type type,
			                                                out int constructorIdx))
			{
				// Fallback (if fully qualified type)

				// NOTE: Keep the exact same descriptor name to allow sub-processes to extract
				//       the type as well.
				if (constructorIdx < 0)
				{
					// Test Factory
					testDescriptor =
						new TestDescriptor(descriptorName, new ClassDescriptor(type));
				}
				else
				{
					testDescriptor =
						new TestDescriptor(descriptorName, new ClassDescriptor(type),
						                   constructorIdx);
				}
			}

			Assert.NotNull(testDescriptor,
			               "Test descriptor '{0}' referenced in quality condition '{1}' is not " +
			               "part of the supported descriptors, nor was its name a valid Type.",
			               descriptorName, conditionMsg.Name);

			return testDescriptor;
		}

		[NotNull]
		private T GetInstanceDescriptor<T>(
			[NotNull] InstanceConfigurationMsg instanceConfigMsg) where T : InstanceDescriptor
		{
			string instanceDescriptorName = instanceConfigMsg.InstanceDescriptorName;
			Assert.True(StringUtils.IsNotEmpty(instanceDescriptorName),
			            $"Instance descriptor name is missing in configuration: {instanceConfigMsg}");

			string trimmedName = instanceDescriptorName.Trim();
			T instanceDescriptor = InstanceDescriptors.GetInstanceDescriptor<T>(trimmedName);

			Assert.NotNull(instanceDescriptor,
			               "Instance descriptor '{0}' referenced in '{1}' does not exist or is not supported",
			               instanceDescriptorName, instanceConfigMsg.Name);

			return instanceDescriptor;
		}
	}
}
