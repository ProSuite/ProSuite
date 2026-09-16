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

			InstanceDescriptor descriptor = instanceConfiguration.InstanceDescriptor;

			if (descriptor == null)
			{
				return false;
			}

			IInstanceInfo instanceInfo = InstanceDescriptorUtils.GetInstanceInfo(descriptor);

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
				if (paramValue == null)
				{
					// New (optional) parameter value that has not been persisted yed.
					// The default value will be added below.
					continue;
				}

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
						else if (TestParameterTypeUtils.AreCompatibleParameterTypes(
							         value.DataType, testParam.Type))
						{
							addValue = value;
						}
						else
						{
							var datasetParameter = value as DatasetTestParameterValue;

							if (datasetParameter != null)
							{
								Dataset dataset = datasetParameter.DatasetValue;

								if (dataset != null)
								{
									TestParameterType parameterType =
										TestParameterTypeUtils.GetParameterType(testParam.Type);

									if (TestParameterTypeUtils.IsValidDataset(
										    parameterType, dataset))
									{
										addValue = new DatasetTestParameterValue(
											testParam, dataset,
											datasetParameter.FilterExpression,
											datasetParameter.UsedAsReferenceData);
										newParameters = true;
										invalidValues.Add(value);
									}
								}
								else if (datasetParameter.ValueSource != null)
								{
									addValue = value;
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
		public static TestParameterValue AddParameterValue(
			[NotNull] InstanceConfiguration instanceConfiguration,
			[NotNull] string parameterName,
			[CanBeNull] Dataset value,
			string filterExpression = null,
			bool usedAsReferenceData = false)
		{
			TestParameterValue result = InstanceConfigurationUtils.AddParameterValue(
				instanceConfiguration, parameterName, value, filterExpression, usedAsReferenceData);

			TestParameterTypeUtils.AssertValidDataset(Assert.NotNull(result.DataType), value);

			return result;
		}

		[Obsolete("Use method InstanceConfigurationUtils.AddScalarParameterValue")]
		[CanBeNull]
		public static TestParameterValue AddParameterValue(
			[NotNull] InstanceConfiguration instanceConfiguration,
			[NotNull] string parameterName,
			[CanBeNull] string value)
		{
			return InstanceConfigurationUtils.AddScalarParameterValue(
				instanceConfiguration, parameterName, value);
		}

		[CanBeNull]
		public static TestParameterValue AddParameterValue(
			[NotNull] InstanceConfiguration instanceConfiguration,
			[NotNull] string parameterName,
			object value)
		{
			if (value is Dataset dataset)
			{
				return AddParameterValue(instanceConfiguration, parameterName, dataset);
			}

			return InstanceConfigurationUtils.AddScalarParameterValue(
				instanceConfiguration, parameterName, value);
		}

		/// <summary>
		/// Determines whether any of the dataset parameters of the specified configuration
		/// references, directly or indirectly, the configuration itself.
		/// </summary>
		/// <param name="testable">The configuration to be checked.</param>
		/// <param name="testParameterName">The name of the parameter that starts the circular
		/// reference, or null if there is none.</param>
		/// <param name="configurationNames">The names of the configurations along the circular
		/// reference, starting and ending with <paramref name="testable"/>.</param>
		public static bool CheckCircularReferencesInGraph(
			[NotNull] InstanceConfiguration testable,
			[CanBeNull] out string testParameterName,
			[NotNull] out NotificationCollection configurationNames)
		{
			Assert.ArgumentNotNull(testable, nameof(testable));

			configurationNames = new NotificationCollection();

			foreach (var datasetValue in
			         testable.ParameterValues.OfType<DatasetTestParameterValue>())
			{
				if (datasetValue.ValueSource == null)
				{
					continue;
				}

				var visitedConfigurations = new List<InstanceConfiguration> { testable };

				if (! LeadsBackToConfiguration(datasetValue.ValueSource, testable,
				                               visitedConfigurations))
				{
					continue;
				}

				foreach (InstanceConfiguration configuration in visitedConfigurations)
				{
					NotificationUtils.Add(configurationNames, configuration.Name);
				}

				NotificationUtils.Add(configurationNames, testable.Name);

				testParameterName = datasetValue.TestParameterName;
				return true;
			}

			testParameterName = null;
			return false;
		}

		public static bool CheckCircularReferencesInGraph(
			[NotNull] InstanceConfiguration testable,
			[CanBeNull] InstanceConfiguration instanceConfiguration,
			[NotNull] NotificationCollection configurationNames)
		{
			Assert.ArgumentNotNull(testable, nameof(testable));
			Assert.ArgumentNotNull(configurationNames, nameof(configurationNames));

			if (instanceConfiguration == null)
			{
				return false;
			}

			var visitedConfigurations = new List<InstanceConfiguration>();

			if (! LeadsBackToConfiguration(instanceConfiguration, testable,
			                               visitedConfigurations))
			{
				return false;
			}

			foreach (InstanceConfiguration configuration in visitedConfigurations)
			{
				NotificationUtils.Add(configurationNames, configuration.Name);
			}

			NotificationUtils.Add(configurationNames, testable.Name);

			return true;
		}

		/// <summary>
		/// Determines whether <paramref name="candidate"/> is <paramref name="searched"/>, or
		/// references it through the transformers of its dataset parameters.
		/// </summary>
		/// <param name="candidate">The configuration to start from.</param>
		/// <param name="searched">The configuration to look for.</param>
		/// <param name="visitedConfigurations">The configurations visited on the way to
		/// <paramref name="candidate"/>. They are not visited a second time, which makes sure
		/// that configurations referencing each other in a circle do not result in an endless
		/// recursion. If this method returns true, the list contains the configurations leading
		/// to <paramref name="searched"/> (excluding it), which is used for the error message.
		/// </param>
		private static bool LeadsBackToConfiguration(
			[NotNull] InstanceConfiguration candidate,
			[NotNull] InstanceConfiguration searched,
			[NotNull] List<InstanceConfiguration> visitedConfigurations)
		{
			if (searched.Equals(candidate))
			{
				return true;
			}

			// Compare by reference, not with Equals(): a circle can only be closed by the very
			// same configuration instance.
			if (visitedConfigurations.Any(visited => ReferenceEquals(visited, candidate)))
			{
				return false;
			}

			visitedConfigurations.Add(candidate);

			foreach (var datasetValue in candidate.ParameterValues
			                                      .OfType<DatasetTestParameterValue>())
			{
				if (datasetValue.ValueSource != null &&
				    LeadsBackToConfiguration(datasetValue.ValueSource, searched,
				                             visitedConfigurations))
				{
					return true;
				}
			}

			// This configuration does not lead back to the searched one: remove it again
			// (it was added last), so that the names reported are those of the circle only.
			visitedConfigurations.RemoveAt(visitedConfigurations.Count - 1);

			return false;
		}
	}
}
