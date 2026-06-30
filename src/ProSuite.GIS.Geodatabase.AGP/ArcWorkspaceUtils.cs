using System;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.GIS.Geodatabase.API;

namespace ProSuite.GIS.Geodatabase.AGP
{
	internal static class ArcWorkspaceUtils
	{
		public static esriWorkspaceType FromGeodatabaseWorkspaceType(GeodatabaseType geodatabaseType)
		{
			switch (geodatabaseType)
			{
				case GeodatabaseType.Service:
					return esriWorkspaceType.esriRemoteDatabaseWorkspace;
				case GeodatabaseType.Memory:
					// Treat in-memory as a local file workspace for compatibility
					return esriWorkspaceType.esriFileSystemWorkspace;
				case GeodatabaseType.FileSystem:
					return esriWorkspaceType.esriFileSystemWorkspace;
				case GeodatabaseType.LocalDatabase:
					return esriWorkspaceType.esriLocalDatabaseWorkspace;
				case GeodatabaseType.RemoteDatabase:
					return esriWorkspaceType.esriRemoteDatabaseWorkspace;
				default:
					// Preserve original behaviour if enums line up
					return (esriWorkspaceType) geodatabaseType;
			}
		}
	}
}
