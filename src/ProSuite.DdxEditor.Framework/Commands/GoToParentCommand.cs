using System.Drawing;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.Properties;

namespace ProSuite.DdxEditor.Framework.Commands
{
	public class GoToParentCommand : ItemCommandBase<Item>, IGenericItemCommand
	{
		[NotNull] private static readonly Image _image;

		/// <summary>
		/// Initializes the <see cref="GoToParentCommand"/> class.
		/// </summary>
		static GoToParentCommand()
		{
			_image = Resources.GoToParent;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GoToParentCommand"/> class.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="applicationController">The application controller.</param>
		public GoToParentCommand([NotNull] Item item,
		                         [NotNull] IApplicationController applicationController)
			: base(item, applicationController) { }

		public override Image Image => _image;

		public override string Text => "Go Up";

		protected override bool EnabledCore => Item.Parent != null;

		protected override void ExecuteCore()
		{
			ApplicationController.GoToItem(Assert.NotNull(Item.Parent, "parent"));
		}
	}
}
