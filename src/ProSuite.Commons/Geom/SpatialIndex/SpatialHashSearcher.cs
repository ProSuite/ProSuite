using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom.SpatialIndex
{
	public class SpatialHashSearcher<T> : ISpatialSearcher<T>, IEnumerable<T>
	{
		private const int _itemCountThreshold = 200;

		private readonly SpatialHashIndex<T> _spatialHashIndex;

		public SpatialHashSearcher(double xMin, double yMin, double gridSize,
		                           int estimatedMaxTileCount,
		                           double estimatedItemsPerTile)
			: this(new TilingDefinition(xMin, yMin, gridSize, gridSize),
			       estimatedMaxTileCount, estimatedItemsPerTile) { }

		public SpatialHashSearcher(TilingDefinition tilingDefinition,
		                           int estimatedMaxTileCount,
		                           double estimatedItemsPerTile)
		{
			Assert.ArgumentCondition(tilingDefinition.TileWidth > 0, "Tile width must be > 0");
			Assert.ArgumentCondition(tilingDefinition.TileHeight > 0, "Tile height must be > 0");

			_spatialHashIndex = new SpatialHashIndex<T>(
				tilingDefinition, estimatedMaxTileCount, estimatedItemsPerTile);
		}

		[CanBeNull]
		public static SpatialHashSearcher<int> CreateSpatialSearcher(
			[NotNull] Linestring forLinestring)
		{
			if (forLinestring.SegmentCount < _itemCountThreshold)
			{
				// No benefit, probably even up to 300
				return null;
			}

			var gridSize =
				EstimateOptimalGridSize(new[] { forLinestring });

			// Avoid very small grid sizes for very vertical geometries
			if (gridSize < double.Epsilon)
			{
				return null;
			}

			return CreateSpatialSearcher(forLinestring, gridSize);
		}

		[NotNull]
		public static SpatialHashSearcher<int> CreateSpatialSearcher(
			Linestring forLinestring, double gridSize)
		{
			var result = new SpatialHashSearcher<int>(
				forLinestring.XMin, forLinestring.YMin,
				gridSize, forLinestring.SegmentCount, 5);

			Populate(result, forLinestring);

			return result;
		}

		[CanBeNull]
		public static SpatialHashSearcher<SegmentIndex> CreateSpatialSearcher(
			MultiLinestring multiLinestring, double? knownAverageSegmentLength = null)
		{
			if (multiLinestring.SegmentCount < _itemCountThreshold)
			{
				return null;
			}

			double gridSize;

			if (knownAverageSegmentLength != null)
			{
				gridSize = EstimateOptimalGridSize(knownAverageSegmentLength.Value);
			}
			else
			{
				gridSize =
					multiLinestring.SegmentCount > 0
						? EstimateOptimalGridSize(multiLinestring.GetLinestrings())
						: 100;
			}

			var result = new SpatialHashSearcher<SegmentIndex>(
				multiLinestring.XMin, multiLinestring.YMin,
				gridSize, multiLinestring.SegmentCount, 5);

			Populate(result, multiLinestring);

			return result;
		}

		[CanBeNull]
		public static SpatialHashSearcher<SegmentIndex> CreateSpatialSearcher(
			MultiLinestring multiLinestring, double gridSize)
		{
			if (multiLinestring.SegmentCount < _itemCountThreshold)
			{
				return null;
			}

			var result = new SpatialHashSearcher<SegmentIndex>(
				multiLinestring.XMin, multiLinestring.YMin,
				gridSize, multiLinestring.SegmentCount, 5);

			Populate(result, multiLinestring);

			return result;
		}

		[CanBeNull]
		public static SpatialHashSearcher<int> CreateSpatialSearcher<TP>(
			[NotNull] Multipoint<TP> multipoint) where TP : IPnt
		{
			if (multipoint.PointCount < _itemCountThreshold)
			{
				// No benefit, probably even up to 300
				return null;
			}

			var gridSize =
				EstimateOptimalGridSize(multipoint.PointCount, multipoint);

			// Avoid very small grid sizes for very vertical geometries
			if (gridSize < double.Epsilon)
			{
				return null;
			}

			return CreateSpatialSearcher(multipoint, gridSize);
		}

		[NotNull]
		public static SpatialHashSearcher<int> CreateSpatialSearcher<TP>(
			Multipoint<TP> multipoint, double gridSize) where TP : IPnt
		{
			var result = new SpatialHashSearcher<int>(
				multipoint.XMin, multipoint.YMin,
				gridSize, multipoint.PointCount, 5);

			Populate(result, multipoint);

			return result;
		}

		public static SpatialHashSearcher<T> CreateSpatialSearcher(
			[NotNull] IList<T> values,
			Func<T, IBoundedXY> getBoundsFunc,
			double gridSize = double.NaN)
		{
			Assert.ArgumentCondition(values.Count > 0, "Empty value list");

			List<IBoundedXY> valueEnvelopes =
				values.Select(getBoundsFunc).ToList();

			EnvelopeXY fullExtent;
			double? suggestedGridSize = SuggestTileSize(valueEnvelopes, out fullExtent);

			if (double.IsNaN(gridSize))
			{
				if (suggestedGridSize == null)
				{
					throw new ArgumentException(
						"Cannot derive grid size from values, please provide a grid size > 0.");
				}

				gridSize = suggestedGridSize.Value;
			}

			Assert.NotNull(fullExtent);

			// Wild guess: only one in four tiles will contain items
			const double estimatedEmptyTileRatio = 4;
			double estimatedTileCount =
				fullExtent.Width * fullExtent.Height / (gridSize * gridSize) /
				estimatedEmptyTileRatio;

			if (estimatedTileCount < 1) estimatedTileCount = 1;

			double estimatedItemsPerTile = values.Count / estimatedTileCount;

			// If maxTileCount is extremely large, it could be that there is an outlier in the items -> large empty space!
			const int maxDictionarySize = int.MaxValue / 2;
			int dictAllocation =
				estimatedTileCount > maxDictionarySize ? -1 : (int) estimatedTileCount;

			var result =
				new SpatialHashSearcher<T>(
					fullExtent.XMin, fullExtent.YMin, gridSize, dictAllocation,
					estimatedItemsPerTile);

			// populate
			for (int i = 0; i < values.Count; i++)
			{
				result.Add(values[i], valueEnvelopes[i]);
			}

			return result;
		}

		public static double EstimateOptimalGridSize(int pointCount, IBoundedXY envelopeXY)
		{
			if (pointCount == 0)
			{
				// Indexing is not necessary
				return -1;
			}

			double geometryHeight = envelopeXY.YMax - envelopeXY.YMin;
			double geometryWidth = envelopeXY.XMax - envelopeXY.XMin;

			// avoid division by 0
			geometryHeight = MathUtils.AreEqual(geometryHeight, 0) ? 1 : geometryHeight;
			geometryWidth = MathUtils.AreEqual(geometryWidth, 0) ? 1 : geometryWidth;

			double pointDensity = pointCount / geometryHeight / geometryWidth;

			double pointSpacing = 1 / Math.Sqrt(pointDensity);

			return pointSpacing * 2;
		}

		public static double EstimateOptimalGridSize(IEnumerable<Linestring> geometries)
		{
			double curveLength = 0;
			int segmentCount = 0;

			foreach (Linestring linestring in geometries)
			{
				int linestringCount = linestring.SegmentCount;

				if (linestringCount < 1000)
				{
					curveLength += linestring.GetLength2D();
					segmentCount += linestringCount;
				}
				else
				{
					// sample using about 250 segments per linestring
					int step = linestring.SegmentCount / 250;

					for (int i = 0; i < linestringCount; i += step)
					{
						curveLength += linestring[i].Length2D;
						segmentCount++;
					}
				}
			}

			return EstimateOptimalGridSize(curveLength, segmentCount);
		}

		public static double EstimateOptimalGridSize(double curveLength, int segmentCount)
		{
			return EstimateOptimalGridSize(curveLength / segmentCount);
		}

		private static double EstimateOptimalGridSize(double averageSegmentLength)
		{
			// For typical polygons the sweet spot is probably between 2 and 3 times
			// the average segment length.
			// TODO: This might be different for multipatches. An estimated number of
			// segments intersecting the interior of a segments envelope would be useful.
			return averageSegmentLength * 3;
		}

		public void Remove(T value, double xMin, double yMin, double xMax, double yMax)
		{
			_spatialHashIndex.Remove(value, xMin, yMin, xMax, yMax);
		}

		public void Add(T value, double xMin, double yMin, double xMax, double yMax)
		{
			_spatialHashIndex.Add(value, xMin, yMin, xMax, yMax);
		}

		public void Add(T value, IBoundedXY boundsXY)
		{
			Add(value, boundsXY.XMin, boundsXY.YMin, boundsXY.XMax, boundsXY.YMax);
		}

		public IEnumerable<T> Search(IBox searchBox,
		                             double tolerance)
		{
			return Search(searchBox.Min.X, searchBox.Min.Y, searchBox.Max.X,
			              searchBox.Max.Y, tolerance);
		}

		public IEnumerable<T> Search(IBoundedXY searchBox,
		                             double tolerance)
		{
			return Search(searchBox.XMin, searchBox.YMin, searchBox.XMax, searchBox.YMax,
			              tolerance);
		}

		public IEnumerable<T> Search(
			double xMin, double yMin, double xMax, double yMax,
			double tolerance, Predicate<T> predicate = null)
		{
			xMin -= tolerance;
			yMin -= tolerance;
			xMax += tolerance;
			yMax += tolerance;

			foreach (T identifier in _spatialHashIndex.FindIdentifiers(
				         xMin, yMin, xMax, yMax, predicate))
			{
				yield return identifier;
			}
		}

		public IEnumerable<T> Search(
			double xMin, double yMin, double xMax, double yMax,
			[NotNull] IBoundedXY knownBounds, double tolerance,
			Predicate<T> predicate = null)
		{
			xMin = Math.Max(xMin, knownBounds.XMin);
			yMin = Math.Max(yMin, knownBounds.YMin);

			xMax = Math.Min(xMax, knownBounds.XMax);
			yMax = Math.Min(yMax, knownBounds.YMax);

			return Search(xMin, yMin, xMax, yMax, tolerance, predicate);
		}

		private static void Populate(SpatialHashSearcher<int> spatialSearcher,
		                             IPointList pointList)
		{
			for (var i = 0; i < pointList.PointCount; i++)
			{
				IPnt pnt = pointList.GetPoint(i);
				spatialSearcher.Add(i, pnt.X, pnt.Y, pnt.X, pnt.Y);
			}
		}

		private static void Populate(SpatialHashSearcher<int> spatialSearcher,
		                             Linestring linestring)
		{
			for (var i = 0; i < linestring.SegmentCount; i++)
			{
				Line3D line = linestring[i];
				spatialSearcher.Add(i, line.XMin, line.YMin, line.XMax, line.YMax);
			}
		}

		private static void Populate(
			SpatialHashSearcher<SegmentIndex> spatialSearcher,
			MultiLinestring multiLinestring)
		{
			for (int i = 0; i < multiLinestring.Count; i++)
			{
				var linestring = multiLinestring.GetLinestring(i);

				for (var j = 0; j < linestring.SegmentCount; j++)
				{
					Line3D line = linestring[j];
					spatialSearcher.Add(new SegmentIndex(i, j),
					                    line.XMin, line.YMin, line.XMax, line.YMax);
				}
			}
		}

		public static double? SuggestTileSize(
			[NotNull] ICollection<IBoundedXY> envelopes,
			[CanBeNull] out EnvelopeXY unionedEnvelope)
		{
			Assert.ArgumentNotNull(envelopes, nameof(envelopes));

			unionedEnvelope = null;

			int totalCount = 0;
			int nonPointCount = 0;
			double totalSideLengths = 0;

			foreach (IBoundedXY envelope in envelopes)
			{
				if (unionedEnvelope == null)
				{
					unionedEnvelope = new EnvelopeXY(envelope);
				}
				else
				{
					unionedEnvelope.EnlargeToInclude(envelope);
				}

				double width = envelope.XMax - envelope.XMin;
				double height = envelope.YMax - envelope.YMin;

				if (width != 0 || height != 0)
				{
					totalSideLengths += width;
					totalSideLengths += height;

					nonPointCount++;
				}

				totalCount++;
			}

			double? result;
			if (totalSideLengths > 0)
			{
				// Non-points: Use average size, we want to limit the number of tiles per item:
				result = nonPointCount > 0
					         ? totalSideLengths / nonPointCount
					         : double.NaN;
			}
			else if (unionedEnvelope != null &&
			         unionedEnvelope.Width > 0 && unionedEnvelope.Height > 0)
			{
				// All points: Use density, we want to limit the number of items per tile:
				result = totalCount > 1
					         ? (unionedEnvelope.Width + unionedEnvelope.Height) / totalCount
					         : double.NaN;
			}
			else
			{
				result = null;
			}

			return result;
		}

		public IEnumerator<T> GetEnumerator()
		{
			return _spatialHashIndex.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
