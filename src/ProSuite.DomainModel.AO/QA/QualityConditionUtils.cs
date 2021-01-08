using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.AO.QA
{
	[CLSCompliant(false)]
	public static class QualityCondition_Utils
	{
		/// <summary>
		/// Synchronize parameters with TestFactory.
		/// </summary>
		/// <returns>true, if not all parameters fit to the TestFactory.</returns>
		public static bool SyncParameterValues([NotNull] QualityCondition qualityCondition)
		{
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));

			if (qualityCondition.TestDescriptor == null)
			{
				return false;
			}

			TestFactory factory =
				TestDescriptorUtils.GetTestFactory(qualityCondition.TestDescriptor);
			if (factory == null)
			{
				return false;
			}

			var validValuesByParameter =
				new Dictionary<TestParameter, IList<TestParameterValue>>();
			var parametersByName = new Dictionary<string, TestParameter>();

			foreach (TestParameter param in factory.Parameters)
			{
				validValuesByParameter.Add(param, new List<TestParameterValue>());
				parametersByName.Add(param.Name, param);
			}

			var invalidValues = new List<TestParameterValue>();

			foreach (TestParameterValue paramValue in qualityCondition.ParameterValues)
			{
				string name = paramValue.TestParameterName;

				TestParameter param;
				if (! parametersByName.TryGetValue(name, out param))
				{
					invalidValues.Add(paramValue);
				}
				else
				{
					validValuesByParameter[param].Add(paramValue);
				}
			}

			var newParameters = false;
			var validValues = new List<TestParameterValue>();

			foreach (KeyValuePair<TestParameter, IList<TestParameterValue>> pair
				in validValuesByParameter)
			{
				TestParameter testParam = pair.Key;
				IList<TestParameterValue> values = pair.Value;

				if (values.Count == 0 && testParam.ArrayDimension == 0)
				{
					validValues.Add(TestParameter_Utils.GetEmptyParameterValue(testParam));
					newParameters = true;
				}
				else
				{
					var add = true;
					foreach (TestParameterValue value in values)
					{
						TestParameterValue addValue = null;
						if (value.DataType == null)
						{
							value.DataType = testParam.Type;
							addValue = value;
						}
						else if (value.DataType == testParam.Type)
						{
							addValue = value;
						}
						else
						{
							var datasetParameter = value as DatasetTestParameterValue;

							if (datasetParameter != null)
							{
								Dataset dataset = datasetParameter.DatasetValue;
								TestParameterType parameterType =
									TestParameter_Utils.GetParameterType(testParam.Type);

								if (dataset != null &&
								    TestParameter_Utils.IsValidDataset(parameterType, dataset))
								{
									addValue = new DatasetTestParameterValue(
										testParam, dataset,
										datasetParameter.FilterExpression,
										datasetParameter.UsedAsReferenceData);
									newParameters = true;
									invalidValues.Add(value);
								}
							}
						}

						if (add && addValue != null)
						{
							validValues.Add(addValue);
						}
						else
						{
							invalidValues.Add(value);
						}

						if (testParam.ArrayDimension == 0)
						{
							add = false;
						}
					}
				}
			}

			if (newParameters)
			{
				qualityCondition.ClearParameterValues();
				foreach (TestParameterValue value in validValues)
				{
					qualityCondition.AddParameterValue(value);
				}
			}
			else
			{
				foreach (TestParameterValue value in invalidValues)
				{
					qualityCondition.RemoveParameterValue(value);
				}
			}

			return newParameters || invalidValues.Count > 0;
		}

		public static void AddParameterValue(QualityCondition qualityCondition,
		                                     [NotNull] string parameterName,
		                                     [CanBeNull] Dataset value,
		                                     string filterExpression = null,
		                                     bool usedAsReferenceData = false)
		{
			Assert.ArgumentNotNullOrEmpty(parameterName, nameof(parameterName));

			TestFactory factory =
				TestDescriptorUtils.GetTestFactory(qualityCondition.TestDescriptor);

			TestParameter parameter = Assert.NotNull(factory).GetParameter(parameterName);

			TestParameter_Utils.AssertValidDataset(parameter, value);
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
				TestDescriptorUtils.GetTestFactory(qualityCondition.TestDescriptor);

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
