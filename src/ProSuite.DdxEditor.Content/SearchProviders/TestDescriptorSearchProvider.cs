using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.QA.TestDescriptors;
using ProSuite.DdxEditor.Framework.Search;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DdxEditor.Content.SearchProviders
{
	public class TestDescriptorSearchProvider :
		SearchProviderBase<TestDescriptor, TestDescriptorTableRow>
	{
		[NotNull] private readonly ITestDescriptorRepository _repository;

		public TestDescriptorSearchProvider(
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder)
			: base(modelBuilder, "Find &Test Descriptor...")
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_repository = modelBuilder.TestDescriptors;
		}

		protected override IEnumerable<TestDescriptorTableRow> GetRowsCore()
		{
			IDictionary<int, int> refCountMap =
				_repository.GetReferencingQualityConditionCount();

			return _repository.GetAll()
							  .OrderBy(t => t.Name)
							  .Select(t =>
							  {
								  int refCount;
								  if (!refCountMap.TryGetValue(t.Id, out refCount))
								  {
									  refCount = 0;
								  }

								  return new TestDescriptorTableRow(t, refCount);
							  });
		}
	}
}
