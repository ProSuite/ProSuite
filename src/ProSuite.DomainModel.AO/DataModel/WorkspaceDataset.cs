using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	public class WorkspaceDataset
	{
		public WorkspaceDataset([NotNull] string name,
		                        [CanBeNull] string featureDatasetName,
		                        [NotNull] Dataset dataset)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			Name = name;
			FeatureDatasetName = featureDatasetName;
			Dataset = dataset;
		}

		[NotNull]
		public string Name { get; }

		[CanBeNull]
		public string FeatureDatasetName { get; }

		[NotNull]
		public Dataset Dataset { get; }
	}
}
