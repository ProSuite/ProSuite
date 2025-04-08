using System;
using ArcGIS.Core.Data;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList.Test;

public static class WorkListTestUtils
{
	public static GdbWorkspaceIdentity CreateWorkspaceProxy()
	{
		var connector = new FileGeodatabaseConnectionPath(new Uri(@"C:\temp\foo.gdb", UriKind.Absolute));
		return new GdbWorkspaceIdentity(connector, "magic connection string");
	}

	public static GdbTableIdentity CreateTableProxy()
	{
		return new GdbTableIdentity("myTable", 42, CreateWorkspaceProxy());
	}

	//public static GdbTableIdentity CreateTableProxy(GdbWorkspaceIdentity workspaceProxy)
	//{
	//	return new GdbTableIdentity("myTable", 42, workspaceProxy);
	//}

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
}
