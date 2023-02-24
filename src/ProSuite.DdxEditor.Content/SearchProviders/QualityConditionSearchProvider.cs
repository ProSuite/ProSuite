using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Content.QA.InstanceConfig;
using ProSuite.DdxEditor.Framework.Search;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DdxEditor.Content.SearchProviders
{
	public class QualityConditionSearchProvider :
		SearchProviderBase<InstanceConfiguration, InstanceConfigurationTableRow>
	{
		[NotNull] private readonly CoreDomainModelItemModelBuilder _modelBuilder;

		public QualityConditionSearchProvider(
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder)
			: base(modelBuilder, "Find &Quality Condition...", Resources.QualityConditionsOverlay)
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_modelBuilder = modelBuilder;
		}

		protected override IEnumerable<InstanceConfigurationTableRow> GetRowsCore()
		{
			IQualityConditionRepository repository = _modelBuilder.QualityConditions;

			IList<QualityCondition> qualityConditions =
				_modelBuilder.IncludeQualityConditionsBasedOnDeletedDatasets
					? repository.GetAll()
					: repository.GetAllNotInvolvingDeletedDatasets();

			IDictionary<int, int> qSpecCountMap =
				repository.GetReferencingQualitySpecificationCount();

			foreach (QualityCondition qc in qualityConditions.OrderBy(q => q.Name))
			{
				if (! qSpecCountMap.TryGetValue(qc.Id, out int refCount))
				{
					refCount = 0;
				}

				yield return new InstanceConfigurationTableRow(qc, refCount);
			}
		}
	}
}
