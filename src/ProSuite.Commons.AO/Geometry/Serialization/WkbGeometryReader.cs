using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom.Wkb;

namespace ProSuite.Commons.AO.Geometry.Serialization
{
	public class WkbGeometryReader : WkbReader
	{
		public static IArrayProvider<WKSPointZ> WksPointArrayProvider { get; set; }

		public bool GroupPolyhedraByPointId { get; set; }

		public IGeometry ReadGeometry([NotNull] Stream stream)
		{
			using (BinaryReader reader = InitializeReader(stream))
			{
				ReadWkbType(reader, true,
				            out WkbGeometryType geometryType, out Ordinates ordinates);

				switch (geometryType)
				{
					case WkbGeometryType.Point:
						return ReadPoint(reader, ordinates);
					case WkbGeometryType.MultiPoint:
						return ReadMultipoint(reader, ordinates);
					case WkbGeometryType.LineString:
					case WkbGeometryType.MultiLineString:
						return ReadPolyline(reader, geometryType, ordinates);
					case WkbGeometryType.Polygon:
					case WkbGeometryType.MultiPolygon:
						return ReadPolygon(reader, geometryType, ordinates);
					case WkbGeometryType.MultiSurface:
						return ReadMultipatch(reader, ordinates);
					default:
						throw new NotImplementedException(
							$"Unsupported geometry type: {geometryType}");
				}
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="WkbReader"/> class
		/// </summary>
		/// <param name="assumeWkbPolygonsClockwise">Whether it should be
		/// assumed that the provided byte arrays do not conform to the WKB OGC 1.2 specification
		/// which states that a polygon's outer ring should be oriented in counter-clockwise
		/// orientation.
		/// Wkb produced by ArcObjects (IWkb interface) is counter-clockwise, OGC 1.2 conform.
		/// Wkb produced by SDE.ST_AsBinary operation is clockwise (!) and
		/// assumeWkbPolygonsClockwise should be set to true.</param>
		public WkbGeometryReader(bool assumeWkbPolygonsClockwise = false) : base(
			assumeWkbPolygonsClockwise) { }

		public IPoint ReadPoint([NotNull] Stream stream)
		{
			using (BinaryReader reader = InitializeReader(stream))
			{
				ReadWkbType(reader, true,
				            out WkbGeometryType geometryType, out Ordinates ordinates);

				if (geometryType != WkbGeometryType.Point)
				{
					throw new NotSupportedException(
						$"Cannot read {geometryType} as point.");
				}

				return ReadPoint(reader, ordinates);
			}
		}

		public IMultipoint ReadMultipoint([NotNull] Stream stream)
		{
			using (BinaryReader reader = InitializeReader(stream))
			{
				ReadWkbType(reader, true,
				            out WkbGeometryType geometryType, out Ordinates ordinates);

				if (geometryType != WkbGeometryType.MultiPoint)
				{
					throw new NotSupportedException(
						$"Cannot read {geometryType} as multipoint.");
				}

				return ReadMultipoint(reader, ordinates);
			}
		}

		public IPolyline ReadPolyline([NotNull] Stream stream)
		{
			using (BinaryReader reader = InitializeReader(stream))
			{
				ReadWkbType(reader, true,
				            out WkbGeometryType geometryType, out Ordinates ordinates);

				if (geometryType != WkbGeometryType.LineString &&
				    geometryType != WkbGeometryType.MultiLineString)
				{
					throw new NotSupportedException(
						$"Cannot read {geometryType} as polyline.");
				}

				return ReadPolyline(reader, geometryType, ordinates);
			}
		}

		public IPolygon ReadPolygon([NotNull] Stream stream)
		{
			using (BinaryReader reader = InitializeReader(stream))
			{
				ReadWkbType(reader, true,
				            out WkbGeometryType geometryType, out Ordinates ordinates);

				return ReadPolygon(reader, geometryType, ordinates);
			}
		}

		public IMultiPatch ReadMultipatch([NotNull] Stream stream)
		{
			using (BinaryReader reader = InitializeReader(stream))
			{
				ReadWkbType(reader, true,
				            out WkbGeometryType geometryType, out Ordinates ordinates);

				Assert.AreEqual(WkbGeometryType.MultiSurface, geometryType,
				                "Unexpected geometry type: {0}", geometryType);

				return ReadMultipatch(reader, ordinates);
			}
		}

		private IPolygon ReadPolygon(BinaryReader reader, WkbGeometryType geometryType,
		                             Ordinates ordinates)
		{
			if (geometryType == WkbGeometryType.Polygon)
			{
				var rings =
					ReadSingleExteriorRingPolygon(reader, ordinates).Cast<IGeometry>();

				return CreatePolycurve<IPolygon>(rings, ordinates);
			}

			if (geometryType == WkbGeometryType.MultiPolygon)
			{
				int polygonCount = checked((int) reader.ReadUInt32());

				List<IGeometry> allRings = new List<IGeometry>();
				for (int i = 0; i < polygonCount; i++)
				{
					ReadWkbType(reader, false,
					            out geometryType, out ordinates);

					allRings.AddRange(ReadSingleExteriorRingPolygon(reader, ordinates));
				}

				return CreatePolycurve<IPolygon>(allRings, ordinates);
			}

			throw new NotSupportedException(
				$"Cannot read {geometryType} as polygon.");
		}

		private IMultiPatch ReadMultipatch(BinaryReader reader,
		                                   Ordinates expectedOrdinates)
		{
			IMultiPatch result = new MultiPatchClass();

			if (GroupPolyhedraByPointId)
			{
				GeometryUtils.MakePointIDAware(result);
			}

			int polyhedraCount = checked((int) reader.ReadUInt32());

			for (int i = 0; i < polyhedraCount; i++)
			{
				WkbGeometryType geometryType;
				Ordinates ordinates;
				ReadWkbType(reader, false,
				            out geometryType, out ordinates);

				Assert.AreEqual(WkbGeometryType.PolyhedralSurface, geometryType,
				                "Unexpected geometry type");

				Assert.AreEqual(expectedOrdinates, ordinates,
				                "Unexpected ordinates dimension");

				int polygonCount = checked((int) reader.ReadUInt32());

				for (int p = 0; p < polygonCount; p++)
				{
					ReadWkbType(reader, false,
					            out geometryType, out expectedOrdinates);

					Assert.AreEqual(WkbGeometryType.Polygon, geometryType,
					                "Unexpected geometry type");

					var rings = ReadSingleExteriorRingPolygon(reader, ordinates, false).ToList();

					if (rings.Count == 0) continue;

					if (GroupPolyhedraByPointId)
					{
						AssignPointIds(rings, i);
					}

					var outerRingType = rings.Count > 1
						                    ? esriMultiPatchRingType.esriMultiPatchOuterRing
						                    : p == 0 || GroupPolyhedraByPointId
							                    ? esriMultiPatchRingType.esriMultiPatchFirstRing
							                    : esriMultiPatchRingType.esriMultiPatchRing;

					IRing outerRing = rings[0];
					GeometryFactory.AddRingToMultiPatch(outerRing, result, outerRingType);

					if (rings.Count > 1)
					{
						for (int r = 1; r < rings.Count; r++)
						{
							IRing innerRing = rings[r];
							GeometryFactory.AddRingToMultiPatch(
								innerRing, result, esriMultiPatchRingType.esriMultiPatchInnerRing);
						}
					}
				}
			}

			return result;
		}

		private static void AssignPointIds([NotNull] IEnumerable<IRing> rings, int id)
		{
			foreach (IRing ring in rings)
			{
				AssignPointIds(ring, id);

				// NOTE: Inner rings do not have PointIDs in Safa multipatches
				id = 0;
			}
		}

		private static void AssignPointIds([NotNull] IRing ring, int id)
		{
			GeometryUtils.MakePointIDAware(ring);

			IPointCollection points = (IPointCollection) ring;

			GeometryUtils.AssignConstantPointID(points, id);
		}

		private static IPoint ReadPoint(BinaryReader reader, Ordinates ordinates)
		{
			WKSPointZ wksPointZ = ReadPointCore(reader, ordinates, new WksPointZFactory());

			bool zAware = ordinates == Ordinates.Xyz || ordinates == Ordinates.Xyzm;

			IPoint result = GeometryFactory.CreatePoint(wksPointZ);

			if (zAware)
			{
				GeometryUtils.MakeZAware(result);
			}

			return result;
		}

		private static IMultipoint ReadMultipoint([NotNull] BinaryReader reader,
		                                          Ordinates ordinates)
		{
			int pointCount = checked((int) reader.ReadUInt32());

			var geometryBuilder = new WksPointListBuilder();

			WKSPointZ[] wksZPoints =
				ReadMultipointCore(reader, ordinates, pointCount, geometryBuilder);

			bool zAware = ordinates == Ordinates.Xyz || ordinates == Ordinates.Xyzm;
			bool mAware = ordinates == Ordinates.Xym || ordinates == Ordinates.Xyzm;

			IMultipoint result = GeometryFactory.CreateEmptyMultipoint(zAware, mAware);

			GeometryUtils.SetWKSPointZs(result, wksZPoints);

			return result;
		}

		private static IPolyline ReadPolyline(BinaryReader reader, WkbGeometryType geometryType,
		                                      Ordinates ordinates)
		{
			var geometryBuilder = new WksPointListBuilder();

			IEnumerable<WKSPointZ[]> linestrings =
				ReadLinestrings(reader, geometryType, ordinates, geometryBuilder);

			IEnumerable<IPath> paths =
				ToPaths(linestrings, geometryType, ordinates, geometryBuilder);

			return CreatePolycurve<IPolyline>(paths, ordinates);
		}

		private static T CreatePolycurve<T>([NotNull] IEnumerable<IGeometry> parts,
		                                    Ordinates ordinates)
			where T : IPolycurve
		{
			var partArray = parts.ToArray();

			if (partArray.Length == 0)
			{
				return CreateEmptyPolycurve<T>(ordinates);
			}

			IPolycurve result = CreateEmptyPolycurve<T>(ordinates, partArray[0]);

			GeometryUtils.GeometryBridge.AddGeometries((IGeometryCollection) result, ref partArray);

			return (T) result;
		}

		private static T CreateEmptyPolycurve<T>(Ordinates ordinates,
		                                         [CanBeNull] IGeometry template = null)
			where T : IPolycurve
		{
			bool createPolygon = typeof(IPolygon).IsAssignableFrom(typeof(T));

			if (template != null)
			{
				return createPolygon
					       ? (T) GeometryFactory.CreateEmptyPolygon(template)
					       : (T) GeometryFactory.CreateEmptyPolyline(template);
			}

			bool zAware = ordinates == Ordinates.Xyz || ordinates == Ordinates.Xyzm;
			bool mAware = ordinates == Ordinates.Xym || ordinates == Ordinates.Xyzm;

			return createPolygon
				       ? (T) GeometryFactory.CreatePolygon(null, zAware, mAware)
				       : (T) GeometryFactory.CreateEmptyPolyline(null, zAware, mAware);
		}

		[NotNull]
		private IEnumerable<IRing> ReadSingleExteriorRingPolygon(
			[NotNull] BinaryReader reader, Ordinates ordinates, bool? reverseOrder = null)
		{
			if (reader == null)
			{
				throw new ArgumentNullException(nameof(reader));
			}

			int ringCount = checked((int) reader.ReadUInt32());

			bool zAware = ordinates == Ordinates.Xyz || ordinates == Ordinates.Xyzm;
			bool mAware = ordinates == Ordinates.Xym || ordinates == Ordinates.Xyzm;

			// NOTE: Somtimes this takes ca. 250ms (may be when the license is checked?)!
			IRing ringTemplate = GeometryFactory.CreateEmptyRing(zAware, mAware);

			if (ringCount > 0)
			{
				bool reverse = reverseOrder ?? ! AssumeWkbPolygonsClockwise;
				var geometryBuilder = new WksPointListBuilder(reverse);

				foreach (WKSPointZ[] wksPoints in ReadLinestringsCore(
					         reader, ordinates, ringCount, geometryBuilder))
				{
					IRing resultRing = GeometryFactory.Clone(ringTemplate);

					GeometryUtils.SetWKSPointZs(resultRing, wksPoints);

					yield return resultRing;
				}
			}
		}

		private static IEnumerable<IPath> ToPaths(IEnumerable<WKSPointZ[]> wksLinestrings,
		                                          WkbGeometryType geometryType,
		                                          Ordinates ordinates,
		                                          WksPointListBuilder geometryBuilder)
		{
			IPath pathTemplate = CreateEmptyPath(ordinates);

			foreach (WKSPointZ[] wksPoints in wksLinestrings)
			{
				IPath resultPath = geometryType == WkbGeometryType.MultiLineString
					                   ? GeometryFactory.Clone(pathTemplate)
					                   : pathTemplate;

				GeometryUtils.SetWKSPointZs(resultPath, wksPoints);

				yield return resultPath;
			}
		}

		private static IPath CreateEmptyPath(Ordinates ordinates)
		{
			bool zAware = ordinates == Ordinates.Xyz || ordinates == Ordinates.Xyzm;
			bool mAware = ordinates == Ordinates.Xym || ordinates == Ordinates.Xyzm;

			// NOTE: Somtimes this takes ca. 250ms (may be when the license is checked?)!

			IPath result = GeometryFactory.CreateEmptyPath(zAware, mAware);

			return result;
		}
	}
}
