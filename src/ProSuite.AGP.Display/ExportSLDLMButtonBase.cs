using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Collections;
using ProSuite.Commons.IO;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Display;

/// <summary>
/// Export SLD/LM config from the active map (or a group layer)
/// to an XML or CSV file. We use the term "Symbol Level" to refer
/// to what ArcGIS Pro (and the SDK) knows as "Symbol Layer" -- we
/// do so to avoid confusion with map layers.
/// </summary>
public abstract class ExportSLDLMButtonBase : ButtonCommandBase
{
	// Remembered options (in session only):
	private readonly ExportSLDLMOptions _options = new();
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	protected override async Task<bool> OnClickAsyncCore()
	{
		var map = MapView.Active?.Map;
		if (map is null) return false;

		var owner = Application.Current.MainWindow;

		_options.SetMap(map);
		_options.RestoreOptions();

		var dialog = new ExportSLDLMDialog(_options) { Owner = owner };
		var result = dialog.ShowDialog();
		if (result != true) return true; // user canceled
		_options.RememberOptions();

		ILayerContainer container = _options.GroupLayerItem?.GroupLayer ?? (ILayerContainer) map;

		bool includeMasking = _options.IncludeMaskingInfo;
		bool extraMasking = _options.ExtraMaskingInfo;

		var config = await QueuedTask.Run(() => CollectConfig(map, container, includeMasking, extraMasking));

		if (! string.IsNullOrWhiteSpace(_options.Remark))
		{
			config.AddFirst(new XElement("Remark", _options.Remark.Trim()));
		}

		if (string.IsNullOrWhiteSpace(_options.ConfigFilePath))
		{
			Gateway.ShowError("Must specify an export file path", _msg);
			return false;
		}

		var extension = Path.GetExtension(_options.ConfigFilePath).ToLowerInvariant();
		if (string.IsNullOrEmpty(extension)) extension = ".xml"; // default to XML

		switch (extension)
		{
			case ".xml":
				config.Save(_options.ConfigFilePath);
				break;
			case ".csv":
				SaveAsCSV(config, _options.ConfigFilePath);
				break;
			default:
				throw new NotSupportedException(
					$"Export to file of type {extension} is not supported");
		}

		if (owner is not null)
		{
			var successDialog = new ExportDoneDialog
			                    {
									Owner = owner,
				                    Heading = "SLD/LM configuration exported to:",
				                    FilePath = _options.ConfigFilePath
			                    };

			successDialog.ShowDialog();
		}

		return true;
	}

	#region Transform XML to CSV and save

	/// <remarks>
	/// Difference from CSV written by the older Python export tool:
	/// field SymbolMatch (the field value(s) of an UVR or "*") instead
	/// of just numbering the symbols; section "Symbol Levels" instead
	/// of "Symbol Layers" (for consistency with XML export, where the
	/// old ArcMap term "symbol level" is used to avoid confusion between
	/// layer and symbol layer); "LevelName" instead of "ZGroup" and
	/// a separate SymbolLevel (i.e., level number) field.
	/// </remarks>
	private static void SaveAsCSV(XElement config, string filePath)
	{
		if (config is null)
			throw new ArgumentNullException(nameof(config));

		var mapName = (string) config.Attribute("map");
		var groupName = (string) config.Attribute("groupLayer");

		var order = config.Element("DrawingOrder") ??
		            throw new FormatException("Missing required element <DrawingOrder>");
		var levels = config.Element("SymbolLevels") ??
		             throw new FormatException("Missing required element <SymbolLevels>");
		var masking = config.Element("MaskingLayers"); // optional (can be null)
		var omitMasks = masking is null;

		// should already be in a defined order
		var maskUriList = masking?.Elements("MaskingLayer").Select(m => (string) m.Attribute("uri")).ToList();
		const string sep = ", ";

		using var stream = File.Open(filePath, FileMode.Create); // create or overwrite
		using var writer = new StreamWriter(stream, Encoding.UTF8);
		using var csv = new CsvWriter(writer, ';');

		csv.WriteLine($"# SLD/LM Configuration: map={mapName}, group={groupName ?? "(entire map)"}");

		csv.WriteLine(string.Empty);
		csv.WriteRecord("# Section", "Drawing Order (top to bottom)");
		if (omitMasks)
			csv.WriteRecord("# order", "LayerURI", "ParentName", "LayerName", "LevelName");
		else
			csv.WriteRecord("# order", "LayerURI", "ParentName", "LayerName", "LevelName", "MaskedBy", "MaskNames");
		foreach (var layer in order.Elements("Layer"))
		{
			var layerName = (string) layer.Attribute("name");
			var parentName = (string) layer.Attribute("parent");
			var layerUri = (string) layer.Attribute("uri");

			// two cases: (1) Levels with optional MaskedBy, or (2) no Levels and optional MaskedBy
			if (layer.Elements("Level").Any())
			{
				foreach (var level in layer.Elements("Level"))
				{
					var levelName = (string) level.Attribute("name");

					if (omitMasks)
					{
						csv.WriteRecord("order", layerUri, parentName, layerName, levelName);
					}
					else
					{
						var maskedBy = string.Join(sep, level.Elements("MaskedBy").Select(m => GetMaskLayerKey((string)m.Attribute("uri"), maskUriList)));
						var maskNames = Utils.JoinInfix(sep, level.Elements("MaskedBy").Select(m => (string)m.Attribute("uri")));

						csv.WriteRecord("order", layerUri, parentName, layerName, levelName, maskedBy, maskNames);
					}
				}
			}
			else if (! omitMasks)
			{
				var maskedBy = string.Join(sep, layer.Elements("MaskedBy").Select(m => GetMaskLayerKey((string) m.Attribute("uri"), maskUriList)));
				var maskNames = Utils.JoinInfix(sep, layer.Elements("MaskedBy").Select(m => (string)m.Attribute("uri")));

				csv.WriteRecord("order", layerUri, parentName, layerName, string.Empty, maskedBy, maskNames);
			}
		}

		csv.WriteLine(string.Empty);
		csv.WriteRecord("# Section", "Symbol Levels");
		if (omitMasks)
			csv.WriteRecord("# symlyr", "LayerURI", "ParentName", "LayerName", "SymbolMatch", "SymbolLabel", "SymbolLevel", "LevelName");
		else
			csv.WriteRecord("# symlyr", "LayerURI", "ParentName", "LayerName", "SymbolMatch", "SymbolLabel", "SymbolLevel", "LevelName", "MaskedBy", "MaskNames");
		// levels in XML: Layer / Symbol / Level / MaskedBy
		foreach (var layer in levels.Elements("Layer"))
		{
			var layerName = (string) layer.Attribute("name");
			var layerParent = (string) layer.Attribute("parent");
			var layerUri = (string) layer.Attribute("uri");

			foreach (var symbol in layer.Elements("Symbol"))
			{
				var match = (string) symbol.Attribute("match");
				var label = (string) symbol.Attribute("label");

				int symbolLevel = 0;
				foreach (var level in symbol.Elements("Level"))
				{
					symbolLevel += 1;
					var levelName = (string) level.Attribute("name");

					if (omitMasks)
					{
						csv.WriteRecord("symlyr", layerUri, layerParent, layerName, match, label, symbolLevel, levelName);
					}
					else
					{
						var masks = level.Elements("MaskedBy").ToList();
						var maskKeys = string.Join(sep, masks.Select(m => GetMaskLayerKey((string)m.Attribute("uri"), maskUriList)));
						var maskNames = Utils.JoinInfix(sep, masks.Select(m => $"{m.Attribute("parent")}\\{m.Attribute("name")}"));

						csv.WriteRecord("symlyr", layerUri, layerParent, layerName, match, label, symbolLevel, levelName, maskKeys, maskNames);
					}
				}
			}
		}

		if (masking is not null)
		{
			csv.WriteLine(string.Empty);
			csv.WriteRecord("# Section", "Masking Layers");
			csv.WriteRecord("# masking", "LayerURI", "ParentName", "LayerName", "MaskingLayerKey");
			foreach (var maskingLayer in masking.Elements("MaskingLayer"))
			{
				var uri = (string) maskingLayer.Attribute("uri");
				var parent = (string) maskingLayer.Attribute("parent");
				var name = (string) maskingLayer.Attribute("name");
				csv.WriteRecord("masking", uri, parent, name, GetMaskLayerKey(uri, maskUriList));
			}
		}

		csv.WriteLine(string.Empty);
		csv.WriteRecord("# Section", "Notes");
		csv.WriteRecord("# note", "Informational text (not used on import)");
		if (masking is not null)
		{
			csv.WriteRecord("note", "The key in the MaskedBy fields is only to be used within this file; it is not maintained in any way in ArcGIS Pro");
			csv.WriteRecord("note", "The MaskedBy field of the order records is authoritative; the MaskedBy field of the symlyr records should agree but is ignored on import");
			csv.WriteRecord("note", "MaskNames is purely informational and ignored on import (it's the longest common infix of the mask layer URIs)");
		}
		csv.WriteRecord("note", "LayerURI identifies a layer within a map; LayerName and ParentName help with readability (ignored on import)");
		csv.WriteRecord("note", "We recommend short but descriptive names (no blanks, no punctuation) for ZGroups (level names)");
		csv.WriteRecord("note", "Level names must be unique per map (a limit of the import/export tools; for ArcGIS they must be unique per controlling layer)");
	}

	private static string GetMaskLayerKey(string maskLayerUri, IList<string> maskLayerList)
	{
		if (maskLayerList is null) return "M?";
		var index = maskLayerList.IndexOf(maskLayerUri);
		return index < 0 ? "M?" : $"M{1 + index}";
	}

	#endregion

	// <SLDLM map="MapName" [groupLayer="LayerName"] [omitMasks="true"]>
	//   <Remark>...</Remark>                          Optional remark (any text)
	//   <DrawingOrder>
	//     <Layer name="" parent="" uri="" />          Layer w/o SymLyrs, not masked
	//     <Layer name="" parent="" uri="">            Layer w/o SymLyrs, masked
	//       <MaskedBy name="" parent="" uri="" />
	//     </Layer>
	//     <Layer name="" parent="" uri="">            Layer with SymLyrs, some masked
	//       <Level name="" />
	//       <Level name="">
	//         <MaskedBy name="" parent="" uri="" />   Zero, one, or many layer masks
	//       </Level>
	//     </Layer>
	//   </DrawingOrder>
	//   <SymbolLevels>
	//     <Layer name="" parent="" uri="">
	//       <Renderer type="simple|unique" fields="(for unique only)" />
	//       <Symbol match="" label="" type="">
	//         <Level name="" type="" />               First level of this symbol
	//         <Level name="" type="">                 Second level, masked
	//           <MaskedBy name="" parent="" uri="" /> Masking info here is informational
	//         </Level>
	//       </Symbol>
	//     </Layer>
	//   </SymbolLevels>
	//   <MaskingLayers>                               This section is informational (ignored on import)
	//     <MaskingLayer name="" parent="" uri="">
	//       <MaskedLevel level="" name="" parent="" />
	//       <MaskedLayer name="" parent="" zgroups="foo, bar, baz" />
	//     </MaskingLayer>
	//   </MaskingLayers>
	// </SLDLM>

	public static XElement CollectConfig(Map map, ILayerContainer container = null, bool includeMasking = true, bool extraMasking = false)
	{
		extraMasking &= includeMasking;

		var order = GetDrawingOrder(container ?? map, map, includeMasking);
		var symlyrs = GetSymbolLevels(container ?? map, map, extraMasking);
		var masking = extraMasking ? null : GetMaskingLayers(order);

		var mapAttr = new XAttribute("map", map.Name ?? string.Empty);
		var groupLayerAttr = container is Layer group
			                     ? new XAttribute("groupLayer", group.Name ?? string.Empty)
			                     : null;

		var includeMaskingAttr = new XAttribute("includeMasking", includeMasking);
		var extraMaskingAttr = new XAttribute("extraMasking", extraMasking);

		return new XElement("SLDLM",
		                    mapAttr, groupLayerAttr, includeMaskingAttr, extraMaskingAttr,
		                    order, symlyrs, masking);
	}

	private static IDictionary<string, Layer> GetLayersByURI(ILayerContainer container)
	{
		var result = new Dictionary<string, Layer>();
		foreach (var layer in container.GetLayersAsFlattenedList())
		{
			if (string.IsNullOrEmpty(layer.URI))
				continue; // anno sublyrs?
			result.Add(layer.URI, layer);
		}

		return result;
	}

	private static XElement GetDrawingOrder(ILayerContainer container, Map map, bool includeMasking = false)
	{
		var allLayers = GetLayersByURI(map);

		var result = new XElement("DrawingOrder");

		// Special case if container is a Group Layer with SLD:
		if (container is GroupLayer topGroupLayer)
		{
			var sld = GetSLD(topGroupLayer);
			if (sld.UseSLD)
			{
				var lm = GetLM(topGroupLayer, allLayers);

				var layerElement = MakeLayer(topGroupLayer);
				result.Add(layerElement);

				foreach (var level in sld.SymbolLayers)
				{
					var levelElement = MakeLevel(level, null);
					MaskedBy(levelElement, includeMasking ? lm.GetForLevel(level) : null);
					layerElement.Add(levelElement);
				}

				return result;
			}
		}

		// ASSUME GetLayersAsFlattenedList() yields the layers of the layer tree in pre-order!
		var layersInPreOrder = container.GetLayersAsFlattenedList();

		Layer controllingLayer = null;
		foreach (var layer in layersInPreOrder)
		{
			if (! layer.IsNestedLayer(controllingLayer))
				controllingLayer = null;
			if (controllingLayer != null)
				continue;
			if (layer is FeatureLayer featureLayer)
			{
				var sld = GetSLD(featureLayer);
				var lm = GetLM(featureLayer, allLayers);
				if (sld.UseSLD)
				{
					var layerElement = MakeLayer(featureLayer);
					result.Add(layerElement);

					foreach (var level in sld.SymbolLayers)
					{
						var levelElement = MakeLevel(level, null);
						MaskedBy(levelElement, includeMasking ? lm.GetForLevel(level) : null);
						layerElement.Add(levelElement);
					}
				}
				else
				{
					// A feature layer without SLD (but it may have LM)
					var layerElement = MakeLayer(featureLayer);
					MaskedBy(layerElement, includeMasking ? lm.GetAll() : null);
					result.Add(layerElement);
				}
			}
			else if (layer is GroupLayer groupLayer)
			{
				var sld = GetSLD(groupLayer);
				if (sld.UseSLD)
				{
					var lm = GetLM(groupLayer, allLayers);

					var layerElement = MakeLayer(groupLayer);
					result.Add(layerElement);

					foreach (var level in sld.SymbolLayers)
					{
						var levelElement = MakeLevel(level, null);
						MaskedBy(levelElement, includeMasking ? lm.GetForLevel(level) : null);
						layerElement.Add(levelElement);
					}

					// A group layer with SLD becomes the "controlling layer"
					// that overrides the default drawing order of nested layers
					controllingLayer = layer;
				}
				// else: nothing to do for this group layer
			}
		}

		return result;
	}

	private static XElement GetMaskingLayers(XElement drawingOrder)
	{
		if (drawingOrder is null)
			throw new ArgumentNullException(nameof(drawingOrder));

		// Derive Masking Layers from the given drawing order, that is, create:
		//
		// <MaskingLayers>
		//   <MaskingLayer name parent>
		//     <MaskedLevel level name parent> or
		//     <MaskedLayer levels name parent>
		//   ...
        //
		// given:
		//
		// <DrawingOrder>
		//   <Layer name="" parent="" uri="" />
		//   <Layer name="" parent="" uri="">
		//     <MaskedBy name="" parent="" uri="" />
		//   </Layer>
		//   <Layer name="" parent="" uri="">
		//     <Level name="" />
		//     <Level name="">
		//       <MaskedBy name="" parent="" uri="" />
		//     </Level>
		//   </Layer>
		//   ...


		var masking = new Dictionary<string, XElement>(); // MaskingLayer by URI

		foreach (var layerElem in drawingOrder.Elements("Layer"))
		{
			var layerName = (string) layerElem.Attribute("name");
			var layerParent = (string) layerElem.Attribute("parent");

			foreach (var layerMaskingElem in layerElem.Elements("MaskedBy"))
			{
				var uri = (string) layerMaskingElem.Attribute("uri");
				if (uri is null) continue; // most illogical

				if (! masking.TryGetValue(uri, out var maskingElem))
				{
					maskingElem = MakeMaskingLayer(layerMaskingElem);
					masking.Add(uri, maskingElem);
				}

				var maskedElem = new XElement("MaskedLayer");
				maskedElem.Add(new XAttribute("name", layerName ?? string.Empty));
				if (! string.IsNullOrEmpty(layerParent))
					maskedElem.Add(new XAttribute("parent", layerParent));
				maskingElem.Add(maskedElem);
			}

			foreach (var levelElem in layerElem.Elements("Level"))
			{
				var levelName = (string) levelElem.Attribute("name");

				foreach (var levelMaskingElem in levelElem.Elements("MaskedBy"))
				{
					var uri = (string) levelMaskingElem.Attribute("uri");
					if (uri is null) continue; // most illogical

					if (! masking.TryGetValue(uri, out var maskingElem))
					{
						maskingElem = MakeMaskingLayer(levelMaskingElem);
						masking.Add(uri, maskingElem);
					}

					var maskedElem = new XElement("MaskedLevel");
					maskedElem.Add(new XAttribute("level", levelName ?? string.Empty));
					maskedElem.Add(new XAttribute("name", layerName ?? string.Empty));
					if (! string.IsNullOrEmpty(layerParent))
						maskedElem.Add(new XAttribute("parent", layerParent));
					maskingElem.Add(maskedElem);
				}
			}
		}

		// Establish a canonical order for easy diff'ability

		var maskingElems = masking.Values
		                          .OrderBy(x => (string) x.Attribute("parent"))
		                          .ThenBy(x => (string) x.Attribute("name"));

		var comparer = new NumericStringComparer();
		foreach (var maskingElem in maskingElems)
		{
			var ordered = maskingElem
			              .Elements().OrderBy(e => (string) e.Attribute("parent"))
			              .ThenBy(e => (string) e.Attribute("name"))
			              .ThenBy(e => (string) e.Attribute("level"), comparer);

			maskingElem.ReplaceNodes(ordered);
		}

		var result = new XElement("MaskingLayers");
		result.Add(new XAttribute("note", "ignored on import"));
		result.Add(maskingElems);

		return result;
	}

	private static XElement GetSymbolLevels(ILayerContainer container, Map map, bool includeMasking = true)
	{
		var allLayers = GetLayersByURI(map);
		var result = new XElement("SymbolLevels");

		//   <SymbolLayers>
		//     <Layer name="" parent="" uri="">
		//       <Renderer type="simple|unique" fields="(for unique only)" />
		//       <Symbol match="" label="" type="">
		//         <Level name="" type="" />
		//         <Level name="" type="">
		//           <MaskedBy name="" parent="" uri="" />    ignored on import
		//         </Level>
		//       </Symbol>
		//     </Layer>
		//   </SymbolLayers>

		foreach (var layer in container.GetLayersAsFlattenedList())
		{
			if (layer is FeatureLayer featureLayer)
			{
				var cim = featureLayer.GetDefinition();
				if (cim is CIMGeoFeatureLayerBase gfl)
				{
					var layerXml = MakeLayer(layer);

					layerXml.Add(MakeRendererInfo(gfl.Renderer));

					var lm = includeMasking ? GetLM(featureLayer, allLayers) : null;
					var symbols = GetRendererSymbols(gfl.Renderer, lm);
					layerXml.Add(symbols);

					result.Add(layerXml);
				}
			}
		}

		return result;
	}

	private static IEnumerable<XElement> GetRendererSymbols(CIMRenderer renderer, MaskLayers maskLayers = null)
	{
		if (renderer is null)
		{
			yield break;
		}

		const bool ignoredOnImport = true;

		if (renderer is CIMUniqueValueRenderer unique)
		{
			// the default symbol
			var symbol = unique.DefaultSymbol?.Symbol;
			var xml = MakeSymbol("*", unique.DefaultLabel, symbol);
			var levels = GetSymbolLayerNames(symbol);
			foreach (var level in levels)
			{
				var levelElement = MakeLevel(level.Name, level.Type);
				var maskedBy = maskLayers?.GetForLevel(level.Name);
				MaskedBy(levelElement, maskedBy, ignoredOnImport);
				xml.Add(levelElement);
			}

			yield return xml;

			// per class symbols
			foreach (var clazz in Utils.GetUniqueValueClasses(unique))
			{
				symbol = clazz.Symbol?.Symbol;
				var discriminator = Utils.FormatDiscriminatorValue(clazz.Values);
				xml = MakeSymbol(discriminator, clazz.Label, symbol);
				levels = GetSymbolLayerNames(symbol);
				foreach (var level in levels)
				{
					var levelElement = MakeLevel(level.Name, level.Type);
					var maskedBy = maskLayers?.GetForLevel(level.Name);
					MaskedBy(levelElement, maskedBy, ignoredOnImport);
					xml.Add(levelElement);
				}
				yield return xml;
			}
		}
		else if (renderer is CIMSimpleRenderer simple)
		{
			var symbol = simple.Symbol?.Symbol;
			var xml = MakeSymbol("*", simple.Label, symbol);
			var levels = GetSymbolLayerNames(symbol);
			foreach (var level in levels)
			{
				var levelElement = MakeLevel(level.Name, level.Type);
				var maskedBy = maskLayers?.GetForLevel(level.Name);
				MaskedBy(levelElement, maskedBy, ignoredOnImport);
				xml.Add(levelElement);
			}
			yield return xml;
		}
		else
		{
			throw new NotSupportedException($"Renderer type not supported: {renderer.GetType().Name}");
		}
	}

	private static SymbolLevel[] GetSymbolLayerNames(CIMSymbol cim)
	{
		if (cim is CIMMultiLayerSymbol { SymbolLayers.Length: > 0 } ml)
		{
			return ml.SymbolLayers
			         .Select(sl => new SymbolLevel(sl.Name, Utils.GetPrettyTypeName(sl)))
			         .ToArray();
		}

		if (cim is CIMTextSymbol)
		{
			return new[] { new SymbolLevel(string.Empty, "(text)") };
		}

		return new[] { new SymbolLevel(string.Empty, Utils.GetPrettyTypeName(cim)) };
	}

	private static void MaskedBy(XElement element, IEnumerable<Layer> maskedBy,
	                             bool ignoredOnImport = false)
	{
		if (element is not null && maskedBy is not null)
		{
			foreach (var maskingLayer in maskedBy)
			{
				var maskedByElement = MakeMaskedBy(maskingLayer);

				string value = ignoredOnImport ? "ignored on import" : null;
				maskedByElement.SetAttributeValue("info", value);

				element.Add(maskedByElement);
			}
		}
	}

	private static XElement MakeMaskedBy(Layer maskingLayer)
	{
		return MakeLayer(maskingLayer, "MaskedBy");
	}

	private static XElement MakeLayer(Layer layer, string elementName)
	{
		if (layer is null)
			throw new ArgumentNullException(nameof(layer));
		if (string.IsNullOrEmpty(elementName))
			throw new ArgumentNullException(nameof(elementName));

		var result = new XElement(elementName);

		result.Add(new XAttribute("name", layer.Name ?? string.Empty));

		var parentName = layer.Parent is Layer parent ? parent.Name ?? string.Empty : null;
		if (parentName is not null)
		{
			result.Add(new XAttribute("parent", parentName));
		}

		result.Add(new XAttribute("uri", layer.URI ?? string.Empty));

		return result;
	}

	private static XElement MakeLayer(Layer layer/*, IEnumerable<Layer> maskedBy = null*/)
	{
		var result = MakeLayer(layer, "Layer");

		//if (maskedBy is not null)
		//{
		//	foreach (var maskingLayer in maskedBy)
		//	{
		//		result.Add(MakeMaskedBy(maskingLayer));
		//	}
		//}

		return result;
	}

	private static XElement MakeLevel(string name, string type/*, IEnumerable<Layer> maskedBy = null*/)
	{
		var result = new XElement("Level");

		result.Add(new XAttribute("name", name ?? string.Empty));

		if (! string.IsNullOrEmpty(type))
		{
			result.Add(new XAttribute("type", type));
		}

		//if (maskedBy is not null)
		//{
		//	foreach (var maskingLayer in maskedBy)
		//	{
		//		result.Add(MakeMaskedBy(maskingLayer));
		//	}
		//}

		return result;
	}

	private static XElement MakeSymbol(string match, string label, CIMSymbol symbol)
	{
		var result = new XElement("Symbol");

		result.Add(new XAttribute("match", match ?? "*"));
		result.Add(new XAttribute("label", label ?? string.Empty));

		if (symbol is not null)
		{
			result.Add(new XAttribute("type", Utils.GetPrettyTypeName(symbol)));
		}

		return result;
	}

	private static XElement MakeRendererInfo(CIMRenderer renderer)
	{
		var result = new XElement("Renderer");

		if (renderer is null)
		{
			result.Add(new XAttribute("type", "null"));
		}
		else if (renderer is CIMUniqueValueRenderer unique)
		{
			result.Add(new XAttribute("type", "unique"));

			var fields = unique.Fields ?? Array.Empty<string>();
			result.Add(new XAttribute("fields", string.Join(",", fields)));

			// Note: there cannot be Alternate Symbols for the DefaultSymbol
			var hasAlternateSymbols = Utils.GetUniqueValueClasses(unique)
			                            .Any(c => c.AlternateSymbols is { Length: > 0 });
			
			result.Add(new XAttribute("alternateSymbols",
			                          hasAlternateSymbols ? "IGNORED" : "none"));
		}
		else if (renderer is CIMSimpleRenderer simple)
		{
			result.Add(new XAttribute("type", "simple"));
			bool hasAlternateSymbols = simple.AlternateSymbols is { Length: > 0 };
			result.Add(new XAttribute("alternateSymbols",
			                          hasAlternateSymbols ? "IGNORED" : "none"));
		}
		else
		{
			var type = renderer.GetType().Name;
			result.Add(new XAttribute("type", type));
		}

		return result;
	}

	private static XElement MakeMaskingLayer(XElement maskedBy)
	{
		if (maskedBy is null)
			throw new ArgumentNullException(nameof(maskedBy));

		var result = new XElement("MaskingLayer");

		var name = (string) maskedBy.Attribute("name");
		result.Add(new XAttribute("name", name ?? string.Empty));

		var parent = (string)maskedBy.Attribute("parent");
		if (!string.IsNullOrEmpty(parent))
		{
			result.Add(new XAttribute("parent", parent));
		}

		var uri = (string) maskedBy.Attribute("uri");
		result.Add(new XAttribute("uri", uri ?? string.Empty));

		return result;
	}

	private static SymbolLayerDrawing GetSLD(Layer layer)
	{
		CIMSymbolLayerDrawing sld = null;
		if (layer is FeatureLayer featureLayer)
		{
			var cim = (CIMGeoFeatureLayerBase) featureLayer.GetDefinition();
			sld = cim.SymbolLayerDrawing;
		}
		else if (layer is GroupLayer groupLayer)
		{
			var cim = (CIMGroupLayer) groupLayer.GetDefinition();
			sld = cim.SymbolLayerDrawing;
		}
		// else: only FeatureLayer and GroupLayer may have SLD

		if (sld?.SymbolLayers is null)
			return new SymbolLayerDrawing(false);

		var levels = sld.SymbolLayers.Select(sid => sid.SymbolLayerName);
		return new SymbolLayerDrawing(sld.UseSymbolLayerDrawing, levels);
	}

	private static MaskLayers GetLM(BasicFeatureLayer layer, IDictionary<string, Layer> allLayers)
	{
		var cim = layer.GetDefinition();
		var maskingLayers = cim?.LayerMasks;
		if (maskingLayers is null) return MaskLayers.None;
		if (maskingLayers.Length <= 0) return MaskLayers.None;
		
		if (cim is CIMGeoFeatureLayerBase { MaskedSymbolLayers: { } msl })
		{
			if (maskingLayers.Length != msl.Length)
				throw new InvalidOperationException(
					"LayerMasks and MaskedSymbolLayers both exist but do not have " +
					$"the same length (expect parallel arrays) for layer {layer.Name}");

			var list = new List<MaskLayer>();
			for (int i = 0; i < maskingLayers.Length; i++)
			{
				var uri = maskingLayers[i];
				var maskLayer = new MaskLayer(uri);
				maskLayer.AddSymbolLayers(msl[i].SymbolLayers.Select(sl => sl.SymbolLayerName));
				list.Add(maskLayer);
			}
			return new MaskLayers(list, allLayers);
		}

		return new MaskLayers(maskingLayers, allLayers);
	}

	private static MaskLayers GetLM(GroupLayer groupLayer, IDictionary<string, Layer> allLayers)
	{
		// The CIM's LayerMasks property also exists on the GroupLayer
		// (being the union of the contained layers' LayerMasks properties),
		// but the MaskedSymbolLayers property only exists on the feature layers!

		var result = new Dictionary<string, MaskLayer>();
		var nestedLayers = groupLayer.GetLayersAsFlattenedList();
		foreach (var nestedLayer in nestedLayers.OfType<BasicFeatureLayer>())
		{
			var cim = nestedLayer.GetDefinition();
			var maskingLayers = cim.LayerMasks;
			if (maskingLayers is null) continue; // no LM
			if (maskingLayers.Length <= 0) continue; // no LM

			if (cim is CIMGeoFeatureLayerBase { MaskedSymbolLayers: { } msl })
			{
				if (maskingLayers.Length != msl.Length)
					throw new InvalidOperationException(
						"LayerMasks and MaskedSymbolLayers both exist but do not have " +
						$"the same length (expect parallel arrays) for layer {nestedLayer.Name} " +
						$"(in group layer {groupLayer.Name})");

				for (int i = 0; i < maskingLayers.Length; i++)
				{
					var uri = maskingLayers[i];
					var zGroups = msl[i].SymbolLayers.Select(sl => sl.SymbolLayerName);
					if (result.TryGetValue(uri, out var maskLayer))
					{
						maskLayer.AddSymbolLayers(zGroups);
					}
					else
					{
						maskLayer = new MaskLayer(uri);
						maskLayer.AddSymbolLayers(zGroups);
						result.Add(uri, maskLayer);
					}
				}
			}
			else
			{
				foreach (var uri in maskingLayers)
				{
					result[uri] = new MaskLayer(uri).AllSymbolLayers();
				}
			}
		}

		return new MaskLayers(result.Values, allLayers);
	}

	#region Nested types

	private class SymbolLevel
	{
		public string Name { get; }
		public string Type { get; }

		public SymbolLevel(string name, string type)
		{
			Name = name ?? string.Empty;
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public override string ToString()
		{
			return $"Name={Name}, Type={Type}";
		}
	}

	private class SymbolLayerDrawing
	{
		public SymbolLayerDrawing(bool useSLD, IEnumerable<string> levels = null)
		{
			HasSLD = levels != null;
			UseSLD = useSLD;

			if (useSLD && levels is null)
				throw new ArgumentException("Cannot UseSLD if no Symbol Layers (levels) are defined",
				                            nameof(useSLD));

			var list = (levels ?? Enumerable.Empty<string>()).ToList();
			SymbolLayers = new ReadOnlyCollection<string>(list);
		}

		public bool HasSLD { get; }
		public bool UseSLD { get; }

		public IReadOnlyList<string> SymbolLayers { get; }

		public override string ToString()
		{
			return $"HasSLD={HasSLD}, UseSLD={UseSLD}, SymbolLayers.Count={SymbolLayers.Count}";
		}
	}

	private class MaskLayer
	{
		private readonly HashSet<string> _symbolLayers;
		private bool _allSymbolLayers;

		public MaskLayer(string uri)
		{
			URI = uri ?? throw new ArgumentNullException(nameof(uri));
			_symbolLayers = new HashSet<string>(StringComparer.Ordinal);
			_allSymbolLayers = false;
		}

		public string URI { get; }

		public bool MasksLevel(string name)
		{
			if (name is null) return false;
			return _allSymbolLayers || _symbolLayers.Contains(name);
		}

		public MaskLayer AllSymbolLayers()
		{
			_allSymbolLayers = true;
			return this;
		}

		public void AddSymbolLayers(IEnumerable<string> names)
		{
			if (names is null)
				throw new ArgumentNullException(nameof(names));
			_symbolLayers.UnionWith(names);
		}

		public override string ToString()
		{
			var levels = _allSymbolLayers ? "*" : string.Join(", ", _symbolLayers);
			return $"{URI} masking {levels}";
		}
	}

	private class MaskLayers
	{
		private readonly MaskLayer[] _maskLayers;
		private readonly IDictionary<string, Layer> _allLayers;

		private MaskLayers()
		{
			_maskLayers = Array.Empty<MaskLayer>();
			_allLayers = new Dictionary<string, Layer>(0);
		}

		public MaskLayers(IEnumerable<string> maskLayers, IDictionary<string, Layer> allLayers)
		: this(maskLayers?.Select(uri => new MaskLayer(uri).AllSymbolLayers()), allLayers)
		{ }

		public MaskLayers(IEnumerable<MaskLayer> maskLayers, IDictionary<string, Layer> allLayers)
		{
			_maskLayers = maskLayers?.ToArray() ?? throw new ArgumentNullException(nameof(maskLayers));
			_allLayers = allLayers ?? throw new ArgumentNullException(nameof(allLayers));
		}

		public IEnumerable<Layer> GetAll()
		{
			return _maskLayers.OrderBy(ml => ml.URI)
			                  .Select(ml => GetLayer(ml.URI));
		}

		public IEnumerable<Layer> GetForLevel(string levelName)
		{
			return _maskLayers.Where(ml => ml.MasksLevel(levelName))
			                  .OrderBy(ml => ml.URI)
			                  .Select(ml => GetLayer(ml.URI));
		}

		private Layer GetLayer(string layerUri)
		{
			if (_allLayers.TryGetValue(layerUri, out var layer))
			{
				return layer;
			}

			throw new InvalidOperationException($"No layer by that URI: {layerUri}");
		}

		public override string ToString()
		{
			return _maskLayers.Length > 0 ? $"Count = {_maskLayers.Length}" : "None";
		}

		public static readonly MaskLayers None = new();
	}

	#endregion
}
