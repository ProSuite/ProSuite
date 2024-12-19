using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Events;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Editing.Events;
using ArcGIS.Desktop.Editing.Templates;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.Selection;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing.CreateFeatures;

// Similar to CreateMultiplePointsToolBase, but can create AND delete
// points in one go. For usage also see the subclass (may in another repo).
//
// Typical DAML entry:
// 
//         <tool id="ProSuite_Editing_AddRemovePointsTool"
//               caption="Add/Remove Points" keytip="ARP"
//               className="...AddRemovePointsTool"
//               smallImage="Images/AddRemovePointsTool70.png"
//               largeImage="Images/AddRemovePointsTool70.png"
//               condition="esri_mapping_mapPane"
//               categoryRefID="esri_editing_construction_point">
//           <tooltip heading="Add/Remove Points">
// Add and remove multiple points in one go:
// - click on existing point to remove it,
// - click anywhere else to create a point.
// - hit ENTER or F2 to commit,
// - hit ESCAPE to abort.
// Works on the current template, which must be for a point feature layer.
//           </tooltip>
//         </tool>

/// <summary>
/// Add and remove multiple point features in one go:
/// click on existing point: mark for deletion,
/// click on empty space: schedule for creation
/// enter/esc to commit/abort (2nd esc clears selection).
/// This base class operates on the current template;
/// subclasses may operate on any layer.
/// </summary>
[UsedImplicitly]
public class AddRemovePointsTool : MapTool
{
	private readonly IList<Element> _elements;
	private CIMSymbolReference _addSymbol;
	private CIMSymbolReference _removeSymbol;
	private SubscriptionToken _editCompletedToken;

	private static readonly IMsg _msg = Msg.ForCurrentClass();

	[PublicAPI]
	public AddRemovePointsTool()
	{
		IsSketchTool = true;
		UseSnapping = true;
		SketchType = SketchGeometryType.Point;
		SketchOutputMode = SketchOutputMode.Map; // sketch in map coordinates
		UsesCurrentTemplate = true; // only get activated when there is a current template
		FireSketchEvents = true; // so others could intervene (the overrides here are not affected)
		IsWYSIWYG = true;

		//ContextToolbarID = null; // use default
		//ContextMenuID = null; // use default
		//SegmentContextMenuID = null; // use default

		_elements = new List<Element>();
		CurrentValues = new Dictionary<string, object>();
	}

	#region Customizable

	protected override void OnUpdate()
	{
		// Do NOT use MapTool.CurrentTemplate nor MapTool.ActiveMapView
		// because those are only updated on tool activation! Use the
		// globals MapView.Active and EditingTemplate.Current instead.

		try
		{
			// OnUpdate() must be very fast because it is called very
			// frequently! No need to test for MapView.Active: that's
			// handled by the esri_mapping_mapPane condition in DAML;
			// set_DisabledText is slow but get_DisabledText is fast.

			const string disabledText = "No point feature template selected";

			var template = EditingTemplate.Current;

			if (template?.Layer is not FeatureLayer featureLayer ||
			    featureLayer.ShapeType != esriGeometryType.esriGeometryPoint)
			{
				Enabled = false;

				if (DisabledTooltip != disabledText)
				{
					DisabledTooltip = disabledText;
				}
			}
			else
			{
				Enabled = true;
			}
		}
		catch (Exception ex)
		{
			Enabled = false;

			if (DisabledTooltip != ex.Message)
			{
				DisabledTooltip = ex.Message;
			}
		}
	}

	protected IDictionary<string, object> CurrentValues { get; }

	[CanBeNull]
	protected virtual FeatureLayer TargetLayer
	{
		get
		{
			// Get layer from current template if it's a point feature layer:
			if (CurrentTemplate?.Layer is not FeatureLayer featureLayer) return null;
			if (featureLayer.ShapeType != esriGeometryType.esriGeometryPoint) return null;
			return featureLayer;
			// Alternative idea: first/single selected point feature layer in ToC
		}
	}

	protected virtual bool DoubleClickCommits => true;

	protected virtual bool SelectNewFeatures => true;

	protected virtual SelectionSettings GetSelectionSettings()
	{
		return new SelectionSettings();
	}

	protected virtual bool AllowEscapeToDefaultTool => false;

	/// <remarks>Will be called on the MCT</remarks>
	protected virtual void Activate()
	{
		CurrentValues.Clear();

		var inspector = CurrentTemplate?.Inspector;
		if (inspector is not null)
		{
			foreach (var attribute in inspector)
			{
				CurrentValues[attribute.FieldName] = attribute.CurrentValue;
			}
		}
	}

	protected virtual bool WantHandleKey(KeyEventArgs args)
	{
		return false;
	}

	protected virtual bool HandleKeyEvent(KeyEventArgs args)
	{
		return false; // subclass may override and return true if handled
	}

	protected virtual string GetSketchTip()
	{
		var adds = _elements.Count(e => e.Operation == Operation.Add);
		var dels = _elements.Count(e => e.Operation == Operation.Remove);
		if (adds <= 0 && dels <= 0) return null; // no sketch tip
		var sb = new StringBuilder();
		sb.Append("ENTER commit ESC abort");
		if (adds > 0) sb.Append($" +{adds}");
		if (dels > 0) sb.Append($" -{dels}");
		return sb.ToString();
	}

	protected virtual CIMSymbolReference GetPreviewSymbolAdd(
		FeatureLayer targetLayer, Dictionary<string, object> values)
	{
		// by default, use the layer's symbol, but in a lighter color
		return Lighten(LookupSymbol(targetLayer, values));
	}

	protected virtual CIMSymbolReference GetPreviewSymbolRemove(
		FeatureLayer targetLayer)
	{
		return null; // use default symbol; subclass may override
	}

	#endregion

	#region Base overrides

	protected override async Task OnToolActivateAsync(bool hasMapViewChanged)
	{
		// NB: if UsesCurrentTemplate, this is only called
		// if a current template is actually selected!

		try
		{
			await QueuedTask.Run(() =>
			{
				// Ensure default symbols are created (needs MCT)
				_ = AddSymbol;
				_ = RemoveSymbol;

				CurrentValues.Clear(); // defined start

				Activate(); // subclass hook

				if (IsWYSIWYG)
				{
					UpdateSketch();
				}
			});

			_activated = true;
			_firstMove = true;

			// Subscribe to EditCompleted (but at most once) to get Undo/Redo events:
			if (_editCompletedToken is not null)
			{
				EditCompletedEvent.Unsubscribe(_editCompletedToken);
				_editCompletedToken = null;
			}

			_editCompletedToken = EditCompletedEvent.Subscribe(OnEditCompleted);
		}
		catch (Exception ex)
		{
			Gateway.ShowError(ex, _msg);
		}
	}

	protected override Task OnToolDeactivateAsync(bool hasMapViewChanged)
	{
		try
		{
			_activated = false;

			ClearElements();

			if (_editCompletedToken is not null)
			{
				EditCompletedEvent.Unsubscribe(_editCompletedToken);
				_editCompletedToken = null;
			}
		}
		catch (Exception ex)
		{
			Gateway.LogError(ex, _msg);
		}

		return Task.FromResult(0);
	}

	//protected override void OnCurrentTemplateUpdated()
	//{
	//	var template = base.CurrentTemplate;
	//	base.OnCurrentTemplateUpdated();
	//}

	//protected override Task OnSelectionChangedAsync(MapSelectionChangedEventArgs args)
	//{
	//	// probably nothing to do here
	//	return base.OnSelectionChangedAsync(args);
	//}

	protected override async void OnToolDoubleClick(MapViewMouseButtonEventArgs args)
	{
		try
		{
			if (DoubleClickCommits)
			{
				 await QueuedTask.Run(Commit);
			}
		}
		catch (Exception ex)
		{
			Gateway.ShowError(ex, _msg);
		}
	}

	protected override void OnToolKeyDown(MapViewKeyEventArgs args)
	{
		try
		{
			if (args.Key is Key.F2 or Key.Enter)
			{
				args.Handled = true; // commit
			}
			else if (args.Key is Key.Escape)
			{
				var keepActivated = ! AllowEscapeToDefaultTool;
				args.Handled = HasSketch || HasSelection || keepActivated;
			}
			else if (WantHandleKey(args))
			{
				args.Handled = true; // subclass stuff
			}
			else if (args.Key is Key.Z && IsCtrlDown)
			{
				args.Handled = _elements.Count > 0; // anything to undo?
			}
		}
		catch (Exception ex)
		{
			Gateway.LogError(ex, _msg);
		}
	}

	private bool HasSelection => (ActiveMapView?.Map?.SelectionCount ?? 0) > 0;
	private bool HasSketch => _elements.Count > 0;

	protected override async Task HandleKeyDownAsync(MapViewKeyEventArgs args)
	{
		try
		{
			if (args.Key is Key.F2 or Key.Enter)
			{
				await QueuedTask.Run(Commit);
			}
			else if (args.Key == Key.Escape)
			{
				// 1. clear sketch
				// 2. clear selection
				// 3. activate Explore Tool

				if (HasSketch)
				{
					await QueuedTask.Run(Abort);
				}
				else if (HasSelection)
				{
					await QueuedTask.Run(ClearSelection);
				}
				//else if (AllowEscapeToDefaultTool)
				//{
				//	// Activate Explore Tool (better: configured Default Tool)
				//	// SetCurrentToolAsync(null) activates the default tool
				//	const string ExploreToolID = "esri_mapping_exploreTool";
				//	FrameworkApplication.SetCurrentToolAsync(ExploreToolID);
				//	// Beware: await on SetCurrentToolAsync() results in a deadlock! Others experience the same:
				//	// https://community.esri.com/t5/arcgis-pro-sdk-questions/deactivate-a-map-tool-on-sketch-completed/td-p/878882
				//}
			}
			else if (HandleKeyEvent(args))
			{
				if (IsWYSIWYG)
				{
					await QueuedTask.Run(UpdateSketch);
				}
			}
			else if (args.Key == Key.Z && IsCtrlDown)
			{
				if (_elements.Count > 0)
				{
					var element = _elements[_elements.Count - 1];
					element.Overlay.Dispose();
					_elements.RemoveAt(_elements.Count - 1);
				}
			}
		}
		catch (Exception ex)
		{
			Gateway.ReportError(ex, _msg);
		}
	}

	private static bool IsCtrlDown => Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

	protected override bool? OnActivePaneChanged(Pane pane)
	{
		// Called when the active (map) pane changed
		try
		{
			ClearElements();
		}
		catch (Exception ex)
		{
			Gateway.LogError(ex, _msg);
		}
		return null;
	}

	protected override Task<bool> OnSketchCanceledAsync()
	{
		try
		{
			ClearElements();
			return Task.FromResult(true);
		}
		catch (Exception ex)
		{
			Gateway.ReportError(ex, _msg);
			return Task.FromResult(true);
		}
	}

	protected override Task<bool> OnSketchModifiedAsync()
	{
		// nothing to do here: for SketchType == Point, Modified and Complete occur
		// immediately one after the other, though called from different threads(!)
		return Task.FromResult(true);
	}

	protected override async Task<bool> OnSketchCompleteAsync(Geometry sketch)
	{
		try
		{
			if (sketch is MapPoint point)
			{
				await QueuedTask.Run(() => UpdateElements(point));
			}
			//else: should not occur because we asked for a point sketch
		}
		catch (Exception ex)
		{
			Gateway.ReportError(ex, _msg);
		}

		return true;
	}

	#endregion

	#region Hack

	// When the SketchSymbol is initially set in OnActivate,
	// it is drawn far too small (symbol properties look good
	// and remain unchanged, so it must be some ref scale issue);
	// setting it again on first mouse move solves the problem:

	private bool _activated; // set to true in OnActivate, to false in OnDeactivate
	private bool _firstMove; // set to true in OnActivate, to false in OnMouseMove

	protected override void OnToolMouseMove(MapViewMouseEventArgs args)
	{
		try
		{
			if (_activated && _firstMove)
			{
				_firstMove = false;
				var symbol = SketchSymbol;
				SketchSymbol = null;
				SketchSymbol = symbol;
			}
		}
		catch (Exception ex)
		{
			Gateway.LogError(ex, _msg);
		}
	}

	#endregion

	private Task OnEditCompleted(EditCompletedEventArgs args)
	{
		try
		{
			if (args.CompletedType == EditCompletedType.Undo ||
			    args.CompletedType == EditCompletedType.Redo)
			{
				if (_elements.Count > 0)
				{
					ClearElements();
					_msg.Warn($"{Caption}: sketch discarded because an {args.CompletedType} " +
					          $"operation occurred; please sketch again");
					// TODO could analyze args.Creates/Deletes/Modifies and update _elements accordingly
				}
			}
		}
		catch (Exception ex)
		{
			Gateway.ReportError(ex, _msg);
		}

		return Task.CompletedTask;
	}

	/// <summary>Update sketch symbol and tip based on current values</summary>
	/// <remarks>Must call on MCT</remarks>
	private void UpdateSketch()
	{
		var referenceScale = TargetLayer?.Map?.ReferenceScale ?? -1;
		var symbol = LookupSymbol(TargetLayer, CurrentValues, referenceScale);
		SketchSymbol = symbol.SetAlpha(67f);
		SketchTip = GetSketchTip();
	}

	/// <remarks>Must call on MCT</remarks>
	private void UpdateElements(MapPoint point)
	{
		// - sketch within eps of existing element: drop this element
		// - otherwise: add new element
		//   - if within eps of existing point feature: of type Remove
		//   - otherwise: of type Add

		var settings = GetSelectionSettings();
		var tolerancePixels = settings.SelectionTolerancePixels;
		var delta = MapUtils.ConvertScreenPixelToMapLength(ActiveMapView, tolerancePixels, point);

		var element = FindElement(_elements, point, delta);

		if (element is null)
		{
			var oid = FindExisting(TargetLayer, point, delta, out var snapped);
			if (oid < 0)
			{
				var values = CurrentValues.ToDictionary(p => p.Key, p => p.Value); // copy!

				// NB: CurrentTemplate.GetSymbol() gives symbol from template, BUT
				// ignores overrides from updated inspector values! Must roll our own:

				var symbol = GetPreviewSymbolAdd(TargetLayer, values);
				var referenceScale = TargetLayer?.Map?.ReferenceScale ?? -1;
				var overlay = AddOverlay(point, symbol ?? AddSymbol, referenceScale);

				element = new Element(point, Operation.Add, overlay, values);
				_elements.Add(element);
			}
			else
			{
				var symbol = GetPreviewSymbolRemove(TargetLayer);
				var overlay = AddOverlay(snapped ?? point, symbol ?? RemoveSymbol);

				element = new Element(snapped ?? point, Operation.Remove, overlay, oid);
				_elements.Add(element);
			}
		}
		else
		{
			// already sketched point:
			element.Overlay.Dispose();
			_elements.Remove(element);
		}

		SketchTip = GetSketchTip();
	}

	private void ClearElements()
	{
		foreach (var element in _elements)
		{
			element.Overlay?.Dispose();
		}

		_elements.Clear();

		SketchTip = GetSketchTip();
	}

	/// <remarks>Must call on MCT</remarks>
	private void ClearSelection()
	{
		var map = ActiveMapView?.Map;
		map?.ClearSelection();
	}

	/// <summary>
	/// Find element in given <paramref name="elements"/> closest to given
	/// <paramref name="point"/> but no farther than <paramref name="radius"/> away.
	/// </summary>
	private static Element FindElement(IList<Element> elements, MapPoint point, double radius)
	{
		if (elements is null) return null;
		if (point is null) return null;

		var rr = radius * radius;
		var minDistance = double.MaxValue;
		Element minElement = null;

		foreach (var element in elements)
		{
			var dx = element.Point.X - point.X;
			var dy = element.Point.Y - point.Y;
			var dd = dx * dx + dy * dy;
			if (dd > rr) continue;
			if (dd < minDistance)
			{
				minDistance = dd;
				minElement = element;
			}
		}

		return minElement;
	}

	/// <summary>
	/// Find point feature on given <paramref name="layer"/> closest to given
	/// <paramref name="point"/> but no farther than <paramref name="radius"/> away.
	/// </summary>
	/// <returns>OID of feature (and <paramref name="snapped"/>) if found,
	/// -1 if not found (<paramref name="snapped"/> is null)</returns>
	private static long FindExisting(Layer layer, MapPoint point, double radius, out MapPoint snapped)
	{
		const long notFound = -1;
		var rr = radius * radius;
		var minDistance = double.MaxValue;
		long minObjectID = notFound;
		snapped = null;

		if (layer is BasicFeatureLayer featureLayer)
		{
			using var table = featureLayer.GetTable();
			if (table is not FeatureClass featureClass) return notFound;
			using var defn = featureClass.GetDefinition();

			double cx = point.X;
			double cy = point.Y;
			var extent = EnvelopeBuilderEx.CreateEnvelope(cx - radius, cy - radius, cx + radius, cy + radius);

			var query = new SpatialQueryFilter();
			var oidFieldName = defn.GetObjectIDField();
			var shapeFieldName = defn.GetShapeField();
			query.SubFields = string.Concat(oidFieldName, ",", shapeFieldName);
			query.FilterGeometry = extent;
			query.SpatialRelationship = SpatialRelationship.Intersects;

			using var cursor = featureLayer.Search(query);
			if (cursor is null) return notFound;

			while (cursor.MoveNext())
			{
				using var row = cursor.Current; // row.Dispose() is called for each iteration (good)
				if (row is not Feature feature) continue;
				var shape = feature.GetShape();
				if (shape is not MapPoint existing) continue;
				var dx = existing.X - point.X;
				var dy = existing.Y - point.Y;
				var dd = dx * dx + dy * dy;
				if (dd > rr) continue; // too far away
				if (dd < minDistance)
				{
					minDistance = dd;
					minObjectID = feature.GetObjectID();
					snapped = existing;
				}
			}
		}

		return minObjectID;
	}

	private Task<bool> Commit()
	{
		var targetLayer = TargetLayer;
		if (targetLayer is null)
		{
			return Task.FromResult(false);
		}

		var adds = _elements.Where(e => e.Operation == Operation.Add).ToList();
		var drops = _elements.Where(e => e.Operation == Operation.Remove).ToList();

		var operation = new EditOperation();
		operation.Name = $"Update {targetLayer.Name} (+{adds.Count} -{drops.Count})";
		operation.SelectNewFeatures = SelectNewFeatures;

		foreach (var element in adds)
		{
			operation.Create(targetLayer, element.Point, element.Values);
		}

		if (drops.Any())
		{
			var oids = drops.Select(e => e.ObjectID).Where(oid => oid >= 0).ToList();
			operation.Delete(targetLayer, oids);
		}

		try
		{
			if (operation.IsEmpty)
			{
				// No adds and no drops: nothing to do (Execute on empty op gives error)
				return Task.FromResult(true);
			}

			return operation.ExecuteAsync();
		}
		finally
		{
			ClearElements();
		}
	}

	private void Abort()
	{
		ClearElements();
	}

	#region Symbology

	/// <summary>
	/// Get the symbol that a feature with the given <paramref name="values"/>
	/// on the given <paramref name="layer"/> would be drawn with.
	/// </summary>
	protected static CIMSymbolReference LookupSymbol(
		FeatureLayer layer, IDictionary<string, object> values = null, double scaleDenom = 0D)
	{
		if (layer is null) return null;
		var cimRenderer = layer.GetRenderer();
		var namedValues = new NamedValues(values);
		return SymbolUtils.GetSymbol(cimRenderer, namedValues, scaleDenom);
	}

	private CIMSymbolReference AddSymbol => _addSymbol ??= CreateAddSymbol();

	private CIMSymbolReference RemoveSymbol => _removeSymbol ??= CreateRemoveSymbol();

	private static CIMSymbolReference CreateAddSymbol()
	{
		var green = ColorFactory.Instance.GreenRGB;
		const double size = 10; // pt
		var marker = SymbolFactory.Instance.ConstructMarker(green, size, SimpleMarkerStyle.Circle);
		var symbol = SymbolFactory.Instance.ConstructPointSymbol(marker);
		symbol.UseRealWorldSymbolSizes = false; // unsure, seems to have no effect here
		return symbol.MakeSymbolReference();
	}

	private static CIMSymbolReference CreateRemoveSymbol()
	{
		var red = ColorFactory.Instance.RedRGB;
		const double size = 14; // pt
		var marker = SymbolFactory.Instance.ConstructMarker(red, size, SimpleMarkerStyle.X);
		var symbol = SymbolFactory.Instance.ConstructPointSymbol(marker);
		symbol.UseRealWorldSymbolSizes = false; // unsure, seems to have no effect here
		return symbol.MakeSymbolReference();
	}

	private static CIMSymbolReference Lighten(CIMSymbolReference symbol)
	{
		return symbol.Blend(ColorUtils.WhiteRGB, 0.33f);
	}

	#endregion

	#region Nested types

	private class Element
	{
		public MapPoint Point { get; }
		public Operation Operation { get; }
		public IDisposable Overlay { get; }
		public long ObjectID { get; } // only for existing points to be deleted; otherwise -1
		public Dictionary<string, object> Values { get; } // field values for Operation Add

		public Element(MapPoint point, Operation op, IDisposable overlay, long oid)
		{
			Point = point ?? throw new ArgumentNullException(nameof(point));
			Operation = op;
			Overlay = overlay;
			ObjectID = oid;
			Values = null;
		}

		public Element(MapPoint point, Operation op, IDisposable overlay, Dictionary<string, object> values)
		{
			Point = point ?? throw new ArgumentNullException(nameof(point));
			Operation = op;
			Overlay = overlay;
			ObjectID = -1;
			Values = values;
		}

		public override string ToString()
		{
			if (Operation == Operation.Add)
			{
				var sb = new StringBuilder("Add new");

				if (Values != null)
				{
					foreach (var pair in Values)
					{
						var field = pair.Key;
						var value = pair.Value;
						if (value is string or double or float or bool
						    or int or long or short or byte
						    or uint or ulong or ushort or sbyte
						    or DateTime or DateTimeOffset)
						{
							sb.Append($" {field}={value}");
						}
					}
				}

				return sb.ToString();
			}

			if (Operation == Operation.Remove)
			{
				return $"Remove OID {ObjectID}";
			}

			return base.ToString();
		}
	}

	private enum Operation { Add, Remove }

	/// <summary>Adapter from Dictionary to <see cref="INamedValues"/></summary>
	private class NamedValues : INamedValues
	{
		private readonly IDictionary<string, object> _dict;

		public NamedValues([CanBeNull] IDictionary<string, object> dict)
		{
			_dict = dict;
		}

		public bool Exists(string name)
		{
			if (name is null || _dict is null) return false;
			return _dict.ContainsKey(name);
		}

		public object GetValue(string name)
		{
			if (name is null || _dict is null) return null;
			return _dict.TryGetValue(name, out var value) ? value : null;
		}
	}

	#endregion
}
