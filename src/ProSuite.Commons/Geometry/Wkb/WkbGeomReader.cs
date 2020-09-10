using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProSuite.Commons.Geometry.Wkb
{
	public class WkbGeomReader : WkbReader
	{
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
				                    geometryType == WkbGeometryType.MultiPolygon;

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
				GeomBuilder geometryBuilder = new GeomBuilder(true);

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
