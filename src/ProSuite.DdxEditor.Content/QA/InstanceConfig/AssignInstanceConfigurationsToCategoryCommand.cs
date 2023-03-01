using System.Collections.Generic;
using System.Linq;
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
	public class AssignInstanceConfigurationsToCategoryCommand :
		ItemsCommandBase<InstanceConfigurationItem>
	{
		[NotNull] private readonly IInstanceConfigurationContainerItem _containerItem;

		/// <summary>
		/// Initializes a new instance of the <see cref="AssignInstanceConfigurationsToCategoryCommand"/> class.
		/// </summary>
		/// <param name="instanceConfigItems">The selected child items.</param>
		/// <param name="containerItem">The container item</param>
		/// <param name="applicationController">The application controller.</param>
		public AssignInstanceConfigurationsToCategoryCommand(
			[NotNull] ICollection<InstanceConfigurationItem> instanceConfigItems,
			[NotNull] IInstanceConfigurationContainerItem containerItem,
			[NotNull] IApplicationController applicationController)
			: base(instanceConfigItems, applicationController)
		{
			Assert.ArgumentNotNull(containerItem, nameof(containerItem));

			_containerItem = containerItem;
		}

		public override string Text => "Assign to Category...";

		protected override bool EnabledCore =>
			Items.Count > 0 && ! ApplicationController.HasPendingChanges;

		protected override void ExecuteCore()
		{
			if (! _containerItem.AssignToCategory(Items, ApplicationController.Window,
			                                      out DataQualityCategory category))
			{
				return;
			}

			QualityConditionContainerUtils.RefreshAssignmentTarget(
				category, ApplicationController, _containerItem);

			Item gotoItem = Items.FirstOrDefault();

			GoToItem<InstanceConfiguration>(gotoItem);
		}

		private void GoToItem<T>([CanBeNull] Item item) where T : InstanceConfiguration
		{
			if (item == null)
			{
				return;
			}

			if (ApplicationController.GoToItem(item))
			{
				return;
			}

			if (item is EntityItem<T, T> entityItem)
			{
				Entity entity = _containerItem.GetInstanceConfiguration(entityItem);
				if (entity != null)
				{
					ApplicationController.GoToItem(entity);
				}
			}
		}
	}
}
