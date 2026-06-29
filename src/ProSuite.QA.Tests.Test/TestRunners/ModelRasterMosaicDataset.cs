using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.QA.Tests.Test.TestRunners
{
	internal class ModelRasterMosaicDataset : RasterMosaicDataset, IDdxRasterDataset
	{
		public ModelRasterMosaicDataset(string name) : base(name)
		{
			GeometryType = new GeometryTypeRasterMosaic();
		}
	}
}
