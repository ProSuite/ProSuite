using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Com;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Writes polygons as a GeoJSON FeatureCollection for the feature check service.
	/// </summary>
	public static class SemanticImageMatchGeoJson
	{
		/// <summary>
		/// The coordinates of a polygon as a GeoJSON MultiPolygon coordinate array.
		/// Curves are replaced by short straight segments. The text also serves as the
		/// input of the geometry fingerprint, so it must not depend on anything but the
		/// geometry.
		/// </summary>
		[NotNull]
		public static string GetCoordinates([NotNull] IPolygon polygon)
		{
			Assert.ArgumentNotNull(polygon, nameof(polygon));

			var linearized = (IPolygon4) GeometryFactory.Clone(polygon);
			try
			{
				if (GeometryUtils.HasNonLinearSegments(linearized))
				{
					GeometryUtils.EnsureLinearized(linearized, 0.01);
				}

				var result = new StringBuilder("[");
				var exteriorRings = (IGeometryCollection) linearized.ExteriorRingBag;
				for (var i = 0; i < exteriorRings.GeometryCount; i++)
				{
					var exteriorRing = (IRing) exteriorRings.Geometry[i];
					if (i > 0)
					{
						result.Append(',');
					}

					result.Append('[');
					AppendRing(result, exteriorRing);

					var interiorRings =
						(IGeometryCollection) linearized.InteriorRingBag[exteriorRing];
					for (var j = 0; j < interiorRings.GeometryCount; j++)
					{
						result.Append(',');
						AppendRing(result, (IRing) interiorRings.Geometry[j]);
					}

					result.Append(']');
				}

				result.Append(']');
				return result.ToString();
			}
			finally
			{
				ComUtils.ReleaseComObject(linearized);
			}
		}

		/// <param name="features">Pairs of an optional object identifier and the
		/// coordinates from <see cref="GetCoordinates"/>.</param>
		/// <param name="epsgCode">The coordinate system. Without it, the service reads
		/// the coordinates as WGS 84.</param>
		[NotNull]
		public static string WriteFeatureCollection(
			[NotNull] IEnumerable<KeyValuePair<long?, string>> features,
			int epsgCode)
		{
			var result = new StringBuilder();
			result.Append("{\"type\":\"FeatureCollection\",");
			result.AppendFormat(CultureInfo.InvariantCulture,
			                    "\"crs\":{{\"type\":\"name\",\"properties\":{{\"name\":\"EPSG:{0}\"}}}},",
			                    epsgCode);
			result.Append("\"features\":[");

			var first = true;
			foreach (KeyValuePair<long?, string> feature in features)
			{
				if (! first)
				{
					result.Append(',');
				}

				first = false;

				result.Append("{\"type\":\"Feature\",\"properties\":{");
				if (feature.Key.HasValue)
				{
					result.AppendFormat(CultureInfo.InvariantCulture, "\"{0}\":{1}",
					                    FeatureCheckClient.ObjectIdProperty, feature.Key.Value);
				}

				result.Append("},\"geometry\":{\"type\":\"MultiPolygon\",\"coordinates\":");
				result.Append(feature.Value);
				result.Append("}}");
			}

			result.Append("]}");
			return result.ToString();
		}

		private static void AppendRing([NotNull] StringBuilder result, [NotNull] IRing ring)
		{
			var points = (IPointCollection) ring;
			result.Append('[');

			IPoint point = new PointClass();
			for (var i = 0; i < points.PointCount; i++)
			{
				points.QueryPoint(i, point);
				if (i > 0)
				{
					result.Append(',');
				}

				result.AppendFormat(CultureInfo.InvariantCulture, "[{0},{1}]",
				                    point.X.ToString("0.###", CultureInfo.InvariantCulture),
				                    point.Y.ToString("0.###", CultureInfo.InvariantCulture));
			}

			result.Append(']');
		}
	}
}
