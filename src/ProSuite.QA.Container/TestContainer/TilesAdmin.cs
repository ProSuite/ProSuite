using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Geom;
using System;
using System.Collections.Generic;
using System.Runtime;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestContainer
{
	internal class TilesAdmin
	{
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

		private void EnsureLoaded([NotNull] TileCache tileCache, [NotNull] Tile tile, [NotNull] IReadOnlyTable table)
		{
			if (tileCache.GetLoadedExtent(table) != null)
			{
				return;
			}

			IDictionary<BaseRow, CachedRow> cachedRows =
				_tileEnumContext.OverlappingFeatures.GetOverlappingCachedRows(table, tile.Box);
			tileCache.LoadCachedTableRows(cachedRows, table, tile, _tileEnumContext);
		}

		public IEnumerable<IReadOnlyRow> Search(IReadOnlyTable table, ISpatialFilter queryFilter, QueryFilterHelper filterHelper)
		{
			HashSet<int> handledOids = new HashSet<int>();
			foreach (var tile in GetTiles(queryFilter.Geometry))
			{
				TileCache tileCache = tile.Item1;
				EnsureLoaded(tile.Item1, tile.Item2, table);

				IEnumerable<IReadOnlyRow>
					rows = tileCache.Search(table, queryFilter, filterHelper, null);
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

		private IEnumerable<Tuple<TileCache, Tile>> GetTiles(IGeometry geometry)
		{
			foreach (Tile tile in _tileEnumContext.TileEnum.EnumTiles(geometry))
			{
				if (!_caches.TryGetValue(tile.Box, out TileCache cache))
				{

					if (_boxComparer.Equals(_tileCache.CurrentTileBox, tile.Box))
					{
						cache = _tileCache;
					}
					else
					{
						cache = _tileCache.Clone();
						cache.SetCurrentTileBox(tile.Box);
					}

					_caches.Add(tile.Box, cache);
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
				Box cachedBox = pair.Key;
				if (cachedBox.Min.Y < tileBox.Min.Y
				    || (cachedBox.Min.Y == tileBox.Min.Y && cachedBox.Min.X < tileBox.Min.X))
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