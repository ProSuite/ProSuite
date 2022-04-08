using System.Drawing;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;

namespace ProSuite.DdxEditor.Content.QA.Categories
{
	public class EditDataQualityCategoryCommand :
		ItemCommandBase<DataQualityCategoryItem>
	{
		private readonly IApplicationController _applicationController;

		public EditDataQualityCategoryCommand(
			[NotNull] DataQualityCategoryItem item,
			[NotNull] IApplicationController applicationController)
			: base(item)
		{
			Assert.ArgumentNotNull(applicationController, nameof(applicationController));

			_applicationController = applicationController;
		}

		public override string Text => "Edit Category...";

		public override Image Image => Resources.Edit;

		protected override bool EnabledCore
		{
			get
			{
				if (_applicationController.HasPendingChanges)
				{
					return false;
				}

				return _applicationController.CurrentItem != Item || ! Item.IsBeingEdited;
			}
		}

		protected override void ExecuteCore()
		{
			Item.EditOnce = true;
			_applicationController.LoadItem(Item);
		}
	}
}
