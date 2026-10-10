using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Surface
{
	/// <summary>
	/// Base class for the carriers that record frozen triangles: the point is discarded when it
	/// falls into the frozen area, and the triangle above it plus that triangle's three
	/// neighbours are frozen when they satisfy the freeze condition.
	/// </summary>
	public abstract class FreezingSpikeFreePointInserter : SpikeFreePointInserter
	{
		private readonly TinTriangleClass _adjacentTriangle1 = new TinTriangleClass();
		private readonly TinTriangleClass _adjacentTriangle2 = new TinTriangleClass();
		private readonly TinTriangleClass _adjacentTriangle3 = new TinTriangleClass();

		protected FreezingSpikeFreePointInserter(double freezeDistance, double insertionBuffer)
			: base(freezeDistance, insertionBuffer) { }

		protected override bool IsSpike(ITinEdit tin, ITinAdvanced advancedTin, IPoint point)
		{
			ITinTriangle triangle = advancedTin.FindTriangle(point);

			if (RejectsPointAt(triangle, point.X, point.Y))
			{
				return true;
			}

			if (SatisfiesFreezeCondition(triangle, point))
			{
				Freeze(tin, triangle);
				return true;
			}

			triangle.QueryAdjacentTriangles(_adjacentTriangle1, _adjacentTriangle2,
			                                _adjacentTriangle3);

			FreezeIfQualified(tin, _adjacentTriangle1, point);
			FreezeIfQualified(tin, _adjacentTriangle2, point);
			FreezeIfQualified(tin, _adjacentTriangle3, point);

			return false;
		}

		/// <summary>Does the frozen area cover this location?</summary>
		protected abstract bool RejectsPointAt([NotNull] ITinTriangle triangle,
		                                       double x, double y);

		protected abstract void Freeze([NotNull] ITinEdit tin, [NotNull] ITinTriangle triangle);

		/// <summary>
		/// Has this triangle already been frozen?
		/// </summary>
		protected virtual bool IsAlreadyFrozen([NotNull] ITinTriangle triangle)
		{
			return false;
		}

		private void FreezeIfQualified(ITinEdit tin, ITinTriangle neighbour, IPoint point)
		{
			if (! IsAlreadyFrozen(neighbour) && SatisfiesFreezeCondition(neighbour, point))
			{
				Freeze(tin, neighbour);
			}
		}
	}
}
