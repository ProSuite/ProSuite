using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.Microservices.Definitions.Shared.Ddx;

namespace ProSuite.Microservices.Client.DataModel
{
	public interface IModelFactory
	{
		DdxModel CreateModel([NotNull] ModelMsg modelMsg);

		Dataset CreateDataset([NotNull] DatasetMsg datasetMsg);
	}
}
