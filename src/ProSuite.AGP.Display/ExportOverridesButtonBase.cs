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
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
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
		// TODO Do we really want to sort layers? I think the original order is more useful (and also stable). When layers are moved, this is significant and should be visible in the diff
		sortedList.Sort((a, b) => string.Compare(a.Name, b.Name,
		                                         StringComparison.OrdinalIgnoreCase));

		foreach (var layer in sortedList)
		{
			if (layer is FeatureLayer featureLayer)
			{
				// Hint: GetRenderer() is more to the point and slightly faster than GetDefinition()
				var renderer = featureLayer.GetRenderer();

				var layerXml = MakeLayer(layer, "Layer");
				layerXml.Add(MakeRendererInfo(renderer));
				layerXml.Add(GetSymbols(renderer));

				yield return layerXml;
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

	private static XElement GetSymbol(string discriminator, string label, CIMSymbolReference symref)
	{
		var xml = MakeSymbol(discriminator, label, symref);

		if (symref is null) return xml;

		var overrides = symref.PrimitiveOverrides;
		if (overrides is null) return xml;

		{
			var symbol = symref.Symbol;
			var list = new List<OverrideItem>();

			foreach (var po in overrides)
			{
				var name = po.PrimitiveName;
				var prim = SymbolUtils.FindPrimitiveByName<CIMObject>(symbol, name, out var path);

				list.Add(new OverrideItem(po, path, prim));
			}

			foreach (var prim in list.OrderBy(item => item.Path).ThenBy(item => item.PrimitiveName))
			{
				xml.Add(MakeOverride(prim));
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

	private static XElement MakeOverride(OverrideItem item)
	{
		var result = new XElement("Override");
		result.Add(new XAttribute("path", item.Path ?? "NOT FOUND"));
		result.Add(new XAttribute("name", item.PrimitiveName));
		result.Add(new XAttribute("property", item.PropertyName));
		result.Add(new XAttribute("expression", item.Expression));
		result.Add(new XAttribute("primitive", item.PrimitiveType ?? "NOT FOUND"));
		return result;
	}

	#region Nested types

	private readonly struct OverrideItem
	{
		[NotNull] private CIMPrimitiveOverride Override { get; }
		[CanBeNull] public string Path { get; }
		[CanBeNull] private CIMObject Primitive { get; }

		public string PrimitiveName => Override.PrimitiveName;
		public string PrimitiveType => Utils.GetPrettyTypeName(Primitive);
		public string PropertyName => Override.PropertyName;
		public string Expression => GetExpression(Override);

		public OverrideItem([NotNull] CIMPrimitiveOverride po, string path, CIMObject primitive)
		{
			Override = po ?? throw new ArgumentNullException(nameof(po));
			Path = path;
			Primitive = primitive;
		}

		private static string GetExpression(CIMPrimitiveOverride po)
		{
			if (po is null)
				throw new ArgumentNullException(nameof(po));

			if (po.ValueExpressionInfo != null &&
			    !string.IsNullOrEmpty(po.ValueExpressionInfo.Expression))
			{
				return po.ValueExpressionInfo.Expression;
			}

			if (!string.IsNullOrEmpty(po.Expression))
			{
				return po.Expression;
			}

			return null;
		}
	}

	#endregion
}
