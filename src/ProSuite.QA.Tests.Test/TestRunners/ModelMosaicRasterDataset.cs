
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.QA.Tests.Test.TestRunners
{
	internal class ModelMosaicRasterDataset : SimpleRasterDataset, IDdxRasterDataset
	{
		public ModelMosaicRasterDataset(string name)
				:base(name)
		{
			GeometryType = new GeometryTypeRasterMosaic();
		}
	}
}
