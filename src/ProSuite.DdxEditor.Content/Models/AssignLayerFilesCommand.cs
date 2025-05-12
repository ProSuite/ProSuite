using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Env;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Models
{
	internal class AssignLayerFilesCommand<M> : ItemCommandBase<ModelItemBase<M>>
		where M : DdxModel
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AssignLayerFilesCommand&lt;M&gt;"/> class.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="applicationController">The application controller.</param>
		public AssignLayerFilesCommand([NotNull] ModelItemBase<M> item,
		                               [NotNull] IApplicationController applicationController)
			: base(item, applicationController) { }

		public override string Text => "Assign Missing Layer Files...";

		protected override bool EnabledCore =>
			! ApplicationController.HasPendingChanges && Item.Children.Count > 0;

		public override Image Image => Resources.AssignMissingLayerFiles;

		protected override void ExecuteCore()
		{
			string folderPath;
			using (var form = new AssignLayerFilesForm())
			{
				DialogResult result = UIEnvironment.ShowDialog(
					form, ApplicationController.Window);

				if (result != DialogResult.OK)
				{
					return;
				}

				folderPath = form.FolderPath;
			}

			try
			{
				Item.AssignLayerFiles(folderPath);
			}
			finally
			{
				ApplicationController.ReloadCurrentItem();
			}
		}
	}
}
