using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA.Repositories;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.Core.QA
{
	public static class InstanceConfigurationUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static DatasetTestParameterValue AddParameterValue(
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

			instanceConfiguration.AddParameterValue(parameterValue);

			return parameterValue;
		}

		public static DatasetTestParameterValue AddParameterValue(
			[NotNull] InstanceConfiguration instanceConfiguration,
			[NotNull] string parameterName,
			[CanBeNull] TransformerConfiguration transformerConfig,
			string filterExpression = null,
			bool usedAsReferenceData = false)
		{
			Assert.ArgumentNotNullOrEmpty(parameterName, nameof(parameterName));

			IInstanceInfo instanceInfo =
				InstanceDescriptorUtils.GetInstanceInfo(instanceConfiguration.InstanceDescriptor);

			TestParameter parameter = Assert.NotNull(instanceInfo).GetParameter(parameterName);

			var parameterValue = new DatasetTestParameterValue(parameter, null,
			                                                   filterExpression,
			                                                   usedAsReferenceData)
			                     {
				                     ValueSource = transformerConfig,
				                     DataType = parameter.Type
			                     };

			instanceConfiguration.AddParameterValue(parameterValue);

			return parameterValue;
		}

		public static void AddParameterValue(InstanceConfiguration instanceConfiguration,
		                                     [NotNull] string parameterName,
		                                     object value)
		{
			if (value is Dataset dataset)
			{
				AddParameterValue(instanceConfiguration, parameterName, dataset);
			}
			else if (value is TransformerConfiguration transformerConfig)
			{
				AddParameterValue(instanceConfiguration, parameterName, transformerConfig);
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

		public static void InitializeParameterValues(
			[NotNull] IInstanceInfo instanceInfo,
			[NotNull] InstanceConfiguration instanceConfiguration)
		{
			Dictionary<string, TestParameter> parametersByName =
				instanceInfo.Parameters.ToDictionary(testParameter => testParameter.Name);

			foreach (TestParameterValue parameterValue in instanceConfiguration.ParameterValues)
			{
				if (parametersByName.TryGetValue(parameterValue.TestParameterName,
				                                 out TestParameter testParameter))
				{
					parameterValue.DataType = testParameter.Type;
				}
				else
				{
					_msg.WarnFormat(
						"{0} / Test parameter value {1}: No parameter found in {2}. The constructor Id might be incorrect.",
						instanceConfiguration, parameterValue.TestParameterName, instanceInfo);
				}
			}
		}

		/// <summary>
		/// All persistent entities that are part of the specified quality conditions are loaded
		/// and initialized. The entities are quality conditions, their issue filters, transformers
		/// and their respective TestParameterValues and finally all the referenced datasets.
		/// </summary>
		/// <param name="conditions"></param>
		/// <param name="domainTransactions"></param>
		/// <param name="instanceConfigurations"></param>
		/// <returns>All datasets that are involved in any associated entity of the
		/// conditions in the specification.</returns>
		public static ICollection<Dataset> InitializeAssociatedConfigurationsTx(
			[NotNull] ICollection<QualityCondition> conditions,
			[NotNull] IDomainTransactionManager domainTransactions,
			[CanBeNull] IInstanceConfigurationRepository instanceConfigurations = null)
		{
			Stopwatch watch = _msg.DebugStartTiming("Reattaching and initializing {0} conditions",
			                                        conditions.Count);

			bool hasUninitializedAssociations = false;

			// Avoid no session while getting referenced datasets, attach conditions first:
			var enabledConditions = new List<QualityCondition>();
			foreach (QualityCondition condition in conditions)
			{
				if (condition.IsPersistent)
				{
					// Do not re-attach un-persisted (e.g. customized) conditions.
					domainTransactions.Reattach(condition);

					if (! domainTransactions.IsInitialized(condition.ParameterValues))
					{
						hasUninitializedAssociations = true;
					}
				}

				enabledConditions.Add(condition);
			}

			// TODO: Find out where the specification is loaded for the first time and make sure
			//       this method is called.
			ICollection<Dataset> datasets = null;

			if (instanceConfigurations != null && hasUninitializedAssociations)
			{
				// NOTE: For large sets of conditions, this is slightly slower than looping
				// through all conditions and lazily getting all ParameterValues.
				// With many transformers it is expected to be faster due to the eagerly fetching
				// of all ParameterValues in one round-trip instead of repeatedly lazy-loading the
				// ParameterValues per condition. However, if everything has been already
				// initialized, just re-attaching is 2 orders of magnitude faster.
				// Hence it should only be run if we know that there are un-initialized
				// associations.
				datasets = instanceConfigurations.GetAllReferencedDatasets(enabledConditions, true)
				                                 .ToList();
			}

			ReattachAllTransformersAndFilters(enabledConditions, domainTransactions);

			if (datasets == null)
			{
				datasets = GetQualityConditionDatasets(enabledConditions);

				// Re-attach conditions because otherwise a LazyLoadException occurs when getting
				// the issue filters
				foreach (QualityCondition condition in enabledConditions)
				{
					foreach (Dataset dataset in GetIssueFilterDatasets(condition))
					{
						datasets.Add(dataset);
					}
				}
			}

			domainTransactions.Reattach(datasets);

			_msg.DebugStopTiming(watch, "Conditions loaded and reattached ({0:N0} datasets(s))",
			                     datasets.Count);

			return datasets;
		}

		public static string GetDatasetCategoryName(
			[NotNull] DatasetTestParameterValue datasetParameterValue)
		{
			if (datasetParameterValue.DatasetValue != null)
			{
				return datasetParameterValue.DatasetValue.DatasetCategory?.Name;
			}

			return GetDatasetCategoryName(datasetParameterValue.ValueSource);
		}

		public static string GetDatasetModelName(
			[NotNull] DatasetTestParameterValue datasetParameterValue)
		{
			if (datasetParameterValue.DatasetValue != null)
			{
				return datasetParameterValue.DatasetValue.Model?.Name;
			}

			if (datasetParameterValue.ValueSource != null)
			{
				return GetDatasetModelName(datasetParameterValue.ValueSource);
			}

			return null;
		}

		[CanBeNull]
		public static string GetDatasetCategoryName(
			[CanBeNull] InstanceConfiguration instanceConfiguration)
		{
			if (instanceConfiguration == null)
			{
				return null;
			}

			var distinctCategories = instanceConfiguration.GetDatasetParameterValues(false, true)
			                                              .Select(d => d.DatasetCategory)
			                                              .Distinct().ToList();

			if (distinctCategories.Count == 0)
			{
				return null;
			}

			if (distinctCategories.Count == 1)
			{
				return distinctCategories[0].Name;
			}

			return StringUtils.Concatenate(distinctCategories.OrderBy(c => c?.Name),
			                               c => c?.Name ?? "<no category>",
			                               ", ");
		}

		[CanBeNull]
		public static string GetDatasetModelName(
			[CanBeNull] InstanceConfiguration instanceConfiguration)
		{
			if (instanceConfiguration == null)
			{
				return null;
			}

			var distinctModels = instanceConfiguration.GetDatasetParameterValues(false, true)
			                                          .Select(d => d.Model)
			                                          .Distinct().ToList();

			if (distinctModels.Count == 0)
			{
				return null;
			}
			else if (distinctModels.Count == 1)
			{
				return distinctModels[0].Name;
			}
			else
			{
				return "<multiple>";
			}
		}

		private static void ReattachAllTransformersAndFilters(
			[NotNull] IEnumerable<QualityCondition> conditions,
			[NotNull] IDomainTransactionManager domainTransactions)
		{
			var topLevelTransformers = new List<TransformerConfiguration>();
			foreach (QualityCondition condition in conditions)
			{
				ReattachAndAddTransformers(condition.ParameterValues, topLevelTransformers,
				                           domainTransactions);

				foreach (IssueFilterConfiguration issueFilter in
				         condition.IssueFilterConfigurations)
				{
					domainTransactions.Reattach(issueFilter);

					ReattachAndAddTransformers(issueFilter.ParameterValues, topLevelTransformers,
					                           domainTransactions);
				}
			}

			// Now we have the top-level transformers. Recursively re-attach the referenced transformers
			ReattachTransformersRecursively(topLevelTransformers, domainTransactions);
		}

		private static void ReattachAndAddTransformers(
			IEnumerable<TestParameterValue> parameterValues,
			ICollection<TransformerConfiguration> toResultList,
			IDomainTransactionManager domainTransactions)
		{
			foreach (var parameterValue in parameterValues)
			{
				if (! (parameterValue is DatasetTestParameterValue datasetParamterValue))
				{
					continue;
				}

				TransformerConfiguration transformer = datasetParamterValue.ValueSource;

				if (transformer == null)
				{
					continue;
				}

				domainTransactions.Reattach(transformer);
				toResultList.Add(transformer);
			}
		}

		private static void ReattachTransformersRecursively(
			[NotNull] List<TransformerConfiguration> transformers,
			IDomainTransactionManager domainTransactions)
		{
			while (transformers.Count > 0)
			{
				var nextLevelTransformers = new List<TransformerConfiguration>();

				foreach (TransformerConfiguration transformer in transformers)
				{
					ReattachAndAddTransformers(transformer.ParameterValues, nextLevelTransformers,
					                           domainTransactions);
				}

				transformers = nextLevelTransformers;
			}
		}

		/// <summary>
		/// Gets all datasets that are involved in any of the conditions of the provided specification,
		/// including datasets that are part of a transformer or filter used in a condition.
		/// </summary>
		/// <param name="conditions"></param>
		/// <returns></returns>
		private static ICollection<Dataset> GetQualityConditionDatasets(
			IEnumerable<QualityCondition> conditions)
		{
			var datasets = new HashSet<Dataset>();
			foreach (QualityCondition condition in conditions)
			{
				const bool includeRecursively = true;
				foreach (Dataset dataset in condition.GetDatasetParameterValues(
					         includeRecursively, includeRecursively))
				{
					datasets.Add(dataset);
				}
			}

			return datasets;
		}

		private static IEnumerable<Dataset> GetIssueFilterDatasets(
			[NotNull] QualityCondition condition)
		{
			// Initialize Issue filters:
			foreach (var issueFilterConfig in condition.IssueFilterConfigurations)
			{
				foreach (Dataset dataset in issueFilterConfig.GetDatasetParameterValues(true, true))
				{
					yield return dataset;
				}
			}
		}
	}
}
