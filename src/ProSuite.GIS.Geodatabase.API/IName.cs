namespace ProSuite.GIS.Geodatabase.API
{
	public interface IName
	{
		string NameString { set; get; }

		object Open();
	}

	/// <summary>
	/// The name object representing a dataset in a geodatabase.
	/// </summary>
	public interface IDatasetName : IName
	{
		string Name { get; }

		esriDatasetType Type { get; }

		IWorkspaceName WorkspaceName { get; }
	}

	/// <summary>
	/// The name object that represents a layer join in a map.
	/// </summary>
	public interface IMemoryRelQueryTableName : IName
	{
		bool ForwardDirection { get; }

		esriRelCardinality Cardinality { get; }

		//esriJointType JoinType { get; }

		IDatasetName SourceTable { get; }

		IDatasetName DestinationTable { get; }

		string PrimaryKey { get; }

		string ForeignKey { get; }
	}
}
