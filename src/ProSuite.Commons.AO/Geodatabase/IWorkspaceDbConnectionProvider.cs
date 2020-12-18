using System;
using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.Commons.AO.Geodatabase
{
	[CLSCompliant(false)]
	public interface IWorkspaceDbConnectionProvider
	{
		esriConnectionDBMS Dbms { get; }

		string SdeRepositoryOwner { get; }
	}
}
