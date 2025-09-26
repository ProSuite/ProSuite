using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Models
{
	internal class AssignDatasetCategoriesBasedOnFeatureDatasetsCommand<M> :
		ItemCommandBase<ModelItemBase<M>>
		where M : DdxModel
	{
		public AssignDatasetCategoriesBasedOnFeatureDatasetsCommand(
			[NotNull] ModelItemBase<M> item,
			[NotNull] IApplicationController applicationController)
			: base(item, applicationController) { }

		public override string Text => "Assign Dataset Categories From Feature Datasets";

		protected override bool EnabledCore =>
			! ApplicationController.HasPendingChanges &&
			Item.Children.Count > 0;

		protected override void ExecuteCore()
		{
			try
			{
				Item.AssignDatasetCategoriesBasedOnFeatureDatasets();
			}
			finally
			{
				ApplicationController.ReloadCurrentItem();
			}
		}
	}
}
