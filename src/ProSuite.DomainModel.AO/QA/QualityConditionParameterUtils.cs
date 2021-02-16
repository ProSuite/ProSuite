using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.AO.QA
{
	public static class QualityConditionParameterUtils
	{
		public static void AddParameterValue(QualityCondition qualityCondition,
		                                     [NotNull] string parameterName,
		                                     [CanBeNull] Dataset value,
		                                     string filterExpression = null,
		                                     bool usedAsReferenceData = false)
		{
			Assert.ArgumentNotNullOrEmpty(parameterName, nameof(parameterName));

			TestFactory factory =
				TestFactoryUtils.GetTestFactory(qualityCondition.TestDescriptor);

			TestParameter parameter = Assert.NotNull(factory).GetParameter(parameterName);

			TestParameterTypeUtils.AssertValidDataset(parameter, value);
			var parameterValue = new DatasetTestParameterValue(parameter, value,
			                                                   filterExpression,
			                                                   usedAsReferenceData);
			parameterValue.DataType = parameter.Type;
			qualityCondition.AddParameterValue(parameterValue);
		}

		public static void AddParameterValue(QualityCondition qualityCondition,
		                                     [NotNull] string parameterName,
		                                     [CanBeNull] string value)
		{
			AddScalarParameterValue(qualityCondition, parameterName, value);
		}

		public static void AddParameterValue(QualityCondition qualityCondition,
		                                     [NotNull] string parameterName,
		                                     object value)
		{
			var dataset = value as Dataset;
			if (dataset != null)
			{
				AddParameterValue(qualityCondition, parameterName, dataset);
			}
			else
			{
				AddScalarParameterValue(qualityCondition, parameterName, value);
			}
		}

		private static void AddScalarParameterValue(QualityCondition qualityCondition,
		                                            [NotNull] string parameterName,
		                                            [CanBeNull] object value)
		{
			Assert.ArgumentNotNullOrEmpty(parameterName, nameof(parameterName));

			TestFactory factory =
				TestFactoryUtils.GetTestFactory(qualityCondition.TestDescriptor);

			TestParameter parameter = Assert.NotNull(factory).GetParameter(parameterName);

			if (! parameter.IsConstructorParameter && parameter.Type.IsValueType &&
			    (value == null || value as string == string.Empty))
			{
				return;
			}

			var parameterValue = new ScalarTestParameterValue(parameter, value);
			parameterValue.DataType = parameter.Type;
			qualityCondition.AddParameterValue(parameterValue);
		}
	}
}
