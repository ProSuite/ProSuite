namespace ProSuite.DomainModel.DataModel
{
	public interface INamed
	{
		string Name { get; }
	}

	public interface IDataset : INamed
	{

	}

	public interface IObjectDataset : IDataset
	{

	}

	public interface IVectorDataset : IObjectDataset
	{

	}
}
