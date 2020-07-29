namespace ProSuite.DomainModel.DataModel
{
	public class Dataset : IDataset
	{
		public Dataset(string name)
		{
			Name = name;
		}

		public string Name { get; }
	}
}
