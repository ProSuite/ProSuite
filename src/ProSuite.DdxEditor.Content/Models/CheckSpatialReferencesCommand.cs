using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Models
{
	public class CheckSpatialReferencesCommand<T> : ItemCommandBase<ModelItemBase<T>>
		where T : DdxModel
	{
		private readonly CoreDomainModelItemModelBuilder _modelBuilder;

		public CheckSpatialReferencesCommand(
			[NotNull] ModelItemBase<T> item,
			[NotNull] IApplicationController applicationController,
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder)
			: base(item, applicationController)
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_modelBuilder = modelBuilder;
		}

		#region Overrides of CommandBase

		public override string Text => "Check Spatial References";

		protected override bool EnabledCore
		{
			get
			{
				var model = _modelBuilder.ReadOnlyTransaction<DdxModel>(Item.GetEntity);

				return model.SpatialReferenceDescriptor != null;
			}
		}

		protected override void ExecuteCore()
		{
			Item.CheckSpatialReferences();
		}

		#endregion
	}
}
