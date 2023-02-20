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
		[NotNull] private readonly IApplicationController _applicationController;

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
			: base(qualityConditionItems)
		{
			Assert.ArgumentNotNull(applicationController, nameof(applicationController));
			Assert.ArgumentNotNull(containerItem, nameof(containerItem));

			_containerItem = containerItem;
			_applicationController = applicationController;
		}

		public override string Text => "Assign to Category...";

		protected override bool EnabledCore =>
			Items.Count > 0 && ! _applicationController.HasPendingChanges;

		protected override void ExecuteCore()
		{
			Item currentItem = _applicationController.CurrentItem;

			DataQualityCategory category;
			if (! _containerItem.AssignToCategory(Items,
			                                      _applicationController.Window,
			                                      out category))
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

			if (_applicationController.GoToItem(currentItem))
			{
				return;
			}

			var entityItem = currentItem as QualitySpecificationItem;
			if (entityItem != null)
			{
				QualitySpecification entity = _containerItem.GetQualitySpecification(entityItem);
				if (entity != null)
				{
					_applicationController.GoToItem(entity);
				}
			}
		}

		private void RefreshAssignmentTarget([CanBeNull] DataQualityCategory category)
		{
			if (category == null)
			{
				RefreshQualitySpecificationsItem(
					_applicationController.FindFirstItem<QAItem>());
			}
			else
			{
				if (category.CanContainOnlyQualitySpecifications)
				{
					_applicationController.RefreshItem(category);
				}
				else
				{
					RefreshQualitySpecificationsItem(
						_applicationController.FindItem(category));
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
				_applicationController.RefreshItem(childItem);
			}
		}
	}
}
