using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Finder;
using ProSuite.DdxEditor.Content.Models;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework;
using ProSuite.DomainModel.AO.DataModel;

namespace ProSuite.DdxEditor.Content.AttributeDependencies
{
	public class ExportAttributeDependenciesCommand :
		ExchangeAttributeDependenciesCommand<AttributeDependenciesItem>
	{
		private static readonly Image _image = Resources.Export;

		private readonly bool _filterByModel;

		public ExportAttributeDependenciesCommand(
			[NotNull] AttributeDependenciesItem item,
			[NotNull] IApplicationController applicationController, bool filterByModel)
			: base(item, applicationController)
		{
			_filterByModel = filterByModel;
		}

		public override Image Image => _image;

		public override string Text => _filterByModel
			                               ? string.Format("Export {0} by Model...", Item.Text)
			                               : string.Format("Export all {0}...", Item.Text);

		public override string ShortText => _filterByModel
			                                    ? "Export by model..."
			                                    : "Export all...";

		protected override void ExecuteCore()
		{
			Model model = null;
			if (_filterByModel)
			{
				model = FindModel(ApplicationController.Window);
				if (model == null)
				{
					return; // user cancelled
				}
			}

			using (var dialog = new SaveFileDialog())
			{
				string xmlFilePath = GetSelectedFileName(dialog);

				if (! string.IsNullOrEmpty(xmlFilePath))
				{
					Item.ExportAttributeDependencies(xmlFilePath, model);
				}
			}
		}

		private Model FindModel(IWin32Window owner)
		{
			IList<ModelTableRow> list = Item.GetModelTableRows();

			IFinder<ModelTableRow> finder = new Finder<ModelTableRow>();

			ModelTableRow selectedRow = finder.ShowDialog(owner, list);

			return selectedRow?.Model;
		}
	}
}
