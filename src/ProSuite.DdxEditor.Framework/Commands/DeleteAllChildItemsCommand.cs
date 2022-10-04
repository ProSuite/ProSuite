using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Framework.Commands
{
	public class DeleteAllChildItemsCommand : ItemCommandBase<Item>
	{
		[NotNull] private readonly IApplicationController _applicationController;

		/// <summary>
		/// Initializes a new instance of the <see cref="DeleteAllChildItemsCommand"/> class.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="applicationController">The application controller.</param>
		public DeleteAllChildItemsCommand(
			[NotNull] Item item,
			[NotNull] IApplicationController applicationController)
			: base(item)
		{
			Assert.ArgumentNotNull(applicationController, nameof(applicationController));

			_applicationController = applicationController;
		}

		public override string Text => "Delete All...";

		protected override bool EnabledCore => Item.Children.Count > 0 &&
		                                       ! _applicationController.HasPendingChanges;

		protected override void ExecuteCore()
		{
			_applicationController.DeleteItems(Item.Children);
		}
	}
}
