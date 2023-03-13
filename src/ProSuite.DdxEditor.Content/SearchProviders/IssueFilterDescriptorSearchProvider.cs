using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Content.QA.InstanceDescriptors;
using ProSuite.DdxEditor.Framework.Search;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DdxEditor.Content.SearchProviders
{
	public class IssueFilterDescriptorSearchProvider
		: SearchProviderBase<InstanceDescriptor, InstanceDescriptorTableRow>
	{
		[NotNull] private readonly IInstanceDescriptorRepository _repository;

		public IssueFilterDescriptorSearchProvider(
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder)
			: base(modelBuilder, "Find &Issue Filter Descriptor...",
			       Resources.IssueFilterDescriptorsOverlay)
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
