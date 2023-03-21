using NUnit.Framework;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Core.Test.DataModel
{
	public abstract class ModelTestBase
	{
		[Test]
		public void CanAddVectorDataset()
		{
			DdxModel model = CreateModel();
			model.AddDataset(CreateDataset("dataset"));
			Assert.AreEqual(1, model.Datasets.Count);
		}

		protected abstract Dataset CreateDataset(string name);

		protected abstract DdxModel CreateModel();
	}
}
