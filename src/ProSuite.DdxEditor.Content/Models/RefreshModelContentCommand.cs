using System.Drawing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DomainModel.AO.DataModel;

namespace ProSuite.DdxEditor.Content.Models
{
	public class RefreshModelContentCommand<E> : ItemCommandBase<ModelItemBase<E>>
		where E : Model
	{
		[NotNull] private static readonly Image _image;
		[NotNull] private readonly IApplicationController _applicationController;
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
			: base(item)
		{
			_applicationController = applicationController;
		}

		public override Image Image => _image;

		public override string Text => "Refresh Model Content";

		public override string ToolTip => _toolTip;

		protected override bool EnabledCore
		{
			get
			{
				string message;
				bool enabled = Item.CanRefreshModelContent(out message);

				if (enabled && _applicationController.HasPendingChanges)
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
			_applicationController.GoToItem(Item);

			try
			{
				Item.RefreshModelContent();
			}
			finally
			{
				_applicationController.ReloadCurrentItem();
			}
		}
	}
}
