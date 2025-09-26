using System.Drawing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Models
{
	public class RefreshModelContentCommand<E> : ItemCommandBase<ModelItemBase<E>>
		where E : DdxModel
	{
		[NotNull] private static readonly Image _image;
		private string _toolTip;

		static RefreshModelContentCommand()
		{
			_image = Resources.RefreshModelContent;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RefreshModelContentCommand&lt;E&gt;"/> class.
		/// </summary>
		/// <param name="item">The model item.</param>
		/// <param name="applicationController">The application controller.</param>
		public RefreshModelContentCommand(
			[NotNull] ModelItemBase<E> item,
			[NotNull] IApplicationController applicationController)
			: base(item, applicationController) { }

		public override Image Image => _image;

		public override string Text => "Refresh Model Content";

		public override string ToolTip => _toolTip;

		protected override bool EnabledCore
		{
			get
			{
				bool enabled = Item.CanRefreshModelContent(out string message);

				if (enabled && ApplicationController.HasPendingChanges)
				{
					message = "Please save or discard pending changes first";
					enabled = false;
				}

				_toolTip = enabled
					           ? string.Empty
					           : message;
				return enabled;
			}
		}

		protected override void ExecuteCore()
		{
			ApplicationController.GoToItem(Item);

			try
			{
				Item.RefreshModelContent();
			}
			finally
			{
				ApplicationController.ReloadCurrentItem();
			}
		}
	}
}
