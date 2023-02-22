using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.QA.InstanceDescriptors;
using ProSuite.DdxEditor.Framework.Search;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DdxEditor.Content.SearchProviders
{
	public class TransformerDescriptorSearchProvider
		: SearchProviderBase<InstanceDescriptor, InstanceDescriptorTableRow>
	{
		[NotNull] private readonly IInstanceDescriptorRepository _repository;

		public TransformerDescriptorSearchProvider(
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder)
			: base(modelBuilder, "Find &Transformer Descriptor...")
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_repository = modelBuilder.InstanceDescriptors;
		}

		protected override IEnumerable<InstanceDescriptorTableRow> GetRowsCore()
		{
			return InstanceDescriptorItemUtils.GetTransformerDescriptorTableRows(_repository);
		}
	}
}
