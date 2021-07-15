using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainServices.AO.QA.VerifiedDataModel
{
	public class VerifiedSimpleRasterMosaicDataset : SimpleRasterMosaicDataset
	{
		public VerifiedSimpleRasterMosaicDataset([NotNull] string name) : base(name) { }
	}
}
