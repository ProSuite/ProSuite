using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DdxEditor.Content.QA.InstanceConfig
{
	internal static class InstanceConfigTableRows
	{
		[NotNull]
		public static IEnumerable<InstanceConfigurationInCategoryTableRow>
			GetInstanceConfigs<T>(
				[NotNull] CoreDomainModelItemModelBuilder modelBuilder,
				[CanBeNull] DataQualityCategory category) where T : InstanceConfiguration
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			return GetInstanceConfigurationTableRows<T>(
					category,
					modelBuilder.InstanceConfigurations,
					modelBuilder.IncludeQualityConditionsBasedOnDeletedDatasets)
				.OrderBy(row => row.Name);
		}

		[NotNull]
		public static IEnumerable<InstanceConfigurationDatasetTableRow>
			GetInstanceConfigurationDatasetTableRows<T>(
				[NotNull] CoreDomainModelItemModelBuilder modelBuilder,
				[CanBeNull] DataQualityCategory category) where T : InstanceConfiguration
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			return GetInstanceConfigurationDatasetTableRows<T>(
					category,
					modelBuilder.InstanceConfigurations,
					modelBuilder.IncludeQualityConditionsBasedOnDeletedDatasets)
				.OrderBy(row => string.Format("{0}||{1}",
				                              row.Name, row.DatasetName));
		}

		[NotNull]
		private static IEnumerable<InstanceConfigurationInCategoryTableRow>
			GetInstanceConfigurationTableRows<T>(
				[CanBeNull] DataQualityCategory category,
				[NotNull] IInstanceConfigurationRepository repository,
				bool includeQualityConditionsBasedOnDeletedDatasets) where T : InstanceConfiguration
		{
			IDictionary<int, int> usageCountMap = null;

			foreach (T instanceConfig in repository.Get<T>(
				         category, includeQualityConditionsBasedOnDeletedDatasets))
			{
				if (usageCountMap == null)
				{
					usageCountMap = repository.GetReferenceCounts<T>()
					                          .ToDictionary(rc => rc.EntityId,
					                                        rc => rc.UsageCount);
				}

				if (! usageCountMap.TryGetValue(instanceConfig.Id,
				                                out int refCount))
				{
					refCount = 0;
				}

				yield return new InstanceConfigurationInCategoryTableRow(instanceConfig, refCount);
			}
		}

		[NotNull]
		private static IEnumerable<InstanceConfigurationDatasetTableRow>
			GetInstanceConfigurationDatasetTableRows<T>(
				[CanBeNull] DataQualityCategory category,
				[NotNull] IInstanceConfigurationRepository instanceConfigurations,
				bool includeQualityConditionsBasedOnDeletedDatasets) where T : InstanceConfiguration
		{
			IDictionary<int, int> usageCountMap = null; // created lazily

			IDictionary<T, IList<DatasetTestParameterValue>> datasetsByQConId =
				instanceConfigurations.GetWithDatasetParameterValues<T>(category);

			foreach (KeyValuePair<T, IList<DatasetTestParameterValue>> pair in datasetsByQConId)
			{
				T instanceConfig = pair.Key;
				IList<DatasetTestParameterValue> datasetParameterValues = pair.Value;

				if (usageCountMap == null)
				{
					usageCountMap = instanceConfigurations.GetReferenceCounts<T>()
					                                      .ToDictionary(
						                                      rc => rc.EntityId,
						                                      rc => rc.UsageCount);
				}

				int qualitySpecificationRefCount;
				if (! usageCountMap.TryGetValue(instanceConfig.Id,
				                                out qualitySpecificationRefCount))
				{
					qualitySpecificationRefCount = 0;
				}

				var tableRows = new List<InstanceConfigurationDatasetTableRow>();

				var anyDeletedDatasets = false;
				try
				{
					foreach (DatasetTestParameterValue datasetValue in datasetParameterValues)
					{
						Dataset dataset = datasetValue.DatasetValue;
						if (dataset == null && datasetValue.ValueSource == null)
						{
							continue;
						}

						// TODO: Recursively find the deleted datasets in transformers -> Query!
						if (! includeQualityConditionsBasedOnDeletedDatasets &&
						    dataset != null && dataset.Deleted)
						{
							anyDeletedDatasets = true;
							break;
						}

						tableRows.Add(
							new InstanceConfigurationDatasetTableRow(
								instanceConfig, datasetValue, qualitySpecificationRefCount));
					}
				}
				catch (TypeLoadException e)
				{
					tableRows.Add(
						new InstanceConfigurationDatasetTableRow(instanceConfig,
						                                         e.Message,
						                                         qualitySpecificationRefCount));
				}

				if (! anyDeletedDatasets)
				{
					foreach (InstanceConfigurationDatasetTableRow tableRow in tableRows)
					{
						yield return tableRow;
					}
				}
			}
		}
	}
}
