using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainModel.AO.QA
{
	public interface IRowFilterConfigurationProvider
	{
		[NotNull]
		IEnumerable<RowFilterConfiguration> GetFilterConfigurations(
			[NotNull] Dataset forDataset,
			[CanBeNull] DataQualityCategory category);
	}
}
