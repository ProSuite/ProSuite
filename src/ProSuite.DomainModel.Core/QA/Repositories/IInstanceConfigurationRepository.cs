using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.QA.Repositories
{
	public interface IInstanceConfigurationRepository : IRepository<InstanceConfiguration>
	{
		IList<TransformerConfiguration> GetTransformerConfigurations();

		IList<RowFilterConfiguration> GetRowFilterConfigurations();

		IList<IssueFilterConfiguration> GetIssueFilterConfigurations();

		IList<T> Get<T>(InstanceDescriptor descriptor) where T : InstanceConfiguration;

		HashSet<int> GetIdsInvolvingDeletedDatasets<T>() where T : InstanceConfiguration;

		IList<T> Get<T>(
			DataQualityCategory category,
			bool includeQualityConditionsBasedOnDeletedDatasets = true)
			where T : InstanceConfiguration;

		IDictionary<T, IList<DatasetTestParameterValue>> GetWithDatasetParameterValues<T>(
			DataQualityCategory category) where T : InstanceConfiguration;

		IList<ReferenceCount> GetReferenceCounts<T>() where T : InstanceConfiguration;

		IList<InstanceConfiguration> GetReferencingConfigurations(
			TransformerConfiguration transformer);
	}
}
