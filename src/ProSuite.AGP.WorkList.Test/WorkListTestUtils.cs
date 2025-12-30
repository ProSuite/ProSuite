using System;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList.Test;

public static class WorkListTestUtils
{
	public static GdbWorkspaceIdentity CreateWorkspaceProxy()
	{
		var connector =
			new FileGeodatabaseConnectionPath(new Uri(@"C:\temp\foo.gdb", UriKind.Absolute));
		return new GdbWorkspaceIdentity(connector, "magic connection string");
	}

	public static GdbTableIdentity CreateTableProxy()
	{
		return new GdbTableIdentity("myTable", 42, CreateWorkspaceProxy());
	}

	public static GdbRowIdentity CreateRowProxy(long oid)
	{
		GdbTableIdentity tableProxy = CreateTableProxy();
		return new GdbRowIdentity(oid, tableProxy);
	}

	public static GdbRowIdentity CreateRowProxy()
	{
		GdbTableIdentity tableProxy = CreateTableProxy();
		return new GdbRowIdentity(99, tableProxy);
	}

	public static Geometry GetAOI()
	{
		SpatialReference sref = SpatialReferenceBuilder.CreateSpatialReference(2056, 5729);
		return GeometryFactory.CreatePolygon(GeometryFactory.CreateEmptyEnvelope(), sref);
	}
}
