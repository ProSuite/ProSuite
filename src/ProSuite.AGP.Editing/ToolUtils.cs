using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing.Templates;
using ArcGIS.Desktop.Internal.Mapping;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing
{
	// todo daro use GeometryUtils, GeometryFactory
	public static class ToolUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull]
		public static Cursor GetCursor([NotNull] byte[] bytes)
		{
			Assert.ArgumentNotNull(bytes, nameof(bytes));

			return new Cursor(new MemoryStream(bytes));
		}

		public static string GetDisabledReasonNoGeometryMicroservice()
		{
			return
				"Geometry Microservice not found or not started. Please make sure the latest ProSuite Extension is installed.";
		}

		/// <summary>
		/// Determines whether the sketch geometry is a single click.
		/// </summary>
		/// <param name="sketchGeometry">The sketch geometry</param>
		/// <param name="selectionTolerancePixels">The selection tolerance. If the provided sketch
		/// geometry (envelope) is smaller than this value, it is assumed to be a single click.
		/// NOTE: Newer laptops have probably such a high pixel density, that the selection settings
		/// should not be a fixed pixel count but specified in mm and then calculated.</param>
		/// <returns></returns>
		public static bool IsSingleClickSketch([NotNull] Geometry sketchGeometry,
		                                       int selectionTolerancePixels)
		{
			_msg.VerboseDebug(() => $"Sketch width: {sketchGeometry.Extent.Width}, " +
			                        $"sketch height: {sketchGeometry.Extent.Height}");

			double selectionToleranceMapUnits = MapUtils.ConvertScreenPixelToMapLength(
				MapView.Active, selectionTolerancePixels, sketchGeometry.Extent.Center);

			_msg.VerboseDebug(() => $"Selection tolerance in map units: {selectionToleranceMapUnits}");

			return sketchGeometry.Extent.Width <= selectionToleranceMapUnits &&
			       sketchGeometry.Extent.Height <= selectionToleranceMapUnits;
		}

		public static Geometry GetSinglePickSelectionArea([NotNull] Geometry sketchGeometry,
		                                                  int selectionTolerancePixels)
		{
			MapPoint sketchPoint = sketchGeometry.Extent.Center;

			return ExpandGeometryByPixels(sketchPoint, selectionTolerancePixels);
		}

		public static Geometry SketchToSearchGeometry([NotNull] Geometry sketch,
		                                              int selectionTolerancePixels,
		                                              out bool singleClick)
		{
			singleClick = IsSingleClickSketch(sketch, selectionTolerancePixels);

			if (singleClick)
			{
				sketch = GetSinglePickSelectionArea(sketch, selectionTolerancePixels);
			}

			return sketch;
		}

		/// <summary>
		/// Make the given sketch geometry into a polygon suitable for searching.
		/// If the sketch geometry comes from a "point click", create an envelope
		/// around this point (width=height=2*tolerance); otherwise return the
		/// sketch geometry unmodified.
		/// </summary>
		public static Geometry SketchToSearchGeometry(Geometry sketch, double tolerance,
		                                              out bool isPointClick)
		{
			var extent = sketch.Extent;
			isPointClick = extent.Width <= tolerance && extent.Height <= tolerance;

			if (isPointClick)
			{
				var clickPoint = sketch.Extent.Center;

				var delta = tolerance * 2;

				// Expand the envelope of the clicked point by tolerance on all sides,
				// then make it a Polygon to have a high-level geometry:

				var clickExtent = clickPoint.Extent;
				sketch = GeometryFactory.CreatePolygon(
					clickExtent.Expand(delta, delta, false),
					clickExtent.SpatialReference);
			}

			return sketch;
		}

		public static double ConvertScreenPixelToMapLength(MapView mapView, int pixels, MapPoint atPoint)
		{
			return MapUtils.ConvertScreenPixelToMapLength(mapView, pixels, atPoint);
		}

		/// <summary>
		/// Determines if the provided sketch selects the specified derived geometry.
		/// </summary>
		/// <param name="sketch">The sketch geometry to be used as test geometry.
		/// In case of a single point make sure it has been buffered by the search tolerance.</param>
		/// <param name="derivedGeometry"></param>
		/// <param name="singlePick">For single pick, the derived geometry only needs to intersect
		/// to be considered selected. Otherwise, the derived geometry must be fully contained in
		/// the sketch.</param>
		/// <returns></returns>
		public static bool IsSelected([NotNull] Geometry sketch,
		                              [NotNull] Geometry derivedGeometry,
		                              bool singlePick)
		{
			if (GeometryUtils.Disjoint(sketch, derivedGeometry))
			{
				return false;
			}

			if (singlePick)
			{
				// Any intersection is enough:
				return true;
			}

			return GeometryUtils.Contains(sketch, derivedGeometry);
		}

		/// <summary>
		/// Selects the specified features but only in the layers that already have a selection.
		/// </summary>
		/// <param name="newFeatures"></param>
		/// <param name="mapView"></param>
		public static void SelectNewFeatures(IEnumerable<Feature> newFeatures, MapView mapView)
		{
			var layersWithSelection =
				SelectionUtils.GetSelection(mapView.Map).Keys.OfType<BasicFeatureLayer>().ToList();

			SelectionUtils.SelectFeatures(newFeatures, layersWithSelection);
		}

		private static Geometry ExpandGeometryByPixels(Geometry sketchGeometry,
		                                               int pixelBufferDistance)
		{
			double bufferDistance =
				MapUtils.ConvertScreenPixelToMapLength(
				MapView.Active, pixelBufferDistance, sketchGeometry.Extent.Center);

			double envelopeExpansion = bufferDistance * 2;

			Envelope envelope = sketchGeometry.Extent;

			// NOTE: MapToScreen in stereo map is sensitive to Z value (Picker location!)

			// Rather than creating a non-Z-aware polygon with elliptic arcs by using buffer...
			//Geometry selectionGeometry =
			//	GeometryEngine.Instance.Buffer(sketchGeometry, bufferDistance);

			// Just expand the envelope
			// .. but PickerViewModel needs a polygon to display selection geometry (press space).

			// HasZ, HasM and HasID are inherited from input geometry. Thereßss no need
			// for GeometryUtils.EnsureGeometrySchema()

			return GeometryFactory.CreatePolygon(
				envelope.Expand(envelopeExpansion, envelopeExpansion, false),
				envelope.SpatialReference);
		}

		public static async Task<bool> FlashResultPolygonsAsync(
			[NotNull] MapView activeView,
			[NotNull] IDictionary<Feature, Geometry> resultFeatures,
			int maxFeatureCountThreshold = 5)
		{
			if (resultFeatures.Count > maxFeatureCountThreshold)
			{
				_msg.InfoFormat("{0} have been updated (no feature flashing).",
				                resultFeatures.Count);
				return false;
			}

			var polySymbol = SymbolFactory.Instance.DefaultPolygonSymbol;

			foreach (Geometry resultGeometry in resultFeatures.Values)
			{
				if (! (resultGeometry is Polygon poly))
				{
					continue;
				}

				using (await activeView.AddOverlayAsync(poly,
				                                        polySymbol.MakeSymbolReference()))
				{
					await Task.Delay(400);
				}
			}

			return true;
		}

		public static HashSet<long> GetEditableClassHandles([NotNull] MapView mapView)
		{
			IEnumerable<BasicFeatureLayer> basicFeatureLayers =
				MapUtils.GetFeatureLayers<BasicFeatureLayer>(
					mapView.Map, bfl => bfl?.IsEditable == true);

			HashSet<long> editableClassHandles = basicFeatureLayers
			                                     .Select(l => l.GetTable().Handle.ToInt64())
			                                     .ToHashSet();

			return editableClassHandles;
		}

		public static FeatureClass GetCurrentTargetFeatureClass(
			[CanBeNull] EditingTemplate editTemplate)
		{
			// TODO: Notifications
			FeatureLayer featureLayer = CurrentTargetLayer(editTemplate);

			return featureLayer?.GetFeatureClass();
		}

		[CanBeNull]
		public static FeatureLayer CurrentTargetLayer([CanBeNull] EditingTemplate editTemplate)
		{
			var featureLayer = editTemplate?.Layer as FeatureLayer;

			return featureLayer;
		}

		/// <summary>
		/// Get the current selection color (or the default Cyan RGB
		/// if anything goes wrong).
		/// </summary>
		[NotNull]
		public static CIMColor GetSelectionColor()
		{
			var defaultColor = ColorUtils.CyanRGB;

			try
			{
				using var settings = SelectionSettings.CreateFromUserProfile();
				var xml = settings.DefaultSelectionColor;
				if (string.IsNullOrEmpty(xml))
					return defaultColor;
				var color = CIMColor.GetColorObject(xml);
				return color ?? defaultColor;
			}
			catch (Exception ex)
			{
				_msg.Error($"Error getting user profile selection color: {ex.Message}", ex);
				return defaultColor;
			}
		}
	}
}
