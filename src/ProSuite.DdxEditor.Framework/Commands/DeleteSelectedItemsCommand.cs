using System.Collections.Generic;
using System.Drawing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.Properties;

namespace ProSuite.DdxEditor.Framework.Commands
{
	public class DeleteSelectedItemsCommand : ItemsCommandBase<Item>
	{
		[NotNull] private static readonly Image _image;

		static DeleteSelectedItemsCommand()
		{
			_image = Resources.Delete;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DeleteSelectedItemsCommand"/> class.
		/// </summary>
		/// <param name="items">The selected child items.</param>
		/// <param name="applicationController">The application controller.</param>
		public DeleteSelectedItemsCommand(
			[NotNull] ICollection<Item> items,
			[NotNull] IApplicationController applicationController) : base(
			items, applicationController) { }

		public override Image Image => _image;

		public override string Text => "Delete...";

		protected override bool EnabledCore =>
			Items.Count > 0 && ! ApplicationController.HasPendingChanges;

		protected override void ExecuteCore()
		{
			ApplicationController.DeleteItems(Items);
		}
	}
}
