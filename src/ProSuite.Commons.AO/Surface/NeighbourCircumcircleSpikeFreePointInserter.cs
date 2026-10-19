using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;

namespace ProSuite.Commons.AO.Surface
{
	/// <summary>
	/// Keeps no freeze state at all: for every point the freeze condition is evaluated again,
	/// against the containing triangle and against the neighbours whose circumcircle covers the
	/// point.
	/// </summary>
	/// <remarks>
	/// The neighbours have to count, because the containing triangle can be spoiled by an
	/// already inserted low node while pure canopy triangles still surround it. The circumcircle
	/// is what makes "near enough" precise: it is exactly the area in which a new point would
	/// flip that triangle. Without the filter every neighbour counts and far too much freezes.
	/// </remarks>
	public sealed class NeighbourCircumcircleSpikeFreePointInserter : SpikeFreePointInserter
	{
		private readonly TinTriangleClass _neighbour1 = new TinTriangleClass();
		private readonly TinTriangleClass _neighbour2 = new TinTriangleClass();
		private readonly TinTriangleClass _neighbour3 = new TinTriangleClass();

		public NeighbourCircumcircleSpikeFreePointInserter(
			double freezeDistance, double insertionBuffer)
			: base(freezeDistance, insertionBuffer) { }

		protected override SpikeFreeFreezeCarrier FreezeCarrier =>
			SpikeFreeFreezeCarrier.NeighbourCircumcircle;

		protected override bool IsSpike(ITinEdit tin, ITinAdvanced advancedTin, IPoint point)
		{
			ITinTriangle triangle = advancedTin.FindTriangle(point);

			if (SatisfiesFreezeCondition(triangle, point))
			{
				return true;
			}

			if (triangle.IsEmpty)
			{
				return false;
			}

			triangle.QueryAdjacentTriangles(_neighbour1, _neighbour2, _neighbour3);

			return Applies(_neighbour1, point) ||
			       Applies(_neighbour2, point) ||
			       Applies(_neighbour3, point);
		}

		private bool Applies(ITinTriangle neighbour, IPoint point)
		{
			if (neighbour.IsEmpty)
			{
				return false;
			}

			neighbour.QueryVertices(out WKSPointZ v0, out WKSPointZ v1, out WKSPointZ v2);

			return IsInCircumcircle(v0, v1, v2, point.X, point.Y) &&
			       SatisfiesFreezeCondition(neighbour, point);
		}
	}
}
