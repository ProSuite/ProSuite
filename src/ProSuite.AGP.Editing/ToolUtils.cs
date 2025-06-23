using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing.Templates;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Internal.Mapping;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.AGP.Windows;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Input;
using Point = System.Windows.Point;

namespace ProSuite.AGP.Editing
{
	// todo: daro use GeometryUtils, GeometryFactory
	public static class ToolUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull]
		public static Cursor GetCursor([NotNull] byte[] bytes)
		{
			return CursorUtils.GetCursor(bytes);
		}

		public static Cursor CreateCursor(byte[] baseImage,
		                                  byte[] overlay1 = null,
		                                  int xHotspot = 0,
		                                  int yHotspot = 0)
		{
			return CursorUtils.CreateCursor(baseImage, overlay1, xHotspot, yHotspot);
		}

		public static Cursor CreateCursor(byte[] baseImage,
		                                  byte[] overlay1 = null,
		                                  byte[] overlay2 = null,
		                                  byte[] overlay3 = null,
		                                  int xHotspot = 0,
		                                  int yHotspot = 0)
		{
			return CursorUtils.CreateCursor(baseImage,
			                                overlay1, overlay2, overlay3,
			                                xHotspot, yHotspot);
		}

		public static string GetDisabledReasonNoGeometryMicroservice()
		{
			return
				"Geometry Microservice not found or not started. Please make sure the latest ProSuite Extension is installed.";
		}

		/// <summary>
		/// Determines whether the sketch geometry is a single click. Call this method after a
		/// polygon sketch has been simplified.
		/// </summary>
		/// <param name="sketchGeometry">The sketch geometry</param>
		/// <returns></returns>
		public static bool IsSingleClickSketch([NotNull] Geometry sketchGeometry)
		{
			return ! (sketchGeometry.Extent.Width > 0 || sketchGeometry.Extent.Height > 0);
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
			if (sketch is MapPoint sketchPoint)
			{
				// Pre-determined single click (new standard) -> expand to tolerance
				singleClick = true;
				return GetSinglePickSelectionArea(sketchPoint, selectionTolerancePixels);
			}

			// Consider removing, if this is really not called any more:
			singleClick = IsSingleClickSketch(sketch);

			if (singleClick)
			{
				Point mouseScreenPosition = MouseUtils.GetMouseScreenPosition();
				MapPoint mouseMapPosition = MapView.Active.ScreenToMap(mouseScreenPosition);
				sketch = GetSinglePickSelectionArea(mouseMapPosition, selectionTolerancePixels);
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

		public static double ConvertScreenPixelToMapLength(MapView mapView, int pixels,
		                                                   MapPoint atPoint)
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
			var editableClassHandles = new HashSet<long>();

			IEnumerable<BasicFeatureLayer> editableFeatureLayers =
				MapUtils.GetEditableLayers<BasicFeatureLayer>(mapView.Map);

			foreach (var featureLayer in editableFeatureLayers)
			{
				FeatureClass featureClass = featureLayer.GetFeatureClass();

				if (featureClass == null)
				{
					continue;
				}

				featureClass = DatasetUtils.GetDatabaseFeatureClass(featureClass);

				editableClassHandles.Add(featureClass.Handle.ToInt64());
			}

			return editableClassHandles;
		}

		[CanBeNull]
		public static FeatureClass GetCurrentTargetFeatureClass(
			[CanBeNull] EditingTemplate editTemplate,
			bool unwrapJoins = true)
		{
			FeatureLayer featureLayer = CurrentTargetLayer(editTemplate);

			var layerFeatureClass = featureLayer?.GetFeatureClass();

			if (layerFeatureClass == null)
			{
				return null;
			}

			return unwrapJoins
				       ? DatasetUtils.GetDatabaseFeatureClass(layerFeatureClass)
				       : layerFeatureClass;
		}

		[NotNull]
		public static FeatureClass GetCurrentTargetFeatureClass(bool unwrapJoins,
		                                                        [CanBeNull] out Subtype subtype)
		{
			EditingTemplate editingTemplate = EditingTemplate.Current;
			subtype = null;

			if (editingTemplate == null)
			{
				throw new InvalidOperationException("No editing template is currently selected");
			}

			FeatureClass currentTargetClass =
				GetCurrentTargetFeatureClass(editingTemplate, unwrapJoins);

			if (currentTargetClass == null)
			{
				throw new InvalidOperationException("No current target feature class");
			}

			FeatureClassDefinition classDefinition = currentTargetClass.GetDefinition();

			string subtypeField = classDefinition.GetSubtypeField();

			if (! string.IsNullOrEmpty(subtypeField))
			{
				object subtypeValue = editingTemplate.Inspector[subtypeField];

				if (subtypeValue != null && subtypeValue != DBNull.Value)
				{
					//NOTE: Subtypes can be based on short integers
					int subtypeCode = Convert.ToInt32(subtypeValue);
					subtype = classDefinition.GetSubtypes()
					                         .FirstOrDefault(s => s.GetCode() == subtypeCode);
				}
			}

			return currentTargetClass;
		}

		[CanBeNull]
		public static FeatureLayer CurrentTargetLayer([CanBeNull] EditingTemplate editTemplate)
		{
			var featureLayer = editTemplate?.Layer as FeatureLayer;

			return featureLayer;
		}

		public static SketchGeometryType GetSketchGeometryType()
		{
			return MapView.Active?.GetSketchType() ?? SketchGeometryType.None;
		}

		public static SketchGeometryType? ToggleSketchGeometryType(
			SketchGeometryType? toggleType,
			SketchGeometryType? currentSketchType,
			SketchGeometryType defaultSketchType)
		{
			SketchGeometryType? type;

			switch (toggleType)
			{
				// TODO: If the default is Polygon and the currentSketch is already Polygon -> Rectangle
				case SketchGeometryType.Polygon:
					type = currentSketchType == SketchGeometryType.Polygon
						       ? defaultSketchType
						       : toggleType;
					break;
				case SketchGeometryType.Lasso:
					type = currentSketchType == SketchGeometryType.Lasso
						       ? defaultSketchType
						       : toggleType;
					break;
				default:
					type = toggleType;
					break;
			}

			return type;
		}

		/// <summary>
		/// Determines whether the input features are simple enough to be processed with topolgical operator or union.
		/// All non-simple reasons except short segments and empty parts are considered non-reasonably-simple:
		/// Incorrect orientation: parts disappear
		/// Self-intersections: parts of a part can disappear
		/// Un-closed polygons: part disappearance has been observed
		/// </summary>
		/// <param name="features"></param>
		/// <returns></returns>
		public static bool ReasonablySimple([NotNull] IEnumerable<Feature> features)
		{
			Assert.ArgumentNotNull(features, nameof(features));

			var result = true;

			foreach (Feature feature in features)
			{
				if (! ReasonablySimple(feature))
				{
					result = false;
				}
			}

			return result;
		}

		/// <summary>
		/// Determines whether the input feature is simple enough to be processed with topolgical operator or union.
		/// All non-simple reasons except short segments and empty parts are considered non-reasonably-simple:
		/// Incorrect orientation: parts disappear
		/// Self-intersections: parts of a part can disappear
		/// Un-closed polygons: part disappearance has been observed
		/// </summary>
		/// <param name="feature"></param>
		/// <returns></returns>
		private static bool ReasonablySimple([NotNull] Feature feature)
		{
			Assert.ArgumentNotNull(feature, nameof(feature));

			// TODO: Test if this simple check is sufficient for the edit use cases.

			Geometry firstShape = feature.GetShape();

			bool simple = GeometryEngine.Instance.IsSimpleAsFeature(firstShape, true);

			return simple;
		}

		/// <summary>
		/// Finds the features that intersect the specified selection.
		/// </summary>
		/// <param name="selection"></param>
		/// <param name="activeMapView"></param>
		/// <param name="targetFeatureSelection"></param>
		/// <param name="extraTargetSearchTolerance"></param>
		/// <param name="targetFeatureClassPredicate"></param>
		/// <param name="cancellabelProgressor"></param>
		/// <returns></returns>
		[NotNull]
		public static IList<Feature> GetIntersectingFeatures(
			[NotNull] IDictionary<MapMember, List<long>> selection,
			MapView activeMapView,
			TargetFeatureSelection targetFeatureSelection,
			double extraTargetSearchTolerance,
			[CanBeNull] Predicate<FeatureClass> targetFeatureClassPredicate,
			[CanBeNull] CancelableProgressor cancellabelProgressor)
		{
			Envelope inExtent = activeMapView.Extent;

			if (targetFeatureSelection == TargetFeatureSelection.SelectedFeatures)
			{
				// NOTE: cracking within selection is signalled to the server by an empty target list.
				return new List<Feature>();
			}

			var featureFinder = new FeatureFinder(activeMapView, targetFeatureSelection)
			                    {
				                    FeatureClassPredicate = targetFeatureClassPredicate
			                    };

			// They might be stored (insert target vertices):
			featureFinder.ReturnUnJoinedFeatures = true;
			featureFinder.ExtraSearchTolerance = extraTargetSearchTolerance;

			// Set the feature classes to ignore
			IEnumerable<FeatureSelectionBase> featureClassSelections =
				featureFinder.FindIntersectingFeaturesByFeatureClass(
					selection, null, inExtent, cancellabelProgressor);

			if (cancellabelProgressor != null &&
			    cancellabelProgressor.CancellationToken.IsCancellationRequested)
			{
				return new List<Feature>();
			}

			var foundFeatures = new List<Feature>();

			foreach (FeatureSelectionBase selectionBase in featureClassSelections)
			{
				foundFeatures.AddRange(selectionBase.GetFeatures());
			}

			return foundFeatures;
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
