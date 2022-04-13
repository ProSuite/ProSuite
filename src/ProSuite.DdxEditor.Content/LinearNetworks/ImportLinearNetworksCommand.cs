using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework;

namespace ProSuite.DdxEditor.Content.LinearNetworks
{
	internal class ImportLinearNetworksCommand : ExchangeLinearNetworksCommand<LinearNetworksItem>
	{
		private static readonly Image _image = Resources.Import;

		public ImportLinearNetworksCommand([NotNull] LinearNetworksItem item,
		                                   [NotNull] IApplicationController applicationController)
			: base(item, applicationController) { }

		public override Image Image => _image;

		public override string Text => string.Format("Import {0}...", Item.Text);

		protected override void ExecuteCore()
		{
			try
			{
				using (var dialog = new OpenFileDialog())
				{
					dialog.Multiselect = false;

					string xmlFilePath = GetSelectedFileName(dialog);

					if (! string.IsNullOrEmpty(xmlFilePath))
					{
						Item.ImportNetworks(xmlFilePath);
					}
				}
			}
			finally
			{
				ApplicationController.ReloadCurrentItem();
			}
		}
	}
}
