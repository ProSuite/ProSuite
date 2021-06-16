using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Core.QA.Repositories
{
	public interface IQualitySpecificationRepository : IRepository<QualitySpecification>
	{
		[NotNull]
		IList<QualitySpecification> Get([NotNull] IList<Dataset> datasets,
		                                bool excludeHidden = false);

		IList<QualitySpecification> Get([NotNull] IList<int> datasetIds,
		                                bool excludeHidden);

		[CanBeNull]
		QualitySpecification Get([NotNull] string name);

		[NotNull]
		IList<QualitySpecification> Get([NotNull] QualityCondition qualityCondition);

		[NotNull]
		IList<QualitySpecification> Get([CanBeNull] DataQualityCategory category,
		                                bool includeSubCategories = false);

		[NotNull]
		IList<QualitySpecification> Get(
			[NotNull] IEnumerable<DataQualityCategory> categories);
	}
}
