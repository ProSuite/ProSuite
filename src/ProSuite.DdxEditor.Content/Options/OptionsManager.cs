using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Env;
using ProSuite.DdxEditor.Content.Models;
using ProSuite.DdxEditor.Content.QA.Categories;
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
					       _modelBuilder.ListQualityConditionsWithDataset
			       };
		}

		protected override void ApplyOptions(OptionSettings options)
		{
			_modelBuilder.IncludeDeletedModelElements = options.ShowDeletedModelElements;
			_modelBuilder.IncludeQualityConditionsBasedOnDeletedDatasets =
				options.ShowQualityConditionsBasedOnDeletedDatasets;
			_modelBuilder.ListQualityConditionsWithDataset =
				options.ListQualityConditionsWithDataset;
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

				DialogResult result = UIEnvironment.ShowDialog(form, owner);

				if (result != DialogResult.OK)
				{
					return;
				}

				if (form.ShowDeletedModelElements != _modelBuilder.IncludeDeletedModelElements)
				{
					_modelBuilder.IncludeDeletedModelElements = form.ShowDeletedModelElements;

					RefreshDataModels(applicationController);
				}

				var refreshQualityConditions = false;
				if (form.ShowQualityConditionsBasedOnDeletedDatasets !=
				    _modelBuilder.IncludeQualityConditionsBasedOnDeletedDatasets)
				{
					_modelBuilder.IncludeQualityConditionsBasedOnDeletedDatasets =
						form.ShowQualityConditionsBasedOnDeletedDatasets;

					refreshQualityConditions = true;
				}

				if (form.ListQualityConditionsWithDataset !=
				    _modelBuilder.ListQualityConditionsWithDataset)
				{
					_modelBuilder.ListQualityConditionsWithDataset =
						form.ListQualityConditionsWithDataset;

					refreshQualityConditions = true;
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

			if (controller.CurrentItem != null)
			{
				controller.RefreshItem(controller.CurrentItem);
			}

			// TODO: in the treeview, a parent node is incorrectly selected afterwards
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
