using System;
using System.Collections.Generic;
using System.IO;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geometry.Wkb
{
	public abstract class WkbReader
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
		/// assumeWkbPolygonsClockwise should be set to true.
		/// Probably most other implementations conform to OGC 1.2</param>
		protected WkbReader(bool assumeWkbPolygonsClockwise)
		{
			AssumeWkbPolygonsClockwise = assumeWkbPolygonsClockwise;
		}

		protected bool AssumeWkbPolygonsClockwise { get; }

		/// <summary>
		/// Reads the first boolean of the provides stream and initializes the appropriate
		/// binary reader which will be at position 1 when returned.
		/// </summary>
		/// <param name="stream"></param>
		/// <returns></returns>
		protected static BinaryReader InitializeReader(Stream stream)
		{
			BinaryReader binaryReader = new BinaryReader(stream);

			bool isLittleEndian = binaryReader.ReadBoolean();

			BinaryReader reader;

			if (! isLittleEndian)
			{
				binaryReader.Dispose();

				reader = new BigEndianBinaryReader(stream);
				reader.ReadBoolean();
			}
			else
			{
				reader = binaryReader;
			}

			return reader;
		}

		protected static void ReadWkbType([NotNull] BinaryReader reader,
		                                  bool skipEndianness,
		                                  out WkbGeometryType geometryType,
		                                  out Ordinates ordinates)
		{
			if (! skipEndianness)
			{
				reader.ReadBoolean();
			}

			int type = checked((int) reader.ReadUInt32());

			geometryType = (WkbGeometryType) (type % 1000);

			ordinates = GetOrdinates(type);
		}

		protected static P ReadPointCore<P>(BinaryReader reader, Ordinates ordinates,
		                                    IPointFactory<P> pointBuilder)
		{
			switch (ordinates)
			{
				case Ordinates.Xy:
					return pointBuilder.CreatePointXy(reader.ReadDouble(),
					                                  reader.ReadDouble());
				case Ordinates.Xyz:
					return pointBuilder.CreatePointXyz(reader.ReadDouble(),
					                                   reader.ReadDouble(),
					                                   reader.ReadDouble());
				case Ordinates.Xym:
					return pointBuilder.CreatePointXym(reader.ReadDouble(),
					                                   reader.ReadDouble(),
					                                   reader.ReadDouble());

				case Ordinates.Xyzm:
					return pointBuilder.CreatePointXyzm(reader.ReadDouble(),
					                                    reader.ReadDouble(),
					                                    reader.ReadDouble(),
					                                    reader.ReadDouble());

				default: throw new NotSupportedException(ordinates.ToString());
			}
		}

		/// <summary>
		/// Reads a LineString or multiple LineString geometries using the specified reader.
		/// </summary>
		/// <typeparam name="L"></typeparam>
		/// <typeparam name="P"></typeparam>
		/// <param name="reader"></param>
		/// <param name="geometryType"></param>
		/// <param name="ordinates"></param>
		/// <param name="geometryBuilder"></param>
		/// <returns></returns>
		protected static IEnumerable<L> ReadLinestrings<L, P>(
			[NotNull] BinaryReader reader,
			WkbGeometryType geometryType,
			Ordinates ordinates,
			[NotNull] GeometryBuilderBase<L, P> geometryBuilder)
		{
			IEnumerable<L> linestrings;

			if (geometryType == WkbGeometryType.MultiLineString ||
			    geometryType == WkbGeometryType.Polygon ||
			    geometryType == WkbGeometryType.MultiPolygon)
			{
				linestrings =
					ReadLinestrings(reader, ordinates, geometryBuilder);
			}

			else if (geometryType == WkbGeometryType.LineString)
			{
				L linestring = ReadLinestringCore(reader, ordinates, geometryBuilder);

				linestrings = new[] {linestring};
			}
			else
			{
				throw new NotSupportedException(
					$"Cannot read {geometryType} as lineString or multiple lineStrings.");
			}

			return linestrings;
		}

		protected static IEnumerable<L> ReadLinestringsCore<L, P>(
			[NotNull] BinaryReader reader,
			Ordinates ordinates,
			int linestringCount,
			[NotNull] GeometryBuilderBase<L, P> geometryBuilder)
		{
			for (int i = 0; i < linestringCount; i++)
			{
				yield return ReadLinestringCore(reader, ordinates, geometryBuilder);
			}
		}

		/// <summary>
		/// Reads multiple (>1) line strings, including the initial count.
		/// </summary>
		/// <typeparam name="L"></typeparam>
		/// <typeparam name="P"></typeparam>
		/// <param name="reader"></param>
		/// <param name="ordinates"></param>
		/// <param name="geometryBuilder"></param>
		/// <returns></returns>
		private static IEnumerable<L> ReadLinestrings<L, P>(
			[NotNull] BinaryReader reader,
			Ordinates ordinates,
			[NotNull] GeometryBuilderBase<L, P> geometryBuilder)
		{
			int linestringCount = checked((int) reader.ReadUInt32());

			return ReadLinestrings(reader, ordinates, linestringCount, geometryBuilder);
		}

		private static IEnumerable<L> ReadLinestrings<L, P>(
			[NotNull] BinaryReader reader,
			Ordinates expectedOrdinates,
			int linestringCount,
			[NotNull] GeometryBuilderBase<L, P> geometryBuilder)
		{
			for (int i = 0; i < linestringCount; i++)
			{
				ReadWkbType(reader, false,
				            out WkbGeometryType geometryType, out Ordinates ordinates);

				Assert.AreEqual(WkbGeometryType.LineString, geometryType,
				                "Unexpected geometry type");

				Assert.AreEqual(expectedOrdinates, ordinates,
				                "Linestring with inconsistent ordinates encountered.");

				yield return ReadLinestringCore(reader, ordinates, geometryBuilder);
			}
		}

		private static L ReadLinestringCore<L, P>(BinaryReader reader, Ordinates ordinates,
		                                          GeometryBuilderBase<L, P> geometryBuilder)
		{
			int pointCount = checked((int) reader.ReadUInt32());

			IPointFactory<P> builder = geometryBuilder.GetPointFactory(ordinates);

			return geometryBuilder.CreateLinestring(
				ReadPointsCore(reader, ordinates, builder, pointCount), pointCount);
		}

		private static IEnumerable<P> ReadPointsCore<P>([NotNull] BinaryReader reader,
		                                                Ordinates ordinates,
		                                                [NotNull] IPointFactory<P> builder,
		                                                int pointCount)
		{
			for (int i = 0; i < pointCount; i++)
			{
				yield return ReadPointCore(reader, ordinates, builder);
			}
		}

		private static Ordinates GetOrdinates(int type)
		{
			if (type >= 1000 && type < 2000)
				return Ordinates.Xyz;
			if (type >= 2000 && type < 3000)
				return Ordinates.Xym;
			if (type >= 3000 && type < 4000)
				return Ordinates.Xyzm;

			return Ordinates.Xy;
		}
	}
}
