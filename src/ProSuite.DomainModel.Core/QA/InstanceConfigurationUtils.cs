using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.Core.QA
{
	public static class InstanceConfigurationUtils
	{
		public static TestParameterValue AddParameterValue(
			[NotNull] InstanceConfiguration instanceConfiguration,
			[NotNull] string parameterName,
			[CanBeNull] Dataset value,
			string filterExpression = null,
			bool usedAsReferenceData = false)
		{
			Assert.ArgumentNotNullOrEmpty(parameterName, nameof(parameterName));

			IInstanceInfo instanceInfo =
				InstanceDescriptorUtils.GetInstanceInfo(instanceConfiguration.InstanceDescriptor);

			TestParameter parameter = Assert.NotNull(instanceInfo).GetParameter(parameterName);

			var parameterValue = new DatasetTestParameterValue(parameter, value,
			                                                   filterExpression,
			                                                   usedAsReferenceData)
			                     {
				                     DataType = parameter.Type
			                     };

			return instanceConfiguration.AddParameterValue(parameterValue);
		}

		public static void AddParameterValue(IssueFilterConfiguration qualityCondition,
		                                     [NotNull] string parameterName,
		                                     [CanBeNull] string value)
		{
			AddScalarParameterValue(qualityCondition, parameterName, value);
		}

		public static void AddParameterValue(InstanceConfiguration instanceConfiguration,
		                                     [NotNull] string parameterName,
		                                     object value)
		{
			var dataset = value as Dataset;
			if (dataset != null)
			{
				AddParameterValue(instanceConfiguration, parameterName, dataset);
			}
			else
			{
				AddScalarParameterValue(instanceConfiguration, parameterName, value);
			}
		}

		public static TestParameterValue AddScalarParameterValue(
			InstanceConfiguration instanceConfiguration,
			[NotNull] string parameterName,
			[CanBeNull] object value)
		{
			Assert.ArgumentNotNullOrEmpty(parameterName, nameof(parameterName));

			IInstanceInfo instanceInfo =
				InstanceDescriptorUtils.GetInstanceInfo(instanceConfiguration.InstanceDescriptor);

			Assert.NotNull(instanceInfo, "Cannot create instance info for {0}",
			               instanceConfiguration);

			TestParameter parameter = instanceInfo.GetParameter(parameterName);

			if (! parameter.IsConstructorParameter && parameter.Type.IsValueType &&
			    (value == null || value as string == string.Empty))
			{
				return null;
			}

			var parameterValue = new ScalarTestParameterValue(parameter, value)
			                     {
				                     DataType = parameter.Type
			                     };

			return instanceConfiguration.AddParameterValue(parameterValue);
		}
	}
}
