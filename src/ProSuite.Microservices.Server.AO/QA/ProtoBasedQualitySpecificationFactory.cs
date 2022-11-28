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
using ProSuite.Commons.Text;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.AO.QA.Xml;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA.Standalone;
using ProSuite.DomainServices.AO.QA.Standalone.XmlBased;
using ProSuite.DomainServices.AO.QA.VerifiedDataModel;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.QA.Container;
using ProSuite.QA.Core;

namespace ProSuite.Microservices.Server.AO.QA
{
	public class ProtoBasedQualitySpecificationFactory : QualitySpecificationFactoryBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly ISupportedInstanceDescriptors _instanceDescriptors;

		/// <summary>
		/// QualitySpecification factory that uses a fine-grained proto buf message as input.
		/// </summary>
		/// <param name="modelFactory">The model factory</param>
		/// <param name="instanceDescriptors">All supported instance configurations</param>
		/// <param name="datasetOpener"></param>
		public ProtoBasedQualitySpecificationFactory(
			[NotNull] IVerifiedModelFactory modelFactory,
			ISupportedInstanceDescriptors instanceDescriptors,
			[NotNull] IOpenDataset datasetOpener) : base(modelFactory, datasetOpener)
		{
			_instanceDescriptors = instanceDescriptors;
		}

		private IDictionary<string, Model> ModelsByWorkspaceId { get; set; }

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

			// Prepare models:
			ModelsByWorkspaceId = GetModelsByWorkspaceId(dataSources, qualityConditionMsgs);

			const bool ignoreConditionsForUnknownDatasets = true;
			Dictionary<string, QualityCondition> qualityConditions =
				CreateQualityConditions(qualityConditionMsgs, ignoreConditionsForUnknownDatasets);

			var result = new QualitySpecification(conditionListSpecificationMsg.Name)
			             {
				             Description = conditionListSpecificationMsg.Description
			             };

			AddElements(result, qualityConditions,
			            conditionListSpecificationMsg.Elements);

			// TODO: TileSize, Url, Notes?

			return result;
		}

		[NotNull]
		private static void AddElements(
			QualitySpecification toQualitySpecification,
			[NotNull] IDictionary<string, QualityCondition> qualityConditionsByName,
			[NotNull] IEnumerable<QualitySpecificationElementMsg> specificationElementMsgs)
		{
			Assert.ArgumentNotNull(qualityConditionsByName, nameof(qualityConditionsByName));

			foreach (QualitySpecificationElementMsg element in specificationElementMsgs)
			{
				string conditionName = Assert.NotNullOrEmpty(
					element.Condition.Name, "Empty or null condition name.");

				QualityCondition qualityCondition = qualityConditionsByName[conditionName];

				toQualitySpecification.AddElement(qualityCondition, element.StopOnError,
				                                  element.AllowErrors);
			}
		}

		private static IDictionary<string, TestDescriptor> GetReferencedTestDescriptorsByName(
			[NotNull] IEnumerable<QualityConditionMsg> referencedConditions,
			[NotNull] IDictionary<string, XmlTestDescriptor> xmlTestDescriptorsByName)
		{
			var result = new Dictionary<string, TestDescriptor>(
				StringComparer.OrdinalIgnoreCase);

			foreach (QualityConditionMsg condition in referencedConditions)
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

		private Dictionary<string, QualityCondition> CreateQualityConditions(
			[NotNull] IList<QualityConditionMsg> conditionMessages,
			bool ignoreConditionsForUnknownDatasets)
		{
			var qualityConditions = new Dictionary<string, QualityCondition>(
				StringComparer.OrdinalIgnoreCase);

			Func<string, IList<Dataset>> getDatasetsByName = name => new List<Dataset>();

			foreach (QualityConditionMsg conditionMsg in conditionMessages)
			{
				TestDescriptor testDescriptor =
					GetTestDescriptor(conditionMsg);

				DatasetSettings datasetSettings =
					new DatasetSettings(getDatasetsByName, ignoreConditionsForUnknownDatasets);

				QualityCondition qualityCondition =
					CreateQualityCondition(conditionMsg, testDescriptor,
					                       datasetSettings);

				if (qualityCondition == null)
				{
					HandleNoConditionCreated(conditionMsg.Name, ModelsByWorkspaceId,
					                         ignoreConditionsForUnknownDatasets,
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

		private QualityCondition CreateQualityCondition(
			[NotNull] QualityConditionMsg conditionMsg,
			[NotNull] TestDescriptor testDescriptor,
			DatasetSettings datasetSettings)
		{
			var result = new QualityCondition(conditionMsg.Name, testDescriptor);

			AddIssueFilters(result, conditionMsg, datasetSettings);

			// The result will be set to null, if there are missing datasets:
			result = ConfigureParameters(result, conditionMsg.Parameters, datasetSettings);

			return result;
		}

		private TransformerConfiguration CreateTransformerConfiguration(
			[NotNull] InstanceConfigurationMsg transformerConfigurationMsg,
			[NotNull] DatasetSettings datasetSettings)
		{
			TransformerDescriptor transformerDescriptor =
				GetTransformerDescriptor(transformerConfigurationMsg);

			var result =
				new TransformerConfiguration(transformerConfigurationMsg.Name,
				                             transformerDescriptor);

			// The result will be set to null, if there are missing datasets:
			result = ConfigureParameters(result, transformerConfigurationMsg.Parameters,
			                             datasetSettings);

			return result;
		}

		private void AddIssueFilters(
			[NotNull] QualityCondition qualityCondition,
			[NotNull] QualityConditionMsg conditionMsg,
			[NotNull] DatasetSettings datasetSettings)
		{
			string issueFilterExpression = conditionMsg.IssueFilterExpression;

			if (string.IsNullOrWhiteSpace(issueFilterExpression))
			{
				return;
			}

			foreach (InstanceConfigurationMsg issueFilterMsg in conditionMsg.ConditionIssueFilters)
			{
				IssueFilterConfiguration issueFilterConfiguration =
					CreateIssueFilterConfiguration(issueFilterMsg, datasetSettings);

				qualityCondition.AddIssueFilterConfiguration(issueFilterConfiguration);
			}

			// Validation (move to somewhere else?)
			IList<string> issueFilterNames =
				FilterUtils.GetFilterNames(issueFilterExpression);

			foreach (string issueFilterName in issueFilterNames)
			{
				string expressionName = issueFilterName.Trim();

				if (! conditionMsg.ConditionIssueFilters.Any(f => f.Name.Equals(expressionName)))
				{
					Assert.Fail($"missing issue filter named {expressionName}");
				}
			}

			qualityCondition.IssueFilterExpression = issueFilterExpression;
		}

		private IssueFilterConfiguration CreateIssueFilterConfiguration(
			[NotNull] InstanceConfigurationMsg issueFilterConfigurationMsg,
			[NotNull] DatasetSettings datasetSettings)
		{
			IssueFilterDescriptor issueFilterDescriptor =
				GetIssueFilterDescriptor(issueFilterConfigurationMsg);

			var result =
				new IssueFilterConfiguration(issueFilterConfigurationMsg.Name,
				                             issueFilterDescriptor);

			// The result will be set to null, if there are missing datasets:
			result = ConfigureParameters(result, issueFilterConfigurationMsg.Parameters,
			                             datasetSettings);

			return result;
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
						"configuration message does not match instance descriptor descriptor.");
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

			// TODO: Handle missing datasets in transformersformers and issue filters!
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

		private Dataset GetDataset([NotNull] InstanceConfiguration createdConfiguration,
		                           [NotNull] ParameterMsg parameterMsg,
		                           [NotNull] TestParameter testParameter,
		                           [NotNull] DatasetSettings datasetSettings)
		{
			Dataset dataset = TestParameterValueUtils.GetDataset(
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
		private IDictionary<string, Model> GetModelsByWorkspaceId(
			[NotNull] IEnumerable<DataSource> allDataSources,
			[NotNull] IList<QualityConditionMsg> referencedConditions)
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
			[NotNull] IEnumerable<QualityConditionMsg> referencedConditions)
		{
			Model result = ModelFactory.CreateModel(workspace, modelName, null,
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

		[CanBeNull]
		private ISpatialReference GetMainSpatialReference(
			[NotNull] Model model,
			[NotNull] string workspaceId,
			[NotNull] IEnumerable<QualityConditionMsg> referencedConditions)
		{
			IEnumerable<Dataset> referencedDatasets = GetReferencedDatasets(
				model, workspaceId, referencedConditions);

			return GetMainSpatialReference(model, referencedDatasets);
		}

		[NotNull]
		private static IEnumerable<Dataset> GetReferencedDatasets(
			[NotNull] Model model,
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

		private TransformerDescriptor GetTransformerDescriptor(
			[NotNull] InstanceConfigurationMsg instanceConfigMsg)
		{
			TransformerDescriptor transformerDescriptor =
				GetInstanceDescriptor<TransformerDescriptor>(instanceConfigMsg);

			return transformerDescriptor;
		}

		private IssueFilterDescriptor GetIssueFilterDescriptor(
			[NotNull] InstanceConfigurationMsg instanceConfigMsg)
		{
			IssueFilterDescriptor instanceDescriptor =
				GetInstanceDescriptor<IssueFilterDescriptor>(instanceConfigMsg);

			if (instanceDescriptor is IssueFilterDescriptor issueFilterDescriptor)
			{
				return issueFilterDescriptor;
			}

			throw new AssertionException($"Instance descriptor {instanceDescriptor.Name} is " +
			                             "null or not of type IssueFilterDescriptor");
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
