using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.Geom.SpatialIndex
{
	public class ParallelSpatialHashIndex<T> : IEnumerable<T>
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		// ConcurrentDictionary to allow thread-safe operations
		[NotNull] private readonly ConcurrentDictionary<TileIndex, List<T>> _tiles;

		[NotNull] private readonly TilingDefinition _tilingDefinition;

		private readonly int _estimatedItemsPerTile;

		// Thread-local storage to avoid contention when used in parallel
		[ThreadStatic] private static HashSet<T> _threadLocalFoundIdentifiers;

		public ParallelSpatialHashIndex(double xMin, double yMin, double gridsize,
		                                int estimatedMaxTileCount,
		                                double estimatedItemsPerTile)
			: this(new TilingDefinition(xMin, yMin, gridsize, gridsize),
			       estimatedMaxTileCount, estimatedItemsPerTile) { }

		public ParallelSpatialHashIndex(
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

			// Switched to ConcurrentDictionary
			_tiles = new ConcurrentDictionary<TileIndex, List<T>>(
				Environment.ProcessorCount, estimatedMaxTileCount);

			if (double.IsNaN(estimatedItemsPerTile) || estimatedItemsPerTile < 0)
			{
				estimatedItemsPerTile = 1;
			}

			_estimatedItemsPerTile = (int) Math.Ceiling(estimatedItemsPerTile);
		}

		public void Add(T identifier, double x, double y)
		{
			TileIndex tileIndex = _tilingDefinition.GetTileIndexAt(x, y);
			IEnumerable < TileIndex > intersectedTile = new List<TileIndex> { tileIndex };
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
			// Using Parallel.ForEach to process tiles in parallel
			Parallel.ForEach(intersectedTiles, intersectedTileIdx =>
			{
				List<T> tileGeometryRefs = _tiles.GetOrAdd(
					intersectedTileIdx,
					_ => new List<T>(_estimatedItemsPerTile)
				);

				// Need to synchronize access to the List since List<T> is not thread-safe
				lock (tileGeometryRefs)
				{
					if (_msg.IsVerboseDebugEnabled &&
					    tileGeometryRefs.Count >= _estimatedItemsPerTile)
					{
						_msg.DebugFormat(
							"Number of items in tile {0} is exceeding the estimated maximum and now contains {1} items",
							intersectedTileIdx, tileGeometryRefs.Count + 1);
					}

					tileGeometryRefs.Add(identifier);
				}
			});
		}

		public IEnumerable<T> FindIdentifiers(
			double xMin, double yMin, double xMax, double yMax,
			[CanBeNull] Predicate<T> predicate = null)
		{
			// Get thread-local HashSet to avoid contention
			HashSet<T> foundIdentifiers = GetThreadLocalFoundIdentifiers();
			foundIdentifiers.Clear();

			// Get intersecting tiles
			var intersectingTiles = _tilingDefinition.GetIntersectingTiles(
				xMin, yMin, xMax, yMax).ToList();

			// Use ConcurrentBag to collect results from parallel processing
			var resultBag = new ConcurrentBag<T>();

			// Process tiles in parallel
			Parallel.ForEach(intersectingTiles, neighborTileIdx =>
			{
				foreach (T geometryIdentifier in
				         FindItemsWithinTile(neighborTileIdx, predicate))
				{
					resultBag.Add(geometryIdentifier);
				}
			});

			// Make results distinct by adding to HashSet
			foreach (var item in resultBag)
			{
				foundIdentifiers.Add(item);
			}

			return foundIdentifiers;
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
			// Get thread-local HashSet
			HashSet<T> foundIdentifiers = GetThreadLocalFoundIdentifiers();
			foundIdentifiers.Clear();

			// Process tile values in parallel to collect all unique identifiers
			var allIdentifiers = new ConcurrentBag<T>();

			Parallel.ForEach(_tiles, tileEntry =>
			{
				List<T> tileGeometryRefs = tileEntry.Value;
				lock (tileGeometryRefs)
				{
					foreach (T identifier in tileGeometryRefs)
					{
						allIdentifiers.Add(identifier);
					}
				}
			});

			// Make results distinct
			foreach (var item in allIdentifiers)
			{
				foundIdentifiers.Add(item);
			}

			return foundIdentifiers.GetEnumerator();
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

			// Take a snapshot of the list to avoid race conditions
			T[] snapshot;
			lock (tileGeometryRefs)
			{
				snapshot = tileGeometryRefs.ToArray();
			}

			foreach (T geometryIdentifier in snapshot)
			{
				if (predicate == null || predicate(geometryIdentifier))
				{
					yield return geometryIdentifier;
				}
			}
		}

		// Helper method to get or create thread-local HashSet
		private static HashSet<T> GetThreadLocalFoundIdentifiers()
		{
			if (_threadLocalFoundIdentifiers == null)
			{
				_threadLocalFoundIdentifiers = new HashSet<T>();
			}

			return _threadLocalFoundIdentifiers;
		}
	}
}
