using System;
using System.IO;
using System.Linq;
using System.Xml;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Editing.Templates;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI;

namespace ProSuite.AGP.Editing.Diagnostics;

public abstract class LoadXmlGeometryButtonBase : Button
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private string _lastOpenedDirectory;

	protected override void OnClick()
	{
		ViewUtils.Try(OnClickCore, _msg);
	}

	private async void OnClickCore()
	{
		string fileName = GetInputFilePath(_lastOpenedDirectory);
		if (string.IsNullOrEmpty(fileName))
		{
			_msg.Debug("No file selected.");
			return;
		}

		DirectoryInfo parentDirectory = Directory.GetParent(fileName);
		_lastOpenedDirectory = parentDirectory?.FullName;

		string xmlString;
		try
		{
			xmlString = await File.ReadAllTextAsync(fileName);
		}
		catch (Exception ex)
		{
			_msg.Error($"Failed to read '{fileName}'.", ex);
			return;
		}

		await QueuedTask.Run(() =>
		{
			EditingTemplate template = EditingTemplate.Current;
			if (template == null)
			{
				_msg.Warn(
					"No active edit template. Select a feature creation template first.");
				return;
			}

			Geometry geometry;
			try
			{
				geometry = GeometryFromXml(xmlString);
			}
			catch (Exception ex)
			{
				_msg.Error($"Failed to parse geometry from '{Path.GetFileName(fileName)}'.",
				           ex);
				return;
			}

			var op = new EditOperation
			         {
				         Name = "Create feature from XML geometry"
			         };
			op.Create(template, geometry);

			if (! op.Execute())
			{
				_msg.Warn($"Edit operation failed: {op.ErrorMessage}");
			}
			else
			{
				_msg.InfoFormat("Created feature from '{0}' using template '{1}'.",
				                Path.GetFileName(fileName), template.Name);
			}
		});
	}

	private static Geometry GeometryFromXml(string xml)
	{
		string rootName;
		using (var reader = XmlReader.Create(new StringReader(xml)))
		{
			reader.MoveToContent();
			rootName = reader.LocalName;
		}

		return rootName switch
		{
			"PointN" => MapPointBuilderEx.FromXml(xml),
			"MultipointN" => MultipointBuilderEx.FromXml(xml),
			"PolylineN" => PolylineBuilderEx.FromXml(xml),
			"PolygonN" => PolygonBuilderEx.FromXml(xml),
			"EnvelopeN" => EnvelopeBuilderEx.FromXml(xml),
			_ => throw new NotSupportedException($"Unsupported geometry type: '{rootName}'")
		};
	}

	[CanBeNull]
	private static string GetInputFilePath([CanBeNull] string initialDirectory)
	{
		var filter = BrowseProjectFilter.GetFilter("esri_browseDialogFilters_xmlFiles");

		var dialog = new OpenItemDialog();
		dialog.BrowseFilter = filter;
		dialog.Title = "Open XML Geometry File";
		if (! string.IsNullOrEmpty(initialDirectory))
		{
			dialog.InitialLocation = initialDirectory;
		}

		return dialog.ShowDialog() == true ? dialog.Items?.FirstOrDefault()?.Path : null;
	}
}
