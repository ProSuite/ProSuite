using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Env;
using ProSuite.DdxEditor.Content.Models;
using ProSuite.DdxEditor.Content.QA.Categories;
using ProSuite.DdxEditor.Content.QA.InstanceDescriptors;
using ProSuite.DdxEditor.Content.QA.QCon;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.Options;

namespace ProSuite.DdxEditor.Content.Options
{
	public class OptionsManager : OptionsManagerBase<OptionSettings>
	{
		[NotNull] private readonly CoreDomainModelItemModelBuilder _modelBuilder;

		public OptionsManager([NotNull] CoreDomainModelItemModelBuilder modelBuilder,
		                      [NotNull] ISettingsPersister<OptionSettings> persister)
			: base(persister)
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_modelBuilder = modelBuilder;
		}

		protected override OptionSettings GetOptions()
		{
			return new OptionSettings
			       {
				       ShowDeletedModelElements =
					       _modelBuilder.IncludeDeletedModelElements,
				       ShowQualityConditionsBasedOnDeletedDatasets =
					       _modelBuilder.IncludeQualityConditionsBasedOnDeletedDatasets,
				       ListQualityConditionsWithDataset =
					       _modelBuilder.ListQualityConditionsWithDataset,
				       UseClassicConditionSpecification =
					       _modelBuilder.UseClassicConditionSpecification
			       };
		}

		protected override void ApplyOptions(OptionSettings options)
		{
			_modelBuilder.IncludeDeletedModelElements = options.ShowDeletedModelElements;
			_modelBuilder.IncludeQualityConditionsBasedOnDeletedDatasets =
				options.ShowQualityConditionsBasedOnDeletedDatasets;
			_modelBuilder.ListQualityConditionsWithDataset =
				options.ListQualityConditionsWithDataset;
			_modelBuilder.UseClassicConditionSpecification =
				options.UseClassicConditionSpecification;
		}

		public override void ShowOptionsDialog(IApplicationController applicationController,
		                                       IWin32Window owner)
		{
			using (var form = new OptionsForm())
			{
				form.ShowDeletedModelElements = _modelBuilder.IncludeDeletedModelElements;
				form.ShowQualityConditionsBasedOnDeletedDatasets =
					_modelBuilder.IncludeQualityConditionsBasedOnDeletedDatasets;
				form.ListQualityConditionsWithDataset =
					_modelBuilder.ListQualityConditionsWithDataset;
				form.UseClassicConditionSpecification =
					_modelBuilder.UseClassicConditionSpecification;

				DialogResult result = UIEnvironment.ShowDialog(form, owner);

				if (result != DialogResult.OK)
				{
					return;
				}

				bool refreshDataModels =
					form.ShowDeletedModelElements !=
					_modelBuilder.IncludeDeletedModelElements;

				bool refreshAlgorithmDescriptors =
					form.UseClassicConditionSpecification !=
					_modelBuilder.UseClassicConditionSpecification;

				bool refreshQualityConditions =
					refreshAlgorithmDescriptors ||
					form.ShowQualityConditionsBasedOnDeletedDatasets !=
					_modelBuilder.IncludeQualityConditionsBasedOnDeletedDatasets ||
					form.ListQualityConditionsWithDataset !=
					_modelBuilder.ListQualityConditionsWithDataset;

				// The refresh removes the tree nodes of the affected items, including
				// the node of the item that is currently being edited. Pending changes
				// must be settled before that: settling them afterwards - from the
				// selection change that the node removal triggers in the tree view -
				// would delete tree nodes while the tree view is still removing them,
				// which terminates the process (see TreeViewUpdateScope). This is the
				// same precondition that IApplicationController.RefreshItem() enforces.
				if ((refreshDataModels || refreshQualityConditions) &&
				    applicationController.HasPendingChanges &&
				    ! applicationController.PrepareItemSelection(
					    applicationController.CurrentItem))
				{
					// cancelled by the user: leave the options unchanged
					return;
				}

				ApplyOptions(new OptionSettings
				             {
					             ShowDeletedModelElements = form.ShowDeletedModelElements,
					             ShowQualityConditionsBasedOnDeletedDatasets =
						             form.ShowQualityConditionsBasedOnDeletedDatasets,
					             ListQualityConditionsWithDataset =
						             form.ListQualityConditionsWithDataset,
					             UseClassicConditionSpecification =
						             form.UseClassicConditionSpecification
				             });

				if (refreshDataModels)
				{
					RefreshDataModels(applicationController);
				}

				if (refreshAlgorithmDescriptors)
				{
					RefreshAlgorithmDescriptors(applicationController);
				}

				if (refreshQualityConditions)
				{
					RefreshQualityConditions(applicationController);
				}
			}
		}

		private static void RefreshQualityConditions(
			[NotNull] IApplicationController controller)
		{
			Item currentItem = controller.CurrentItem;

			foreach (QualityConditionsItem item in
			         controller.FindItems<QualityConditionsItem>()
			                   .Where(i => i.HasChildrenLoaded))
			{
				if (item != currentItem)
				{
					// just reload the child items
					item.RefreshChildren();
				}
			}

			foreach (DataQualityCategoryItem item in
			         controller.FindItems<DataQualityCategoryItem>()
			                   .Where(i => i.HasChildrenLoaded))
			{
				if (item != currentItem)
				{
					// just reload the child items
					item.RefreshChildren();
				}
			}

			// Refreshing the current item reloads its content control (see
			// InstanceConfigurationControlFactory), which is what makes the
			// "Use classic condition specification" toggle take effect immediately
			// for a currently open condition/transformer/filter item, without a
			// restart. The caller has already settled any pending changes (which
			// RefreshItem() requires); if the user cancelled that prompt, we are not
			// called at all.
			if (currentItem != null && ! controller.HasPendingChanges)
			{
				controller.RefreshItem(currentItem);
			}

			// TODO: in the treeview, a parent node is incorrectly selected afterwards
		}

		private static void RefreshAlgorithmDescriptors([NotNull] IApplicationController controller)
		{
			Item currentItem = controller.CurrentItem;

			foreach (AlgorithmDescriptorsItem item in
			         controller.FindItems<AlgorithmDescriptorsItem>()
			                   .Where(i => i.HasChildrenLoaded))
			{
				if (item != currentItem)
				{
					item.RefreshChildren();
				}
			}
		}

		private static void RefreshDataModels([NotNull] IApplicationController controller)
		{
			var item = controller.FindFirstItem<ModelsItemBase>();

			if (item == null || ! item.HasChildrenLoaded)
			{
				return;
			}

			foreach (Item child in item.Children)
			{
				child.RefreshChildren();
			}
		}
	}
}
