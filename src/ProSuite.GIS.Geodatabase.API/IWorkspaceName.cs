using System.Collections.Generic;

namespace ProSuite.GIS.Geodatabase.API
{
	public interface IWorkspaceName : IName
	{
		string PathName { get; }

		string WorkspaceFactoryProgID { get; }

		string BrowseName { get; }

		//IWorkspaceFactory WorkspaceFactory { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][return: MarshalAs(UnmanagedType.Interface)] get; }

		IEnumerable<KeyValuePair<string, string>> ConnectionProperties { get; }

		esriWorkspaceType Type { get; }

		string Category { get; }

		string ConnectionString { get; }
	}
}
