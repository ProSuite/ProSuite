using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing
{
	public static class ToolUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Loads a cursor from a byte array
		/// </summary>
		/// <param name="bytes">The byte array</param>
		/// <returns>Cursor instance</returns>
		[NotNull]
		public static Cursor GetCursor([NotNull] byte[] bytes)
		{
			Assert.ArgumentNotNull(bytes, nameof(bytes));

			return new Cursor(new MemoryStream(bytes));
		}

		public static bool IsSingleClickSketch([NotNull] Geometry sketchGeometry)
		{
			bool hasExtent = sketchGeometry.Extent.Width > 0 || sketchGeometry.Extent.Height > 0;

			return ! hasExtent;
		}

		public static Geometry GetSinglePickSelectionArea([NotNull] Geometry sketchGeometry,
		                                                  int selectionTolerancePixels)
		{
			MapPoint sketchPoint = CreatePointFromSketchPolygon(sketchGeometry);

			return BufferGeometryByPixels(sketchPoint, selectionTolerancePixels);
		}

		public static Geometry SketchToSearchGeometry([NotNull] Geometry sketch,
		                                              int selectionTolerancePixels,
		                                              out bool isSinglePick)
		{
			isSinglePick = IsSingleClickSketch(sketch);

			if (isSinglePick)
			{
				sketch = GetSinglePickSelectionArea(
					sketch, selectionTolerancePixels);
			}

			return sketch;
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
		public static void SelectNewFeatures(List<Feature> newFeatures,
		                                     MapView mapView)
		{
			List<BasicFeatureLayer> layersWithSelection = mapView.Map.GetSelection().Keys
			                                                     .Where(l => l is BasicFeatureLayer)
			                                                     .Cast<BasicFeatureLayer>()
			                                                     .ToList();

			SelectionUtils.SelectFeatures(newFeatures, layersWithSelection);
		}

		private static MapPoint CreatePointFromSketchPolygon(Geometry sketchGeometry)
		{
			var clickCoord =
				new Coordinate2D(sketchGeometry.Extent.XMin, sketchGeometry.Extent.YMin);

			MapPoint sketchPoint =
				MapPointBuilder.CreateMapPoint(clickCoord, sketchGeometry.SpatialReference);

			return sketchPoint;
		}

		private static Geometry BufferGeometryByPixels(Geometry sketchGeometry,
		                                               int pixelBufferDistance)
		{
			double bufferDistance = MapUtils.ConvertScreenPixelToMapLength(pixelBufferDistance);

			Geometry selectionGeometry =
				GeometryEngine.Instance.Buffer(sketchGeometry, bufferDistance);

			return selectionGeometry;
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
	}
}
