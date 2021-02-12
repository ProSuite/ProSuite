using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.Commons.AO.Geodatabase
{
	public interface IWorkspaceDbConnectionProvider
	{
		esriConnectionDBMS Dbms { get; }

		string SdeRepositoryOwner { get; }
	}
}
