using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geometry.Wkb
{
	/// <summary>
	/// Writes the provided geometries in little-endian WKB format.
	/// </summary>
	public class WkbGeomWriter : WkbWriter
	{
		public byte[] WritePoint([NotNull] IPnt point, Ordinates ordinates = Ordinates.Xyz)
		{
			MemoryStream memoryStream = InitializeWriter();

			WriteWkbType(WkbGeometryType.Point, ordinates);

			WritePointCore(point, ordinates);

			return memoryStream.ToArray();
		}

		public byte[] WriteMultipolygon(MultiPolycurve multipolygon,
		                                Ordinates ordinates = Ordinates.Xyz)
		{
			return WriteMultipolygon(
				GeomTopoOpUtils.GetConnectedComponents(multipolygon, double.Epsilon).ToList(),
				ordinates);
		}

		public byte[] WriteMultipolygon([NotNull] ICollection<RingGroup> ringGroups,
		                                Ordinates ordinates = Ordinates.Xyz)
		{
			if (ringGroups.Count == 1)
			{
				return WritePolygon(ringGroups.First(), ordinates);
			}

			// TODO: Initialize with the proper size or allow providing the actual byte[]
			MemoryStream memoryStream = InitializeWriter();

			WriteWkbType(WkbGeometryType.MultiPolygon, ordinates);

			Writer.Write(ringGroups.Count);

			foreach (RingGroup ringGroup in ringGroups)
			{
				WriteWkbType(WkbGeometryType.Polygon, ordinates);

				IList<IPointList> rings = ringGroup.GetLinestrings().Cast<IPointList>().ToList();

				WritePolygonCore(rings, ordinates);
			}

			return memoryStream.ToArray();
		}

		public byte[] WritePolygon([NotNull] RingGroup ringGroup,
		                           Ordinates ordinates = Ordinates.Xyz)
		{
			// TODO: Initialize with the proper size or allow providing the actual byte[]
			MemoryStream memoryStream = InitializeWriter();

			WriteWkbType(WkbGeometryType.Polygon, ordinates);

			IList<IPointList> rings = ringGroup.GetLinestrings().Cast<IPointList>().ToList();

			WritePolygonCore(rings, ordinates);

			return memoryStream.ToArray();
		}

		public byte[] WriteMultiLinestring([NotNull] MultiLinestring multiLinestring,
		                                   Ordinates ordinates = Ordinates.Xyz)
		{
			// TODO: Initialize with the proper size or allow providing the actual byte[]
			MemoryStream memoryStream = InitializeWriter();

			if (multiLinestring.Count == 1)
			{
				WriteLinestring(multiLinestring.GetLinestring(0), ordinates);
			}
			else
			{
				WriteWkbType(WkbGeometryType.MultiLineString, ordinates);

				Writer.Write(multiLinestring.Count);

				foreach (Linestring linestring in multiLinestring.GetLinestrings())
				{
					WriteLinestring(linestring, ordinates);
				}
			}

			return memoryStream.ToArray();
		}

		public byte[] WriteMultipoint([NotNull] ICollection<IPnt> multipoint,
		                              Ordinates ordinates = Ordinates.Xyz)
		{
			MemoryStream memoryStream = InitializeWriter();

			WriteWkbType(WkbGeometryType.MultiPoint, ordinates);

			Writer.Write(multipoint.Count);

			foreach (IPnt point in multipoint)
			{
				WritePointCore(point, ordinates);
			}

			return memoryStream.ToArray();
		}
	}
}
