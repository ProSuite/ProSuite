using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Display;

/// <summary>
/// Export Overrides config, e.g. from the active map, to an XML file.
/// "Overrides" refers to symbol property connections (aka attribute mappings)
/// </summary>
public abstract class ExportOverridesButtonBase : ButtonCommandBase
{
	// Remembered options (in session only):
	private readonly ExportOverridesOptions _options = new();
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	protected override async Task<bool> OnClickCore()
	{
		var map = MapView.Active?.Map;
		if (map is null) return false;

		var owner = Application.Current.MainWindow;

		_options.SetMap(map);
		_options.RestoreOptions();

		var dialog = new ExportOverridesDialog(_options) { Owner = owner };
		var result = dialog.ShowDialog();
		if (result != true) return true; // user canceled
		_options.RememberOptions();

		ILayerContainer container = _options.GroupLayerItem?.GroupLayer ?? (ILayerContainer) map;

		var config = await QueuedTask.Run(() => CollectConfig(map, container));

		if (! string.IsNullOrWhiteSpace(_options.Remark))
		{
			config.AddFirst(new XElement("Remark", _options.Remark.Trim()));
		}

		if (string.IsNullOrWhiteSpace(_options.ConfigFilePath))
		{
			Gateway.HandleError("Must specify an export file path", _msg);
			return false;
		}

		var extension = Path.GetExtension(_options.ConfigFilePath).ToLowerInvariant();
		if (string.IsNullOrEmpty(extension)) extension = ".xml"; // default to XML

		switch (extension)
		{
			case ".xml":
				config.Save(_options.ConfigFilePath);
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
				                    Heading = "Overrides configuration exported to:",
				                    FilePath = _options.ConfigFilePath
			                    };

			successDialog.ShowDialog();
		}

		return true;
	}

	public static XElement CollectConfig(Map map, ILayerContainer container = null)
	{
		var overrides = GetLayers(container ?? map);

		var mapAttr = new XAttribute("map", map.Name ?? string.Empty);
		var groupLayerAttr = container is Layer group
			                     ? new XAttribute("groupLayer", group.Name ?? string.Empty)
			                     : null;

		return new XElement("Overrides", mapAttr, groupLayerAttr, overrides);
	}

	private static IEnumerable<XElement> GetLayers(ILayerContainer container)
	{
		var sortedList = container.GetLayersAsFlattenedList().ToList();
		sortedList.Sort((a, b) => string.Compare(a.Name, b.Name,
		                                         StringComparison.OrdinalIgnoreCase));

		foreach (var layer in sortedList)
		{
			if (layer is FeatureLayer featureLayer)
			{
				var cim = featureLayer.GetDefinition();
				if (cim is CIMGeoFeatureLayerBase gfl)
				{
					var layerXml = MakeLayer(layer, "Layer");

					layerXml.Add(MakeRendererInfo(gfl.Renderer));
					layerXml.Add(GetSymbols(gfl.Renderer));

					yield return layerXml;
				}
			}
		}
	}

	private static IEnumerable<XElement> GetSymbols(CIMRenderer renderer)
	{
		if (renderer is null)
		{
			yield break;
		}

		if (renderer is CIMUniqueValueRenderer unique)
		{
			// TODO handle the default symbol???

			// per class symbols
			foreach (var clazz in Utils.GetUniqueValueClasses(unique))
			{
				var discriminator = Utils.FormatDiscriminatorValue(clazz.Values);
				var symbolXml = GetSymbol(discriminator, clazz.Label, clazz.Symbol);

				yield return symbolXml;
			}
		}
		else if (renderer is CIMSimpleRenderer simple)
		{
			var symbolXml = GetSymbol("*", simple.Label, simple.Symbol);

			yield return symbolXml;
		}
		else
		{
			throw new NotSupportedException(
				$"Renderer type not supported: {renderer.GetType().Name}");
		}
	}

	private static XElement GetSymbol(string discriminator, string label, CIMSymbolReference symbol)
	{
		var xml = MakeSymbol(discriminator, label, symbol);

		if (symbol?.PrimitiveOverrides != null)
		{
			//cache the symbol structure
			IList<SymbolPrimitive> primitives = GetPrimitivesFromSymbol(symbol.Symbol);

			List<XElement> primitiveOverrides = new List<XElement>();
			foreach (var mapping in symbol.PrimitiveOverrides)
			{
				SymbolPrimitive primitive =
					primitives.FirstOrDefault(p => string.Equals(p.Name, mapping.PrimitiveName));

				if (primitive != null)
				{
					primitiveOverrides.Add(MakePrimitiveOverride(primitive, mapping));
				}
				else
				{
					_msg.Warn(
						$"Graphic primitive '{mapping.PrimitiveName}' not found in symbol '{label}'");
				}
			}

			//sort by path
			primitiveOverrides.Sort((a, b) => string.Compare(a.Attribute("path")?.Value,
			                                                 b.Attribute("path")?.Value,
			                                                 StringComparison.OrdinalIgnoreCase));

			foreach (XElement element in primitiveOverrides)
			{
				xml.Add(element);
			}
		}

		return xml;
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

	private static XElement MakeRendererInfo(CIMRenderer renderer)
	{
		var result = new XElement("Renderer");

		if (renderer is CIMUniqueValueRenderer unique)
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
			var type = renderer?.GetType().Name ?? "(null)";
			result.Add(new XAttribute("type", type));
		}

		return result;
	}

	private static XElement MakeSymbol(string match, string label, CIMSymbolReference symbol)
	{
		var result = new XElement("Symbol");

		result.Add(new XAttribute("match", match ?? "*"));
		result.Add(new XAttribute("label", label ?? string.Empty));

		if (symbol is not null)
		{
			result.Add(new XAttribute("type", Utils.GetPrettyTypeName(symbol.Symbol)));
		}

		return result;
	}

	private static XElement MakePrimitiveOverride(SymbolPrimitive primitive,
	                                              CIMPrimitiveOverride cim)
	{
		if (primitive is null)
			throw new ArgumentNullException(nameof(primitive));
		if (! string.Equals(cim.PrimitiveName, primitive.Name))
			throw new ArgumentException("Primitive and CIMPrimitiveOverride do not match");

		var result = new XElement("PrimitiveOverride");

		result.Add(new XAttribute("path", primitive.Path ?? string.Empty));
		result.Add(new XAttribute("name", primitive.Name ?? string.Empty));
		result.Add(new XAttribute("propertyName", cim.PropertyName));
		result.Add(new XAttribute("expression", cim.Expression));
		//TODO arcadeexpressions / ValueExpressionInfo

		if (! string.IsNullOrEmpty(primitive.Type))
		{
			result.Add(new XAttribute("type", primitive.Type));
		}

		return result;
	}

	//TODO: refactor/move to SymbolUtils
	//TODO consolidate path with SymbolUtils.FindPrimitiveByPath
	private static IList<SymbolPrimitive> GetPrimitivesFromSymbol(CIMSymbol symbol)
	{
		var result = new List<SymbolPrimitive>();

		if (symbol is CIMMultiLayerSymbol multiLayerSymbol)
		{
			if (multiLayerSymbol.Effects != null) //Global Effects
			{
				for (int i = 0; i < multiLayerSymbol.Effects.Length; i++)
				{
					var effect = multiLayerSymbol.Effects[i];
					if (! string.IsNullOrEmpty(effect.PrimitiveName))
					{
						result.Add(new SymbolPrimitive($"effect {i}", effect.PrimitiveName,
						                               Utils.GetPrettyTypeName(effect)));
					}
				}
			}

			if (multiLayerSymbol.SymbolLayers != null)
			{
				for (int i = 0; i < multiLayerSymbol.SymbolLayers.Length; i++)
				{
					var symbolLayer = multiLayerSymbol.SymbolLayers[i];
					var slid = i + 1;
					if (! string.IsNullOrEmpty(symbolLayer.PrimitiveName))
					{
						result.Add(new SymbolPrimitive($"layer {slid}", symbolLayer.PrimitiveName,
						                               Utils.GetPrettyTypeName(symbolLayer)));
					}

					if (symbolLayer is CIMMarker marker)
					{
						var placement = marker.MarkerPlacement;
						if (placement != null &&
						    ! string.IsNullOrEmpty(placement.PrimitiveName))
						{
							result.Add(new SymbolPrimitive(
								           $"layer {slid} placement", placement.PrimitiveName,
								           Utils.GetPrettyTypeName(placement)));
						}

						if (symbolLayer is CIMVectorMarker vectorMarker)
						{
							if (vectorMarker.MarkerGraphics != null)
							{
								for (int j = 0; j < vectorMarker.MarkerGraphics.Length; j++)
								{
									var graphic = vectorMarker.MarkerGraphics[j];
									if (! string.IsNullOrEmpty(graphic.PrimitiveName))
									{
										result.Add(new SymbolPrimitive(
											           $"layer {slid} graphic {j}",
											           graphic.PrimitiveName,
											           Utils.GetPrettyTypeName(graphic)));
									}
								}
							}
						}
					}

					if (symbolLayer.Effects != null)
					{
						for (int j = 0; j < symbolLayer.Effects.Length; j++)
						{
							var effect = symbolLayer.Effects[j];
							if (! string.IsNullOrEmpty(effect.PrimitiveName))
							{
								result.Add(new SymbolPrimitive(
									           $"layer {slid} effect {j}", effect.PrimitiveName,
									           Utils.GetPrettyTypeName(effect)));
							}
						}
					}
				}
			}
		}

		return result;
	}

	#region Nested types

	private class SymbolPrimitive
	{
		public string Path { get; }
		public string Name { get; }
		public string Type { get; }

		public SymbolPrimitive(string path, string name, string type)
		{
			Path = path ?? throw new ArgumentNullException(nameof(path));
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public override string ToString()
		{
			return $"Path={Path}, Name={Name}, Type={Type}";
		}
	}

	#endregion
}
