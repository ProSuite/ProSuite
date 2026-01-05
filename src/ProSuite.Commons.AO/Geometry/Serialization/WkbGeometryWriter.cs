using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry.ExtractParts;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Geom.Wkb;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Geometry.Serialization
{
	public class WkbGeometryWriter : WkbWriter
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static IArrayProvider<WKSPointZ> WksPointArrayProvider { get; set; }

		public byte[] WriteGeometry([NotNull] IGeometry geometry)
		{
			try
			{
				switch (geometry.GeometryType)
				{
					case esriGeometryType.esriGeometryPoint:
						return WritePoint((IPoint) geometry);
					case esriGeometryType.esriGeometryMultipoint:
						return WriteMultipoint((IMultipoint) geometry);
					case esriGeometryType.esriGeometryPolyline:
						return WritePolyline((IPolyline) geometry);
					case esriGeometryType.esriGeometryPolygon:
						return WritePolygon((IPolygon) geometry);
					case esriGeometryType.esriGeometryMultiPatch:
						return WriteMultipatch((IMultiPatch) geometry);
					default:
						throw new NotImplementedException(
							$"Geometry type {geometry.GeometryType} is not implemented.");
				}
			}
			catch (Exception e)
			{
				_msg.Debug($"Error writing geometry to WKB: {GeometryUtils.ToString(geometry)}",
				           e);
				throw;
			}
		}

		public byte[] WritePoint([NotNull] IPoint point)
		{
			Ordinates ordinates = GetOrdinatesDimension(point);

			// TODO: Initialize with the proper size or allow providing the actual byte[]
			MemoryStream memoryStream = InitializeWriter();

			WriteWkbType(WkbGeometryType.Point, ordinates);

			WritePointCore(point, ordinates);

			// TODO: Return byte array used for initialization
			return memoryStream.ToArray();
		}

		public byte[] WriteMultipoint(IMultipoint multipoint)
		{
			MemoryStream memoryStream = InitializeWriter();

			Ordinates ordinates = GetOrdinatesDimension(multipoint);

			WriteWkbType(WkbGeometryType.MultiPoint, ordinates);

			var pointCollection = (IPointCollection4) multipoint;

			int pointCount = pointCollection.PointCount;

			Writer.Write(pointCount);

			WKSPointZ[] pointArray = GetWksPointArray(pointCollection.PointCount);

			GeometryUtils.QueryWKSPointZs(pointCollection, pointArray);

			var pointList = new WksPointZPointList(pointArray, 0, pointCount);

			for (int i = 0; i < pointCount; i++)
			{
				WriteWkbType(WkbGeometryType.Point, ordinates);

				WritePointCore(pointList, i, ordinates);
			}

			return memoryStream.ToArray();
		}

		public byte[] WritePolygon([NotNull] IPolygon polygon)
		{
			// TODO: Initialize with the proper size or allow providing the actual byte[]
			MemoryStream memoryStream = InitializeWriter();

			Ordinates ordinates = GetOrdinatesDimension(polygon);

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

			Ordinates ordinates = GetOrdinatesDimension(polyline);

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

		public byte[] WriteMultipatch([NotNull] IMultiPatch multipatch,
		                              bool groupPartsByPointIDs = false)
		{
			Assert.True(GeometryUtils.IsRingBasedMultipatch(multipatch),
			            "Unsupported (non-ring-based) multipatch.");

			// TODO: Initialize with the proper size or allow providing the actual byte[]
			MemoryStream memoryStream = InitializeWriter();

			Ordinates ordinates = GetOrdinatesDimension(multipatch);

			WriteWkbType(WkbGeometryType.MultiSurface, ordinates);

			var geometryParts =
				GeometryPart.FromGeometry(multipatch, groupPartsByPointIDs).ToList();

			Writer.Write(geometryParts.Count);

			foreach (GeometryPart part in geometryParts)
			{
				WriteWkbType(WkbGeometryType.PolyhedralSurface, ordinates);

				List<List<IRing>> polygonGroup = GetPolygons(multipatch, part).ToList();

				Writer.Write(polygonGroup.Count);

				foreach (List<IRing> rings in polygonGroup)
				{
					WriteWkbType(WkbGeometryType.Polygon, ordinates);

					WriteLineStringsCore(GetAsPointList(rings).ToList(), ordinates);
				}
			}

			return memoryStream.ToArray();
		}

		private static IEnumerable<List<IRing>> GetPolygons([NotNull] IMultiPatch multipatch,
		                                                    [NotNull] GeometryPart part)
		{
			var result = new Dictionary<IRing, List<IRing>>();

			foreach (IRing ring in part.LowLevelGeometries.Cast<IRing>())
			{
				bool beginning = false;
				esriMultiPatchRingType type = multipatch.GetRingType(ring, ref beginning);

				if (type != esriMultiPatchRingType.esriMultiPatchInnerRing)
				{
					result.Add(ring, new List<IRing>());
				}
				else
				{
					IRing outerRing = multipatch.FindBeginningRing(ring);

					Assert.True(result.ContainsKey(outerRing),
					            "No outer ring found for inner ring.");

					result[outerRing].Add(ring);
				}
			}

			foreach (KeyValuePair<IRing, List<IRing>> keyValuePair in result)
			{
				List<IRing> polygonRings = keyValuePair.Value;

				polygonRings.Insert(0, keyValuePair.Key);

				yield return polygonRings;
			}
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

		private static IEnumerable<IPointList> GetAsPointList(
			[NotNull] ICollection<IRing> rings)
		{
			foreach (IRing ring in rings)
			{
				IPointCollection4 pointCollection = (IPointCollection4) ring;

				WKSPointZ[] pointArray = new WKSPointZ[pointCollection.PointCount];

				GeometryUtils.QueryWKSPointZs(pointCollection, pointArray);

				yield return new WksPointZPointList(pointArray, 0, pointCollection.PointCount);
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

		private static Ordinates GetOrdinatesDimension(IGeometry geometry)
		{
			Ordinates ordinates = GeometryUtils.IsZAware(geometry) ? Ordinates.Xyz : Ordinates.Xy;

			if (GeometryUtils.IsMAware(geometry))
			{
				ordinates = ordinates == Ordinates.Xy ? Ordinates.Xym : Ordinates.Xyzm;
			}

			return ordinates;
		}
	}
}
