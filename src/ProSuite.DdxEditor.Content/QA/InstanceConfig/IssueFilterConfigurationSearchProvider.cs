using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Search;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.InstanceConfig
{
	public class IssueFilterConfigurationSearchProvider :
		SearchProviderBase<InstanceConfiguration, InstanceConfigurationTableRow>
	{
		[NotNull] private readonly CoreDomainModelItemModelBuilder _modelBuilder;

		public IssueFilterConfigurationSearchProvider(
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder)
			: base(modelBuilder, "Find &Issue Filter Configuration...")
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_modelBuilder = modelBuilder;
		}

		protected override IEnumerable<InstanceConfigurationTableRow> GetRowsCore()
		{
			return InstanceConfigTableRows.GetInstanceConfigurationTableRows<IssueFilterConfiguration>(
				_modelBuilder);
		}
	}
}
