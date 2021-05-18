using System;
using System.Collections.Generic;
using System.IO;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geometry.Wkb
{
	public class WkbWriter
	{
		private static readonly byte[] _doubleNaN =
			{0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xf8, 0x7f};

		public bool ReversePolygonWindingOrder { get; set; } = true;

		protected BinaryWriter Writer { get; set; }

		protected MemoryStream InitializeWriter()
		{
			MemoryStream memoryStream = new MemoryStream();

			Writer = new BinaryWriter(memoryStream);

			return memoryStream;
		}

		protected void WriteWkbType(WkbGeometryType geometryType, Ordinates ordinates)
		{
			// Byte order: NDR / little endian
			Writer.Write(true);

			uint type = (uint) ordinates + (uint) geometryType;

			Writer.Write(type);
		}

		protected void WritePolygonCore(IList<IPointList> ringGroup, Ordinates ordinates)
		{
			if (ringGroup.Count == 0)
			{
				Writer.Write(0);
				return;
			}

			WriteLineStringsCore(ringGroup, ordinates, ReversePolygonWindingOrder);
		}

		protected void WriteLineStrings(ICollection<IPointList> linestrings,
		                                Ordinates ordinates,
		                                bool reversePointOrder = false)
		{
			WriteLineStrings(linestrings, linestrings.Count, ordinates, reversePointOrder);
		}

		protected void WriteLineStrings(IEnumerable<IPointList> linestrings,
		                                int knownLinestringCount,
		                                Ordinates ordinates,
		                                bool reversePointOrder = false)
		{
			Writer.Write(knownLinestringCount);

			foreach (IPointList linestring in linestrings)
			{
				WriteLinestring(linestring, ordinates, reversePointOrder);
			}
		}

		/// <summary>
		/// Writes a linestring including the preceding type information.
		/// </summary>
		/// <param name="linestring"></param>
		/// <param name="ordinates"></param>
		/// <param name="reversePointOrder"></param>
		protected void WriteLinestring(IPointList linestring,
		                               Ordinates ordinates,
		                               bool reversePointOrder = false)
		{
			WriteWkbType(WkbGeometryType.LineString, ordinates);

			WriteLinestringCore(linestring, ordinates, reversePointOrder);
		}

		protected void WriteLineStringsCore(ICollection<IPointList> linestrings,
		                                    Ordinates ordinates,
		                                    bool reversePointOrder = false)
		{
			Writer.Write(linestrings.Count);

			foreach (IPointList linestring in linestrings)
			{
				WriteLinestringCore(linestring, ordinates, reversePointOrder);
			}
		}

		/// <summary>
		/// Writes a linestring without the preceding type information.
		/// </summary>
		/// <param name="linestring"></param>
		/// <param name="ordinates"></param>
		/// <param name="reversePointOrder"></param>
		protected void WriteLinestringCore([NotNull] IPointList linestring,
		                                   Ordinates ordinates,
		                                   bool reversePointOrder = false)
		{
			int pointCount = linestring.PointCount;

			Writer.Write(pointCount);

			if (reversePointOrder)
			{
				for (int i = pointCount - 1; i >= 0; i--)
				{
					WritePointCore(linestring, i, ordinates);
				}
			}
			else
			{
				for (int i = 0; i < pointCount; i++)
				{
					WritePointCore(linestring, i, ordinates);
				}
			}
		}

		protected void WritePointCore(IPointList pointList, int index, Ordinates ordinates)
		{
			pointList.GetCoordinates(index, out double x, out double y, out double z);

			// NaN is not allowed
			WriteXyCoordinates(x, y);

			if (ordinates == Ordinates.Xyz || ordinates == Ordinates.Xyzm)
			{
				// Z Value (NaN is allowed)
				WriteDoubleOrNan(z);
			}

			if (ordinates == Ordinates.Xym || ordinates == Ordinates.Xyzm)
				throw new NotImplementedException("M values are currently not supported.");
		}

		protected void WritePointCore(IPnt point, Ordinates ordinates)
		{
			WriteXyCoordinates(point.X, point.Y);

			if (ordinates == Ordinates.Xyz || ordinates == Ordinates.Xyzm)
			{
				// Z Value:
				WriteDoubleOrNan(point[2]);
			}

			if (ordinates == Ordinates.Xym || ordinates == Ordinates.Xyzm)
				throw new NotImplementedException("M values are currently not supported.");
		}

		protected void WriteXyCoordinates(double x, double y)
		{
			Writer.Write(x);
			Writer.Write(y);
		}

		protected void WriteDoubleOrNan(double value)
		{
			if (double.IsNaN(value))
				Writer.Write(_doubleNaN);
			else
				Writer.Write(value);
		}
	}
}
