using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.QA.QSpec;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.InstanceConfig
{
	public class AssignInstanceConfigurationToCategoryCommand :
		ItemsCommandBase<InstanceConfigurationItem>
	{
		[NotNull] private readonly IInstanceConfigurationContainerItem _containerItem;
		[NotNull] private readonly IApplicationController _applicationController;

		/// <summary>
		/// Initializes a new instance of the <see cref="AssignInstanceConfigurationToCategoryCommand"/> class.
		/// </summary>
		/// <param name="instanceConfigItems">The selected child items.</param>
		/// <param name="containerItem">The container item</param>
		/// <param name="applicationController">The application controller.</param>
		public AssignInstanceConfigurationToCategoryCommand(
			[NotNull] ICollection<InstanceConfigurationItem> instanceConfigItems,
			[NotNull] IInstanceConfigurationContainerItem containerItem,
			[NotNull] IApplicationController applicationController)
			: base(instanceConfigItems)
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
			if (! _containerItem.AssignToCategory(Items, _applicationController.Window,
			                                      out category))
			{
				return;
			}

			QualityConditionContainerUtils.RefreshAssignmentTarget(
				category, _applicationController, _containerItem);

			GoToItem<InstanceConfiguration>(currentItem);
		}

		private void GoToItem<T>([CanBeNull] Item item) where T : InstanceConfiguration
		{
			if (item == null)
			{
				return;
			}

			if (_applicationController.GoToItem(item))
			{
				return;
			}

			var entityItem = item as EntityItem<T, T>;

			if (entityItem != null)
			{
				Entity entity = _containerItem.GetInstanceConfiguration(entityItem);
				if (entity != null)
				{
					_applicationController.GoToItem(entity);
				}
			}
		}
	}
}
