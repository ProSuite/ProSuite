using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainServices.AO.QA.VerifiedDataModel
{
	public class VerifiedRasterMosaicDataset : RasterMosaicDataset
	{
		public VerifiedRasterMosaicDataset([NotNull] string name) : base(name) { }
	}
}
