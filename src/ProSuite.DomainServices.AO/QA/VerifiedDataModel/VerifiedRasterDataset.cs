using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainServices.AO.QA.VerifiedDataModel
{
	public class VerifiedRasterDataset : RasterDataset
	{
		public VerifiedRasterDataset([NotNull] string name) : base(name) { }
	}
}
