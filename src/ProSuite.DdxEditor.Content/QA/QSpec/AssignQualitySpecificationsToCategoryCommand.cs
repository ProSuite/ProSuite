using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	public class AssignQualitySpecificationsToCategoryCommand :
		ItemsCommandBase<QualitySpecificationItem>
	{
		[NotNull] private readonly IQualitySpecificationContainerItem _containerItem;

		/// <summary>
		/// Initializes a new instance of the <see cref="AssignQualitySpecificationsToCategoryCommand"/> class.
		/// </summary>
		/// <param name="qualityConditionItems">The selected child items.</param>
		/// <param name="containerItem">The container item</param>
		/// <param name="applicationController">The application controller.</param>
		public AssignQualitySpecificationsToCategoryCommand(
			[NotNull] ICollection<QualitySpecificationItem> qualityConditionItems,
			[NotNull] IQualitySpecificationContainerItem containerItem,
			[NotNull] IApplicationController applicationController)
			: base(qualityConditionItems, applicationController)
		{
			Assert.ArgumentNotNull(containerItem, nameof(containerItem));

			_containerItem = containerItem;
		}

		public override string Text => "Assign to Category...";

		protected override bool EnabledCore =>
			Items.Count > 0 && ! ApplicationController.HasPendingChanges;

		protected override void ExecuteCore()
		{
			Item currentItem = ApplicationController.CurrentItem;

			if (! _containerItem.AssignToCategory(Items,
			                                      ApplicationController.Window,
			                                      out DataQualityCategory category))
			{
				return;
			}

			RefreshAssignmentTarget(category);

			GoToItem(currentItem);
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

			if (currentItem is QualitySpecificationItem entityItem)
			{
				QualitySpecification entity = _containerItem.GetQualitySpecification(entityItem);
				if (entity != null)
				{
					ApplicationController.GoToItem(entity);
				}
			}
		}

		private void RefreshAssignmentTarget([CanBeNull] DataQualityCategory category)
		{
			if (category == null)
			{
				RefreshQualitySpecificationsItem(
					ApplicationController.FindFirstItem<QAItem>());
			}
			else
			{
				if (category.CanContainOnlyQualitySpecifications)
				{
					ApplicationController.RefreshItem(category);
				}
				else
				{
					RefreshQualitySpecificationsItem(
						ApplicationController.FindItem(category));
				}
			}
		}

		private void RefreshQualitySpecificationsItem([CanBeNull] Item parentItem)
		{
			if (parentItem == null || ! parentItem.HasChildrenLoaded)
			{
				return;
			}

			QualitySpecificationsItem childItem = parentItem.Children
			                                                .OfType<QualitySpecificationsItem>()
			                                                .FirstOrDefault();
			if (childItem != null)
			{
				ApplicationController.RefreshItem(childItem);
			}
		}
	}
}
