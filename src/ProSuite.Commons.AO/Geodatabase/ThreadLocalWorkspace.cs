using System.Collections.Generic;
using System.Threading;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.Commons.AO.Geodatabase
{
	/// <summary>
	/// Provides a workspace to be used in various threads.
	/// </summary>
	public class ThreadLocalWorkspace
	{
		private readonly string[] _connectionProps;
		private readonly object[] _connectionValues;

		private readonly string _workspaceFactoryProgId;

		private ThreadLocal<IWorkspace> _workspace;

		/// <summary>
		/// Should be called on the main thread.
		/// </summary>
		/// <param name="workspace">The workspace to be used later on from other threads</param>
		public ThreadLocalWorkspace(IWorkspace workspace)
		{
			_workspaceFactoryProgId = WorkspaceUtils.GetFactoryProgId(workspace);

			// Copied from VersionDeleter:
			IPropertySet propSet = workspace.ConnectionProperties;
			int propertyCount = propSet.Count;
			object oNames;
			object oValues;
			propSet.GetAllProperties(out oNames, out oValues);
			var names = (IList<object>) oNames;
			var values = (IList<object>) oValues;

			_connectionProps = new string[propertyCount];
			_connectionValues = new object[propertyCount];

			for (var propertyIndex = 0; propertyIndex < propertyCount; propertyIndex++)
			{
				_connectionProps[propertyIndex] = (string) names[propertyIndex];
				_connectionValues[propertyIndex] = values[propertyIndex];
			}
		}

		public IWorkspace Workspace
		{
			get
			{
				if (_workspace == null)
				{
					_workspace = new ThreadLocal<IWorkspace>(OpenWorkspace);
				}

				return _workspace.Value;
			}
		}

		private IWorkspace OpenWorkspace()
		{
			IPropertySet connectionProperties = new PropertySetClass();

			int propertyCount = _connectionProps.Length;

			for (var propertyIndex = 0; propertyIndex < propertyCount; propertyIndex++)
			{
				connectionProperties.SetProperty(
					_connectionProps[propertyIndex],
					_connectionValues[propertyIndex]);
			}

			IWorkspaceFactory wsFactory =
				WorkspaceUtils.GetWorkspaceFactory(_workspaceFactoryProgId);

			IWorkspace workspace = wsFactory.Open(connectionProperties, 0);

			return workspace;
		}
	}
}
