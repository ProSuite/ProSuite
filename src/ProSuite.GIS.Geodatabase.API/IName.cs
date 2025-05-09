namespace ProSuite.GIS.Geodatabase.API
{
	public interface IName
	{
		string NameString { set; get; }

		object Open();
	}

	public interface IDatasetName : IName
	{
		string Name { get; }

		esriDatasetType Type { get; }

		IWorkspaceName WorkspaceName { get; }
	}
}
