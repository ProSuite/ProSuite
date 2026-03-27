namespace ProSuite.QA.Tests.ParameterTypes
{
	public enum PolylineUsage
	{
		AsIs,
		AsPolygonIfClosedElseAsPolyline,
		AsPolygonIfClosedElseIgnore,
		AsPolygonIfClosedElseReportIssue
	}
}
