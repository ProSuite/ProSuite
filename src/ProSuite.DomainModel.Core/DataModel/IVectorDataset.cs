namespace ProSuite.DomainModel.Core.DataModel
{
	public interface IVectorDataset : IObjectDataset, ISpatialDataset
	{
		double MinimumSegmentLength { get; set; }
	}
}