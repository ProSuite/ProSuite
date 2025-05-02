namespace ProSuite.DomainModel.Core.DataModel
{
	/// <summary>
	/// Special model access only used during harvesting
	/// </summary>
	public interface IModelHarvest
	{
		Dataset GetExistingDataset(string datasetName, bool useIndex);
		Association GetExistingAssociation(string associationName, bool useIndex);
	}
}
