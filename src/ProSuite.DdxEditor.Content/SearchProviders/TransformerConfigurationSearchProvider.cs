using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Content.QA.InstanceConfig;
using ProSuite.DdxEditor.Framework.Search;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.SearchProviders
{
	public class TransformerConfigurationSearchProvider :
		SearchProviderBase<InstanceConfiguration, InstanceConfigurationTableRow>
	{
		[NotNull] private readonly CoreDomainModelItemModelBuilder _modelBuilder;

		public TransformerConfigurationSearchProvider(
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder)
			: base(modelBuilder, "Find &Transformer Configuration...",
			       Resources.TransformerConfigurationsOverlay)
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_modelBuilder = modelBuilder;
		}

		protected override IEnumerable<InstanceConfigurationTableRow> GetRowsCore()
		{
			return InstanceConfigTableRows
				.GetInstanceConfigurationTableRows<TransformerConfiguration>(
					_modelBuilder, null);
		}
	}
}
