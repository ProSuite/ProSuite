using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DdxEditor.Content.QA.InstanceConfig
{
	internal  static class InstanceConfigTableRows
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

				if (!usageCountMap.TryGetValue(instanceConfig.Id,
				                               out int refCount))
				{
					refCount = 0;
				}

				yield return new InstanceConfigurationInCategoryTableRow(instanceConfig, refCount);
			}
		}


	}
}
