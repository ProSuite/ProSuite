using System.Collections.Generic;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Dialogs;
using ProSuite.DdxEditor.Content.QA.Categories;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	public static class QualitySpecificationContainerUtils
	{
		public static bool AssignToCategory(
			[NotNull] ICollection<QualitySpecificationItem> items,
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder,
			[NotNull] IWin32Window owner,
			[CanBeNull] out DataQualityCategory category)
		{
			Assert.ArgumentNotNull(items, nameof(items));
			Assert.ArgumentNotNull(owner, nameof(owner));
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			IList<DataQualityCategory> categories =
				DataQualityCategoryUtils.GetCategories(modelBuilder,
				                                       c => c.CanContainQualitySpecifications);

			const string title = "Assign to Category";
			if (categories.Count == 0)
			{
				Dialog.InfoFormat(owner, title,
				                  "There are no categories which can contain quality specifications");
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
				                   "Do you want to assign {0} quality specification(s) to {1}?",
				                   items.Count, selectedCategory.QualifiedName)))
			{
				category = null;
				return false;
			}

			modelBuilder.UseTransaction(
				delegate
				{
					foreach (QualitySpecificationItem item in items)
					{
						QualitySpecification qualitySpecification =
							Assert.NotNull(item.GetEntity());

						qualitySpecification.Category = selectedCategory.DataQualityCategory;
					}
				});

			category = selectedCategory.DataQualityCategory;
			return true;
		}

		[NotNull]
		public static QualitySpecificationItem CreateCopy(
			[NotNull] QualitySpecificationItem item,
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder,
			[NotNull] IQualitySpecificationContainer container,
			[NotNull] IQualitySpecificationContainerItem containerItem)
		{
			QualitySpecification copy = modelBuilder.ReadOnlyTransaction(
				delegate
				{
					QualitySpecification spec = Assert.NotNull(item.GetEntity());
					copy = spec.CreateCopy();

					SetName(copy, container, containerItem);

					return copy;
				});

			return new QualitySpecificationItem(
				modelBuilder, copy,
				containerItem, modelBuilder.QualitySpecifications);
		}

		private static void SetName(
			[NotNull] QualitySpecification copy,
			[NotNull] IQualitySpecificationContainer container,
			[NotNull] IQualitySpecificationContainerItem containerItem)
		{
			Assert.ArgumentNotNull(copy, nameof(copy));

			string name = copy.Name.Trim();
			var sameName = new List<string>();

			foreach (Item item in container.GetQualitySpecificationItems(containerItem))
			{
				var child = (QualitySpecificationItem) item;

				QualitySpecification sibling = Assert.NotNull(child.GetEntity());

				if (sibling.Name.Trim().StartsWith(name))
				{
					sameName.Add(sibling.Name.Trim());
				}
			}

			string baseName = name;

			var copyNumber = 1;
			while (sameName.Contains(name))
			{
				name = string.Format("{0} ({1})", baseName, copyNumber);
				copyNumber++;
			}

			copy.Name = name;
		}
	}
}
