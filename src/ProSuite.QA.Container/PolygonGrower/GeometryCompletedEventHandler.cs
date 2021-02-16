namespace ProSuite.QA.Container.PolygonGrower
{
	public delegate void GeometryCompletedEventHandler<T>(
		RingGrower<T> sender, LineList<T> closedPolygon) where T : class, ILineDirectedRow;
}
