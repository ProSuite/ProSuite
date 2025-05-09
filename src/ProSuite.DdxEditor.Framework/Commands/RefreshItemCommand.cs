using System.Drawing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.Properties;

namespace ProSuite.DdxEditor.Framework.Commands
{
	public class RefreshItemCommand : ItemCommandBase<Item>, IGenericItemCommand
	{
		[NotNull] private static readonly Image _image;

		/// <summary>
		/// Initializes the <see cref="RefreshItemCommand"/> class.
		/// </summary>
		static RefreshItemCommand()
		{
			_image = Resources.Reload;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RefreshItemCommand"/> class.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="applicationController">The application controller.</param>
		public RefreshItemCommand([NotNull] Item item,
		                          [NotNull] IApplicationController applicationController)
			: base(item, applicationController) { }

		public override Image Image => _image;

		public override string Text => "Refresh";

		protected override bool EnabledCore => ! ApplicationController.HasPendingChanges;

		protected override void ExecuteCore()
		{
			ApplicationController.RefreshItem(Item);
		}
	}
}
