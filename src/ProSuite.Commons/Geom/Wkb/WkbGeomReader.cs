using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;

namespace ProSuite.Commons.Geom.Wkb
{
	public class WkbGeomReader : WkbReader
	{
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
		public WkbGeomReader(bool assumeWkbPolygonsClockwise = false)
			: base(assumeWkbPolygonsClockwise) { }

		public IPnt ReadPoint(Stream stream)
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

				IPointFactory<IPnt> builder = new PntFactory();

				return ReadPointCore(reader, ordinates, builder);
			}
		}

		public MultiPolycurve ReadMultiPolycurve(Stream stream)
		{
			using (BinaryReader reader = InitializeReader(stream))
			{
				ReadWkbType(reader, true,
				            out WkbGeometryType geometryType, out Ordinates ordinates);

				bool reverseOrder = geometryType == WkbGeometryType.Polygon ||
				                    geometryType == WkbGeometryType.MultiPolygon &&
				                    ! AssumeWkbPolygonsClockwise;

				GeomBuilder geometryBuilder = new GeomBuilder(reverseOrder);

				IEnumerable<Linestring> linestrings =
					ReadLinestrings(reader, geometryType, ordinates, geometryBuilder);

				return new MultiPolycurve(linestrings);
			}
		}

		public IList<RingGroup> ReadMultiPolygon(Stream stream)
		{
			using (BinaryReader reader = InitializeReader(stream))
			{
				ReadWkbType(reader, true,
				            out WkbGeometryType geometryType, out Ordinates ordinates);

				if (geometryType == WkbGeometryType.Polygon)
				{
					RingGroup result = ReadPolygonCore(reader, ordinates);

					return new List<RingGroup> {result};
				}

				if (geometryType == WkbGeometryType.MultiPolygon)
				{
					uint polygonCount = reader.ReadUInt32();

					var result = new List<RingGroup>((int) polygonCount);

					for (int i = 0; i < polygonCount; i++)
					{
						ReadWkbType(reader, false,
						            out geometryType, out ordinates);

						result.Add(ReadPolygonCore(reader, ordinates));
					}

					return result;
				}

				throw new NotSupportedException($"Cannot read {geometryType} as MultiPolygon.");
			}
		}

		public RingGroup ReadPolygon(Stream stream)
		{
			using (BinaryReader reader = InitializeReader(stream))
			{
				ReadWkbType(reader, true,
				            out WkbGeometryType geometryType, out Ordinates ordinates);

				if (geometryType == WkbGeometryType.Polygon)
				{
					RingGroup result = ReadPolygonCore(reader, ordinates);

					return result;
				}

				throw new NotSupportedException($"Cannot read {geometryType} as Polygon.");
			}
		}

		public Multipoint<IPnt> ReadMultiPoint(Stream stream)
		{
			using (BinaryReader reader = InitializeReader(stream))
			{
				ReadWkbType(reader, true,
				            out WkbGeometryType geometryType, out Ordinates ordinates);

				if (geometryType != WkbGeometryType.MultiPoint)
				{
					throw new NotSupportedException(
						$"Cannot read {geometryType} as point.");
				}

				int pointCount = checked((int) reader.ReadUInt32());

				var geometryBuilder = new GeomBuilder(false);

				Multipoint<IPnt> multipoint =
					ReadMultipointCore(reader, ordinates, pointCount, geometryBuilder);

				return multipoint;
			}
		}

		public IList<Polyhedron> ReadMultiSurface(Stream stream)
		{
			using (BinaryReader reader = InitializeReader(stream))
			{
				ReadWkbType(reader, true,
				            out WkbGeometryType geometryType, out Ordinates ordinates);

				if (geometryType != WkbGeometryType.MultiSurface)
				{
					throw new NotSupportedException(
						$"Cannot read {geometryType} as multi-surface.");
				}

				List<Polyhedron> result = ReadMultiSurfaceCore(reader, ordinates);

				return result;
			}
		}

		private List<Polyhedron> ReadMultiSurfaceCore(BinaryReader reader,
		                                              Ordinates expectedOrdinates)
		{
			var result = new List<Polyhedron>();

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

				Polyhedron polyhedron = ReadPolyhedronCore(reader);

				result.Add(polyhedron);
			}

			return result;
		}

		private Polyhedron ReadPolyhedronCore(BinaryReader reader)
		{
			int polygonCount = checked((int) reader.ReadUInt32());

			var ringGroups = new List<RingGroup>();

			for (int p = 0; p < polygonCount; p++)
			{
				WkbGeometryType geometryType;
				Ordinates expectedOrdinates;
				ReadWkbType(reader, false,
				            out geometryType, out expectedOrdinates);

				Assert.AreEqual(WkbGeometryType.Polygon, geometryType,
				                "Unexpected geometry type");

				RingGroup ringGroup = ReadPolygonCore(reader, expectedOrdinates);

				if (ringGroup.PartCount == 0) continue;

				ringGroups.Add(ringGroup);
			}

			var polyhedron = new Polyhedron(ringGroups);
			return polyhedron;
		}

		private RingGroup ReadPolygonCore(BinaryReader reader, Ordinates ordinates)
		{
			int ringCount = checked((int) reader.ReadUInt32());

			if (ringCount > 0)
			{
				bool reverseOrder = ! AssumeWkbPolygonsClockwise;
				GeomBuilder geometryBuilder = new GeomBuilder(reverseOrder);

				List<Linestring> rings =
					ReadLinestringsCore(reader, ordinates, ringCount, geometryBuilder).ToList();

				RingGroup result = new RingGroup(rings.First(), rings.Skip(1));

				return result;
			}

			// Allow empty?
			return null;
		}
	}
}
