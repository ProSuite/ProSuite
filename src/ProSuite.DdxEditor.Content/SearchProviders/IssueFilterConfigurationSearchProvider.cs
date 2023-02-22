using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Content.QA.InstanceConfig;
using ProSuite.DdxEditor.Framework.Search;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.SearchProviders
{
	public class IssueFilterConfigurationSearchProvider :
		SearchProviderBase<InstanceConfiguration, InstanceConfigurationTableRow>
	{
		[NotNull] private readonly CoreDomainModelItemModelBuilder _modelBuilder;

		public IssueFilterConfigurationSearchProvider(
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder)
			: base(modelBuilder, "Find &Issue Filter Configuration...",
			       Resources.IssueFilterOverlay)
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_modelBuilder = modelBuilder;
		}

		protected override IEnumerable<InstanceConfigurationTableRow> GetRowsCore()
		{
			return InstanceConfigTableRows
				.GetInstanceConfigurationTableRows<IssueFilterConfiguration>(
					_modelBuilder);
		}
	}
}
