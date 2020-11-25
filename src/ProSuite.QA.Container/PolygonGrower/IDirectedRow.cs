using System;

namespace ProSuite.QA.Container.PolygonGrower
{
	[CLSCompliant(false)]
	public interface IDirectedRow
	{
		ITopologicalLine TopoLine { get; }
		ITableIndexRow Row { get; }
		bool IsBackward { get; }
	}

	[CLSCompliant(false)]
	public interface IDirectedRow<T> : IDirectedRow where T : ITopologicalLine
	{
		T TopologicalLine { get; }
	}
}
