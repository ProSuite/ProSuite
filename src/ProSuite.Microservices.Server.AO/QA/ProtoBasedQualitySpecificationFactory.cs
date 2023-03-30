using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Xml;
using ProSuite.DomainServices.AO.QA.Standalone;
using ProSuite.DomainServices.AO.QA.Standalone.XmlBased;
using ProSuite.DomainServices.AO.QA.VerifiedDataModel;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.QA.Core;

namespace ProSuite.Microservices.Server.AO.QA
{
	public class ProtoBasedQualitySpecificationFactory
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly ISupportedInstanceDescriptors _instanceDescriptors;

		/// <summary>
		/// QualitySpecification factory that uses a fine-grained proto buf message as input.
		/// </summary>
		/// <param name="modelFactory">The model factory</param>
		/// <param name="instanceDescriptors">All supported instance configurations</param>
		public ProtoBasedQualitySpecificationFactory(
			[NotNull] IVerifiedModelFactory modelFactory,
			[NotNull] ISupportedInstanceDescriptors instanceDescriptors)
		{
			ModelFactory = modelFactory;

			_instanceDescriptors = instanceDescriptors;
		}

		/// <summary>
		/// QualitySpecification factory that uses a fine-grained proto buf message as input.
		/// </summary>
		/// <param name="modelsByWorkspaceId">The known models by workspace Id</param>
		/// <param name="instanceDescriptors">All supported instance configurations</param>
		public ProtoBasedQualitySpecificationFactory(
			[NotNull] IDictionary<string, DdxModel> modelsByWorkspaceId,
			[NotNull] ISupportedInstanceDescriptors instanceDescriptors)
		{
			ModelsByWorkspaceId = modelsByWorkspaceId;

			_instanceDescriptors = instanceDescriptors;
		}

		private IVerifiedModelFactory ModelFactory { get; }

		private IDictionary<string, DdxModel> ModelsByWorkspaceId { get; set; }

		[NotNull]
		public QualitySpecification CreateQualitySpecification(
			[NotNull] ConditionListSpecificationMsg conditionListSpecificationMsg,
			[NotNull] IEnumerable<DataSource> dataSources)
		{
			CultureInfo origCulture = Thread.CurrentThread.CurrentCulture;
			try
			{
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

				return CreateQualitySpecificationCore(
					conditionListSpecificationMsg, dataSources);
			}
			finally
			{
				Thread.CurrentThread.CurrentCulture = origCulture;
			}
		}

		private QualitySpecification CreateQualitySpecificationCore(
			[NotNull] ConditionListSpecificationMsg conditionListSpecificationMsg,
			[NotNull] IEnumerable<DataSource> dataSources)
		{
			List<QualityConditionMsg> qualityConditionMsgs =
				conditionListSpecificationMsg.Elements.Select(e => e.Condition).ToList();

			// Prepare models (if stand-alone, the models must be harvested):
			if (ModelsByWorkspaceId == null)
			{
				ModelsByWorkspaceId = GetModelsByWorkspaceId(dataSources, qualityConditionMsgs);
			}

			const bool ignoreConditionsForUnknownDatasets = true;
			Dictionary<string, QualityCondition> qualityConditions =
				CreateQualityConditions(qualityConditionMsgs, ignoreConditionsForUnknownDatasets);

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

		[NotNull]
		private Dictionary<string, QualityCondition> CreateQualityConditions(
			[NotNull] IList<QualityConditionMsg> conditionMessages,
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
					StandaloneVerificationUtils.HandleNoConditionCreated(
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

		#region Instance configurations

		[CanBeNull]
		private QualityCondition CreateQualityCondition(
			[NotNull] QualityConditionMsg conditionMsg,
			[NotNull] DatasetSettings datasetSettings)
		{
			TestDescriptor testDescriptor = GetTestDescriptor(conditionMsg);

			var result = new QualityCondition(conditionMsg.Name, testDescriptor);
			result.SetCloneId(conditionMsg.ConditionId);

			AddIssueFilters(result, conditionMsg, datasetSettings);

			// The result will be set to null, if there are missing datasets:
			result = ConfigureParameters(result, conditionMsg.Parameters, datasetSettings);

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

			InstanceFactory instanceFactory =
				Assert.NotNull(InstanceFactoryUtils.CreateFactory(created));

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

				TestParameterValue parameterValue =
					TestParameterTypeUtils.GetEmptyParameterValue(testParameter);

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
					parameterValue.StringValue = parameterMsg.Value;
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

			TestParameterTypeUtils.AssertValidDataset(testParameter, dataset);

			return dataset;
		}

		#endregion

		[NotNull]
		private IDictionary<string, DdxModel> GetModelsByWorkspaceId(
			[NotNull] IEnumerable<DataSource> allDataSources,
			[NotNull] IList<QualityConditionMsg> referencedConditions)
		{
			var result = new Dictionary<string, DdxModel>(StringComparer.OrdinalIgnoreCase);

			foreach (DataSource dataSource in allDataSources)
			{
				Assert.True(int.TryParse(dataSource.ID, out int modelId),
				            $"Invalid datasource id: {dataSource.ID}");

				result.Add(dataSource.ID,
				           CreateModel(dataSource.OpenWorkspace(),
				                       dataSource.DisplayName,
				                       modelId,
				                       dataSource.DatabaseName,
				                       dataSource.SchemaOwner,
				                       referencedConditions));
			}

			return result;
		}

		[NotNull]
		private DdxModel CreateModel(
			[NotNull] IWorkspace workspace,
			[NotNull] string modelName,
			int workspaceId,
			[CanBeNull] string databaseName,
			[CanBeNull] string schemaOwner,
			[NotNull] IEnumerable<QualityConditionMsg> referencedConditions)
		{
			Model result = ModelFactory.CreateModel(workspace, modelName, workspaceId,
			                                        databaseName, schemaOwner);

			if (result.SpatialReferenceDescriptor == null)
			{
				IEnumerable<Dataset> referencedDatasets = GetReferencedDatasets(
					result, workspaceId.ToString(CultureInfo.InvariantCulture),
					referencedConditions);

				ModelFactory.AssignMostFrequentlyUsedSpatialReference(result, referencedDatasets);
			}

			return result;
		}

		[NotNull]
		private static IEnumerable<Dataset> GetReferencedDatasets(
			[NotNull] DdxModel model,
			[NotNull] string workspaceId,
			[NotNull] IEnumerable<QualityConditionMsg> referencedConditions)
		{
			Assert.ArgumentNotNull(model, nameof(model));
			Assert.ArgumentNotNull(referencedConditions, nameof(referencedConditions));
			Assert.ArgumentNotNullOrEmpty(workspaceId, nameof(workspaceId));

			var datasetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (QualityConditionMsg conditionMsg in referencedConditions)
			{
				foreach (ParameterMsg parameterMsg in conditionMsg.Parameters)
				{
					if (string.IsNullOrEmpty(parameterMsg.WorkspaceId))
					{
						// No dataset parameter
						continue;
					}

					if (! string.Equals(parameterMsg.WorkspaceId, workspaceId,
					                    StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}

					string datasetName = parameterMsg.Value;
					if (datasetName == null || datasetNames.Contains(datasetName))
					{
						continue;
					}

					datasetNames.Add(datasetName);

					Dataset dataset =
						XmlDataQualityUtils.GetDatasetByParameterValue(model, datasetName);

					if (dataset != null)
					{
						yield return dataset;
					}
				}
			}
		}

		#region Instance descriptors

		[NotNull]
		private TestDescriptor GetTestDescriptor(
			[NotNull] QualityConditionMsg conditionMsg)
		{
			string testDescriptorName = conditionMsg.TestDescriptorName;
			Assert.True(StringUtils.IsNotEmpty(testDescriptorName),
			            $"Test descriptor name is missing in condition: {conditionMsg}");

			string trimmedName = testDescriptorName.Trim();
			TestDescriptor testDescriptor = _instanceDescriptors.GetTestDescriptor(trimmedName);

			Assert.NotNull(testDescriptor,
			               "Test descriptor '{0}' referenced in quality condition '{1}' does not exist",
			               testDescriptorName, conditionMsg.Name);

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
			T instanceDescriptor = _instanceDescriptors.GetInstanceDescriptor<T>(trimmedName);

			Assert.NotNull(instanceDescriptor,
			               "Instance descriptor '{0}' referenced in '{1}' does not exist or is not supported",
			               instanceDescriptorName, instanceConfigMsg.Name);

			return instanceDescriptor;
		}

		#endregion
	}
}
