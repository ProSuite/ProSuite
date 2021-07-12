using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.Workflow
{
	public interface IDatasetNameTransformer
	{
		[NotNull]
		string TransformName([NotNull] string datasetName);
	}
}
