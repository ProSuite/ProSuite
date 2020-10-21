using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProSuite.Commons.Geometry.Wkb
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
		/// <see cref="assumeWkbPolygonsClockwise"/> should be set to true.</param>
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

		public IEnumerable<IPnt> ReadMultiPoint(Stream stream)
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

				uint pointCount = reader.ReadUInt32();

				IPointFactory<IPnt> builder = new PntFactory();

				for (int i = 0; i < pointCount; i++)
				{
					yield return ReadPointCore(reader, ordinates, builder);
				}
			}
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
