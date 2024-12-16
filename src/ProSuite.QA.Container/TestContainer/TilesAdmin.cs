using System;
using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Logging;

namespace ProSuite.QA.Container.TestContainer
{
	/// <summary>
	/// A second-level cache for entire <see cref="TileCache"/> instances kept in memory.
	/// Currently, all tiles left and lower than the current tile are removed from the cache
	/// when a new tile is prepared.
	/// TODO: Keep the single tile to the left. Consider direct data access when loading data
	/// in areas left or lower the currently cached tiles to avoid multiple loading of the full tile.
	/// </summary>
	internal class TilesAdmin
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly TileCache _tileCache;
		private readonly ITileEnumContext _tileEnumContext;

		private readonly Dictionary<Box, TileCache> _caches;
		private readonly Box.BoxComparer _boxComparer;

		public TilesAdmin([NotNull] ITileEnumContext context, [NotNull] TileCache tileCache)
		{
			_tileEnumContext = context;
			_tileCache = tileCache;

			_boxComparer = new Box.BoxComparer();
			_caches = new Dictionary<Box, TileCache>(_boxComparer);
		}

		private void EnsureLoaded([NotNull] TileCache tileCache, [NotNull] Tile tile,
		                          [NotNull] CachedTableProps tableProps)
		{
			IReadOnlyTable table = tableProps.Table;
			// Check if tileCache is of current tile and current tile is loading
			if (tileCache.LoadingTileBox != null &&
			    _boxComparer.Equals(tileCache.LoadingTileBox, tileCache.CurrentTileBox) == false)
			{
				Assert.AreEqual(_tileCache, tileCache, "_tileCache instance expected");
				// the table must be already loaded
				IEnvelope loadedTableExtent = tileCache.GetLoadedExtent(table);
				Assert.NotNull(loadedTableExtent, $"Table {table.Name} in current tile not loaded");

				loadedTableExtent.QueryWKSCoords(out WKSEnvelope w);
				Box l = tileCache.LoadingTileBox;
				Assert.True(w.XMin <= l.Min.X && w.YMin <= l.Min.Y &&
				            w.XMax >= l.Max.X && w.YMax >= l.Max.Y,
				            "Extent mismatch");

				return;
			}

			if (tileCache.GetLoadedExtent(table) != null)
			{
				_msg.VerboseDebug(() => $"Tile {tileCache.CurrentTileBox} already loaded for " +
				                        $"{tableProps} ({tileCache.GetCachedRowCount(table)})");
				return;
			}

			IDictionary<BaseRow, CachedRow> cachedRows =
				_tileEnumContext.OverlappingFeatures.GetOverlappingCachedRows(table, tile.Box);
			tileCache.LoadCachedTableRows(cachedRows, tableProps, tile, _tileEnumContext);

			_msg.Debug($"Loaded {cachedRows.Count} rows in {tileCache.CurrentTileBox} " +
			           $"for {tableProps}");
		}

		public IEnumerable<IReadOnlyRow> Search(CachedTableProps tableProps,
		                                        IFeatureClassFilter queryFilter,
		                                        QueryFilterHelper filterHelper)
		{
			IReadOnlyTable table = tableProps.Table;
			HashSet<long> handledOids = new HashSet<long>();
			IGeometry filterGeom = queryFilter.FilterGeometry;

			// TODO!
			if (tableProps.HasGeotransformation != null) { }

			foreach (var tile in GetTiles(filterGeom, table))
			{
				TileCache tileCache = tile.Item1;
				EnsureLoaded(tile.Item1, tile.Item2, tableProps);

				IEnumerable<IReadOnlyRow> rows = tileCache.Search(table, queryFilter, filterHelper);
				if (rows == null)
				{
					continue;
				}

				foreach (var row in rows)
				{
					if (handledOids.Add(row.OID))
					{
						yield return row;
					}
				}
			}
		}

		private IEnumerable<Tuple<TileCache, Tile>> GetTiles(
			[NotNull] IGeometry geometry,
			[NotNull] IReadOnlyTable table)
		{
			foreach (Tile tile in _tileEnumContext.TileEnum.EnumTiles(geometry))
			{
				if (! _caches.TryGetValue(tile.Box, out TileCache cache))
				{
					if (_tileCache.IsLoaded(table, tile))
					{
						cache = _tileCache;
					}
					else
					{
						cache = _tileCache.Clone();
						cache.SetCurrentTileBox(tile.Box);
						_caches.Add(tile.Box, cache);
					}
				}

				yield return new Tuple<TileCache, Tile>(cache, tile);
			}
		}

		internal TileCache PrepareNextTile(Tile tile)
		{
			TileCache nextCache = null;
			Box tileBox = tile.Box;
			List<Box> toRemove = new List<Box>();
			foreach (var pair in _caches)
			{
				// TODO: Keep 1 tile to the left and probably the one directly below
				Box cachedBox = pair.Key;
				if (cachedBox.Min.Y < tileBox.Min.Y
				    || (cachedBox.Min.Y <= tileBox.Min.Y && cachedBox.Min.X < tileBox.Min.X))
				{
					toRemove.Add(cachedBox);
				}

				if (_boxComparer.Equals(tileBox, cachedBox))
				{
					nextCache = pair.Value;
					toRemove.Add(tileBox);
				}
			}

			foreach (Box box in toRemove)
			{
				_caches.Remove(box);
			}

			return nextCache;
		}
	}
}
