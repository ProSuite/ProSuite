using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Search;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	public class QualitySpecificationSearchProvider :
		SearchProviderBase<QualitySpecification, QualitySpecificationTableRow>
	{
		[NotNull] private readonly IQualitySpecificationRepository _repository;

		public QualitySpecificationSearchProvider(
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder)
			: base(modelBuilder, "Find Quality &Specification...")
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_repository = modelBuilder.QualitySpecifications;
		}

		protected override IEnumerable<QualitySpecificationTableRow> GetRowsCore()
		{
			var comparer = new QualitySpecificationListComparer();

			return _repository.GetAll()
			                  .OrderBy(q => q, comparer)
			                  .Select(q => new QualitySpecificationTableRow(q));
		}
	}
}
