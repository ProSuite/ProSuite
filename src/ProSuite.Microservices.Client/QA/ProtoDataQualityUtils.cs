using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.Microservices.Definitions.QA;

namespace ProSuite.Microservices.Client.QA
{
	public static class ProtoDataQualityUtils
	{
		public static ConditionListSpecificationMsg CreateConditionListSpecificationMsg(
			[NotNull] CustomQualitySpecification customSpecification)
		{
			var result = new ConditionListSpecificationMsg
			             {
				             Name = customSpecification.Name,
				             Description = customSpecification.Description ?? string.Empty
			             };

			IDictionary<string, DdxModel> dataSources = new Dictionary<string, DdxModel>();

			// The parameters must be initialized!
			InstanceConfigurationUtils.InitializeParameterValues(customSpecification);

			foreach (QualitySpecificationElement element in customSpecification.Elements)
			{
				if (! element.Enabled)
				{
					continue;
				}

				QualityCondition condition = element.QualityCondition;

				string categoryName = condition.Category?.Name ?? string.Empty;
				string descriptorName = Assert.NotNullOrEmpty(condition.InstanceDescriptor.Name);

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
				                     dataSources);

				foreach (IssueFilterConfiguration filterConfiguration in condition
					         .IssueFilterConfigurations)
				{
					conditionMsg.ConditionIssueFilters.Add(
						CreateInstanceConfigMsg(filterConfiguration, dataSources));
				}

				elementMsg.Condition = conditionMsg;

				result.Elements.Add(elementMsg);
			}

			result.DataSources.AddRange(
				dataSources.Select(kvp => new DataSourceMsg
				                          { Id = kvp.Key, ModelName = kvp.Value.Name }));

			return result;
		}

		private static void AddParameterMessages(
			[NotNull] IEnumerable<TestParameterValue> parameterValues,
			[NotNull] ICollection<ParameterMsg> parameterMsgs,
			IDictionary<string, DdxModel> dataSources)
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
						string wsId = model.Id.ToString();
						if (! dataSources.ContainsKey(wsId))
						{
							dataSources.Add(wsId, model);
						}

						parameterMsg.WorkspaceId = wsId;
					}

					if (datasetParamValue.ValueSource != null)
					{
						TransformerConfiguration transformerConfiguration =
							Assert.NotNull(datasetParamValue.ValueSource);

						parameterMsg.Transformer =
							CreateInstanceConfigMsg(transformerConfiguration, dataSources);
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

		private static InstanceConfigurationMsg CreateInstanceConfigMsg(
			[NotNull] InstanceConfiguration instanceConfiguration,
			[NotNull] IDictionary<string, DdxModel> dataSources)
		{
			var result = new InstanceConfigurationMsg
			             {
				             ConditionId = instanceConfiguration.Id,
				             Name = instanceConfiguration.Name,
				             Url = instanceConfiguration.Url ?? string.Empty,
				             Description = instanceConfiguration.Description ?? string.Empty
			             };

			result.InstanceDescriptorName = instanceConfiguration.InstanceDescriptor.Name;

			AddParameterMessages(instanceConfiguration.ParameterValues, result.Parameters,
			                     dataSources);

			return result;
		}
	}
}
