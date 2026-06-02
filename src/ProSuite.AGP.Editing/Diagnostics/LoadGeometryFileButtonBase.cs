using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Editing.Templates;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using Microsoft.Win32;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Geom.Wkb;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI;

namespace ProSuite.AGP.Editing.Diagnostics;

public abstract class LoadGeometryFileButtonBase : Button
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

		bool isWkb = string.Equals(Path.GetExtension(fileName), ".wkb",
		                           StringComparison.OrdinalIgnoreCase);

		string xmlString = null;
		byte[] wkbBytes = null;
		try
		{
			if (isWkb)
			{
				wkbBytes = await File.ReadAllBytesAsync(fileName);
			}
			else
			{
				xmlString = await File.ReadAllTextAsync(fileName);
			}
		}
		catch (Exception ex)
		{
			_msg.Error($"Failed to read '{fileName}'.", ex);
			return;
		}

		Geometry createdGeometry = await QueuedTask.Run(() =>
		{
			EditingTemplate template = EditingTemplate.Current;
			if (template == null)
			{
				_msg.Warn(
					"No active edit template. Select a feature creation template first.");
				return null;
			}

			Geometry geometry;
			try
			{
				geometry = isWkb
					           ? GeometryFromWkb(Assert.NotNull(wkbBytes))
					           : GeometryFromXml(Assert.NotNull(xmlString));
			}
			catch (Exception ex)
			{
				_msg.Error($"Failed to parse geometry from '{Path.GetFileName(fileName)}'.",
				           ex);
				return null;
			}

			var op = new EditOperation
			         {
				         Name = $"Create feature from {(isWkb ? "WKB" : "XML")} geometry"
			         };

			op.Create(template, geometry);

			if (! op.Execute())
			{
				_msg.Warn($"Edit operation failed: {op.ErrorMessage}");
				return null;
			}

			_msg.InfoFormat("Created feature from '{0}' using template '{1}'.",
			                Path.GetFileName(fileName), template.Name);
			return geometry;
		});

		if (createdGeometry != null)
		{
			await MapView.Active.ZoomToAsync(createdGeometry.Extent);
		}
	}

	private static Geometry GeometryFromWkb([NotNull] byte[] wkbBytes,
	                                        [CanBeNull] SpatialReference spatialReference = null)
	{
		const bool assumeWkbPolygonsClockwise = true;
		WkbGeomReader wkbReader = new WkbGeomReader(assumeWkbPolygonsClockwise);

		Stream memoryStream = new MemoryStream(wkbBytes);
		IBoundedXY geom = wkbReader.ReadGeometry(memoryStream, out WkbGeometryType wkbType);

		// NOTE: Polyhedra fail with a COM exception!
		if (wkbType != WkbGeometryType.PolyhedralSurface &&
		    wkbType != WkbGeometryType.MultiSurface)
		{
			return GeometryEngine.Instance.ImportFromWKB(WkbImportFlags.WkbImportDefaults, wkbBytes,
			                                             spatialReference);
		}

		if (geom is Polyhedron polyhedron)
		{
			return GeomConversionUtils.CreateMultipatch(polyhedron, spatialReference);
		}

		if (geom is MultiPolyhedron multiPolyhedron)
		{
			List<Multipatch> multipatches = new List<Multipatch>();

			int partId = 0;
			foreach (Polyhedron part in multiPolyhedron.Polyhedra)
			{
				multipatches.Add(
					GeomConversionUtils.CreateMultipatch(part, spatialReference, partId++));
			}

			return GeometryUtils.Union(multipatches);
		}

		return GeometryEngine.Instance.ImportFromWKB(WkbImportFlags.WkbImportDefaults, wkbBytes,
		                                             spatialReference);
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
			"MultiPatchN" => MultipatchBuilderEx.FromXml(xml),
			"EnvelopeN" => EnvelopeBuilderEx.FromXml(xml),
			_ => throw new NotSupportedException($"Unsupported geometry type: '{rootName}'")
		};
	}

	[CanBeNull]
	private static string GetInputFilePath([CanBeNull] string initialDirectory)
	{
		var dialog = new OpenFileDialog
		             {
			             Title = "Open XML or WKB Geometry File",
			             Filter = "Geometry files (*.xml;*.wkb)|*.xml;*.wkb|All files (*.*)|*.*",
			             DefaultExt = ".xml"
		             };

		if (! string.IsNullOrEmpty(initialDirectory))
		{
			dialog.InitialDirectory = initialDirectory;
		}

		return dialog.ShowDialog() == true ? dialog.FileName : null;
	}
}
