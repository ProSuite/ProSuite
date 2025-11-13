namespace ProSuite.QA.Tests
{
	public enum PolylineUsage
	{
		AsIs,
		AsPolygonIfClosedElseAsPolyline,
		AsPolygonIfClosedElseIgnore,
		AsPolygonIfClosedElseReportIssue
	}
}
