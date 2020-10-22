using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry.Wkb;

namespace ProSuite.Commons.AO.Geometry.Serialization
{
	[CLSCompliant(false)]
	public class WkbGeometryReader : WkbReader
	{
		public static IArrayProvider<WKSPointZ> WksPointArrayProvider { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="WkbReader"/> class
		/// </summary>
		/// <param name="assumeWkbPolygonsClockwise">Whether it should be
		/// assumed that the provided byte arrays do not conform to the WKB OGC 1.2 specification
		/// which states that a polygon's outer ring should be oriented in counter-clockwise
		/// orientation.
		/// Wkb produced by ArcObjects (IWkb interface) is counter-clockwise, OGC 1.2 conform.
		/// Wkb produced by SDE.ST_AsBinary operation is clockwise (!) and
		/// <see cref="assumeWkbPolygonsClockwise"/> should be set to true.</param>
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

				WKSPointZ wksPointZ = ReadPointCore(reader, ordinates, new WksPointZFactory());

				bool zAware = ordinates == Ordinates.Xyz || ordinates == Ordinates.Xyzm;

				IPoint result = GeometryFactory.CreatePoint(wksPointZ);

				if (zAware)
				{
					GeometryUtils.MakeZAware(result);
				}

				return result;
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

				var geometryBuilder = new WksPointListBuilder();

				IEnumerable<WKSPointZ[]> linestrings =
					ReadLinestrings(reader, geometryType, ordinates, geometryBuilder);

				IEnumerable<IPath> paths =
					ToPaths(linestrings, geometryType, ordinates, geometryBuilder);

				return CreatePolycurve<IPolyline>(paths, ordinates);
			}
		}

		public IPolygon ReadPolygon([NotNull] Stream stream)
		{
			using (BinaryReader reader = InitializeReader(stream))
			{
				ReadWkbType(reader, true,
				            out WkbGeometryType geometryType, out Ordinates ordinates);

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

		private IEnumerable<IRing> ReadSingleExteriorRingPolygon(
			[NotNull] BinaryReader reader, Ordinates ordinates)
		{
			int ringCount = checked((int) reader.ReadUInt32());

			bool zAware = ordinates == Ordinates.Xyz || ordinates == Ordinates.Xyzm;
			bool mAware = ordinates == Ordinates.Xym || ordinates == Ordinates.Xyzm;

			// NOTE: Somtimes this takes ca. 250ms (may be when the license is checked?)!
			IRing ringTemplate = GeometryFactory.CreateEmptyRing(zAware, mAware);

			if (ringCount > 0)
			{
				bool reverseOrder = ! AssumeWkbPolygonsClockwise;
				var geometryBuilder = new WksPointListBuilder(reverseOrder);

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
