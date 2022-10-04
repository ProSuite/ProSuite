using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Notifications;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.AO.QA
{
	public static class TestParameterValueUtils
	{
		/// <summary>
		/// Synchronize parameters with TestFactory.
		/// </summary>
		/// <returns>true, if not all parameters fit to the TestFactory.</returns>
		public static bool SyncParameterValues(
			[NotNull] InstanceConfiguration instanceConfiguration)
		{
			Assert.ArgumentNotNull(instanceConfiguration, nameof(instanceConfiguration));

			if (instanceConfiguration.InstanceDescriptor == null)
			{
				return false;
			}

			IInstanceInfo factory =
				TestFactoryUtils.GetTestFactory(instanceConfiguration.InstanceDescriptor);

			if (factory == null)
			{
				return false;
			}

			return SyncParameterValues(instanceConfiguration, factory);
		}

		public static bool SyncParameterValues(
			[NotNull] InstanceConfiguration instanceConfiguration,
			[CanBeNull] IInstanceInfo instanceInfo)
		{
			if (instanceInfo == null)
			{
				return false;
			}

			var validValuesByParameter =
				new Dictionary<TestParameter, IList<TestParameterValue>>();
			var parametersByName = new Dictionary<string, TestParameter>();

			foreach (TestParameter param in instanceInfo.Parameters)
			{
				validValuesByParameter.Add(param, new List<TestParameterValue>());
				parametersByName.Add(param.Name, param);
			}

			var invalidValues = new List<TestParameterValue>();

			foreach (TestParameterValue paramValue in instanceConfiguration.ParameterValues)
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
					validValues.Add(TestParameterTypeUtils.GetEmptyParameterValue(testParam));
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
									TestParameterTypeUtils.GetParameterType(testParam.Type);

								if (dataset != null &&
								    TestParameterTypeUtils.IsValidDataset(parameterType, dataset))
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
				instanceConfiguration.ClearParameterValues();
				foreach (TestParameterValue value in validValues)
				{
					instanceConfiguration.AddParameterValue(value);
				}
			}
			else
			{
				foreach (TestParameterValue value in invalidValues)
				{
					instanceConfiguration.RemoveParameterValue(value);
				}
			}

			return newParameters || invalidValues.Count > 0;
		}

		[NotNull]
		public static TestParameterValue CreateEmptyParameterValue(
			[NotNull] TestParameter testParameter)
		{
			Assert.ArgumentNotNull(testParameter, nameof(testParameter));

			if (TestParameterTypeUtils.IsDatasetType(testParameter.Type))
			{
				return new DatasetTestParameterValue(testParameter);
			}

			if (testParameter.Type == typeof(double) ||
			    testParameter.Type == typeof(int) ||
			    testParameter.Type == typeof(bool) ||
			    testParameter.Type == typeof(string) ||
			    testParameter.Type == typeof(DateTime) ||
			    testParameter.Type.IsEnum)
			{
				return new ScalarTestParameterValue(testParameter,
				                                    $"{testParameter.DefaultValue ?? GetDefault(testParameter.Type)}");
			}

			return new ScalarTestParameterValue(testParameter,
			                                    $"{testParameter.DefaultValue ?? GetDefault(testParameter.Type)}");
			//throw new ArgumentException("Unhandled type " + _type);
		}

		[NotNull]
		public static TestParameterValue AddParameterValue(
			[NotNull] InstanceConfiguration qualityCondition,
			[NotNull] string parameterName,
			[CanBeNull] Dataset value,
			string filterExpression = null,
			bool usedAsReferenceData = false)
		{
			TestParameterValue result = InstanceConfigurationUtils.AddParameterValue(
				qualityCondition, parameterName, value, filterExpression, usedAsReferenceData);

			TestParameterTypeUtils.AssertValidDataset(Assert.NotNull(result.DataType), value);

			return result;
		}

		[CanBeNull]
		public static TestParameterValue AddParameterValue(
			[NotNull] QualityCondition qualityCondition,
			[NotNull] string parameterName,
			[CanBeNull] string value)
		{
			return InstanceConfigurationUtils.AddScalarParameterValue(
				qualityCondition, parameterName, value);
		}

		[CanBeNull]
		public static TestParameterValue AddParameterValue(
			[NotNull] InstanceConfiguration qualityCondition,
			[NotNull] string parameterName,
			object value)
		{
			var dataset = value as Dataset;
			if (dataset != null)
			{
				return AddParameterValue(qualityCondition, parameterName, dataset);
			}
			else
			{
				return InstanceConfigurationUtils.AddScalarParameterValue(
					qualityCondition, parameterName, value);
			}
		}

		[CanBeNull]
		private static object GetDefault([NotNull] Type type)
		{
			if (! type.IsValueType)
			{
				return null;
			}

			object defaultValue = Activator.CreateInstance(type);

			if (type.IsEnum)
			{
				// Ensure valid value for enums: if default value (0) is not in list, return the first enum item value
				if (! Enum.IsDefined(type, defaultValue))
				{
					string[] values = Enum.GetNames(type);
					if (values.Length > 0)
					{
						return values[0];
					}
				}
			}

			return defaultValue;
		}

		public static bool CheckCircularReferencesInGraph(
			[NotNull] InstanceConfiguration testable,
			[CanBeNull] out string testParameterName,
			[NotNull] out NotificationCollection configurationNames)
		{
			Assert.ArgumentNotNull(testable, nameof(testable));

			configurationNames = new NotificationCollection();

			NotificationUtils.Add(configurationNames, testable.Name);

			foreach (var dsValue in testable.ParameterValues.OfType<DatasetTestParameterValue>())
			{
				testParameterName = dsValue.TestParameterName;

				if (testable.Equals(dsValue.ValueSource) && dsValue.ValueSource != null)
				{
					return true;
				}

				if (CheckCircularReferencesInGraph(testable, dsValue.ValueSource,
				                                   configurationNames))
				{
					return true;
				}

				testParameterName = null;
				return false;
			}

			testParameterName = null;
			return false;
		}

		public static bool CheckCircularReferencesInGraph([NotNull] InstanceConfiguration testable,
		                                                  [CanBeNull]
		                                                  InstanceConfiguration
			                                                  instanceConfiguration,
		                                                  [NotNull]
		                                                  NotificationCollection configurationNames)
		{
			Assert.ArgumentNotNull(testable, nameof(testable));
			Assert.ArgumentNotNull(configurationNames, nameof(configurationNames));

			if (instanceConfiguration == null)
			{
				return false;
			}

			NotificationUtils.Add(configurationNames, instanceConfiguration.Name);

			foreach (var dsValue in instanceConfiguration.ParameterValues
			                                             .OfType<DatasetTestParameterValue>())
			{
				if (testable.Equals(dsValue.ValueSource))
				{
					NotificationUtils.Add(configurationNames, testable.Name);
					return true;
				}

				return CheckCircularReferencesInGraph(testable, dsValue.ValueSource,
				                                      configurationNames);
			}

			return false;
		}
	}
}
