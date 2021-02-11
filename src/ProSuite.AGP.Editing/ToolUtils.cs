using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
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

		public static Geometry GetSinglePickSelectionArea(Geometry sketchGeometry,
		                                                  int selectionTolerancePixels)
		{
			MapPoint sketchPoint = CreatePointFromSketchPolygon(sketchGeometry);

			return BufferGeometryByPixels(sketchPoint, selectionTolerancePixels);
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
