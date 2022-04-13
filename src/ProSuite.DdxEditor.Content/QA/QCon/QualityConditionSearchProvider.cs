using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Search;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DdxEditor.Content.QA.QCon
{
	public class QualityConditionSearchProvider :
		SearchProviderBase<QualityCondition, QualityConditionTableRow>
	{
		[NotNull] private readonly CoreDomainModelItemModelBuilder _modelBuilder;

		public QualityConditionSearchProvider(
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder)
			: base(modelBuilder, "Find &Quality Condition...")
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_modelBuilder = modelBuilder;
		}

		protected override IEnumerable<QualityConditionTableRow> GetRowsCore()
		{
			IQualityConditionRepository repository = _modelBuilder.QualityConditions;

			IList<QualityCondition> qualityConditions =
				_modelBuilder.IncludeQualityConditionsBasedOnDeletedDatasets
					? repository.GetAll()
					: repository.GetAllNotInvolvingDeletedDatasets();

			IDictionary<int, int> qspecCountMap =
				repository.GetReferencingQualitySpecificationCount();

			foreach (QualityCondition qc in qualityConditions.OrderBy(q => q.Name))
			{
				int refCount;
				if (! qspecCountMap.TryGetValue(qc.Id, out refCount))
				{
					refCount = 0;
				}

				yield return new QualityConditionTableRow(qc, refCount);
			}
		}
	}
}
