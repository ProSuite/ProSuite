using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom.SpatialIndex
{
	/// <summary>
	/// Queries over a <see cref="SpatialHashIndex{T}"/> that are asked by a polycurve rather than
	/// by a box.
	/// </summary>
	public static class SpatialHashIndexUtils
	{
		/// <summary>
		/// The identifiers held by the tiles the polycurve reaches, each with the relation of the
		/// tile it was found in. Identifiers whose tiles the polycurve misses entirely are not
		/// returned at all.
		/// </summary>
		/// <remarks>
		/// For a caller that would otherwise test every item against the polycurve one by one:
		/// an item from a <see cref="EnvelopeRelation.Inside"/> tile needs no test, and one from a
		/// <see cref="EnvelopeRelation.Straddling"/> tile still does. On the usual grid most of a
		/// polycurve's tiles are inside it, and the proportion grows with the polycurve.
		/// <para>
		/// The relation is the tile's, not the item's. An item added to a single tile - anything
		/// added by location - is inside the polycurve when its tile is. An item deliberately
		/// added to several tiles is reported once, and is only reported
		/// <see cref="EnvelopeRelation.Inside"/> when every tile it was found in is inside; that
		/// still says nothing about the parts of it lying in tiles this search never looked at.
		/// </para>
		/// </remarks>
		/// <param name="index">The index to search.</param>
		/// <param name="closedPolycurve">The closed polycurve to search with.</param>
		/// <param name="tolerance">The tolerance the caller's own containment test uses. Tiles
		/// are related to the polycurve at the same tolerance, so a tile reported as missed is
		/// one whose points that test would have rejected.</param>
		[NotNull]
		public static IEnumerable<(T Identifier, EnvelopeRelation Relation)> FindIdentifiers<T>(
			[NotNull] SpatialHashIndex<T> index,
			[NotNull] ISegmentList closedPolycurve,
			double tolerance)
		{
			Assert.ArgumentNotNull(index, nameof(index));
			Assert.ArgumentNotNull(closedPolycurve, nameof(closedPolycurve));

			// The identifiers must be made distinct, as they must be for a box search: one item
			// can be held by several tiles.
			var found = new Dictionary<T, EnvelopeRelation>();

			foreach ((TileIndex tile, List<T> items) in OccupiedTiles(index, closedPolycurve,
			                                                         tolerance))
			{
				EnvelopeRelation relation = GeomRelationUtils.GetTileRelation(
					index.TilingDefinition, tile, closedPolycurve, tolerance);

				if (relation == EnvelopeRelation.Disjoint)
				{
					continue;
				}

				foreach (T item in items)
				{
					if (found.TryGetValue(item, out EnvelopeRelation seen) &&
					    seen == EnvelopeRelation.Straddling)
					{
						continue;
					}

					found[item] = relation;
				}
			}

			return found.Select(entry => (entry.Key, entry.Value));
		}

		/// <summary>
		/// The occupied tiles that the polycurve's bounds reach, from whichever end is cheaper to
		/// walk: the tiles the bounds cover, or the tiles the index actually holds.
		/// </summary>
		private static IEnumerable<(TileIndex, List<T>)> OccupiedTiles<T>(
			[NotNull] SpatialHashIndex<T> index,
			[NotNull] IBoundedXY bounds,
			double tolerance)
		{
			double xMin = bounds.XMin - tolerance;
			double yMin = bounds.YMin - tolerance;
			double xMax = bounds.XMax + tolerance;
			double yMax = bounds.YMax + tolerance;

			TilingDefinition tiling = index.TilingDefinition;

			if (index.TileCount > 0 &&
			    tiling.GetIntersectingTileCount(xMin, yMin, xMax, yMax) > index.TileCount)
			{
				TileIndex min = tiling.GetTileIndexAt(xMin, yMin);
				TileIndex max = tiling.GetTileIndexAt(xMax, yMax);

				foreach ((int east, int north, List<T> items) in index.GetTileData())
				{
					if (east >= min.East && east <= max.East &&
					    north >= min.North && north <= max.North)
					{
						yield return (new TileIndex(east, north), items);
					}
				}

				yield break;
			}

			foreach (TileIndex tile in tiling.GetIntersectingTiles(xMin, yMin, xMax, yMax))
			{
				if (index.FindIdentifiers(tile, out List<T> items))
				{
					yield return (tile, items);
				}
			}
		}
	}
}
