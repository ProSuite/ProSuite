using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.Microservices.Definitions.QA;

namespace ProSuite.Microservices.Client.QA
{
	public static class ProtoDataQualityUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private static int _currentModelId = -100;

		/// <summary>
		/// Creates a specification message containing the the protobuf based conditions.
		/// </summary>
		/// <param name="specification"></param>
		/// <param name="supportedInstanceDescriptors">If the optional supported instance descriptors
		/// are specified, the instance descriptor names are checked if they can be resolved and,
		/// potentially translated to the canonical name.</param>
		/// <param name="usedModelsById">The data models that are referenced by the specification.
		/// In case the stand-alone verification is used, make sure to augment the result's DataSourceMsg
		/// list with the respective connection information.</param>
		/// <returns></returns>
		public static ConditionListSpecificationMsg CreateConditionListSpecificationMsg(
			[NotNull] QualitySpecification specification,
			[CanBeNull] ISupportedInstanceDescriptors supportedInstanceDescriptors,
			out IDictionary<int, DdxModel> usedModelsById)
		{
			var result = new ConditionListSpecificationMsg
			             {
				             Name = specification.Name,
				             Description = specification.Description ?? string.Empty
			             };

			// The datasource ID will correspond with the model id. The model id must not be -1
			// (the non-persistent value) to avoid two non-persistent but different models with the
			// same id.
			usedModelsById = new Dictionary<int, DdxModel>();

			// The parameters must be initialized!
			InstanceConfigurationUtils.InitializeParameterValues(specification);

			foreach (QualitySpecificationElement element in specification.Elements)
			{
				if (! element.Enabled)
				{
					continue;
				}

				QualityCondition condition = element.QualityCondition;

				string categoryName = condition.Category?.Name ?? string.Empty;

				string descriptorName =
					GetDescriptorName(condition.TestDescriptor, supportedInstanceDescriptors);

				var elementMsg = new QualitySpecificationElementMsg
				                 {
					                 AllowErrors = element.AllowErrors,
					                 StopOnError = element.StopOnError,
					                 CategoryName = categoryName
				                 };

				var conditionMsg = new QualityConditionMsg
				                   {
					                   ConditionId = condition.Id,
					                   TestDescriptorName = descriptorName,
					                   Name = Assert.NotNull(
						                   condition.Name, "Condition name is null"),
					                   Description = condition.Description ?? string.Empty,
					                   Url = condition.Url ?? string.Empty,
					                   IssueFilterExpression =
						                   condition.IssueFilterExpression ?? string.Empty
				                   };

				AddParameterMessages(condition.ParameterValues, conditionMsg.Parameters,
				                     supportedInstanceDescriptors, usedModelsById);

				foreach (IssueFilterConfiguration filterConfiguration in condition
					         .IssueFilterConfigurations)
				{
					conditionMsg.ConditionIssueFilters.Add(
						CreateInstanceConfigMsg<IssueFilterDescriptor>(
							filterConfiguration, supportedInstanceDescriptors, usedModelsById));
				}

				elementMsg.Condition = conditionMsg;

				result.Elements.Add(elementMsg);
			}

			// NOTE: The added data sources list does not contain connection information.
			//       The caller must assign the catalog path or connection props, if necessary.

			result.DataSources.AddRange(
				usedModelsById.Select(
					kvp => new DataSourceMsg
					       {
						       Id = kvp.Key.ToString(CultureInfo.InvariantCulture),
						       ModelName = kvp.Value.Name
					       }));

			return result;
		}

		private static string GetDescriptorName<T>(
			[NotNull] T instanceDescriptor,
			[CanBeNull] ISupportedInstanceDescriptors supportedInstanceDescriptors)
			where T : InstanceDescriptor
		{
			if (supportedInstanceDescriptors == null)
			{
				// Cannot check. Let's hope the server knows it.
				return instanceDescriptor.Name;
			}

			string descriptorName = instanceDescriptor.Name;

			if (supportedInstanceDescriptors.GetInstanceDescriptor<T>(descriptorName) != null)
			{
				return descriptorName;
			}

			// The instance descriptor name is not known. Try the canonical name:
			string canonicalName = instanceDescriptor.GetCanonicalName();

			// TODO: Automatically add the canonical name as fall-back
			if (supportedInstanceDescriptors.GetInstanceDescriptor<T>(canonicalName) != null)
			{
				return canonicalName;
			}

			// Fall-back: Use AsssemblyQualified type name with constructor
			if (InstanceDescriptorUtils.TryExtractClassInfo(descriptorName, out _, out _))
			{
				// It's in the fully qualified form, good to go
				return descriptorName;
			}

			// It will probably fail on the server, unless it's supported there...
			_msg.DebugFormat(
				"Descriptor name {0} could not be resolved. Let's hope it can be resolved on the server!",
				descriptorName);

			return descriptorName;
		}

		private static void AddParameterMessages(
			[NotNull] IEnumerable<TestParameterValue> parameterValues,
			[NotNull] ICollection<ParameterMsg> parameterMsgs,
			[CanBeNull] ISupportedInstanceDescriptors supportedInstanceDescriptors,
			IDictionary<int, DdxModel> usedModelsById)
		{
			foreach (TestParameterValue parameterValue in parameterValues)
			{
				ParameterMsg parameterMsg = new ParameterMsg
				                            {
					                            Name = parameterValue.TestParameterName
				                            };

				if (parameterValue is DatasetTestParameterValue datasetParamValue)
				{
					if (datasetParamValue.DatasetValue != null)
					{
						parameterMsg.Value = datasetParamValue.DatasetValue.Name;

						DdxModel model = datasetParamValue.DatasetValue.Model;

						int modelId = -1;
						if (model.Id == -1)
						{
							// Find by reference
							foreach (var kvp in usedModelsById)
							{
								if (kvp.Value != model)
								{
									continue;
								}

								modelId = kvp.Key;
								break;
							}

							if (modelId == -1)
							{
								modelId = _currentModelId--;
							}
						}
						else
						{
							modelId = model.Id;
						}

						// NOTE: Fake values (negative, but not -1) are allowed. But they must be unique per model!
						Assert.False(modelId == -1,
						             "Invalid model id (not persistent and no CloneId has been set)");

						if (! usedModelsById.ContainsKey(modelId))
						{
							usedModelsById.Add(modelId, model);
						}

						parameterMsg.WorkspaceId = modelId.ToString(CultureInfo.InvariantCulture);
					}

					if (datasetParamValue.ValueSource != null)
					{
						TransformerConfiguration transformerConfiguration =
							Assert.NotNull(datasetParamValue.ValueSource);

						parameterMsg.Transformer =
							CreateInstanceConfigMsg<TransformerDescriptor>(
								transformerConfiguration, supportedInstanceDescriptors,
								usedModelsById);
					}

					parameterMsg.WhereClause = datasetParamValue.FilterExpression ?? string.Empty;
					parameterMsg.UsedAsReferenceData = datasetParamValue.UsedAsReferenceData;
				}
				else
				{
					parameterMsg.Value = parameterValue.StringValue;
				}

				parameterMsgs.Add(parameterMsg);
			}
		}

		private static InstanceConfigurationMsg CreateInstanceConfigMsg<T>(
			[NotNull] InstanceConfiguration instanceConfiguration,
			[CanBeNull] ISupportedInstanceDescriptors supportedInstanceDescriptors,
			[NotNull] IDictionary<int, DdxModel> usedModelsById)
			where T : InstanceDescriptor
		{
			var result = new InstanceConfigurationMsg
			             {
				             Id = instanceConfiguration.Id,
				             Name = instanceConfiguration.Name,
				             Url = instanceConfiguration.Url ?? string.Empty,
				             Description = instanceConfiguration.Description ?? string.Empty
			             };

			string descriptorName =
				GetDescriptorName((T) instanceConfiguration.InstanceDescriptor,
				                  supportedInstanceDescriptors);

			result.InstanceDescriptorName = descriptorName;

			AddParameterMessages(instanceConfiguration.ParameterValues, result.Parameters,
			                     supportedInstanceDescriptors, usedModelsById);

			return result;
		}
	}
}
