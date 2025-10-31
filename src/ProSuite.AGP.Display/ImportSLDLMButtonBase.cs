using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.IO;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Xml;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace ProSuite.AGP.Display;

/// <summary>
/// Import SLD/LM config into the active map (or a group layer).
/// The imported config must be compatible with the active map!
/// </summary>
// TODO Check if some layer.SetDefinition(cim) can be omitted: because nothing changed or if there's a specific API call, e.g. SetUseSLD(true/false)
[UsedImplicitly]
public abstract class ImportSLDLMButtonBase : ButtonCommandBase
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	// Remembered options (in session only):
	private readonly ImportSLDLMOptions _options = new(ValidateConfig);

	protected override async Task<bool> OnClickAsyncCore()
	{
		var map = MapView.Active?.Map;
		if (map is null) return false;

		var owner = Application.Current.MainWindow;

		_options.SetMap(map);
		_options.RestoreOptions();

		var dialog = new ImportSLDLMDialog(_options) { Owner = owner };
		var result = dialog.ShowDialog();
		if (result != true) return true; // user canceled
		_options.RememberOptions();

		string filePath = _options.ConfigFilePath;

		var config = LoadConfig(filePath);
		var feedback = new Feedback();

		if (! ValidateConfig(config, feedback))
		{
			Utils.ShowFeedback(feedback, owner);
			return false;
		}

		ILayerContainer container = _options.GroupLayerItem?.GroupLayer ?? (ILayerContainer) map;

		await QueuedTask.Run(() => ApplyConfig(map, container, config, feedback));

		// Force a Symbology pane "reload" by clearing the TOC selection (cannot
		// restore the selection here, as then the Symbology pane won't notice anything):
		MapView.Active?.ClearTOCSelection();

		if (owner is not null)
		{
			const string caption = "Import SLD/LM Configuration";

			var message = new StringBuilder();
			message.Append($"SLD/LM configuration applied to {map.Name}");
			if (container is GroupLayer groupLayer)
			{
				message.Append($" (group layer {groupLayer.Name})");
			}

			message.AppendLine(".").AppendLine();
			message.Append("The Symbology pane may not reflect the latest changes but ");
			message.Append("show the old state. Refresh by closing and re-opening the map.");

			//The Symbology pane may not reflect the latest changes but show the old state.
			// Refresh by closing and re-opening the map.

			// Just for reference: the Symbology dockpane has DAML ID "esri_mapping_symbologyDockPane"
			// To get it: FrameworkApplication.DockPaneManager.Find("esri_mapping_symbologyDockPane");

			MessageBox.Show(owner, message.ToString(), caption);
		}

		return true;
	}

	private static Config LoadConfig(string filePath)
	{
		if (string.IsNullOrEmpty(filePath))
			throw new ArgumentNullException(nameof(filePath));

		var extension = Path.GetExtension(filePath);
		extension = extension.ToLowerInvariant();

		switch (extension)
		{
			case ".csv":
				return LoadConfigCSV(filePath);
			case ".xml":
				return LoadConfigXML(filePath);
			default:
				throw new NotSupportedException(
					$"Import SLD/LM from file of type {extension} is not supported");
		}
	}

	private static Config LoadConfigXML(string filePath)
	{
		var document = XDocument.Load(filePath, LoadOptions.SetLineInfo);
		return new Config(document.Root);
	}

	private static Config LoadConfigCSV(string filePath)
	{
		using var reader = File.OpenText(filePath);
		using var csv = new CsvReader(reader);

		// TODO build the X dom from the CSV (inverse of SaveAsCSV() in Export)

		throw new NotImplementedException("Import from CSV is not yet implemented");
	}

	private static void ValidateConfig(string configFilePath, IFeedback feedback)
	{
		if (feedback is null)
			throw new ArgumentNullException(nameof(feedback));

		if (string.IsNullOrEmpty(configFilePath))
		{
			feedback.Error("No config file specified");
			return;
		}

		try
		{
			Config config = LoadConfig(configFilePath);

			ValidateConfig(config, feedback);
		}
		catch (Exception ex)
		{
			feedback.Error($"Cannot validate {configFilePath}: {ex.Message}");
		}
	}

	/// <returns>true iff there are no errors in the given <paramref name="config"/>;
	/// warnings may occur</returns>
	private static bool ValidateConfig(Config config, IFeedback feedback)
	{
		bool valid = true; // optimistic

		var emptyLevelNameReference = config.DrawingOrder.SelectMany(item => item.Levels)
		                                     .FirstOrDefault(level => string.IsNullOrEmpty(level.Name));

		if (emptyLevelNameReference is not null)
		{
			// don't invalidate: it's a smell, but not an error
			var text = emptyLevelNameReference.AppendLineInfo("empty level name");
			feedback.Warning($"{nameof(config.DrawingOrder)} refers to {text}");
		}

		var usedLevelNames = config.DrawingOrder
		                           .SelectMany(orderItem => orderItem.Levels)
		                           .Select(level => level.Name)
		                           .Where(name => !string.IsNullOrEmpty(name))
		                           .ToHashSet(); // implicitly distinct

		var definedLevelNames = config.SymbolLevels
		                              .SelectMany(layer => layer.Symbols)
		                              .SelectMany(symbol => symbol.Levels)
		                              .Select(level => level.Name)
		                              .Where(name => !string.IsNullOrEmpty(name))
		                              .ToHashSet(); // implicitly distinct

		// Check that all level names in DrawingOrder occur in at least one SymbolLevels entry:
		var undefinedLevels = usedLevelNames.Except(definedLevelNames).ToList();
		if (undefinedLevels.Count > 0)
		{
			valid = false;
			var text = string.Join(", ", undefinedLevels.OrderBy(name => name));
			feedback.Error($"{nameof(config.DrawingOrder)} refers to undefined level names: {text}");
		}

		// Check that all level names in SymbolLevels are referenced from DrawingOrder:
		var unusedLevels = definedLevelNames.Except(usedLevelNames).ToList();
		if (unusedLevels.Count > 0)
		{
			// don't invalidate: it's a smell, but not an error
			var text = string.Join(", ", unusedLevels.OrderBy(name => name));
			feedback.Warning($"Assigned symbol levels not used by {nameof(config.DrawingOrder)}: {text}");
		}

		// TODO Check that layer masking is consistent with SLD ordering:
		//   - LM info on DrawingOrder entries is authoritative
		//   - LM info SymbolLevels entries shall be consistent (but may not be if CIM is manipulated outside the Pro UI)
		//   - this means: masks(z,l) are the same for all levels z and child layers l in the group layer
		ValidateMaskingConsistency(config, feedback);

		// TODO Check there is only one SLD controlling layer (not required by Pro, but needed by our import tool(?))

		// Check that entries in SymbolLevels have a valid Renderer (type "simple" or "unique"):
		var missingRenderer = config.SymbolLevels
		                            .Where(layer => !string.Equals(layer.Renderer?.Type, Config.Renderer.SimpleType) &&
		                                            !string.Equals(layer.Renderer?.Type, Config.Renderer.UniqueValueType))
		                            .ToList();
		if (missingRenderer.Count > 0)
		{
			valid = false;
			var lyrs = missingRenderer.Select(item => item.AppendLineInfo(FormatLayer(item)));
			var text = string.Concat(", ", lyrs);
			feedback.Error($"Layers in {nameof(config.SymbolLevels)} having invalid renderer: {text} " +
			               $"(valid renderer types are: '{Config.Renderer.SimpleType}' and '{Config.Renderer.UniqueValueType}')");
		}

		// Check that layers with simple renderer have exactly one Symbol:
		var badSimpleRenderers = config.SymbolLevels
		                                    .Where(item => string.Equals(item.Renderer?.Type, Config.Renderer.SimpleType))
		                                    .Where(item => item.Symbols.Count() != 1).ToList();
		if (badSimpleRenderers.Count > 0)
		{
			valid = false;
			var lyrs = badSimpleRenderers.Select(item => item.AppendLineInfo(FormatLayer(item)));
			var text = string.Concat(", ", lyrs);
			feedback.Error($"Simple renderer needs exactly one symbol: {text}");
		}

		// Check that layers with unique value renderer have at least one symbol:
		var badUniqueRenderers = config.SymbolLevels
		                               .Where(item => string.Equals(item.Renderer?.Type, Config.Renderer.UniqueValueType))
		                               .Where(item => !item.Symbols.Any()).ToList();
		if (badUniqueRenderers.Count > 0)
		{
			valid = false;
			var lyrs = badUniqueRenderers.Select(item => item.AppendLineInfo(FormatLayer(item)));
			var text = string.Concat(", ", lyrs);
			feedback.Error($"Unique value renderer needs at least one symbol: {text}");
		}

		// Check no duplicate layer (by URI) in DrawingOrder:
		var duplicateLayerUris = config.DrawingOrder.GroupBy(layer => layer.Uri)
		                               .Where(g => g.Count() > 1).ToList();
		if (duplicateLayerUris.Count > 0)
		{
			valid = false;
			var lyrs = duplicateLayerUris.Select(g => g.First().AppendLineInfo(g.Key));
			var text = string.Join(", ", lyrs);
			feedback.Error($"Duplicate (by URI) layers in {nameof(config.DrawingOrder)}: {text}");
		}

		return valid;
	}

	private static void ValidateMaskingConsistency(Config config, IFeedback feedback)
	{
		// Check that layer masking is consistent with SLD ordering:
		// - LM info on DrawingOrder entries is authoritative
		// - LM info SymbolLevels entries shall be consistent (but may not be if CIM is manipulated outside the Pro UI)
		// - this means: masks(z,l) are the same for all levels z and child layers l in the group layer

		bool hasExtraMaskedBy = config.SymbolLevels
		                         .SelectMany(layer => layer.Symbols)
		                         .SelectMany(symbol => symbol.Levels)
		                         .Any(sl => sl.MaskedBy.Any());

		if (! hasExtraMaskedBy)
		{
			// Assume this config was created without extra masking info,
			// so there is no consistency we can validate.
			return;
		}

		// Make a dictionary level-name => List of MaskedBy:

		var dict = config.DrawingOrder
		                 .SelectMany(l => l.Levels)
		                 .ToDictionary(l => l.Name, l => l.MaskedBy.ToList());

		// For each level in SymbolLevels, check if its MaskedBy are
		// the same as those in DrawingOrder for the like-named level:

		foreach (var layer in config.SymbolLevels)
		{
			foreach (var symbol in layer.Symbols)
			{
				foreach (var level in symbol.Levels)
				{
					if (dict.TryGetValue(level.Name, out var maskedBy))
					{
						if (! SameMaskedBy(maskedBy, level.MaskedBy))
						{
							feedback.Warning($"Inconsistent MaskedBy's on level {level.Name} in symbol {symbol.Match} (label {symbol.Label}) of layer {layer.Name} (parent {layer.Parent})");
						}
					}
					else if (level.MaskedBy.Any())
					{
						feedback.Warning($"Inconsistent MaskedBy's on level {level.Name} in symbol {symbol.Match} (label {symbol.Label}) of layer {layer.Name} (parent {layer.Parent})");
					}
				}
			}
		}
	}

	private static bool SameMaskedBy(IEnumerable<Config.MaskLayer> a,
	                                 IEnumerable<Config.MaskLayer> b)
	{
		if (a is null && b is null) return true;
		if (a is null || b is null) return false;

		return a.OrderBy(m => m.Uri)
		        .SequenceEqual(b.OrderBy(m => m.Uri), Config.MaskLayerComparer.Instance);
	}

	/// <remarks>Must run on MCT</remarks>
	private static void ApplyConfig(Map map, ILayerContainer container, Config config, IFeedback feedback)
	{
		if (map is null)
			throw new ArgumentNullException(nameof(map));

		_msg.Debug("Applying SLD/LM config to layers");

		using (_msg.IncrementIndentation())
		{
			var manager = map.OperationManager;
			if (manager is not null)
			{
				const string name = "Update SLD/LM";
				var action = () => { ApplyConfig(container ?? map, config, feedback); };

				manager.CreateCompositeOperation(action, name);
			}
			else
			{
				ApplyConfig(container ?? map, config, feedback);
			}
		}
	}

	private static void ApplyConfig(ILayerContainer container, Config config, IFeedback feedback)
	{
		_msg.Debug("Step 1: configure SLD on controlling layer(s); erase on others");
		ApplySymbolLevelDrawing(container, config.DrawingOrder, feedback);

		_msg.Debug("Step 2: set name on each symbol layer (i.e., assign to a drawing level)");
		ApplySymbolLevelNames(container, config.SymbolLevels, feedback);

		_msg.Debug("Step 3: set LayerMasks and also MaskedSymbolLayers where SLD is used");
		ApplyLayerMasking(container, config.DrawingOrder, feedback);
	}

	private static void ApplySymbolLevelDrawing(ILayerContainer container, IEnumerable<Config.OrderItem> order, IFeedback feedback)
	{
		var itemsByUri = order.ToDictionary(item => item.Uri);
		var layersInPreOrder = GetLayersPreOrder(container);

		foreach (var layer in layersInPreOrder)
		{
			if (itemsByUri.TryGetValue(layer.URI, out var item))
			{
				var cim = layer.GetDefinition();
				if (cim is CIMGeoFeatureLayerBase cimFeatureLayer)
				{
					cimFeatureLayer.SymbolLayerDrawing = MakeSymbolLayerDrawing(item.Levels, cimFeatureLayer.SymbolLayerDrawing);
					layer.SetDefinition(cim);
				}
				else if (cim is CIMGroupLayer cimGroupLayer)
				{
					cimGroupLayer.SymbolLayerDrawing = MakeSymbolLayerDrawing(item.Levels, cimGroupLayer.SymbolLayerDrawing);
					layer.SetDefinition(cim);
				}
			}
			else
			{
				var cim = layer.GetDefinition();
				if (cim is CIMGeoFeatureLayerBase cimFeatureLayer)
				{
					cimFeatureLayer.SymbolLayerDrawing = MakeSymbolLayerDrawing(null, false);
					layer.SetDefinition(cim);
				}
				else if (cim is CIMGroupLayer cimGroupLayer)
				{
					cimGroupLayer.SymbolLayerDrawing = MakeSymbolLayerDrawing(null, false);
					layer.SetDefinition(cim);
				}
			}
		}
	}

	private static void ApplySymbolLevelNames(ILayerContainer container, IEnumerable<Config.LevelItem> levels, IFeedback feedback)
	{
		foreach (var entry in levels)
		{
			var layer = container.FindLayer(entry.Uri);
			if (layer is null)
			{
				feedback.Error($"Layer not found: {FormatLayer(entry)} with URI {entry.Uri}; skipping layer");
				continue;
			}

			if (layer is not FeatureLayer featureLayer)
			{
				feedback.Error($"Layer {FormatLayer(entry)} is not a {nameof(FeatureLayer)}; skipping layer");
				continue;
			}

			// Here we can use Get/SetRenderer(), which is a subset of Get/SetDefinition()
			// and thus hoped to be faster.

			var cimRenderer = featureLayer.GetRenderer();

			if (ApplySymbolLevelNames(cimRenderer, entry, feedback))
			{
				featureLayer.SetRenderer(cimRenderer);
			}
		}
	}

	private static bool ApplySymbolLevelNames(CIMRenderer renderer, Config.LevelItem entry, IFeedback feedback)
	{
		if (renderer is CIMUniqueValueRenderer uniqueRenderer)
		{
			return ApplySymbolLevelNames(uniqueRenderer, entry, feedback);
		}

		if (renderer is CIMSimpleRenderer simpleRenderer)
		{
			return ApplySymbolLevelNames(simpleRenderer, entry, feedback);
		}

		feedback.Error(
				$"Layer {FormatLayer(entry)} has a renderer of type " +
				$"{renderer.GetType().Name}, which is not supported; skipping layer");
		return false;
	}

	private static bool ApplySymbolLevelNames(CIMUniqueValueRenderer uniqueRenderer, Config.LevelItem entry, IFeedback feedback)
	{
		if (entry.Renderer.Type != Config.Renderer.UniqueValueType)
		{
			feedback.Error(
				$"Expect a {entry.Renderer.Type} renderer, but layer {FormatLayer(entry)} " +
				$"has a renderer of {uniqueRenderer.GetType().Name}; skipping layer");
			return false;
		}

		if (!SameFields(entry.Renderer.Fields, uniqueRenderer.Fields))
		{
			feedback.Error(
				$"Fields in config ({FormatFields(entry.Renderer.Fields)}) differ from " +
				$"fields on UV renderer in CIM ({FormatFields(uniqueRenderer.Fields)}); " +
				$"skipping layer {FormatLayer(entry)}");
			return false;
		}

		bool rendererChanged = false;
		var usedSyms = new List<Config.Symbol>();

		// Set primitive names on default symbol:

		var defsym = FindSymbol(entry.Symbols, Config.Symbol.MatchAny);
		if (defsym is not null)
		{
			usedSyms.Add(defsym);

			if (UpdateSymbol(uniqueRenderer.DefaultSymbol.Symbol, defsym, feedback))
			{
				rendererChanged = true;
			}
		}

		// Set primitive names on each class symbol:

		foreach (var clazz in Utils.GetUniqueValueClasses(uniqueRenderer))
		{
			var sym = FindSymbol(entry.Symbols, clazz.Values);
			if (sym is null)
			{
				var text = Utils.FormatDiscriminatorValue(clazz.Values);
				feedback.Error(
					$"No symbol found in config for UV class values {text}" +
					$"skipping symbol in this UV class on layer {FormatLayer(entry)}");
				continue;
			}

			usedSyms.Add(sym);

			if (!string.Equals(sym.Label, clazz.Label))
			{
				feedback.Warning(
					$"Label in config ({sym.Label}) differs from label on UV class ({clazz.Label})");
			}

			if (UpdateSymbol(clazz.Symbol?.Symbol, sym, feedback))
			{
				rendererChanged = true;
			}
		}

		var unusedSyms = entry.Symbols.Except(usedSyms).ToList();
		if (unusedSyms.Any())
		{
			var keys = string.Join("; ", unusedSyms.Select(s => s.Match));
			feedback.Warning(
				$"Symbols in the config for layer {FormatLayer(entry)} that are not used in the CIM: {keys}");
		}

		return rendererChanged;
	}

	private static bool ApplySymbolLevelNames(CIMSimpleRenderer simpleRenderer, Config.LevelItem entry, IFeedback feedback)
	{
		if (entry.Renderer.Type != Config.Renderer.SimpleType)
		{
			feedback.Error(
				$"Expect a {entry.Renderer.Type} renderer, but layer {FormatLayer(entry)} " +
				$"has a renderer of type {simpleRenderer.GetType().Name}; skipping layer");
			return false;
		}

		int symCount = entry.Symbols.Count();
		if (symCount < 1)
		{
			feedback.Error(
				$"Config has no symbol for simple renderer " +
				$"on layer {FormatLayer(entry)}; skipping layer");
			return false;
		}
		if (symCount > 1)
		{
			feedback.Error(
				$"Expect exactly one symbol for simple renderer on layer {FormatLayer(entry)} " +
				$"but config has {symCount}; continuing with first symbol");
		}

		return UpdateSymbol(simpleRenderer.Symbol?.Symbol, entry.Symbols.Single(), feedback);
	}

	private static void ApplyLayerMasking(ILayerContainer container, IEnumerable<Config.OrderItem> order, IFeedback feedback)
	{
		// Set cim.LayerMasks (string[]) on group and feature layers, and
		// cim.MaskedSymbolLayers (CIMSymbolLayerMasking[]) on feature layers.

		var itemsByUri = order.ToDictionary(item => item.Uri);
		var layersInPreOrder = GetLayersPreOrder(container);

		GroupLayer controllingLayer = null;
		Config.OrderItem controllingItem = null;

		foreach (var layer in layersInPreOrder)
		{
			if (! layer.IsNestedLayer(controllingLayer))
			{
				controllingLayer = null;
				controllingItem = null;
			}

			_msg.DebugFormat("- processing layer {0}", layer.Name);
			
			if (layer is GroupLayer groupLayer)
			{
				var cim = (CIMGroupLayer) groupLayer.GetDefinition();

				if (itemsByUri.TryGetValue(groupLayer.URI, out var item))
				{
					// TODO CHECK - must be union of LayerMasks in nested layers
					var masking = RegroupMaskingConfig(item.Levels).ToList();
					cim.LayerMasks = MakeLayerMasks(masking.Select(g => g.Key));
				}
				else
				{
					cim.LayerMasks = Array.Empty<string>();
				}

				groupLayer.SetDefinition(cim);

				if (controllingLayer is null && cim.UsesSLD())
				{
					controllingLayer = groupLayer;
					controllingItem = item;
				}
			}
			else if (layer is FeatureLayer featureLayer)
			{
				var cim = (CIMFeatureLayer) featureLayer.GetDefinition();

				// Two cases: (1) nested in controlling layer, or (2) standalone
				if (controllingItem is not null)
				{
					// set LayerMasks and MaskedSymbolLayers on nested feature layer
					// - only those masks that actually apply to this nested layer
					// - only those levels that actually occur on this nested layer
					var subnames = GetPrimitiveNames(cim, feedback);

					var masking = RegroupMaskingConfig(controllingItem.Levels).ToList();
					var relevantMasking = masking.Where(g => g.Any(name => subnames.Contains(name))).ToList();
					var relevantLevels = relevantMasking.Select(g => subnames.Intersect(g));

					cim.LayerMasks = MakeLayerMasks(relevantMasking.Select(g => g.Key)); // empty if none
					cim.MaskedSymbolLayers = MakeSymbolLayerMaskings(relevantLevels); // null if none
				}
				else if (itemsByUri.TryGetValue(featureLayer.URI, out var item))
				{
					// Use presence of Levels to decide: symbol layer masking or regular layer masking
					if (item.Levels.Any())
					{
						// advanced layer masking (i.e., selected symbol layers only)
						var masking = RegroupMaskingConfig(item.Levels).ToList();

						// LayerMasks and MaskedSymbolLayers are parallel arrays!
						cim.LayerMasks = MakeLayerMasks(masking.Select(g => g.Key));
						cim.MaskedSymbolLayers = MakeSymbolLayerMaskings(masking);
					}
					else
					{
						// regular layer masking (i.e., mask entire symbols)
						cim.LayerMasks = MakeLayerMasks(item.MaskedBy.Select(m => m.Uri));
						cim.MaskedSymbolLayers = MakeSymbolLayerMaskings(null);
					}
				}
				else
				{
					cim.LayerMasks = MakeLayerMasks(null);
					cim.MaskedSymbolLayers = MakeSymbolLayerMaskings(null);
				}

				featureLayer.SetDefinition(cim);
			}
			//else: neither group nor feature layer: skip --> TODO well, *all* layers can have LayerMasks!
		}
	}

	private static IReadOnlyList<Layer> GetLayersPreOrder(ILayerContainer container)
	{
		var layersInPreOrder = container.GetLayersAsFlattenedList();

		if (container is GroupLayer gl)
		{
			// Special case: insert container at front if it's a group layer!
			layersInPreOrder = Enumerable.Repeat((Layer) gl, 1).Concat(layersInPreOrder).ToList();
		}

		return layersInPreOrder;
	}

	private static IEnumerable<IGrouping<string, string>> RegroupMaskingConfig(IEnumerable<Config.LayerLevel> levels)
	{
		// Config has: masks per level
		// CIM wants: levels per mask
		// Use LINQ to first flatten and then regroup by mask layer
		return levels.SelectMany(level => level.MaskedBy.Select(m => new { level.Name, m.Uri }))
		             .GroupBy(tuple => tuple.Uri, i => i.Name);
	}

	private static ISet<string> GetPrimitiveNames(CIMGeoFeatureLayerBase cim, IFeedback feedback)
	{
		var names = new HashSet<string>();

		if (cim.Renderer is CIMUniqueValueRenderer uniqueRenderer)
		{
			GetPrimitiveNames(uniqueRenderer.DefaultSymbol?.Symbol, names);
			foreach (var clazz in Utils.GetUniqueValueClasses(uniqueRenderer))
			{
				GetPrimitiveNames(clazz.Symbol?.Symbol, names);
			}
		}
		else if (cim.Renderer is CIMSimpleRenderer simpleRenderer)
		{
			GetPrimitiveNames(simpleRenderer.Symbol?.Symbol, names);
		}
		else
		{
			feedback.Warning($"Renderer of type {cim.Renderer.GetType().Name} is not supported (ignoring this renderer)");
		}

		return names;
	}

	private static void GetPrimitiveNames(CIMSymbol cim, ICollection<string> accumulator)
	{
		if (accumulator is null)
			throw new ArgumentNullException(nameof(accumulator));

		if (cim is CIMMultiLayerSymbol multiLayerSymbol)
		{
			foreach (var symbolLayer in multiLayerSymbol.SymbolLayers)
			{
				if (! string.IsNullOrEmpty(symbolLayer.Name))
				{
					accumulator.Add(symbolLayer.Name);
				}
			}
		}
		//else: must be a CIMTextSymbol, which has no Name property
	}

	#region Creating CIM objects

	private static CIMSymbolLayerDrawing MakeSymbolLayerDrawing(IReadOnlyList<Config.LayerLevel> levels, CIMSymbolLayerDrawing previous)
	{
		// preserve the UseSLD flag, if it existed before; otherwise, turn it on
		var useSLD = previous?.SymbolLayers is null ||
		             previous.SymbolLayers.Length < 1 ||
		             previous.UseSymbolLayerDrawing;

		return levels.Any()
			       ? MakeSymbolLayerDrawing(levels, useSLD)
			       : MakeSymbolLayerDrawing(null, false);
	}

	private static CIMSymbolLayerDrawing MakeSymbolLayerDrawing(IEnumerable<Config.LayerLevel> levels, bool useSLD)
	{
		var symbolLayers = levels?.Select(l => MakeSymbolLayerIdentifier(l.Name)).ToArray();

		return new CIMSymbolLayerDrawing
		       {
			       SymbolLayers = symbolLayers,
			       UseSymbolLayerDrawing = useSLD
		       };
	}

	private static string[] MakeLayerMasks(IEnumerable<string> layerUris)
	{
		// unlike CIM.SymbolLayerMasking, CIM.LayerMasks is empty if no masks
		return layerUris?.ToArray() ?? Array.Empty<string>();
	}

	private static CIMSymbolLayerMasking[] MakeSymbolLayerMaskings(IEnumerable<IEnumerable<string>> levels)
	{
		// unlike CIM.LayerMasks, CIM.SymbolLayerMasking is null if none
		if (levels is null) return null;
		var maskings = levels.Select(MakeSymbolLayerMasking).ToArray();
		return maskings.Length > 0 ? maskings : null;
	}

	private static CIMSymbolLayerMasking MakeSymbolLayerMasking(IEnumerable<string> names)
	{
		var identifiers = names.Select(MakeSymbolLayerIdentifier).ToArray();
		return new CIMSymbolLayerMasking { SymbolLayers = identifiers };
	}

	private static CIMSymbolLayerIdentifier MakeSymbolLayerIdentifier(string name)
	{
		return new CIMSymbolLayerIdentifier { SymbolLayerName = name };
	}

	#endregion

	private static bool UpdateSymbol(CIMSymbol cimSymbol, Config.Symbol configSymbol, IFeedback feedback)
	{
		if (cimSymbol is not CIMMultiLayerSymbol multiLayerSymbol)
		{
			feedback.Error(
				$"Current symbol is not a {nameof(CIMMultiLayerSymbol)} " +
				$"(it's {cimSymbol?.GetType().Name ?? "null"}); skipping symbol");
			return false;
		}

		var cimLevels = multiLayerSymbol.SymbolLayers;
		if (cimLevels is null) return false;

		// TODO also compare labels?
		var cimSymbolType = Utils.GetPrettyTypeName(multiLayerSymbol);
		if (configSymbol.Type != null && !string.Equals(configSymbol.Type, cimSymbolType, StringComparison.Ordinal))
		{
			feedback.Warning($"Symbol types don't agree: config expects {configSymbol.Type} but CIM has {cimSymbolType}");
		}

		int n = cimLevels.Length;
		if (configSymbol.Levels.Count != n)
		{
			feedback.Error(
				$"Current symbol has {n} level(s), but the config expected " +
				$"{configSymbol.Levels.Count} level(s); skipping symbol");
			return false;
		}

		bool modified = false;

		for (int i = 0; i < n; i++)
		{
			var level = configSymbol.Levels[i];

			var cimLevelType = Utils.GetPrettyTypeName(cimLevels[i]);
			if (level.Type != null && !string.Equals(level.Type, cimLevelType, StringComparison.Ordinal))
			{
				feedback.Warning($"Symbol layer types don't agree: config expects {level.Type} but CIM has {cimLevelType}");
			}

			var levelName = string.IsNullOrEmpty(level.Name) ? null : level.Name;

			if (!string.Equals(cimLevels[i].Name, levelName, StringComparison.Ordinal))
			{
				multiLayerSymbol.SymbolLayers[i].Name = levelName;
				modified = true;
			}
		}

		return modified;
	}

	private static Config.Symbol FindSymbol(IEnumerable<Config.Symbol> symbols, CIMUniqueValue[] values)
	{
		var text = Utils.FormatDiscriminatorValue(values);
		return FindSymbol(symbols, text);
	}

	private static Config.Symbol FindSymbol(IEnumerable<Config.Symbol> symbols, string formattedValues)
	{
		return symbols.FirstOrDefault(sym => sym.Match != null && string.Equals(sym.Match, formattedValues));
	}

	private static bool SameFields(string[] configFields, string[] cimFields)
	{
		if (configFields is null || cimFields is null) return false;
		if (configFields.Length != cimFields.Length) return false;

		for (int i = 0; i < configFields.Length; i++)
		{
			if (! string.Equals(configFields[i], cimFields[i], StringComparison.Ordinal))
			{
				return false;
			}
		}

		return true;
	}

	private static string FormatFields(string[] fields)
	{
		if (fields is null) return null;
		const char sep = ',';
		return string.Join(sep, fields);
	}

	private static string FormatLayer(Config.LayerBase layer)
	{
		return $"{layer.Name} in {layer.Parent ?? "Map"}";
	}

	/// <summary>
	/// Just a convenience: typed access to the config XElement
	/// </summary>
	private class Config : XmlWrapperBase
	{
		public Config(XElement xml) : base(xml) { }

		public string MapName => (string) Xml.Attribute("map");
		public string GroupName => (string) Xml.Attribute("groupLayer");
		//public bool IncludeMasking => (bool?) Xml.Attribute("includeMasking") ?? false;
		//public bool ExtraMasking => (bool?) Xml.Attribute("extraMasking") ?? false;
		public string Remark => (string) Xml.Element("Remark");

		public IEnumerable<OrderItem> DrawingOrder => GetDrawingOrder();
		public IEnumerable<LevelItem> SymbolLevels => GetSymbolLevels();

		private IEnumerable<OrderItem> GetDrawingOrder()
		{
			try
			{
				var order = Xml.Elements("DrawingOrder").Single();
				return order.Elements("Layer").Select(x => new OrderItem(x));
			}
			catch
			{
				throw FormatError("Expect exactly one DrawingOrder element");
			}
		}

		private IEnumerable<LevelItem> GetSymbolLevels()
		{
			try
			{
				var symbols = Xml.Elements("SymbolLevels").Single();
				return symbols.Elements("Layer").Select(x => new LevelItem(x));
			}
			catch
			{
				throw FormatError("Expect exactly one SymbolLevel element");
			}
		}

		#region Nested types

		public class OrderItem : LayerBase
		{
			public OrderItem(XElement xml) : base(xml) { }

			public IReadOnlyList<LayerLevel> Levels => GetLevels();
			public IReadOnlyList<MaskLayer> MaskedBy => GetMaskedBy();

			private IReadOnlyList<LayerLevel> GetLevels()
			{
				return Xml.Elements("Level").Select(x => new LayerLevel(x)).ToArray();
			}

			private IReadOnlyList<MaskLayer> GetMaskedBy()
			{
				return Xml.Elements("MaskedBy").Select(x => new MaskLayer(x)).ToArray();
			}
		}

		public class LevelItem : LayerBase
		{
			public LevelItem(XElement xml) : base(xml) { }

			public Renderer Renderer => GetRenderer();

			public IEnumerable<Symbol> Symbols => GetSymbols();

			private Renderer GetRenderer()
			{
				var renderer = Xml.Elements("Renderer").SingleOrDefault() ??
				               throw FormatError("No element Renderer");
				return new Renderer(renderer);
			}

			private IEnumerable<Symbol> GetSymbols()
			{
				return Xml.Elements("Symbol").Select(x => new Symbol(x));
			}
		}

		public class MaskLayer : LayerBase
		{
			public MaskLayer(XElement xml) : base(xml) { }
		}

		public class Renderer : XmlWrapperBase
		{
			public const string SimpleType = "simple";
			public const string UniqueValueType = "unique";

			public Renderer(XElement xml) : base(xml) { }

			public string Type => (string) Xml.Attribute("type");
			public string[] Fields => GetFields(); // only for type unique

			private string[] GetFields()
			{
				var text = (string) Xml.Attribute("fields");
				if (text is null) return null;

				const char sep = ',';
				return text.Split(sep).Select(s => s.Trim()).ToArray();
			}
		}

		public class Symbol : XmlWrapperBase
		{
			public const string MatchAny = "*";

			public Symbol(XElement xml) : base(xml) { }

			public string Match => (string) Xml.Attribute("match");
			public string Label => (string) Xml.Attribute("label");
			public string Type => (string) Xml.Attribute("type");

			public IReadOnlyList<SymbolLevel> Levels => GetLevels();

			private IReadOnlyList<SymbolLevel> GetLevels()
			{
				return Xml.Elements("Level").Select(x => new SymbolLevel(x)).ToArray();
			}
		}

		public abstract class LayerBase : XmlWrapperBase
		{
			protected LayerBase(XElement xml) : base(xml) { }

			public string Name => (string)Xml.Attribute("name");
			public string Parent => (string)Xml.Attribute("parent");
			public string Uri => (string)Xml.Attribute("uri");
		}

		public class LayerLevel : LevelBase
		{
			public LayerLevel(XElement xml) : base(xml) { }

			public IEnumerable<MaskLayer> MaskedBy => GetMaskedBy();
		}

		public class SymbolLevel : LevelBase
		{
			public SymbolLevel(XElement xml) : base(xml) { }

			public string Type => (string) Xml.Attribute("type"); // informational

			public IEnumerable<MaskLayer> MaskedBy => GetMaskedBy();
		}

		public abstract class LevelBase : XmlWrapperBase
		{
			protected LevelBase(XElement xml) : base(xml) { }

			public string Name => (string) Xml.Attribute("name");

			protected IEnumerable<MaskLayer> GetMaskedBy()
			{
				return Xml.Elements("MaskedBy").Select(x => new MaskLayer(x));
			}
		}

		public class MaskLayerComparer : IEqualityComparer<MaskLayer>
		{
			public bool Equals(MaskLayer x, MaskLayer y)
			{
				if (x is null && y is null) return true;
				if (x is null || y is null) return false;
				return string.Equals(x.Name, y.Name) &&
				       string.Equals(x.Parent, y.Parent) &&
				       string.Equals(x.Uri, y.Uri, StringComparison.OrdinalIgnoreCase);
			}

			public int GetHashCode(MaskLayer obj)
			{
				return HashCode.Combine(obj.Name, obj.Parent, obj.Uri.ToLowerInvariant());
			}

			public static MaskLayerComparer Instance { get; } = new();

			private MaskLayerComparer() {}
		}

		#endregion
	}

	public interface IFeedback
	{
		int Warnings { get; }
		int Errors { get; }
		IEnumerable<string> Messages { get; }

		void Warning(string message);

		void Error(string message);
	}

	public class Feedback : IFeedback
	{
		private readonly IList<string> _messages = new List<string>();

		public int Warnings { get; private set; }
		public int Errors { get; private set; }

		public IEnumerable<string> Messages => _messages.AsEnumerable();

		public void Warning(string message)
		{
			Warnings += 1;

			if (! string.IsNullOrEmpty(message))
			{
				_messages.Add(message);
			}
		}

		public void Error(string message)
		{
			Errors += 1;

			if (! string.IsNullOrEmpty(message))
			{
				_messages.Add(message);
			}
		}
	}
}
