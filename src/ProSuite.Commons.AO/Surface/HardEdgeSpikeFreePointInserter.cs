using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.Commons.AO.Surface
{
	/// <summary>
	/// Marks a frozen triangle with a tag and turns its three edges into breaklines. The
	/// breaklines keep the triangle part of the final surface, which is intended, but they cost
	/// a node wherever two of them intersect.
	/// </summary>
	public sealed class HardEdgeSpikeFreePointInserter : FreezingSpikeFreePointInserter
	{
		private const int FrozenTag = 42;

		public HardEdgeSpikeFreePointInserter(double freezeDistance, double insertionBuffer)
			: base(freezeDistance, insertionBuffer) { }

		protected override SpikeFreeFreezeCarrier FreezeCarrier =>
			SpikeFreeFreezeCarrier.HardEdges;

		protected override bool RejectsPointAt(ITinTriangle triangle, double x, double y)
		{
			return IsAlreadyFrozen(triangle);
		}

		protected override bool IsAlreadyFrozen(ITinTriangle triangle)
		{
			return ! triangle.IsEmpty && triangle.TagValue == FrozenTag;
		}

		protected override void Freeze(ITinEdit tin, ITinTriangle triangle)
		{
			tin.SetTriangleTagValue(triangle.Index, FrozenTag);

			for (int i = 0; i < 3; i++)
			{
				tin.SetEdgeType(triangle.Edge[i].Index, esriTinEdgeType.esriTinHardEdge);
			}
		}
	}
}
