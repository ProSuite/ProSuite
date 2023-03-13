using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.QA.InstanceConfig;
using ProSuite.DdxEditor.Content.QA.QSpec;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.QCon
{
	public class AssignQualityConditionsToCategoryCommand :
		ItemsCommandBase<QualityConditionItem>
	{
		[NotNull] private readonly IInstanceConfigurationContainerItem _containerItem;

		/// <summary>
		/// Initializes a new instance of the <see cref="AssignQualityConditionsToCategoryCommand"/> class.
		/// </summary>
		/// <param name="qualityConditionItems">The selected child items.</param>
		/// <param name="containerItem">The container item</param>
		/// <param name="applicationController">The application controller.</param>
		public AssignQualityConditionsToCategoryCommand(
			[NotNull] ICollection<QualityConditionItem> qualityConditionItems,
			[NotNull] IInstanceConfigurationContainerItem containerItem,
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
			if (! _containerItem.AssignToCategory(Items, ApplicationController.Window,
			                                      out DataQualityCategory category))
			{
				return;
			}

			QualityConditionContainerUtils.RefreshQualityConditionAssignmentTarget(category,
				ApplicationController);

			Item gotoItem = Items.FirstOrDefault();

			GoToItem<QualityCondition>(gotoItem);
		}

		private void GoToItem<T>([CanBeNull] Item currentItem) where T : InstanceConfiguration
		{
			if (currentItem == null)
			{
				return;
			}

			if (ApplicationController.GoToItem(currentItem))
			{
				return;
			}

			if (currentItem is EntityItem<T, T> entityItem)
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
