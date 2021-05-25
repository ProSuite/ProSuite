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

		protected static TPoint ReadPointCore<TPoint>(BinaryReader reader,
		                                              Ordinates ordinates,
		                                              IPointFactory<TPoint> pointBuilder)
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
		/// <typeparam name="TMultipoint"></typeparam>
		/// <typeparam name="TLinestring"></typeparam>
		/// <typeparam name="TPoint"></typeparam>
		/// <param name="reader"></param>
		/// <param name="geometryType"></param>
		/// <param name="ordinates"></param>
		/// <param name="geometryBuilder"></param>
		/// <returns></returns>
		protected static IEnumerable<TLinestring> ReadLinestrings<TMultipoint, TLinestring, TPoint>(
			[NotNull] BinaryReader reader,
			WkbGeometryType geometryType,
			Ordinates ordinates,
			[NotNull] GeometryBuilderBase<TMultipoint, TLinestring, TPoint> geometryBuilder)
		{
			IEnumerable<TLinestring> linestrings;

			if (geometryType == WkbGeometryType.MultiLineString ||
			    geometryType == WkbGeometryType.Polygon ||
			    geometryType == WkbGeometryType.MultiPolygon)
			{
				linestrings =
					ReadLinestrings(reader, ordinates, geometryBuilder);
			}

			else if (geometryType == WkbGeometryType.LineString)
			{
				TLinestring linestring = ReadLinestringCore(reader, ordinates, geometryBuilder);

				linestrings = new[] {linestring};
			}
			else
			{
				throw new NotSupportedException(
					$"Cannot read {geometryType} as lineString or multiple lineStrings.");
			}

			return linestrings;
		}

		protected static IEnumerable<TLinestring> ReadLinestringsCore<
			TMultipoint, TLinestring, TPoint>(
			[NotNull] BinaryReader reader,
			Ordinates ordinates,
			int linestringCount,
			[NotNull] GeometryBuilderBase<TMultipoint, TLinestring, TPoint> geometryBuilder)
		{
			for (int i = 0; i < linestringCount; i++)
			{
				yield return ReadLinestringCore(reader, ordinates, geometryBuilder);
			}
		}

		protected static TMultipoint ReadMultipointCore<TMultipoint, TLinestring, TPoint>(
			[NotNull] BinaryReader reader,
			Ordinates ordinates,
			int pointCount,
			[NotNull] GeometryBuilderBase<TMultipoint, TLinestring, TPoint> geometryBuilder)
		{
			IPointFactory<TPoint> builder = geometryBuilder.GetPointFactory(ordinates);

			const bool reReadPointTypes = true;

			IEnumerable<TPoint> readPointsCore =
				ReadPointsCore(reader, ordinates, builder, pointCount, reReadPointTypes);

			return geometryBuilder.CreateMultipoint(readPointsCore, pointCount);
		}

		/// <summary>
		/// Reads multiple (>1) line strings, including the initial count.
		/// </summary>
		/// <typeparam name="TMultipoint"></typeparam>
		/// <typeparam name="TLinestring"></typeparam>
		/// <typeparam name="TPoint"></typeparam>
		/// <param name="reader"></param>
		/// <param name="ordinates"></param>
		/// <param name="geometryBuilder"></param>
		/// <returns></returns>
		private static IEnumerable<TLinestring> ReadLinestrings<TMultipoint, TLinestring, TPoint>(
			[NotNull] BinaryReader reader,
			Ordinates ordinates,
			[NotNull] GeometryBuilderBase<TMultipoint, TLinestring, TPoint> geometryBuilder)
		{
			int linestringCount = checked((int) reader.ReadUInt32());

			return ReadLinestrings(reader, ordinates, linestringCount, geometryBuilder);
		}

		/// <summary>
		/// Reads multiple (>1) line strings, without the initial count.
		/// </summary>
		/// <typeparam name="TMultipoint"></typeparam>
		/// <typeparam name="TLinestring"></typeparam>
		/// <typeparam name="TPoint"></typeparam>
		/// <param name="reader"></param>
		/// <param name="expectedOrdinates"></param>
		/// <param name="linestringCount"></param>
		/// <param name="geometryBuilder"></param>
		/// <returns></returns>
		private static IEnumerable<TLinestring> ReadLinestrings<TMultipoint, TLinestring, TPoint>(
			[NotNull] BinaryReader reader,
			Ordinates expectedOrdinates,
			int linestringCount,
			[NotNull] GeometryBuilderBase<TMultipoint, TLinestring, TPoint> geometryBuilder)
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

		private static TLinestring ReadLinestringCore<TMultipoint, TLinestring, TPoint>(
			BinaryReader reader, Ordinates ordinates,
			GeometryBuilderBase<TMultipoint, TLinestring, TPoint> geometryBuilder)
		{
			int pointCount = checked((int) reader.ReadUInt32());

			IPointFactory<TPoint> builder = geometryBuilder.GetPointFactory(ordinates);

			return geometryBuilder.CreateLinestring(
				ReadPointsCore(reader, ordinates, builder, pointCount), pointCount);
		}

		/// <summary>
		/// Reads the specified number of points, optionally re-reads the type for each point.
		/// </summary>
		/// <typeparam name="TPoint"></typeparam>
		/// <param name="reader"></param>
		/// <param name="ordinates"></param>
		/// <param name="builder"></param>
		/// <param name="pointCount"></param>
		/// <param name="reReadTypeForEachPoint"></param>
		/// <returns></returns>
		private static IEnumerable<TPoint> ReadPointsCore<TPoint>(
			[NotNull] BinaryReader reader,
			Ordinates ordinates,
			[NotNull] IPointFactory<TPoint> builder,
			int pointCount,
			bool reReadTypeForEachPoint = false)
		{
			for (int i = 0; i < pointCount; i++)
			{
				if (reReadTypeForEachPoint)
				{
					ReadWkbType(reader, false, out WkbGeometryType _, out Ordinates _);
				}

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
