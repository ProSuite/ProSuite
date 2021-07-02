
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.QA.Tests.Test.TestRunners
{
	internal class ModelMosaicRasterDataset : SimpleRasterMosaicDataset, IDdxRasterDataset
	{
		public ModelMosaicRasterDataset(string name)
				:base(name)
		{
			GeometryType = new GeometryTypeRasterMosaic();
		}
	}
}
