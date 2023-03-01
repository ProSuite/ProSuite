using System.Drawing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;

namespace ProSuite.DdxEditor.Content.QA.Categories
{
	public class EditDataQualityCategoryCommand : ItemCommandBase<DataQualityCategoryItem>
	{
		public EditDataQualityCategoryCommand(
			[NotNull] DataQualityCategoryItem item,
			[NotNull] IApplicationController applicationController)
			: base(item, applicationController) { }

		public override string Text => "Edit Category...";

		public override Image Image => Resources.Edit;

		protected override bool EnabledCore
		{
			get
			{
				if (ApplicationController.HasPendingChanges)
				{
					return false;
				}

				return ApplicationController.CurrentItem != Item || ! Item.IsBeingEdited;
			}
		}

		protected override void ExecuteCore()
		{
			Item.EditOnce = true;
			ApplicationController.LoadItem(Item);
		}
	}
}
