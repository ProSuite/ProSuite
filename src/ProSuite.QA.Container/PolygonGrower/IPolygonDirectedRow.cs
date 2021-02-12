namespace ProSuite.QA.Container.PolygonGrower
{
	public interface IPolygonDirectedRow : IDirectedRow<TopologicalLine>,
	                                       ILineDirectedRow
	{
		LineListPolygon RightPoly { get; set; }
		LineListPolygon LeftPoly { get; set; }
	}
}
