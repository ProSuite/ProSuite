using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Core.QA.Repositories
{
	public interface IQualityConditionRepository : IRepository<QualityCondition>
	{
		[CanBeNull]
		QualityCondition Get([NotNull] string name);

		[NotNull]
		IList<QualityCondition> GetAll(bool fetchParameterValues);

		[NotNull]
		IList<QualityCondition> Get(IList<int> idList);

		[NotNull]
		IList<QualityCondition> Get([NotNull] TestDescriptor testDescriptor);

		/// <summary>
		/// Gets the count of quality specifications that reference a quality condition,
		/// as a map qualityCondition.Id -> number of referencing quality specifications, 
		/// for all quality conditions.
		/// Unreferenced quality conditions are not contained in the map -> implied count is 0.
		/// </summary>
		/// <returns>dictionary [quality condition id] -> [number of referencing quality specifications]</returns>
		[NotNull]
		IDictionary<int, int> GetReferencingQualitySpecificationCount();

		[NotNull]
		IList<QualityCondition> Get([NotNull] DdxModel model);

		[NotNull]
		IList<string> GetNames(bool includeQualityConditionsBasedOnDeletedDatasets = false);

		/// <summary>
		/// Gets a dictionary of all quality conditions with their dataset parameter values. 
		/// </summary>
		/// <remarks>This method is faster than <see cref="GetAll(bool)"/>, but only returns the
		/// dataset parameter values.</remarks>
		/// <returns></returns>
		[NotNull]
		IDictionary<QualityCondition, IList<DatasetTestParameterValue>>
			GetWithDatasetParameterValues([CanBeNull] DataQualityCategory category = null);

		[NotNull]
		IList<QualityCondition> GetAllNotInvolvingDeletedDatasets();

		[NotNull]
		HashSet<int> GetIdsInvolvingDeletedDatasets();

		[NotNull]
		IList<QualityCondition> Get(
			[CanBeNull] DataQualityCategory category,
			bool includeQualityConditionsBasedOnDeletedDatasets = true);

		[NotNull]
		IList<QualityCondition> Get([NotNull] IEnumerable<DataQualityCategory> categories);

		IList<QualityCondition> GetReferencingConditions(
			IssueFilterConfiguration issueFilter);
	}
}
