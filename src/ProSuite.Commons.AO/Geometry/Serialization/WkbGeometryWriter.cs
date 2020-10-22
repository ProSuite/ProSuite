using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry;
using ProSuite.Commons.Geometry.Wkb;

namespace ProSuite.Commons.AO.Geometry.Serialization
{
	[CLSCompliant(false)]
	public class WkbGeometryWriter : WkbWriter
	{
		public static IArrayProvider<WKSPointZ> WksPointArrayProvider { get; set; }

		public byte[] WritePoint([NotNull] IPoint point)
		{
			Ordinates ordinates = GeometryUtils.IsZAware(point) ? Ordinates.Xyz : Ordinates.Xy;

			if (GeometryUtils.IsMAware(point))
			{
				ordinates = ordinates == Ordinates.Xy ? Ordinates.Xym : Ordinates.Xyzm;
			}

			// TODO: Initialize with the proper size or allow providing the actual byte[]
			MemoryStream memoryStream = InitializeWriter();

			WriteWkbType(WkbGeometryType.Point, ordinates);

			WritePointCore(point, ordinates);

			// TODO: Return byte array used for initialization
			return memoryStream.ToArray();
		}

		public byte[] WritePolygon([NotNull] IPolygon polygon)
		{
			// TODO: Initialize with the proper size or allow providing the actual byte[]
			MemoryStream memoryStream = InitializeWriter();

			Ordinates ordinates = GeometryUtils.IsZAware(polygon) ? Ordinates.Xyz : Ordinates.Xy;

			if (GeometryUtils.IsMAware(polygon))
			{
				ordinates = ordinates == Ordinates.Xy ? Ordinates.Xym : Ordinates.Xyzm;
			}

			int exteriorRingCount = polygon.ExteriorRingCount;

			if (exteriorRingCount == 0)
			{
				return WriteEmptyPolycuve(memoryStream, WkbGeometryType.Polygon, ordinates);
			}

			if (exteriorRingCount == 1)
			{
				WritePolygon(polygon, ordinates);
			}
			else
			{
				// Several exterior rings -> MultiPolygon
				WriteWkbType(WkbGeometryType.MultiPolygon, ordinates);

				Writer.Write(exteriorRingCount);

				var connectedComponents = GeometryUtils.GetConnectedComponents(polygon).ToList();

				foreach (IPolygon component in connectedComponents)
				{
					WritePolygon(component, ordinates);
				}
			}

			return memoryStream.ToArray();
		}

		public byte[] WritePolyline(IPolyline polyline)
		{
			// TODO: Initialize with the proper size or allow providing the actual byte[]
			MemoryStream memoryStream = InitializeWriter();

			Ordinates ordinates = GeometryUtils.IsZAware(polyline) ? Ordinates.Xyz : Ordinates.Xy;

			if (GeometryUtils.IsMAware(polyline))
			{
				ordinates = ordinates == Ordinates.Xy ? Ordinates.Xym : Ordinates.Xyzm;
			}

			int count = ((IGeometryCollection) polyline).GeometryCount;

			if (count == 0)
			{
				return WriteEmptyPolycuve(memoryStream, WkbGeometryType.MultiLineString, ordinates);
			}

			List<IPointList> linestrings = GetPartsAsPointLists(polyline).ToList();
			if (linestrings.Count == 1)
			{
				WriteLinestring(linestrings[0], ordinates);
			}
			else
			{
				WriteWkbType(WkbGeometryType.MultiLineString, ordinates);
				WriteLineStrings(linestrings, ordinates);
			}

			return memoryStream.ToArray();
		}

		private void WritePointCore(IPoint point, Ordinates ordinates)
		{
			WriteXyCoordinates(point.X, point.Y);

			if (ordinates == Ordinates.Xyz || ordinates == Ordinates.Xyzm)
			{
				// Z Value:
				WriteDoubleOrNan(point.Z);
			}

			if (ordinates == Ordinates.Xym || ordinates == Ordinates.Xyzm)
				throw new NotImplementedException("M values are currently not supported.");
		}

		private void WritePolygon(IPolycurve polycurve, Ordinates ordinates)
		{
			WriteWkbType(WkbGeometryType.Polygon, ordinates);

			// Starting with OGC 1.2.0, the orientation is counter-clockwise for exterior rings:
			bool reversePointOrder = polycurve is IPolygon && ReversePolygonWindingOrder;

			WriteLineStringsCore(GetPartsAsPointLists(polycurve).ToList(), ordinates,
			                     reversePointOrder);
		}

		private static IEnumerable<IPointList> GetPartsAsPointLists([NotNull] IPolycurve polycurve)
		{
			int[] pointCounts = GeometryUtils.GetPointCountPerPart(polycurve);

			IPointCollection4 pointCollection = (IPointCollection4) polycurve;
			// For large polygons with many parts (huge lockergestein) getting the wks point array
			// only once and re-using it for each part takes half as long as getting the array for
			// each part.
			WKSPointZ[] pointArray = GetWksPointArray(pointCollection.PointCount);

			GeometryUtils.QueryWKSPointZs(pointCollection, pointArray);

			int currentStart = 0;
			foreach (int pointCount in pointCounts)
			{
				yield return new WksPointZPointList(pointArray, currentStart, pointCount);
				currentStart += pointCount;
			}
		}

		private byte[] WriteEmptyPolycuve([NotNull] MemoryStream memoryStream,
		                                  WkbGeometryType geometryType,
		                                  Ordinates ordinates)
		{
			WriteWkbType(WkbGeometryType.Polygon, ordinates);

			Writer.Write(0);

			return memoryStream.ToArray();
		}

		private static WKSPointZ[] GetWksPointArray(int pointCount)
		{
			// TODO: Consider conditional compilation if > .NET 3.5...
			if (WksPointArrayProvider == null)
			{
				WksPointArrayProvider = new ArrayProvider<WKSPointZ>();
			}

			return WksPointArrayProvider.GetArray(pointCount);
		}
	}
}
