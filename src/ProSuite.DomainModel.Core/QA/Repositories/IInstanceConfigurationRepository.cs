using System;
using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Core.QA.Repositories
{
	public interface IInstanceConfigurationRepository : IRepository<InstanceConfiguration>
	{
		IList<TransformerConfiguration> GetTransformerConfigurations(
			[CanBeNull] IList<int> excludedIds = null);

		IList<IssueFilterConfiguration> GetIssueFilterConfigurations();

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

		IList<InstanceConfiguration> Get(InstanceDescriptor descriptor);

		/// <summary>
		/// Returns the datasets that are directly or indirectly referenced by one of the specified
		/// conditions.
		/// </summary>
		/// <param name="qualityConditions">The referencing conditions</param>
		/// <param name="includeReferenceViaIssueFilters">Whether or not datasets referenced by an
		/// issue filter of a specified conditon should be included.</param>
		/// <param name="testParameterPredicate">Extra predicate to be evaluated on the test
		/// parameter values.</param>
		/// <returns></returns>
		IEnumerable<Dataset> GetAllReferencedDatasets(
			[NotNull] IEnumerable<QualityCondition> qualityConditions,
			bool includeReferenceViaIssueFilters = false,
			[CanBeNull] Predicate<DatasetTestParameterValue> testParameterPredicate = null);
	}
}
