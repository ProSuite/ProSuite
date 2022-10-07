using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	public interface ISpatialDataset : IDdxDataset
	{
		[CanBeNull]
		LayerFile DefaultLayerFile { get; set; }
	}
}
