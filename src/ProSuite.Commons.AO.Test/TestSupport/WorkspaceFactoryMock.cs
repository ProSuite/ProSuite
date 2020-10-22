using System;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.Commons.AO.Test.TestSupport
{
	public class WorkspaceFactoryMock : IWorkspaceFactory
	{
		#region IWorkspaceFactory Members

		public bool ContainsWorkspace(string parentDirectory, IFileNames fileNames)
		{
			throw new NotImplementedException();
		}

		public bool Copy(IWorkspaceName WorkspaceName, string destinationFolder,
		                 out IWorkspaceName workspaceNameCopy)
		{
			throw new NotImplementedException();
		}

		public IWorkspaceName Create(string parentDirectory, string Name,
		                             IPropertySet ConnectionProperties, int hWnd)
		{
			throw new NotImplementedException();
		}

		public UID GetClassID()
		{
			throw new NotImplementedException();
		}

		public IWorkspaceName GetWorkspaceName(string parentDirectory,
		                                       IFileNames fileNames)
		{
			throw new NotImplementedException();
		}

		public bool IsWorkspace(string fileName)
		{
			throw new NotImplementedException();
		}

		public bool Move(IWorkspaceName WorkspaceName, string destinationFolder)
		{
			throw new NotImplementedException();
		}

		public IWorkspace Open(IPropertySet ConnectionProperties, int hWnd)
		{
			throw new NotImplementedException();
		}

		public IWorkspace OpenFromFile(string fileName, int hWnd)
		{
			throw new NotImplementedException();
		}

		public IPropertySet ReadConnectionPropertiesFromFile(string fileName)
		{
			throw new NotImplementedException();
		}

		public esriWorkspaceType WorkspaceType
		{
			get { throw new NotImplementedException(); }
		}

		public string get_WorkspaceDescription(bool plural)
		{
			return "MockWorkspace";
		}

		#endregion
	}
}
