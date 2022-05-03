using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DomainModel.AO.DataModel;

namespace ProSuite.DdxEditor.Content.Models
{
	internal class AssignDatasetCategoriesBasedOnFeatureDatasetsCommand<M> :
		ItemCommandBase<ModelItemBase<M>>
		where M : Model
	{
		private readonly IApplicationController _applicationController;

		public AssignDatasetCategoriesBasedOnFeatureDatasetsCommand(
			[NotNull] ModelItemBase<M> item,
			[NotNull] IApplicationController applicationController)
			: base(item)
		{
			Assert.ArgumentNotNull(applicationController, nameof(applicationController));

			_applicationController = applicationController;
		}

		public override string Text => "Assign Dataset Categories From Feature Datasets";

		protected override bool EnabledCore =>
			! _applicationController.HasPendingChanges &&
			Item.Children.Count > 0;

		protected override void ExecuteCore()
		{
			try
			{
				Item.AssignDatasetCategoriesBasedOnFeatureDatasets();
			}
			finally
			{
				_applicationController.ReloadCurrentItem();
			}
		}
	}
}
