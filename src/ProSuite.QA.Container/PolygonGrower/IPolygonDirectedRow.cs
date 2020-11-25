using System;

namespace ProSuite.QA.Container.PolygonGrower
{
	[CLSCompliant(false)]
	public interface IPolygonDirectedRow : IDirectedRow<TopologicalLine>,
	                                       ILineDirectedRow
	{
		LineListPolygon RightPoly { get; set; }
		LineListPolygon LeftPoly { get; set; }
	}
}
