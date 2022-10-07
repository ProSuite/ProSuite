using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	public class GeometryTypeRasterDataset : GeometryType
	{
		public GeometryTypeRasterDataset() { }

		public GeometryTypeRasterDataset([NotNull] string name) : base(name) { }
	}
}
