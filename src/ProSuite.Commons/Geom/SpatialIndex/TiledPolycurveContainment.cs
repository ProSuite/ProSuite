using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom.SpatialIndex
{
	/// <summary>
	/// Answers containment in one closed polycurve for many points, by asking the question of a
	/// whole tile at a time and remembering the answer.
	/// </summary>
	/// <remarks>
	/// Testing a point against a polycurve costs a ray cast over its segments. Most points of a
	/// query sit in tiles the boundary never comes near, and those tiles answer for every point
	/// in them at once - so classifying the tile once turns the per-point cost into a dictionary
	/// probe, and only the points in tiles the boundary crosses are tested at all.
	/// <para>
	/// One instance belongs to one polycurve and one tolerance: that is what makes remembering
	/// the answers sound, and why this is an object rather than a call. Build one per query and
	/// let it go with the query. Not thread-safe.
	/// </para>
	/// </remarks>
	public class TiledPolycurveContainment
	{
		[NotNull] private readonly TilingDefinition _tiling;
		[NotNull] private readonly ISegmentList _closedPolycurve;
		private readonly double _tolerance;

		[NotNull] private readonly Dictionary<TileIndex, EnvelopeRelation> _relations =
			new Dictionary<TileIndex, EnvelopeRelation>();

		/// <param name="tiling">The tiling whose tiles the points are grouped by. Any tiling
		/// answers correctly; one whose tiles are large enough to hold many points and small
		/// enough to be mostly clear of the boundary answers fastest.</param>
		/// <param name="closedPolycurve">The polycurve to test containment in.</param>
		/// <param name="tolerance">The tolerance the containment test is run at.</param>
		public TiledPolycurveContainment([NotNull] TilingDefinition tiling,
		                                 [NotNull] ISegmentList closedPolycurve,
		                                 double tolerance)
		{
			Assert.ArgumentNotNull(tiling, nameof(tiling));
			Assert.ArgumentNotNull(closedPolycurve, nameof(closedPolycurve));

			_tiling = tiling;
			_closedPolycurve = closedPolycurve;
			_tolerance = tolerance;
		}

		/// <summary>
		/// Whether the polycurve contains the specified location, boundary included - the same
		/// answer as testing it directly, for the same tolerance.
		/// </summary>
		public bool ContainsXY(double x, double y)
		{
			switch (RelationOf(_tiling.GetTileIndexAt(x, y)))
			{
				case EnvelopeRelation.Inside:
					return true;

				case EnvelopeRelation.Disjoint:
					return false;

				default:
					// The boundary runs through this tile, so only the point itself can say.
					return GeomRelationUtils.PolycurveContainsXY(
						_closedPolycurve, new Coordinates2D(x, y), _tolerance);
			}
		}

		/// <summary>
		/// How the specified tile relates to the polycurve, classified on first asking and
		/// remembered.
		/// </summary>
		public EnvelopeRelation RelationOf(TileIndex tile)
		{
			if (! _relations.TryGetValue(tile, out EnvelopeRelation relation))
			{
				relation = GeomRelationUtils.GetTileRelation(_tiling, tile, _closedPolycurve,
				                                             _tolerance);
				_relations.Add(tile, relation);
			}

			return relation;
		}
	}
}
