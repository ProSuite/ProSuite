using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Events;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing.Cracker;

/// <summary>
/// Base class for an additional, toggleable display mode of an editing tool, such as the
/// vertex coordinate labels or the multipatch ring information. It manages the overlays,
/// the enabled state and the redraw on pan/zoom, and it hides the display when zoomed out
/// beyond <see cref="MinimumScaleDenominator"/>.
/// </summary>
/// <remarks>
/// This is the ArcGIS Pro counterpart of the display modes of the ArcObjects
/// CrackPointFeedback, which were re-drawn by the application on every display refresh.
/// In Pro the labels are overlays which must be re-created explicitly, hence the
/// subscription to the <see cref="DrawCompleteEvent"/>.
/// </remarks>
public abstract class OverlayDisplayBase : IDisposable
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private readonly List<IDisposable> _overlays = new List<IDisposable>();

	// The overlays of the rendering currently in progress, swapped in once it is complete
	private readonly List<IDisposable> _newOverlays = new List<IDisposable>();

	private readonly List<Geometry> _shapes = new List<Geometry>();

	private SubscriptionToken _drawCompleteToken;

	private Envelope _lastExtent;
	private double _lastScale = double.NaN;

	private bool _refreshQueued;

	[CanBeNull] private string _lastSuppressionMessage;

	private bool _disposed;

	/// <summary>
	/// The scale denominator beyond which (i.e. zoomed further out than) the display is
	/// hidden. Zooming in again restores it.
	/// </summary>
	public double MinimumScaleDenominator { get; set; } = 1000;

	public bool IsEnabled { get; private set; }

	public bool HasShapes => _shapes.Count > 0;

	/// <summary>
	/// Identifies the features currently displayed (feature class and object ID), or null
	/// if they were set without their features. Lets a subclass tell whether the same
	/// object has been selected again or a different one.
	/// </summary>
	[CanBeNull]
	protected string ShapesSignature { get; private set; }

	/// <summary>The shapes to be displayed, in their original spatial reference.</summary>
	[NotNull]
	protected IList<Geometry> Shapes => _shapes;

	/// <summary>How the mode is called in the log, e.g. "vertex display mode".</summary>
	[NotNull]
	protected abstract string ModeName { get; }

	/// <summary>
	/// How the drawn items are called in the log, e.g. "Vertex labels". Used in plural
	/// form ("... are hidden").
	/// </summary>
	[NotNull]
	protected abstract string DisplayName { get; }

	#region Enabling and shapes

	public Task<bool> ToggleAsync()
	{
		return SetEnabledAsync(! IsEnabled);
	}

	public async Task<bool> SetEnabledAsync(bool enabled)
	{
		if (IsEnabled == enabled)
		{
			return IsEnabled;
		}

		IsEnabled = enabled;

		_msg.InfoFormat("{0} {1}", enabled ? "Enabled" : "Disabled", ModeName);

		if (enabled)
		{
			Subscribe();
		}
		else
		{
			Unsubscribe();
		}

		ClearSuppression();
		_lastExtent = null;

		OnEnabledChangedCore(enabled);

		await RefreshAsync();

		return IsEnabled;
	}

	/// <summary>
	/// Called after the enabled state has changed, before the display is refreshed.
	/// </summary>
	protected virtual void OnEnabledChangedCore(bool enabled) { }

	/// <summary>
	/// Sets the features to be displayed. Must be called on the MCT
	/// (<see cref="Feature.GetShape"/>).
	/// </summary>
	public void SetFeatures([CanBeNull] IEnumerable<Feature> features)
	{
		List<Feature> featureList = features?.ToList();

		SetShapes(featureList?.Select(feature => feature.GetShape()),
		          GetSignature(featureList));
	}

	/// <summary>
	/// Sets the shapes to be displayed and refreshes the display. Must be called on the MCT.
	/// </summary>
	public void SetShapes([CanBeNull] IEnumerable<Geometry> shapes)
	{
		SetShapes(shapes, null);
	}

	private void SetShapes([CanBeNull] IEnumerable<Geometry> shapes,
	                       [CanBeNull] string signature)
	{
		ShapesSignature = signature;

		_shapes.Clear();

		if (shapes is not null)
		{
			foreach (Geometry shape in shapes)
			{
				if (shape is null || shape.IsEmpty)
				{
					continue;
				}

				_shapes.Add(shape);
			}
		}

		// The shapes have changed: do not suppress the redraw in OnDrawCompleted
		_lastExtent = null;

		ClearSuppression();
		OnShapesChangedCore();

		Refresh();
	}

	/// <summary>
	/// Called after the shapes have changed, before the display is refreshed.
	/// </summary>
	protected virtual void OnShapesChangedCore() { }

	// Identifies the features currently displayed, so that a subclass can tell whether the
	// same object has been selected again or a different one. Null if the shapes were set
	// without their features (SetShapes) or if there are none.
	[CanBeNull]
	private static string GetSignature([CanBeNull] IList<Feature> features)
	{
		if (features is null || features.Count == 0)
		{
			return null;
		}

		var keys = new List<string>(features.Count);

		foreach (Feature feature in features)
		{
			using FeatureClass featureClass = feature.GetTable();

			keys.Add($"{featureClass.GetID()}|{featureClass.GetName()}|{feature.GetObjectID()}");
		}

		// The selection order must not matter
		keys.Sort(StringComparer.Ordinal);

		return string.Join(";", keys);
	}

	#endregion

	#region Drawing

	public Task RefreshAsync()
	{
		return QueuedTaskUtils.Run(Refresh);
	}

	/// <summary>Re-creates the overlays. Must be called on the MCT.</summary>
	protected void Refresh()
	{
		_refreshQueued = false;

		if (! IsEnabled || _shapes.Count == 0)
		{
			DisposeOverlays();
			return;
		}

		MapView mapView = MapView.Active;

		Envelope viewExtent = mapView?.Extent;

		if (viewExtent is null)
		{
			DisposeOverlays();
			return;
		}

		double scale = mapView.Camera?.Scale ?? double.NaN;

		// Remember what has been rendered: adding the overlays raises the DrawCompleteEvent,
		// and without this the display would keep re-rendering in response to its own drawing
		// (all the more so with several displays, which trigger each other)
		_lastExtent = viewExtent;
		_lastScale = scale;

		if (! double.IsNaN(scale) && scale > MinimumScaleDenominator)
		{
			DisposeOverlays();

			Suppress(
				$"{DisplayName} are hidden at scales smaller than 1:{MinimumScaleDenominator:N0}. Zoom in to display them.");
			return;
		}

		// Create the new overlays before removing the previous ones and only then swap them:
		// creating them takes a moment for a feature with many vertices, and disposing first
		// would leave the map without labels for that moment, which is visible as a flicker.
		_newOverlays.Clear();

		try
		{
			RenderCore(mapView, viewExtent);
		}
		catch
		{
			Dispose(_newOverlays);
			throw;
		}

		Dispose(_overlays);

		_overlays.AddRange(_newOverlays);
		_newOverlays.Clear();
	}

	/// <summary>
	/// Creates the overlays for the current shapes. Called on the MCT, with the display
	/// already cleared and the minimum scale respected. Implementors are expected to call
	/// <see cref="ClearSuppression"/> once they actually draw something.
	/// </summary>
	protected abstract void RenderCore([NotNull] MapView mapView, [NotNull] Envelope viewExtent);

	/// <summary>
	/// Adds all graphics as a single overlay. Adding them individually does not scale:
	/// a multipatch can easily have thousands of vertices.
	/// </summary>
	protected void AddGraphics([NotNull] MapView mapView, [NotNull] IList<CIMGraphic> graphics)
	{
		if (graphics.Count == 0)
		{
			return;
		}

		// Reference scale -1: the symbol sizes are in points, independent of the map scale
		_newOverlays.Add(mapView.AddOverlay(graphics, -1));
	}

	protected void AddOverlay([CanBeNull] IDisposable overlay)
	{
		if (overlay is not null)
		{
			_newOverlays.Add(overlay);
		}
	}

	public void Clear()
	{
		DisposeOverlays();
	}

	private void DisposeOverlays()
	{
		Dispose(_newOverlays);
		Dispose(_overlays);
	}

	private static void Dispose([NotNull] List<IDisposable> overlays)
	{
		foreach (IDisposable overlay in overlays)
		{
			overlay.Dispose();
		}

		overlays.Clear();
	}

	#endregion

	#region Suppression messages

	/// <summary>
	/// Informs the user why nothing is drawn, but only once per reason: the display is
	/// refreshed on every pan and zoom, and repeating the message would flood the log.
	/// </summary>
	protected void Suppress([NotNull] string message)
	{
		if (string.Equals(_lastSuppressionMessage, message))
		{
			return;
		}

		_lastSuppressionMessage = message;

		_msg.Info(message);
	}

	protected void ClearSuppression()
	{
		_lastSuppressionMessage = null;
	}

	#endregion

	#region Redraw on pan / zoom

	private void Subscribe()
	{
		_drawCompleteToken ??= DrawCompleteEvent.Subscribe(OnDrawCompleted);
	}

	private void Unsubscribe()
	{
		if (_drawCompleteToken != null)
		{
			DrawCompleteEvent.Unsubscribe(_drawCompleteToken);
			_drawCompleteToken = null;
		}
	}

	private void OnDrawCompleted(MapViewEventArgs args)
	{
		try
		{
			if (! IsEnabled)
			{
				return;
			}

			MapView mapView = args.MapView;

			if (mapView is null || mapView != MapView.Active)
			{
				// Refresh renders into the active view and remembers that view's extent.
				// The draws of another view (a second map, a stereo view) would never match
				// it, so every one of them would trigger yet another rendering.
				return;
			}

			Envelope extent = mapView.Extent;

			if (extent is null)
			{
				return;
			}

			double scale = mapView.Camera?.Scale ?? double.NaN;

			// This event is raised repeatedly, also without anything having changed - and in
			// particular by our own drawing (see Refresh)
			if (SameExtent(_lastExtent, extent) && scale.Equals(_lastScale))
			{
				return;
			}

			if (_refreshQueued)
			{
				// A refresh is already on its way and will pick up the current extent
				return;
			}

			_refreshQueued = true;

			QueuedTaskUtils.Run(Refresh);
		}
		catch (Exception e)
		{
			// Event handler: must not throw into the framework
			_msg.Warn($"Error updating the overlays of the {ModeName}", e);
		}
	}

	private static bool SameExtent([CanBeNull] Envelope extent1, [CanBeNull] Envelope extent2)
	{
		if (extent1 is null || extent2 is null)
		{
			return false;
		}

		return extent1.XMin.Equals(extent2.XMin) && extent1.YMin.Equals(extent2.YMin) &&
		       extent1.XMax.Equals(extent2.XMax) && extent1.YMax.Equals(extent2.YMax);
	}

	#endregion

	#region Utilities for implementors

	/// <summary>
	/// Projects the view extent into the shape's spatial reference, so that the vertices
	/// can be tested against it without projecting each one of them.
	/// </summary>
	[NotNull]
	protected static Envelope ProjectToShape([NotNull] Envelope viewExtent,
	                                         [CanBeNull] SpatialReference shapeSpatialReference)
	{
		SpatialReference viewSpatialReference = viewExtent.SpatialReference;

		if (shapeSpatialReference is null || viewSpatialReference is null ||
		    shapeSpatialReference.IsEqual(viewSpatialReference, false))
		{
			return viewExtent;
		}

		return (Envelope) GeometryEngine.Instance.Project(viewExtent, shapeSpatialReference);
	}

	protected static bool IsWithin([NotNull] MapPoint point, [NotNull] Envelope extent)
	{
		return point.X >= extent.XMin && point.X <= extent.XMax &&
		       point.Y >= extent.YMin && point.Y <= extent.YMax;
	}

	#endregion

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;

		Unsubscribe();
		DisposeOverlays();

		_shapes.Clear();
	}
}
