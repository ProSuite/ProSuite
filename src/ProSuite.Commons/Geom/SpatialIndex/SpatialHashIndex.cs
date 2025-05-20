using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

		private readonly int _estimatedItemsPerTile;

		private HashSet<T> _foundIdentifiers;

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

		public void Add(T identifier, double x, double y)
		{
			TileIndex tileIndex = _tilingDefinition.GetTileIndexAt(x, y);
			IEnumerable<TileIndex> intersectedTile = new List<TileIndex> { tileIndex };
			Add(identifier, intersectedTile);
		}

		public void Add(T identifier, Box box)
		{
			IEnumerable<TileIndex> intersectedTiles =
				_tilingDefinition.GetIntersectingTiles(
					box.Min.X, box.Min.Y, box.Max.X, box.Max.Y);

			Add(identifier, intersectedTiles);
		}

		public void Add(T identifier, double xMin, double yMin, double xMax, double yMax)
		{
			IEnumerable<TileIndex> intersectedTiles =
				_tilingDefinition.GetIntersectingTiles(xMin, yMin, xMax, yMax);

			Add(identifier, intersectedTiles);
		}

		public void Add(T identifier, IEnumerable<TileIndex> intersectedTiles)
		{
			foreach (TileIndex intersectedTileIdx in intersectedTiles)
			{
				List<T> tileGeometryRefs;
				if (! _tiles.TryGetValue(intersectedTileIdx, out tileGeometryRefs))
				{
					tileGeometryRefs = new List<T>(_estimatedItemsPerTile);

					_tiles.Add(intersectedTileIdx, tileGeometryRefs);
				}

				if (_msg.IsVerboseDebugEnabled &&
				    tileGeometryRefs.Count >= _estimatedItemsPerTile)
				{
					_msg.DebugFormat(
						"Numer of items in tile {0} is exceeding the estimated maximum and now contains {1} items",
						intersectedTileIdx, tileGeometryRefs.Count + 1);
				}

				tileGeometryRefs.Add(identifier);
			}
		}

		public IEnumerable<T> FindIdentifiers(
			double xMin, double yMin, double xMax, double yMax,
			[CanBeNull] Predicate<T> predicate = null)
		{
			// The resulting identifiers must be made distinct
			// TODO: ConcurrentHashset 
			_foundIdentifiers = _foundIdentifiers ?? new HashSet<T>();
			_foundIdentifiers.Clear();

			// check the intersecting neighbour tiles:
			foreach (TileIndex neighborTileIdx in
			         _tilingDefinition.GetIntersectingTiles(
				         xMin, yMin, xMax, yMax))
			{
				foreach (T geometryIdentifier in
				         FindItemsWithinTile(neighborTileIdx, predicate))
				{
					if (! _foundIdentifiers.Contains(geometryIdentifier))
					{
						_foundIdentifiers.Add(geometryIdentifier);
					}
				}
			}

			return _foundIdentifiers;
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
			// TODO: ConcurrentHashset 
			_foundIdentifiers = _foundIdentifiers ?? new HashSet<T>();
			_foundIdentifiers.Clear();

			foreach (var identifiers in _tiles.Values)
			{
				foreach (T identifier in identifiers)
				{
					_foundIdentifiers.Add(identifier);
				}
			}

			return _foundIdentifiers.GetEnumerator();
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
	}
}
