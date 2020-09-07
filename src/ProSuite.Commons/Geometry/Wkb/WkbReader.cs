using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProSuite.Commons.Geometry.Wkb
{
	public class WkbReader
	{
		private BinaryReader _reader;

		public IList<RingGroup> ReadMultiPolygon(Stream stream)
		{
			using (BinaryReader reader = InitialilzeReader(stream))
			{
				uint type = reader.ReadUInt32();

				WkbGeometryType geometryType = (WkbGeometryType) (type % 1000);

				Ordinates ordinates = GetOrdinates(type);

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
						result.Add(ReadPolygonCore(reader, ordinates));
					}

					return result;
				}

				throw new NotSupportedException($"Cannot read {geometryType} as MultiPolygon.");
			}
		}

		public RingGroup ReadPolygon(Stream stream)
		{
			using (BinaryReader reader = InitialilzeReader(stream))
			{
				uint type = reader.ReadUInt32();

				WkbGeometryType geometryType = (WkbGeometryType) (type % 1000);

				Ordinates ordinates = GetOrdinates(type);

				if (geometryType == WkbGeometryType.Polygon)
				{
					RingGroup result = ReadPolygonCore(reader, ordinates);

					return result;
				}

				throw new NotSupportedException($"Cannot read {geometryType} as Polygon.");
			}
		}

		public MultiPolycurve ReadMultiPolycurve(Stream stream)
		{
			using (BinaryReader reader = InitialilzeReader(stream))
			{
				uint type = reader.ReadUInt32();

				WkbGeometryType geometryType = (WkbGeometryType) (type % 1000);

				Ordinates ordinates = GetOrdinates(type);

				if (geometryType == WkbGeometryType.MultiLineString ||
				    geometryType == WkbGeometryType.Polygon ||
				    geometryType == WkbGeometryType.MultiPolygon)
				{
					uint linestringCount = reader.ReadUInt32();

					return new MultiPolycurve(
						ReadLinestringsCore(reader, linestringCount, ordinates));
				}

				throw new NotSupportedException(
					$"Cannot read {geometryType} as multi-polycurve.");
			}
		}

		public IPnt ReadPoint(Stream stream)
		{
			using (BinaryReader reader = InitialilzeReader(stream))
			{
				uint type = reader.ReadUInt32();

				WkbGeometryType geometryType = (WkbGeometryType) (type % 1000);

				Ordinates ordinates = GetOrdinates(type);

				if (geometryType != WkbGeometryType.Point)
				{
					throw new NotSupportedException(
						$"Cannot read {geometryType} as point.");
				}

				return ReadPointCore(reader, ordinates);
			}
		}

		public IEnumerable<IPnt> ReadMultiPoint(Stream stream)
		{
			using (BinaryReader reader = InitialilzeReader(stream))
			{
				uint type = reader.ReadUInt32();

				WkbGeometryType geometryType = (WkbGeometryType) (type % 1000);

				Ordinates ordinates = GetOrdinates(type);

				if (geometryType != WkbGeometryType.MultiPoint)
				{
					throw new NotSupportedException(
						$"Cannot read {geometryType} as point.");
				}

				uint pointCount = reader.ReadUInt32();

				for (int i = 0; i < pointCount; i++)
				{
					yield return ReadPointCore(reader, ordinates);
				}
			}
		}

		private RingGroup ReadPolygonCore(BinaryReader reader, Ordinates ordinates)
		{
			uint ringCount = reader.ReadUInt32();

			if (ringCount > 0)
			{
				List<Linestring> rings = ReadLinestringsCore(reader, ringCount, ordinates).ToList();

				RingGroup result = new RingGroup(rings.First(), rings.Skip(1));

				return result;
			}

			// Allow empty?
			return null;
		}

		private IEnumerable<Linestring> ReadLinestringsCore(BinaryReader reader, uint count,
		                                                    Ordinates ordinates)
		{
			for (int i = 0; i < count; i++)
			{
				yield return new Linestring(ReadPoints3dCore(reader, ordinates));
			}
		}

		private IEnumerable<Pnt3D> ReadPoints3dCore(BinaryReader reader, Ordinates ordinates)
		{
			uint pointCount = reader.ReadUInt32();

			for (int i = 0; i < pointCount; i++)
				yield return ReadPoint3dCore(reader, ordinates);
		}

		private IPnt ReadPointCore(BinaryReader reader, Ordinates ordinates)
		{
			switch (ordinates)
			{
				case Ordinates.Xy: return new Pnt2D(reader.ReadDouble(), reader.ReadDouble());
				case Ordinates.Xyz:
					return new Pnt3D(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble());
				case Ordinates.Xym:
					throw new NotImplementedException("M-awareness is currently not supported.");
				case Ordinates.Xyzm:
					throw new NotImplementedException("M-awareness is currently not supported.");
				default: throw new NotSupportedException(ordinates.ToString());
			}
		}

		private Pnt3D ReadPoint3dCore(BinaryReader reader, Ordinates ordinates)
		{
			switch (ordinates)
			{
				case Ordinates.Xy:
					return new Pnt3D(reader.ReadDouble(), reader.ReadDouble(), double.NaN);
				case Ordinates.Xyz:
					return new Pnt3D(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble());
				case Ordinates.Xym:
					throw new NotImplementedException("M-awareness is currently not supported.");
				case Ordinates.Xyzm:
					throw new NotImplementedException("M-awareness is currently not supported.");
				default: throw new NotSupportedException(ordinates.ToString());
			}
		}

		private BinaryReader InitialilzeReader(Stream stream)
		{
			BinaryReader binaryReader = new BinaryReader(stream);

			bool isLittleEndian = binaryReader.ReadBoolean();

			if (! isLittleEndian)
			{
				binaryReader.Dispose();

				_reader = new BigEndianBinaryReader(stream);
				_reader.ReadBoolean();
			}
			else
			{
				_reader = binaryReader;
			}

			return _reader;
		}

		private Ordinates GetOrdinates(uint type)
		{
			if (type >= 1000 && type < 2000)
				return Ordinates.Xyz;
			else if (type >= 2000 && type < 3000)
				return Ordinates.Xym;
			else if (type >= 3000 && type < 4000)
				return Ordinates.Xyzm;

			return Ordinates.Xy;
		}
	}
}
