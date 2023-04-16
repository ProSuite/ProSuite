using System.Drawing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.Properties;

namespace ProSuite.DdxEditor.Framework.Commands
{
	public class DeleteItemCommand : ItemCommandBase<Item>, IDeleteItemCommand
	{
		[NotNull] private static readonly Image _image;

		static DeleteItemCommand()
		{
			_image = Resources.Delete;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DeleteItemCommand"/> class.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="applicationController">The application controller.</param>
		public DeleteItemCommand([NotNull] Item item,
		                         [NotNull] IApplicationController applicationController)
			: base(item, applicationController) { }

		public override Image Image => _image;

		public override string Text => "Delete...";

		protected override bool EnabledCore => ApplicationController.CanDeleteItem(Item);

		protected override void ExecuteCore()
		{
			ApplicationController.DeleteItem(Item);
		}
	}
}
