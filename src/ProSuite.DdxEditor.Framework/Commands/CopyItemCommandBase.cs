using System.Drawing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.Properties;

namespace ProSuite.DdxEditor.Framework.Commands
{
	public abstract class CopyItemCommandBase<T> : ItemCommandBase<T> where T : Item
	{
		[CanBeNull] private static readonly Image _image;

		static CopyItemCommandBase()
		{
			_image = Resources.Copy;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CopyItemCommandBase&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="applicationController">The application controller.</param>
		protected CopyItemCommandBase(
			[NotNull] T item,
			[NotNull] IApplicationController applicationController)
			: base(item, applicationController) { }

		public override Image Image => _image;

		public override string Text => "Create Copy...";

		protected override bool EnabledCore => ! ApplicationController.HasPendingChanges;
	}
}
