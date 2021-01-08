using System;

namespace ProSuite.QA.Container.PolygonGrower
{
	[CLSCompliant(false)]
	public delegate void GeometryCompletedEventHandler<T>(
		RingGrower<T> sender, LineList<T> closedPolygon) where T : class, ILineDirectedRow;
}
