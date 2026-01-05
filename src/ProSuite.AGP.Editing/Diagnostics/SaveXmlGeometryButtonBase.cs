using System.IO;
using System.Linq;
using System.Xml;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI;

namespace ProSuite.AGP.Editing.Diagnostics;

public abstract class SaveXmlGeometryButtonBase : Button
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private string _lastOpenedDirectory;

	protected override void OnClick()
	{
		ViewUtils.Try(OnClickCore, _msg);
	}

	private async void OnClickCore()
	{
		var selectedFeatures =
			await QueuedTask.Run(() => SelectionUtils.GetSelectedFeatures(MapView.Active)
			                                         .ToList());

		if (selectedFeatures.Count != 1)
		{
			_msg.Info("Please select a single feature.");
			return;
		}

		string fileName = GetOutputFilePath(_lastOpenedDirectory);
		if (string.IsNullOrEmpty(fileName))
		{
			_msg.Debug("No fileName defined.");
			return;
		}

		DirectoryInfo parentDirectory = Directory.GetParent(fileName);
		_lastOpenedDirectory = parentDirectory?.FullName;

		var feature = selectedFeatures[0];
		var geometry = await QueuedTask.Run(() => feature.GetShape());
		var xmlString = geometry.ToXml();

		var doc = new XmlDocument();
		doc.LoadXml(xmlString);
		doc.Save(fileName);

		string displayValue =
			await QueuedTask.Run(() => GdbObjectUtils.GetDisplayValue(feature));
		_msg.InfoFormat("Feature {0} saved to: {1}", displayValue, fileName);
	}

	[CanBeNull]
	private static string GetOutputFilePath([CanBeNull] string initialDirectory)
	{
		var filter = BrowseProjectFilter.GetFilter("esri_browseDialogFilters_xmlFiles");

		var dialog = new SaveItemDialog();
		dialog.BrowseFilter = filter;
		dialog.DefaultExt = "xml"; // do not include the period!
		dialog.Title = "Save XML File";
		if (! string.IsNullOrEmpty(initialDirectory))
		{
			dialog.InitialLocation = initialDirectory;
		}

		dialog.OverwritePrompt = true;

		return dialog.ShowDialog() == true ? dialog.FilePath : null;
	}
}