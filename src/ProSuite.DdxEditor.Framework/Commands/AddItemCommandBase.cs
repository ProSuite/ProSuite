using System.Drawing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.Properties;

namespace ProSuite.DdxEditor.Framework.Commands
{
	public abstract class AddItemCommandBase<T> : ItemCommandBase<T> where T : Item
	{
		[CanBeNull] private static Image _image;

		/// <summary>
		/// Initializes a new instance of the <see cref="AddItemCommandBase&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="parentItem">The parent item.</param>
		/// <param name="applicationController">The application controller.</param>
		protected AddItemCommandBase([NotNull] T parentItem,
		                             [NotNull] IApplicationController applicationController)
			: base(parentItem, applicationController) { }

		public override Image Image => _image ?? (_image = Resources.Add);

		protected override bool EnabledCore => ! ApplicationController.HasPendingChanges;
	}
}
