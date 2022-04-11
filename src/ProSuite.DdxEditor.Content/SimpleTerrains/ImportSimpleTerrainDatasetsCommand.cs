using System.Drawing;
using System.Windows.Forms;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;

namespace ProSuite.DdxEditor.Content.SimpleTerrains
{
	internal class ImportSimpleTerrainDatasetsCommand
		: ExchangeSimpleTerrainDatasetsCommand<SimpleTerrainDatasetsItem>
	{
		private static readonly Image _image = Resources.Import;

		public ImportSimpleTerrainDatasetsCommand([NotNull] SimpleTerrainDatasetsItem item,
		                                          [NotNull]
		                                          IApplicationController applicationController)
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
						Item.ImportSimpleTerrainDatasets(xmlFilePath);
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
