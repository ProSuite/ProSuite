using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Framework.Commands
{
	public class DeleteAllChildItemsCommand : ItemCommandBase<Item>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DeleteAllChildItemsCommand"/> class.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="applicationController">The application controller.</param>
		public DeleteAllChildItemsCommand(
			[NotNull] Item item,
			[NotNull] IApplicationController applicationController)
			: base(item, applicationController) { }

		public override string Text => "Delete All...";

		protected override bool EnabledCore => Item.Children.Count > 0 &&
		                                       ! ApplicationController.HasPendingChanges;

		protected override void ExecuteCore()
		{
			ApplicationController.DeleteItems(Item.Children);
		}
	}
}
