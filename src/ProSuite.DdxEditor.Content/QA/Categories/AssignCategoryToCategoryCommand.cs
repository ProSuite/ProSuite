using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.Categories
{
	public class AssignCategoryToCategoryCommand :
		ItemCommandBase<DataQualityCategoryItem>
	{
		[NotNull] private readonly IDataQualityCategoryContainerItem _containerItem;

		/// <summary>
		/// Initializes a new instance of the <see cref="AssignCategoryToCategoryCommand"/> class.
		/// </summary>
		/// <param name="categoryItem">The selected child items.</param>
		/// <param name="containerItem">The container item</param>
		/// <param name="applicationController">The application controller.</param>
		public AssignCategoryToCategoryCommand(
			[NotNull] DataQualityCategoryItem categoryItem,
			[NotNull] IDataQualityCategoryContainerItem containerItem,
			[NotNull] IApplicationController applicationController)
			: base(categoryItem, applicationController)
		{
			Assert.ArgumentNotNull(containerItem, nameof(containerItem));

			_containerItem = containerItem;
		}

		public override string Text => "Assign to Category...";

		protected override bool EnabledCore => ! ApplicationController.HasPendingChanges;

		protected override void ExecuteCore()
		{
			Item currentItem = ApplicationController.CurrentItem;

			DataQualityCategory category;
			if (! _containerItem.AssignToCategory(Item, ApplicationController.Window,
			                                      out category))
			{
				return;
			}

			RefreshAssignmentTarget(category);

			GoToItem(currentItem);
		}

		private void RefreshAssignmentTarget([CanBeNull] DataQualityCategory category)
		{
			if (category == null)
			{
				ApplicationController.RefreshFirstItem<QAItem>();
			}
			else
			{
				ApplicationController.RefreshItem(category);
			}
		}

		private void GoToItem([CanBeNull] Item currentItem)
		{
			if (currentItem == null)
			{
				return;
			}

			if (ApplicationController.GoToItem(currentItem))
			{
				return;
			}

			if (currentItem is DataQualityCategoryItem entityItem)
			{
				DataQualityCategory entity = _containerItem.GetDataQualityCategory(entityItem);
				if (entity != null)
				{
					ApplicationController.GoToItem(entity);
				}
			}
		}
	}
}
