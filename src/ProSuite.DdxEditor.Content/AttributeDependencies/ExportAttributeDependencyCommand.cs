using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework;

namespace ProSuite.DdxEditor.Content.AttributeDependencies
{
	public class ExportAttributeDependencyCommand :
		ExchangeAttributeDependenciesCommand<AttributeDependencyItem>
	{
		private static readonly Image _image = Resources.Export;

		public ExportAttributeDependencyCommand(
			[NotNull] AttributeDependencyItem item,
			[NotNull] IApplicationController applicationController)
			: base(item, applicationController) { }

		public override Image Image => _image;

		public override string Text => "Export...";

		protected override void ExecuteCore()
		{
			using (var dialog = new SaveFileDialog())
			{
				string xmlFilePath = GetSelectedFileName(dialog);

				if (! string.IsNullOrEmpty(xmlFilePath))
				{
					Item.ExportEntity(xmlFilePath);
				}
			}
		}
	}
}
