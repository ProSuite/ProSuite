using System.IO;
using System.Windows.Input;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing
{
	public static class ToolUtils
	{
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
	}
}
