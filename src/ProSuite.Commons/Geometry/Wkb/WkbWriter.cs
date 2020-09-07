using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geometry.Wkb
{
	/// <summary>
	/// Writes the provided geometries in little-endian WKB format.
	/// </summary>
	public class WkbWriter
	{
		private static readonly byte[] _doubleNaN =
			{0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xf8, 0x7f};

		private BinaryWriter _writer;

		public byte[] WriteMultipolygon(MultiPolycurve multipolygon)
		{
			return WriteMultipolygon(
				GeomTopoOpUtils.GetConnectedComponents(multipolygon, double.Epsilon).ToList());
		}

		public byte[] WriteMultipolygon([NotNull] ICollection<RingGroup> ringGroups,
		                                Ordinates ordinates = Ordinates.Xyz)
		{
			if (ringGroups.Count == 1)
			{
				return WritePolygon(ringGroups.First(), ordinates);
			}

			// TODO: Initialize with the proper size or allow providing the actual byte[]
			MemoryStream memoryStream = InitialilzeWriter();

			WriteWkbType(WkbGeometryType.MultiPolygon, ordinates);

			_writer.Write(ringGroups.Count);

			foreach (RingGroup ringGroup in ringGroups)
			{
				IList<IPointList> rings = ringGroup.GetLinestrings().Cast<IPointList>().ToList();

				WritePolygonCore(rings, Ordinates.Xyz);
			}

			return memoryStream.ToArray();
		}

		public byte[] WritePolygon([NotNull] RingGroup ringGroup,
		                           Ordinates ordinates = Ordinates.Xyz)
		{
			// TODO: Initialize with the proper size or allow providing the actual byte[]
			MemoryStream memoryStream = InitialilzeWriter();

			WriteWkbType(WkbGeometryType.Polygon, ordinates);

			IList<IPointList> rings = ringGroup.GetLinestrings().Cast<IPointList>().ToList();

			WritePolygonCore(rings, Ordinates.Xyz);

			return memoryStream.ToArray();
		}

		public byte[] WriteMultiLinestring([NotNull] MultiLinestring multiLinestring,
		                                   Ordinates ordinates = Ordinates.Xyz)
		{
			// TODO: Initialize with the proper size or allow providing the actual byte[]
			MemoryStream memoryStream = InitialilzeWriter();

			WriteWkbType(WkbGeometryType.MultiPolygon, ordinates);

			_writer.Write(multiLinestring.Count);

			foreach (Linestring linestring in multiLinestring.GetLinestrings())
			{
				WriteLinestringCore(linestring, ordinates);
			}

			return memoryStream.ToArray();
		}

		public byte[] WritePoint([NotNull] IPnt point, Ordinates ordinates = Ordinates.Xyz)
		{
			MemoryStream memoryStream = InitialilzeWriter();

			WriteWkbType(WkbGeometryType.Point, ordinates);

			WritePointCore(point, ordinates);

			return memoryStream.ToArray();
		}

		public byte[] WriteMultipoint([NotNull] ICollection<IPnt> multipoint,
		                              Ordinates ordinates = Ordinates.Xyz)
		{
			MemoryStream memoryStream = InitialilzeWriter();

			WriteWkbType(WkbGeometryType.MultiPoint, ordinates);

			_writer.Write(multipoint.Count);

			foreach (IPnt point in multipoint)
			{
				WritePointCore(point, ordinates);
			}

			return memoryStream.ToArray();
		}

		private MemoryStream InitialilzeWriter()
		{
			MemoryStream memoryStream = new MemoryStream();

			_writer = new BinaryWriter(memoryStream);

			return memoryStream;
		}

		private void WriteWkbType(WkbGeometryType geometryType, Ordinates ordinates)
		{
			// Byte order: NDR / little endian
			_writer.Write(true);

			uint type = (uint) ordinates + (uint) geometryType;

			_writer.Write(type);
		}

		private void WritePolygonCore(IList<IPointList> ringGroup, Ordinates ordinates)
		{
			if (ringGroup.Count == 0)
			{
				_writer.Write(0);
				return;
			}

			_writer.Write(ringGroup.Count);

			IPointList exteriorRing = ringGroup[0];

			WriteLinestringCore(exteriorRing, ordinates);

			for (int i = 1; i < ringGroup.Count; i++)
			{
				IPointList interiorRing = ringGroup[i];

				WriteLinestringCore(interiorRing, ordinates);
			}
		}

		private void WriteLinestringCore(IPointList linestring, Ordinates ordinates)
		{
			_writer.Write(linestring.PointCount);

			foreach (IPnt point in linestring.AsEnumerablePoints())
			{
				WritePointCore(point, ordinates);
			}
		}

		private void WritePointCore(IPnt point, Ordinates ordinates)
		{
			WriteDouble(point.X);
			WriteDouble(point.Y);

			if (ordinates == Ordinates.Xyz || ordinates == Ordinates.Xyzm)
			{
				// Z Value:
				WriteDouble(point[2]);
			}

			if (ordinates == Ordinates.Xym || ordinates == Ordinates.Xyzm)
				throw new NotImplementedException("M values are currently not supported.");
		}

		private void WriteDouble(double? value)
		{
			if (value.HasValue)
				_writer.Write(value.Value);
			else
				_writer.Write(_doubleNaN);
		}
	}

	public enum Ordinates
	{
		Xy = 0,
		Xyz = 1000,
		Xym = 2000,
		Xyzm = 3000
	}
}
