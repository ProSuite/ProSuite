using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Dialogs;

namespace ProSuite.DdxEditor.Content.QA.Categories
{
	public static class DataQualityCategoryContainerUtils
	{
		public static bool AssignToCategory(
			[NotNull] DataQualityCategoryItem item,
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder,
			[NotNull] IWin32Window owner,
			[CanBeNull] out DataQualityCategory category)
		{
			Assert.ArgumentNotNull(item, nameof(item));
			Assert.ArgumentNotNull(owner, nameof(owner));
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			IList<DataQualityCategory> categories =
				DataQualityCategoryUtils.GetCategories(
					modelBuilder, candidate => CanAssignToCategory(item, candidate));

			const string title = "Assign to Category";
			if (categories.Count == 0)
			{
				Dialog.InfoFormat(owner, title,
				                  "There are no categories which can contain subcategories");
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

			DataQualityCategory targetCategory = selectedCategory.DataQualityCategory;

			DataQualityCategory categoryToAssign;
			if (! IsNameUnique(targetCategory, item, modelBuilder, out categoryToAssign))
			{
				string message =
					targetCategory == null
						? string.Format("There exists another top-level category named '{0}'",
						                categoryToAssign.Name)
						: string.Format(
							"There exists another category named '{0}' in the selected category {1}",
							categoryToAssign.Name,
							targetCategory.GetQualifiedName());

				Dialog.WarningFormat(owner, title, message);
				category = null;
				return false;
			}

			if (Equals(targetCategory, categoryToAssign.ParentCategory))
			{
				string message =
					targetCategory == null
						? string.Format("Category {0} is already a top-level category",
						                categoryToAssign.Name)
						: string.Format("Category {0} is already assigned to category {1}",
						                categoryToAssign.Name, targetCategory.GetQualifiedName());

				Dialog.WarningFormat(owner, title, message);
				category = null;
				return false;
			}

			if (! Dialog.YesNo(owner, title,
			                   string.Format(
				                   "Do you want to assign category {0} to {1}?",
				                   item.Text, selectedCategory.QualifiedName)))
			{
				category = null;
				return false;
			}

			modelBuilder.UseTransaction(() => AssignToCategoryTx(item, targetCategory));

			category = targetCategory;
			return true;
		}

		private static bool IsNameUnique([CanBeNull] DataQualityCategory targetCategory,
		                                 [NotNull] DataQualityCategoryItem item,
		                                 [NotNull] CoreDomainModelItemModelBuilder modelBuilder,
		                                 [NotNull] out DataQualityCategory categoryToAssign)
		{
			IDataQualityCategoryRepository repository =
				Assert.NotNull(modelBuilder.DataQualityCategories);

			DataQualityCategory category = null;
			IList<DataQualityCategory> categoriesAtSameLevel = null;
			modelBuilder.ReadOnlyTransaction(
				() =>
				{
					category = Assert.NotNull(item.GetEntity());

					categoriesAtSameLevel = targetCategory == null
						                        ? repository.GetTopLevelCategories()
						                        : targetCategory.SubCategories;
				});

			categoryToAssign = category;

			foreach (DataQualityCategory categoryAtSameLevel in categoriesAtSameLevel)
			{
				if (string.Equals(categoryAtSameLevel.Name, category.Name,
				                  StringComparison.OrdinalIgnoreCase) &&
				    ! Equals(categoryAtSameLevel, category))
				{
					// there is another category with the same name
					return false;
				}
			}

			return true;
		}

		private static void AssignToCategoryTx([NotNull] DataQualityCategoryItem item,
		                                       [CanBeNull] DataQualityCategory
			                                       targetCategory)
		{
			DataQualityCategory categoryToAssign = Assert.NotNull(item.GetEntity());

			if (categoryToAssign.ParentCategory != null)
			{
				categoryToAssign.ParentCategory.RemoveSubCategory(categoryToAssign);
			}

			if (targetCategory != null)
			{
				targetCategory.AddSubCategory(categoryToAssign);
			}
		}

		private static bool CanAssignToCategory([NotNull] DataQualityCategoryItem item,
		                                        [NotNull] DataQualityCategory candidate)
		{
			if (! candidate.CanContainSubCategories)
			{
				return false;
			}

			DataQualityCategory categoryToAssign = Assert.NotNull(item.GetEntity());

			if (Equals(candidate, categoryToAssign) ||
			    candidate.IsSubCategoryOf(categoryToAssign))
			{
				// exclude categories that would result in cycles
				return false;
			}

			return true;
		}
	}
}
