using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ProSuite.Commons.Com;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
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
			[NotNull] QualitySpecification qualitySpecification)
		{
			IEnumerable<QualityCondition> qualityConditions =
				qualitySpecification.Elements.Select(e => e.QualityCondition);

			InitializeParameterValues(qualityConditions);
		}

		public static void InitializeParameterValues(
			[NotNull] IEnumerable<QualityCondition> qualityConditions)
		{
			foreach (QualityCondition condition in
			         qualityConditions)
			{
				InitializeParameterValues(condition);

				foreach (IssueFilterConfiguration filterConfiguration in
				         condition.IssueFilterConfigurations)
				{
					InitializeParameterValues(filterConfiguration);
				}
			}
		}

		public static void InitializeParameterValues(
			[NotNull] InstanceConfiguration instanceConfiguration)
		{
			IInstanceInfo instanceInfo =
				InstanceDescriptorUtils.GetInstanceInfo(instanceConfiguration.InstanceDescriptor);

			Assert.NotNull(instanceInfo, "Failed to create instance info for {0}",
			               instanceConfiguration.InstanceDescriptor);

			InitializeParameterValues(instanceInfo, instanceConfiguration);
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
						"{0} / Test parameter value {1}: No parameter found in {2}. The constructor Id might be incorrect or an optional parameter might have been added or deleted.",
						instanceConfiguration, parameterValue.TestParameterName, instanceInfo);
				}

				// Recursively include the transformer's parameters
				if (parameterValue.ValueSource != null)
				{
					InitializeParameterValues(parameterValue.ValueSource);
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

			// TOP-5890: Only re-attach mapped entities. Dummy-Datasets (DatasetType.Null) fail:
			var attachableDatasets =
				datasets.Where(d => d.DatasetType != DatasetType.Null).ToList();

			domainTransactions.Reattach(attachableDatasets);

			foreach (IDatasetCollection collectionDataset in attachableDatasets.Where(
				         d => d is IDatasetCollection).Cast<IDatasetCollection>())
			{
				foreach (var containedDataset in collectionDataset.ContainedDatasets)
				{
					Dataset dataset = containedDataset as Dataset;

					DatasetType? datasetType = dataset?.DatasetType;
					if (datasetType != null && datasetType != DatasetType.Null)
					{
						domainTransactions.Reattach(dataset);

						if (dataset is ObjectDataset objectDataset)
						{
							// Prevent LazyInitializationException when accessing Attributes to get OID field:
							domainTransactions.Initialize(objectDataset.Attributes);
						}
					}
				}
			}

			_msg.DebugStopTiming(
				watch, "Conditions with {0} datasets loaded and reattached ({1} datasets(s))",
				datasets.Count, attachableDatasets.Count);

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
				return distinctCategories[0]?.Name;
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

		public static string GenerateName([NotNull] InstanceConfiguration instanceConfiguration)
		{
			string descriptorName = instanceConfiguration.InstanceDescriptor?.Name;

			var datasetParameter =
				instanceConfiguration.ParameterValues.FirstOrDefault(
						p => p is DatasetTestParameterValue && p.StringValue != null)
					as DatasetTestParameterValue;

			string datasetName = datasetParameter?.DatasetValue?.AliasName ??
			                     datasetParameter?.DatasetValue?.Name ??
			                     datasetParameter?.ValueSource?.Name;

			if (descriptorName == null || datasetName == null)
			{
				return null;
			}

			// Consider making configurable similar to batch-create
			return $"{datasetName}_{descriptorName}";
		}

		public static void HandleNoConditionCreated(
			[CanBeNull] string conditionName,
			[NotNull] IDictionary<string, DdxModel> modelsByWorkspaceId,
			bool ignoreConditionsForUnknownDatasets,
			[NotNull] ICollection<DatasetTestParameterRecord> unknownDatasetParameters)
		{
			Assert.True(ignoreConditionsForUnknownDatasets,
			            "ignoreConditionsForUnknownDatasets");
			Assert.True(unknownDatasetParameters.Count > 0,
			            "Unexpected number of unknown datasets");

			_msg.WarnFormat(
				unknownDatasetParameters.Count == 1
					? "Quality condition '{0}' is ignored because the following dataset is not found: {1}"
					: "Quality condition '{0}' is ignored because the following datasets are not found: {1}",
				conditionName,
				ConcatenateUnknownDatasetNames(
					unknownDatasetParameters,
					modelsByWorkspaceId,
					string.Empty));
		}

		[NotNull]
		public static string ConcatenateUnknownDatasetNames(
			[NotNull] IEnumerable<DatasetTestParameterRecord> unknownDatasetParameters,
			[NotNull] IDictionary<string, DdxModel> modelsByWorkspaceId,
			[NotNull] string anonymousWorkspaceId)
		{
			Assert.ArgumentNotNull(unknownDatasetParameters, nameof(unknownDatasetParameters));
			Assert.ArgumentNotNull(modelsByWorkspaceId, nameof(modelsByWorkspaceId));
			Assert.ArgumentNotNull(anonymousWorkspaceId, nameof(anonymousWorkspaceId));

			var sb = new StringBuilder();

			foreach (DatasetTestParameterRecord datasetParameter in unknownDatasetParameters)
			{
				if (sb.Length > 0)
				{
					sb.Append(", ");
				}

				string workspaceId = datasetParameter.WorkspaceId ?? anonymousWorkspaceId;
				DdxModel model;
				if (modelsByWorkspaceId.TryGetValue(workspaceId, out model))
				{
					sb.AppendFormat("{0} ({1})", datasetParameter.DatasetName, model.Name);
				}
				else
				{
					sb.Append(datasetParameter.DatasetName);
				}
			}

			return sb.ToString();
		}

		/// <summary>
		/// Generate instance-configuration styled UUID.
		/// </summary>
		/// <returns></returns>
		[NotNull]
		public static string GenerateUuid([CanBeNull] string uuidValue = null)
		{
			return ComUtils.FormatGuidWithoutCurlyBraces(
				StringUtils.IsNotEmpty(uuidValue) ? new Guid(uuidValue) : Guid.NewGuid());
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
					if (issueFilter.IsPersistent)
					{
						domainTransactions.Reattach(issueFilter);
					}

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

				if (transformer.IsPersistent)
				{
					domainTransactions.Reattach(transformer);
				}

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
