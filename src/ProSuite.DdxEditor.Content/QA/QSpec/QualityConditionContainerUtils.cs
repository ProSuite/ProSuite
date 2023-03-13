using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Dialogs;
using ProSuite.DdxEditor.Content.QA.Categories;
using ProSuite.DdxEditor.Content.QA.InstanceConfig;
using ProSuite.DdxEditor.Content.QA.QCon;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	public static class QualityConditionContainerUtils
	{
		[NotNull]
		public static IEnumerable<QualityConditionDatasetTableRow>
			GetQualityConditionDatasetTableRows(
				[NotNull] CoreDomainModelItemModelBuilder modelBuilder,
				[CanBeNull] DataQualityCategory category)
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			return GetQualityConditionDatasetTableRows(
					category,
					modelBuilder.QualityConditions,
					modelBuilder.IncludeQualityConditionsBasedOnDeletedDatasets)
				.OrderBy(row => string.Format("{0}||{1}",
				                              row.Name, row.DatasetName));
		}

		[NotNull]
		public static IEnumerable<InstanceConfigurationInCategoryTableRow>
			GetQualityConditionTableRows(
				[NotNull] CoreDomainModelItemModelBuilder modelBuilder,
				[CanBeNull] DataQualityCategory category)
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			return GetQualityConditionTableRows(
					category,
					modelBuilder.QualityConditions,
					modelBuilder.IncludeQualityConditionsBasedOnDeletedDatasets)
				.OrderBy(row => row.Name);
		}

		public static bool AssignToCategory(
			[NotNull] ICollection<QualityConditionItem> items,
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder,
			[NotNull] IWin32Window owner,
			[CanBeNull] out DataQualityCategory category)
		{
			IList<InstanceConfiguration> instanceConfigurations = null;
			modelBuilder.UseTransaction(
				() =>
				{
					instanceConfigurations = items.Select(i => Assert.NotNull(i.GetEntity()))
					                              .Cast<InstanceConfiguration>().ToList();
				});

			return AssignToCategory(instanceConfigurations, modelBuilder, owner, out category);
		}

		public static bool AssignToCategory(
			[NotNull] ICollection<InstanceConfigurationItem> items,
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder,
			[NotNull] IWin32Window owner,
			[CanBeNull] out DataQualityCategory category)
		{
			IList<InstanceConfiguration> instanceConfigurations = null;
			modelBuilder.UseTransaction(
				() =>
				{
					instanceConfigurations = items.Select(i => Assert.NotNull(i.GetEntity()))
					                              .ToList();
				});

			return AssignToCategory(instanceConfigurations, modelBuilder, owner, out category);
		}

		public static bool AssignToCategory(
			[NotNull] ICollection<InstanceConfiguration> instanceConfigurations,
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder,
			[NotNull] IWin32Window owner,
			[CanBeNull] out DataQualityCategory category)
		{
			Assert.ArgumentNotNull(instanceConfigurations, nameof(instanceConfigurations));
			Assert.ArgumentNotNull(owner, nameof(owner));
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			IList<DataQualityCategory> categories =
				DataQualityCategoryUtils.GetCategories(modelBuilder,
				                                       c => c.CanContainQualityConditions);

			const string title = "Assign to Category";
			if (categories.Count == 0)
			{
				Dialog.InfoFormat(owner, title,
				                  "There are no categories which can contain quality conditions");
				category = null;
				return false;
			}

			DataQualityCategoryTableRow selectedCategory =
				DataQualityCategoryUtils.SelectCategory(categories, owner);

			if (selectedCategory == null)
			{
				category = null;
				return false;
			}

			if (! Dialog.YesNo(owner, title,
			                   string.Format(
				                   "Do you want to assign {0} quality condition(s) to {1}?",
				                   instanceConfigurations.Count, selectedCategory.QualifiedName)))
			{
				category = null;
				return false;
			}

			modelBuilder.UseTransaction(
				delegate
				{
					foreach (InstanceConfiguration instanceConfiguration in instanceConfigurations)
					{
						instanceConfiguration.Category = selectedCategory.DataQualityCategory;
					}
				});

			category = selectedCategory.DataQualityCategory;
			return true;
		}

		public static void RefreshQualityConditionAssignmentTarget(
			[CanBeNull] DataQualityCategory category,
			[NotNull] IItemNavigation itemNavigation)
		{
			RefreshQualityConditionsItem(
				category == null
					? itemNavigation.FindFirstItem<QAItem>()
					: itemNavigation.FindItem(category), itemNavigation);
		}

		public static void RefreshAssignmentTarget(
			[CanBeNull] DataQualityCategory category,
			[NotNull] IItemNavigation itemNavigation,
			[NotNull] IInstanceConfigurationContainerItem containerItem)
		{
			RefreshInstanceConfigurationsItem(
				category == null
					? itemNavigation.FindFirstItem<QAItem>()
					: itemNavigation.FindItem(category), itemNavigation,
				containerItem.GetType());
		}

		[NotNull]
		private static IEnumerable<InstanceConfigurationInCategoryTableRow>
			GetQualityConditionTableRows(
				[CanBeNull] DataQualityCategory category,
				[NotNull] IQualityConditionRepository repository,
				bool includeQualityConditionsBasedOnDeletedDatasets)
		{
			IDictionary<int, int> qspecCountMap = null;

			foreach (QualityCondition qualityCondition in repository.Get(
				         category, includeQualityConditionsBasedOnDeletedDatasets))
			{
				if (qspecCountMap == null)
				{
					qspecCountMap = repository.GetReferencingQualitySpecificationCount();
				}

				int qualitySpecificationRefCount;
				if (! qspecCountMap.TryGetValue(qualityCondition.Id,
				                                out qualitySpecificationRefCount))
				{
					qualitySpecificationRefCount = 0;
				}

				yield return
					new InstanceConfigurationInCategoryTableRow(qualityCondition,
					                                            qualitySpecificationRefCount);
			}
		}

		[NotNull]
		private static IEnumerable<QualityConditionDatasetTableRow>
			GetQualityConditionDatasetTableRows(
				[CanBeNull] DataQualityCategory category,
				[NotNull] IQualityConditionRepository qualityConditions,
				bool includeQualityConditionsBasedOnDeletedDatasets)
		{
			IDictionary<int, int> qspecCountMap = null; // created lazily

			IDictionary<QualityCondition, IList<DatasetTestParameterValue>> datasetsByQConId =
				qualityConditions.GetWithDatasetParameterValues(category);

			foreach (
				KeyValuePair<QualityCondition, IList<DatasetTestParameterValue>> pair in
				datasetsByQConId)
			{
				QualityCondition qualityCondition = pair.Key;
				IList<DatasetTestParameterValue> datasetParameterValues = pair.Value;

				if (qspecCountMap == null)
				{
					qspecCountMap =
						qualityConditions.GetReferencingQualitySpecificationCount();
				}

				int qualitySpecificationRefCount;
				if (! qspecCountMap.TryGetValue(qualityCondition.Id,
				                                out qualitySpecificationRefCount))
				{
					qualitySpecificationRefCount = 0;
				}

				var tableRows = new List<QualityConditionDatasetTableRow>();

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
							new QualityConditionDatasetTableRow(
								qualityCondition, datasetValue, qualitySpecificationRefCount));
					}
				}
				catch (TypeLoadException e)
				{
					tableRows.Add(
						new QualityConditionDatasetTableRow(qualityCondition,
						                                    e.Message,
						                                    qualitySpecificationRefCount));
				}

				if (! anyDeletedDatasets)
				{
					foreach (QualityConditionDatasetTableRow tableRow in tableRows)
					{
						yield return tableRow;
					}
				}
			}
		}

		private static void RefreshQualityConditionsItem(
			[CanBeNull] Item parentItem,
			[NotNull] IItemNavigation itemNavigation)
		{
			if (parentItem == null || ! parentItem.HasChildrenLoaded)
			{
				return;
			}

			QualityConditionsItem childItem = parentItem.Children
			                                            .OfType<QualityConditionsItem>()
			                                            .FirstOrDefault();
			if (childItem != null)
			{
				itemNavigation.RefreshItem(childItem);
			}
		}

		private static void RefreshInstanceConfigurationsItem(
			[CanBeNull] Item parentItem,
			[NotNull] IItemNavigation itemNavigation,
			Type itemType)
		{
			if (parentItem == null || ! parentItem.HasChildrenLoaded)
			{
				return;
			}

			var childItem = parentItem.Children
			                          .FirstOrDefault(
				                          child => child.GetType() == itemType) as
				                InstanceConfigurationsItem;

			if (childItem != null)
			{
				itemNavigation.RefreshItem(childItem);
			}
		}
	}
}
