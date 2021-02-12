namespace ProSuite.QA.Container.PolygonGrower
{
	public interface IDirectedRow
	{
		ITopologicalLine TopoLine { get; }
		ITableIndexRow Row { get; }
		bool IsBackward { get; }
	}

	public interface IDirectedRow<T> : IDirectedRow where T : ITopologicalLine
	{
		T TopologicalLine { get; }
	}
}
