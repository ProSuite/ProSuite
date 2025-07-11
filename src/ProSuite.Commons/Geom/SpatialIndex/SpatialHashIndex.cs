using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.Geom.SpatialIndex
{
	public class SpatialHashIndex<T> : IEnumerable<T>
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		// NOTE: Using a dictionary of int-lists is considerably faster than a dictionary of point-lists and the performance penalty of the index is small
		// This is most likely due to more efficient memory management when creating one large array rather than many small ones (most likely not even on the LOH)
		// TODO: ConcurrentDictionary, Parallel.Foreach
		[NotNull] private readonly Dictionary<TileIndex, List<T>> _tiles;
		[NotNull] private readonly TilingDefinition _tilingDefinition;

		private int _maxTileEasting = int.MinValue;
		private int _maxTileNorthing = int.MinValue;
		private int _minTileEasting = int.MaxValue;
		private int _minTileNorthing = int.MaxValue;

		private bool _envelopeUpToDate;

		private readonly int _estimatedItemsPerTile;

		private ThreadLocal<HashSet<T>> _foundIdentifiers =
			new ThreadLocal<HashSet<T>>(() => new HashSet<T>());

		public SpatialHashIndex(EnvelopeXY envelope, double gridsize, double estimatedItemsPerTile)
			: this(new TilingDefinition(envelope.XMin, envelope.XMin, gridsize, gridsize),
			       (int) Math.Ceiling(
				       Math.Pow(
					       Math.Max((envelope.XMax - envelope.XMin),
					                (envelope.YMax - envelope.YMin)) / gridsize, 2)),
			       estimatedItemsPerTile) { }

		public SpatialHashIndex(double xMin, double yMin, double gridsize,
		                        int estimatedMaxTileCount,
		                        double estimatedItemsPerTile)
			: this(new TilingDefinition(xMin, yMin, gridsize, gridsize),
			       estimatedMaxTileCount, estimatedItemsPerTile) { }

		public SpatialHashIndex(
			[NotNull] TilingDefinition tilingDefinition,
			int estimatedMaxTileCount,
			double estimatedItemsPerTile)
		{
			_tilingDefinition = tilingDefinition;

			// 10M is the value that was experimentally found to work.
			const int maxDictionaryLengthFor32BitProcess = 10000000;

			if (estimatedMaxTileCount > maxDictionaryLengthFor32BitProcess)
			{
				_msg.DebugFormat(
					"Estimated maximum tile count is too large to pre-assign array. Use larger tile size or smaller extent. Performance will suffer and bad things might happen.");

				estimatedMaxTileCount = maxDictionaryLengthFor32BitProcess;
			}
			else if (estimatedMaxTileCount < 0)
			{
				estimatedMaxTileCount = 0;
			}

			_tiles = new Dictionary<TileIndex, List<T>>(estimatedMaxTileCount);

			if (double.IsNaN(estimatedItemsPerTile) || estimatedItemsPerTile < 0)
			{
				estimatedItemsPerTile = 1;
			}

			_estimatedItemsPerTile = (int) Math.Ceiling(estimatedItemsPerTile);
		}

		public double GridSize => _tilingDefinition.TileWidth;
		public double OriginX => _tilingDefinition.OriginX;
		public double OriginY => _tilingDefinition.OriginY;

		// @PLU: Decided to implement this with raw coordinates instead of EnvelopeXY because these are
		// TileIndexes and not real coordinates. That's also why they're private. If we wanted to expose
		// an envelope, we'd have to calculate it from these.
		private int MinTileEasting
		{
			get
			{
				if (! _envelopeUpToDate) UpdateTileIndexEnvelope();
				return _minTileEasting;
			}
		}

		private int MinTileNorthing
		{
			get
			{
				if (! _envelopeUpToDate) UpdateTileIndexEnvelope();
				return _minTileNorthing;
			}
		}

		private int MaxTileEasting
		{
			get
			{
				if (! _envelopeUpToDate) UpdateTileIndexEnvelope();
				return _maxTileEasting;
			}
		}

		private int MaxTileNorthing
		{
			get
			{
				if (! _envelopeUpToDate) UpdateTileIndexEnvelope();
				return _maxTileNorthing;
			}
		}

		public void Add(T identifier, double x, double y)
		{
			TileIndex tileIndex = _tilingDefinition.GetTileIndexAt(x, y);
			Add(identifier, tileIndex);
		}

		public void Add(T identifier, Box box)
		{
			IEnumerable<TileIndex> intersectedTiles =
				_tilingDefinition.GetIntersectingTiles(
					box.Min.X, box.Min.Y, box.Max.X, box.Max.Y);

			Add(identifier, intersectedTiles);
		}

		public void Remove(T identifier, double xMin, double yMin, double xMax, double yMax)
		{
			IEnumerable<TileIndex> intersectedTiles =
				_tilingDefinition.GetIntersectingTiles(xMin, yMin, xMax, yMax);

			foreach (TileIndex intersectedTileIdx in intersectedTiles)
			{
				List<T> tileGeometryRefs;

				if (! _tiles.TryGetValue(intersectedTileIdx, out tileGeometryRefs))
				{
					continue;
				}

				if (tileGeometryRefs.Contains(identifier))
				{
					tileGeometryRefs.Remove(identifier);
				}
			}
		}

		public void Add(T identifier, double xMin, double yMin, double xMax, double yMax)
		{
			IEnumerable<TileIndex> intersectedTiles =
				_tilingDefinition.GetIntersectingTiles(xMin, yMin, xMax, yMax);

			Add(identifier, intersectedTiles);
		}

		public void Add(T identifier, TileIndex tileIndex)
		{
			List<T> tileGeometryRefs;

			if (! _tiles.TryGetValue(tileIndex, out tileGeometryRefs))
			{
				tileGeometryRefs = new List<T>(_estimatedItemsPerTile);
				_tiles.Add(tileIndex, tileGeometryRefs);
				_envelopeUpToDate = false;
			}

			if (_msg.IsVerboseDebugEnabled &&
			    tileGeometryRefs.Count >= _estimatedItemsPerTile)
			{
				_msg.DebugFormat(
					"Number of items in tile {0} is exceeding the estimated maximum and now contains {1} items",
					tileIndex, tileGeometryRefs.Count + 1);
			}

			tileGeometryRefs.Add(identifier);
		}

		public void Add(T identifier, IEnumerable<TileIndex> intersectedTiles)
		{
			foreach (TileIndex intersectedTileIdx in intersectedTiles)
			{
				Add(identifier, intersectedTileIdx);
			}
		}

		/// <summary>
		/// Get Identifiers per tile, starting with the tile containing to the given point sorted according to the given DistanceMetric.
		/// </summary>
		/// <param name="x">X coordinate</param>
		/// <param name="y">Y coordinate</param>
		/// <param name="metric">The type of distance you want to use to order the tiles</param>
		/// <param name="maxDistance">The maximum distance until which tiles are returned.</param>
		/// <param name="predicate">Predicate to restrict which Elements are returned</param>
		/// <returns></returns>
		public IEnumerable<IEnumerable<T>> FindTilesAround(double x, double y,
		                                                   double maxDistance = double.MaxValue,
		                                                   DistanceMetric metric =
			                                                   DistanceMetric.EuclideanDistance,
		                                                   [CanBeNull] Predicate<T> predicate =
			                                                   null)
		{
			if (_tiles.Count == 0)
				yield break;

			double maxExistingTileDistance = GetDistanceToFurthestPopulatedTile(x, y);

			double effectiveMaxDistance = Math.Min(maxExistingTileDistance, maxDistance);

			foreach (var tileIndex in _tilingDefinition.GetTileIndexAround(
				         x, y, metric, effectiveMaxDistance))
			{
				// Only yield tiles that exist and have items
				if (_tiles.ContainsKey(tileIndex))
				{
					yield return FindItemsWithinTile(tileIndex, predicate);
				}
			}
		}

		public IEnumerable<T> FindIdentifiers(
			double xMin, double yMin, double xMax, double yMax,
			[CanBeNull] Predicate<T> predicate = null)
		{
			// The resulting identifiers must be made distinct
			HashSet<T> resultList = _foundIdentifiers.Value;
			resultList.Clear();

			// check the intersecting neighbour tiles:
			foreach (TileIndex neighborTileIdx in
			         _tilingDefinition.GetIntersectingTiles(
				         xMin, yMin, xMax, yMax))
			{
				foreach (T geometryIdentifier in
				         FindItemsWithinTile(neighborTileIdx, predicate))
				{
					resultList.Add(geometryIdentifier);
				}
			}

			return resultList;
		}

		public IEnumerable<T> FindIdentifiers(
			EnvelopeXY envelope,
			[CanBeNull] Predicate<T> predicate = null)
		{
			return FindIdentifiers(envelope.XMin, envelope.YMin, envelope.XMax, envelope.YMax,
			                       predicate);
		}

		public IEnumerable<T> FindIdentifiers(
			Box box,
			[CanBeNull] Predicate<T> predicate = null)
		{
			return FindIdentifiers(box.Min.X, box.Min.Y, box.Max.X, box.Max.Y, predicate);
		}

		public override string ToString()
		{
			return $"SpatialHashIndex with {_tiles.Count} tiles, estimated items per tile: " +
			       $"{_estimatedItemsPerTile}, {_tiles.Count(kvp => kvp.Value.Count > _estimatedItemsPerTile)} " +
			       $"tiles exceed the estimated item count. Tiling: {_tilingDefinition}";
		}

		public IEnumerator<T> GetEnumerator()
		{
			// The resulting identifiers must be made distinct
			HashSet<T> resultList = _foundIdentifiers.Value;
			resultList.Clear();

			foreach (var identifiers in _tiles.Values)
			{
				foreach (T identifier in identifiers)
				{
					resultList.Add(identifier);
				}
			}

			return resultList.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		private IEnumerable<T> FindItemsWithinTile(TileIndex tileIndex,
		                                           [CanBeNull] Predicate<T> predicate)
		{
			Assert.NotNull(_tiles, "Tiles not initialized");

			List<T> tileGeometryRefs;
			if (! _tiles.TryGetValue(tileIndex, out tileGeometryRefs))
			{
				yield break;
			}

			foreach (T geometryIdentifier in tileGeometryRefs)
			{
				if (predicate == null || predicate(geometryIdentifier))
				{
					yield return geometryIdentifier;
				}
			}
		}

		private double GetDistanceToFurthestPopulatedTile(double x, double y)
		{
			var centerTile = _tilingDefinition.GetTileIndexAt(x, y);

			int furthestEasting = Math.Abs(MaxTileEasting - centerTile.East) >
			                      Math.Abs(MinTileEasting - centerTile.East)
				                      ? MaxTileEasting
				                      : MinTileEasting;

			int furthestNorthing = Math.Abs(MaxTileNorthing - centerTile.North) >
			                       Math.Abs(MinTileNorthing - centerTile.North)
				                       ? MaxTileNorthing
				                       : MinTileNorthing;

			var furthestTile = new TileIndex(furthestEasting, furthestNorthing);

			return TileUtils.TileDistance(centerTile, furthestTile, _tilingDefinition.TileWidth,
			                              _tilingDefinition.TileHeight);
		}

		private void UpdateTileIndexEnvelope()
		{
			foreach (TileIndex tileIndex in _tiles.Keys)
			{
				if (tileIndex.East > _maxTileEasting) _maxTileEasting = tileIndex.East;
				if (tileIndex.East < _minTileEasting) _minTileEasting = tileIndex.East;
				if (tileIndex.North > _maxTileNorthing) _maxTileNorthing = tileIndex.North;
				if (tileIndex.North < _minTileNorthing) _minTileNorthing = tileIndex.North;
			}

			_envelopeUpToDate = true;
		}
	}
}
