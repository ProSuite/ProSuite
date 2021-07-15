using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.QA.Tests.Test.TestRunners
{
	internal class ModelMosaicDefinitionDataset : SimpleRasterDataset, IDdxRasterDataset
	{
		public ModelMosaicDefinitionDataset(string name)
			: base(name)
		{
			GeometryType = new GeometryTypeRasterMosaic();
		}
	}
}
