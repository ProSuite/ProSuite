using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Search;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DdxEditor.Content.QA.InstanceDescriptors
{
	public class IssueFilterDescriptorSearchProvider
		: SearchProviderBase<InstanceDescriptor, InstanceDescriptorTableRow>
	{
		[NotNull] private readonly IInstanceDescriptorRepository _repository;

		public IssueFilterDescriptorSearchProvider(
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder)
			: base(modelBuilder, "Find &Issue Filter Descriptor...")
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_repository = modelBuilder.InstanceDescriptors;
		}

		protected override IEnumerable<InstanceDescriptorTableRow> GetRowsCore()
		{
			return InstanceDescriptorItemUtils.GetIssueFilterDescriptorTableRows(_repository);
		}
	}
}
