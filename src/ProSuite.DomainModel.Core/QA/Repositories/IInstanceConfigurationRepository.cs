using System;
using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.QA.Repositories
{
	public interface IInstanceConfigurationRepository : IRepository<InstanceConfiguration>
	{
		IList<TransformerConfiguration> GetTransformerConfigurations(
			[CanBeNull] IList<int> excludedIds = null);

		IList<RowFilterConfiguration> GetRowFilterConfigurations();

		IList<IssueFilterConfiguration> GetIssueFilterConfigurations();

		IList<T> Get<T>(InstanceDescriptor descriptor) where T : InstanceConfiguration;

		HashSet<int> GetIdsInvolvingDeletedDatasets<T>() where T : InstanceConfiguration;

		IList<T> Get<T>(
			[CanBeNull] DataQualityCategory category,
			bool includeQualityConditionsBasedOnDeletedDatasets = true)
			where T : InstanceConfiguration;

		InstanceConfiguration Get(string name, Type type);

		IDictionary<T, IList<DatasetTestParameterValue>> GetWithDatasetParameterValues<T>(
			[CanBeNull] DataQualityCategory category) where T : InstanceConfiguration;

		IList<ReferenceCount> GetReferenceCounts<T>() where T : InstanceConfiguration;

		IList<InstanceConfiguration> GetReferencingConfigurations(
			[NotNull] TransformerConfiguration transformer);

		IList<InstanceConfiguration> GetReferencingConfigurations(
			RowFilterConfiguration rowFilter);
	}
}
