using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ProSuite.AGP.Editing;

// todo 3D, test multipatch sketch symbol!
public class SymbolizedSketchTypeBasedOnSelection : ISymbolizedSketchType
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	[NotNull] private readonly ISymbolizedSketchTool _tool;
	private bool _showFeatureSketchSymbology;
	private readonly Func<SketchGeometryType> _sketchGeometryTypeFunc;

	/// <summary>
	/// Sets sketch geometry type based on current selection.
	/// Also set sketch symbol bases on current selection if
	/// "Show feature symbology in sketch" (Options > Editing) is turned on.
	/// Annotation feature is not supported. Takes the first feature of the
	/// first FeatureLayer if many features are selected from many FeatureLayers.
	/// </summary>
	/// <param name="tool"></param>
	/// <param name="sketchType">Optional sketch type method that replaces the default sketch type</param>
	public SymbolizedSketchTypeBasedOnSelection([NotNull] ISymbolizedSketchTool tool,
	                                            Func<SketchGeometryType> sketchType = null)
	{
		_tool = tool;
		_sketchGeometryTypeFunc = sketchType;

		_showFeatureSketchSymbology = ApplicationOptions.EditingOptions.ShowFeatureSketchSymbology;

		SketchModifiedEvent.Subscribe(OnSketchModified);
	}

	public void Dispose()
	{
		SketchModifiedEvent.Unsubscribe(OnSketchModified);

		_ = ClearSketchSymbol();
	}

	public async Task ClearSketchSymbol()
	{
		_tool.SetSketchSymbol(null);

		await ApplySketchSymbolWorkAround();
	}

	/// <summary>
	/// Must be called on the MCT.
	/// </summary>
	public async Task SetSketchAppearanceAsync()
	{
		_msg.VerboseDebug(() => nameof(SetSketchAppearanceAsync));

		try
		{
			var selection = SelectionUtils.GetSelection<BasicFeatureLayer>(MapView.Active.Map);
			List<long> oids = GetApplicableSelection(selection, out FeatureLayer featureLayer);

			await TrySetSketchAppearanceAsync(featureLayer, oids);
		}
		catch (Exception ex)
		{
			_msg.Error(ex.Message, ex);
		}
	}

	public async Task SelectionChangedAsync(MapSelectionChangedEventArgs args)
	{
		var selection = SelectionUtils.GetSelection<BasicFeatureLayer>(args.Selection);

		await QueuedTask.Run(async () =>
		{
			List<long> oids = GetApplicableSelection(selection, out FeatureLayer featureLayer);

			await TrySetSketchAppearanceAsync(featureLayer, oids);
		});
	}

	/// <summary>
	/// Is always on MCT.
	/// </summary>
	private async void OnSketchModified(SketchModifiedEventArgs args)
	{
		try
		{
			// TODO: If this is a relevant use case, we should request an OptionChangedEvent
			if (ApplicationOptions.EditingOptions.ShowFeatureSketchSymbology ==
			    _showFeatureSketchSymbology)
			{
				return;
			}

			_showFeatureSketchSymbology =
				ApplicationOptions.EditingOptions.ShowFeatureSketchSymbology;

			var selection = SelectionUtils.GetSelection<BasicFeatureLayer>(MapView.Active.Map);
			List<long> oids = GetApplicableSelection(selection, out FeatureLayer featureLayer);

			// only set sketch symbol not sketch type!
			await TrySetSketchAppearanceAsync(featureLayer, oids);
		}
		catch (Exception ex)
		{
			_msg.Error(ex.Message, ex);
		}
	}

	private async Task TrySetSketchAppearanceAsync([CanBeNull] FeatureLayer featureLayer,
	                                               [CanBeNull] IList<long> oids)
	{
		if (featureLayer == null || oids == null)
		{
			await ClearSketchSymbol();
			return;
		}

		if (ApplicationOptions.EditingOptions.ShowFeatureSketchSymbology)
		{
			GeometryType geometryType =
				GeometryUtils.TranslateEsriGeometryType(featureLayer.ShapeType);

			if (await _tool.CanSetConstructionSketchSymbol(geometryType))
			{
				await SetSketchSymbol(GetSymbolReference(featureLayer, oids));
			}
			else
			{
				_msg.Debug($"Cannot set sketch symbol for geometry type {geometryType}");
				await ClearSketchSymbol();
			}
		}
		else
		{
			_msg.Debug(
				"Cannot set sketch symbol. Show feature symbology in sketch is turned off.");
			await ClearSketchSymbol();
		}
	}

	private async Task SetSketchSymbol(CIMSymbolReference symbolReference)
	{
		_tool.SetSketchSymbol(symbolReference);

		await ApplySketchSymbolWorkAround();
	}

	public void SetSketchType()
	{
		Assert.NotNull(_sketchGeometryTypeFunc, "No _sketchGeometryTypeFunc");

		_tool.SetSketchType(_sketchGeometryTypeFunc());
	}

	private List<long> GetApplicableSelection(
		[NotNull] IDictionary<BasicFeatureLayer, List<long>> selection,
		[CanBeNull] out FeatureLayer featureLayer)
	{
		featureLayer = null;

		if (selection.Count <= 0)
		{
			return null;
		}

		var oidsByLayer = SelectionUtils.GetApplicableSelection(selection, _tool.CanSelectFromLayer)
		                                .ToList();

		int layerCount = oidsByLayer.Count;

		if (layerCount > 1)
		{
			_msg.Debug(
				$"Features from {layerCount} different layers selected. Take the first layer.");
		}

		if (! _tool.CanUseSelection(oidsByLayer.ToDictionary(pair => pair.Key, pair => pair.Value)))
		{
			_msg.Debug("Cannot use selection");
			return null;
		}

		(BasicFeatureLayer layer, List<long> oids) = oidsByLayer.FirstOrDefault();

		if (layer is not FeatureLayer featLayer)
		{
			_msg.Debug(
				"Cannot set sketch symbol. No feature selected or no applicable selection from FeatureLayer");
			return null;
		}

		featureLayer = featLayer;
		return oids;
	}

	[CanBeNull]
	private static CIMSymbolReference GetSymbolReference([NotNull] FeatureLayer layer,
	                                                     [CanBeNull] IList<long> oids)
	{
		if (oids == null || oids.Count < 1)
		{
			return null;
		}

		CIMSymbolReference symbolReference = null;

		var activeMap = MapView.Active?.Map;
		if (activeMap is null)
		{
			return null;
		}

		var scaleDenom = activeMap.ReferenceScale;
		var renderer = layer.GetRenderer();

		IList<CIMSymbolReference> cimSymbolReferences = new List<CIMSymbolReference>();
		IList<CIMSymbolReference> similarCimSymbolReferences = new List<CIMSymbolReference>();
		//IList<CIMSymbol> cimSymbols = new List<CIMSymbol>();

		//List of effects to ignore
		List<Type> cimGeometricEffectTypesToIgnore = new List<Type>
		                                           {
			                                           typeof(CIMGeometricEffectCut),
			                                           typeof(CIMGeometricEffectDashes)
		                                           };

		//bool - flag has value changed
		List<(string PropertyName, bool HasChanged)> strokeProperties = new List<(string, bool)>();
		strokeProperties.Add(new ValueTuple<string, bool>("CapStyle", false));
		strokeProperties.Add(new ValueTuple<string, bool>("JoinStyle", false));
		strokeProperties.Add(new ValueTuple<string, bool>("MiterLimit", false));
		string nameOfWidthProperty = "Width";
		strokeProperties.Add(new ValueTuple<string, bool>(nameOfWidthProperty, false));

		foreach (long oid in oids)
		{
			var feature = GetFeature(layer, oid);

			if (feature == null)
			{
				continue;
			}

			var values = new NamedValues(feature);
			CIMSymbolReference symref = SymbolUtils.GetSymbol(renderer, values, scaleDenom, out _);

			strokeProperties.ForEach(item => item.HasChanged = false );

			if (cimSymbolReferences.All(s => !Similar(s, symref, cimGeometricEffectTypesToIgnore, strokeProperties)))
			//if (cimSymbolReferences.All(s => s.ToJson() != symref.ToJson()))
			{
				cimSymbolReferences.Add(symref);
			}
			else
			{
				//Symbols are same only different width
				(string PropertyName, bool HasChanged) property = strokeProperties.Find(x => x.PropertyName == nameOfWidthProperty);
				if (property.HasChanged)
				{
					similarCimSymbolReferences.Add(symref);
				}
			}
		}

		//all selected has same Symbol
		if (cimSymbolReferences.Count == 1)
		{
			symbolReference = cimSymbolReferences[0];
			//Symbols are same only different width
			if (similarCimSymbolReferences.Count > 0)
			{
				//look for symbol with the greatest width and use it
				similarCimSymbolReferences.Add(symbolReference);

				CIMSymbolReference maxCIMSymbolReference = null;
				double maxWidth = 0;
				double leftPoints;
				double rightPoints;
				foreach (CIMSymbolReference cimSymbolReference in similarCimSymbolReferences)
				{
					CIMSymbol cimSymbol = cimSymbolReference.Symbol;
					SymbolUtils.GetLineWidth(cimSymbol as CIMMultiLayerSymbol, out leftPoints, out rightPoints);

					double width = leftPoints + rightPoints;
					if (width > maxWidth)
					{
						maxWidth = width;
						maxCIMSymbolReference = cimSymbolReference;
					}
				}
				symbolReference = maxCIMSymbolReference;
			}
		}

		if (symbolReference == null)
		{
			if (oids.Count == 1)
			{
				_msg.Debug(
					$"Cannot set sketch symbol: no symbol found in layer {layer.Name} for oid {oids[0]}.");
			}
			else
			{
				_msg.Debug(
					$"Cannot set sketch symbol: found different symbols in selection of in layer {layer.Name}.");
			}

			return null;
		}

		return symbolReference;
	}

	private static bool Similar(CIMSymbolReference symrefFrom, CIMSymbolReference symrefTo, List<Type> cimGeometricEffectTypesToIgnore, List<(string PropertyName, bool HasChanged)> strokeProperties)
	{
		CIMSymbolReference symrefFromClone = symrefFrom.Clone();
		CIMSymbolReference symrefToClone = symrefTo.Clone();
		string symrefStrFrom = symrefFromClone.ToJson();
		string symrefStrTo = symrefToClone.ToJson();

		if (symrefStrFrom == symrefStrTo)
		{
			return true;
		}

		CIMSymbol cimSymbolFrom = symrefFromClone.Symbol;
		CIMSymbol cimSymbolTo = symrefToClone.Symbol;
		CIMLineSymbol cimLineSymbolFrom = cimSymbolFrom as CIMLineSymbol;
		CIMLineSymbol cimLineSymbolTo = cimSymbolTo as CIMLineSymbol;
		if (cimLineSymbolFrom != null && cimLineSymbolTo != null)
		{
			CIMSymbolLayer[] cimSymbolLayersFrom = cimLineSymbolFrom.SymbolLayers;
			CIMSymbolLayer[] cimSymbolLayersTo = cimLineSymbolTo.SymbolLayers;

			if (cimSymbolLayersFrom.Length == cimSymbolLayersTo.Length)
			{
				for (int i=0; i<cimSymbolLayersFrom.Length; i++)
				{
					CIMSymbolLayer cimSymbolLayerFrom = cimSymbolLayersFrom[i];
					CIMSymbolLayer cimSymbolLayerTo = cimSymbolLayersTo[i];
					CIMStroke cimStrokeFrom = cimSymbolLayerFrom as CIMStroke;
					CIMStroke cimStrokeTo = cimSymbolLayerTo as CIMStroke;
					if (cimStrokeFrom == null || cimStrokeTo == null)
					{
						return false;
					}

					//set properties equal
					for (int ii = 0; ii < strokeProperties.Count; ii++)
					{
						(string PropertyName, bool HasChanged) propertyPair = strokeProperties[ii];
						string propertyName = propertyPair.PropertyName;
						PropertyInfo propertyInfoTo = cimStrokeTo.GetType().GetProperty(propertyName);
						PropertyInfo propertyInfoFrom = cimStrokeFrom.GetType().GetProperty(propertyName);
						if (propertyInfoFrom != null && propertyInfoTo != null)
						{
							object valueFrom = propertyInfoFrom.GetValue(cimStrokeFrom);
							object valueTo = propertyInfoTo.GetValue(cimStrokeTo);

							if (propertyInfoTo.Name == ReflectionUtils.GetProperty<CIMStroke>("Width").Name)
							{
								if (Math.Abs(cimStrokeFrom.Width - cimStrokeTo.Width) > Double.Epsilon)
								{
									cimStrokeTo.Width = cimStrokeFrom.Width;
									propertyPair.HasChanged = true;
								}
							}
							else if (!EqualsByValue(valueFrom, valueTo))
							{
								propertyInfoTo.SetValue(cimStrokeTo, valueFrom);
								propertyPair.HasChanged = true;
							}
						}

						//set back
						strokeProperties[ii] = propertyPair;
					}

					CIMGeometricEffect[] cimEffectsFrom = cimStrokeFrom.Effects;
					CIMGeometricEffect[] cimEffectsTo = cimStrokeTo.Effects;
					if (cimEffectsFrom.Length != cimEffectsTo.Length)
					{
						return false;
					}

					if (cimGeometricEffectTypesToIgnore.Count > 0)
					{
						//remove given CIMGeometricEffectTypes
						CIMGeometricEffect[] newcimEffectsFrom = cimEffectsFrom.Where(e => !cimGeometricEffectTypesToIgnore.Contains(e.GetType())).ToArray();
						cimStrokeFrom.Effects = newcimEffectsFrom;
						cimSymbolLayersFrom[i] = cimStrokeFrom;

						CIMGeometricEffect[] newcimEffectsTo = cimEffectsTo.Where(e => !cimGeometricEffectTypesToIgnore.Contains(e.GetType())).ToArray();
						cimStrokeTo.Effects = newcimEffectsTo;
						cimSymbolLayersTo[i] = cimStrokeTo;
					}
				}

				//is it now equals?
				symrefStrFrom = symrefFromClone.ToJson();
				symrefStrTo = symrefToClone.ToJson();
				if (symrefStrFrom == symrefStrTo)
				{
					return true;
				}
			}
		}

		return false;
	}

	private static IDictionary<string, object> ToDictionary(object source)
	{
		var fields = source.GetType().GetFields(
			BindingFlags.GetField |
			BindingFlags.Public |
			BindingFlags.Instance).ToDictionary
		(
			propInfo => propInfo.Name,
			propInfo => propInfo.GetValue(source) ?? string.Empty
		);

		var properties = source.GetType().GetProperties(
			BindingFlags.GetField |
			BindingFlags.GetProperty |
			BindingFlags.Public |
			BindingFlags.Instance).ToDictionary
		(
			propInfo => propInfo.Name,
			propInfo => propInfo.GetValue(source, null) ?? string.Empty
		);

		return fields.Concat(properties).ToDictionary(key => key.Key, value => value.Value); ;
	}

	private static bool IsAnonymousType(object instance)
	{
		if (instance == null)
			return false;

		return instance.GetType().Namespace == null;
	}

	private static IDictionary<string, object> ToFlattenDictionary(object source, string parentPropertyKey = null, IDictionary<string, object> parentPropertyValue = null)
	{
		var propsDic = parentPropertyValue ?? new Dictionary<string, object>();
		//foreach (var item in source.ToDictionary())
		foreach (var item in ToDictionary(source))
		{
			var key = string.IsNullOrEmpty(parentPropertyKey) ? item.Key : $"{parentPropertyKey}.{item.Key}";
			//if (item.Value.IsAnonymousType())
			//	return item.Value.ToFlattenDictionary(key, propsDic);
			if (IsAnonymousType(item.Value))
				return ToFlattenDictionary(item.Value,key, propsDic);
			else
				propsDic.Add(key, item.Value);
		}
		return propsDic;
	}

	public static bool EqualsByValue(object source, object destination)
	{
		//var firstDic = source.ToFlattenDictionary();
		//var secondDic = destination.ToFlattenDictionary();
		var firstDic = ToFlattenDictionary(source);
		var secondDic = ToFlattenDictionary(destination);
		if (firstDic.Count != secondDic.Count)
			return false;
		if (firstDic.Keys.Except(secondDic.Keys).Any())
			return false;
		if (secondDic.Keys.Except(firstDic.Keys).Any())
			return false;
		return firstDic.All(pair => pair.Value.ToString().Equals(secondDic[pair.Key].ToString())
		);
	}

	[CanBeNull]
	private static Feature GetFeature(FeatureLayer layer, long oid)
	{
		using var featureClass = layer.GetFeatureClass();
		if (featureClass is null) return null;
		return GdbQueryUtils.GetFeature(featureClass, oid);
	}

	private static async Task ApplySketchSymbolWorkAround()
	{
		Geometry sketch = await MapView.Active.GetCurrentSketchAsync();

		if (sketch?.IsEmpty == false)
		{
			// We could be in the middle of a sketch phase and de-select some irrelevant selection
			// on an unrelated layer. Rather continue with the wrong sketch symbol than losing the
			// entire sketch!
			return;
		}

		// This is needed (apparently inside a QueuedTask) to make the sketch symbol take effect.
		// It likely triggers the relevant events internally...
		await MapView.Active.ClearSketchAsync();
	}

	#region Nestsed type: NamedValues

	private class NamedValues : INamedValues
	{
		private readonly Row _row;

		public NamedValues([NotNull] Row row)
		{
			_row = row ?? throw new ArgumentNullException(nameof(row));
		}

		public bool Exists(string name)
		{
			return name is not null && _row.FindField(name) >= 0;
		}

		public object GetValue(string name)
		{
			return _row[name];
		}
	}

	#endregion
}
