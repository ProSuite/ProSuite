using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DomainModel.AO.QA
{
	public class RowFilterConfigurationProvider : IRowFilterConfigurationProvider
	{
		[NotNull] private readonly Action<Action> _transactionFunction;
		[NotNull] private readonly IInstanceConfigurationRepository _filterConfigRepository;

		/// <summary>
		/// Initializes a new instance of the <see cref="TestParameterDatasetProviderBase"/> class.
		/// </summary>
		public RowFilterConfigurationProvider(
			[NotNull] Action<Action> transactionFunction,
			[NotNull] IInstanceConfigurationRepository filterConfigRepository)
		{
			Assert.ArgumentNotNull(transactionFunction, nameof(transactionFunction));
			Assert.ArgumentNotNull(filterConfigRepository, nameof(filterConfigRepository));

			_transactionFunction = transactionFunction;
			_filterConfigRepository = filterConfigRepository;
		}

		#region Implementation of IRowFilterConfigurationProvider

		public IEnumerable<RowFilterConfiguration> GetFilterConfigurations(Dataset forDataset,
			DataQualityCategory category)
		{
			IList<RowFilterConfiguration> allFilters = null;

			_transactionFunction(() => allFilters = category == null
				                                        ? _filterConfigRepository
					                                        .GetRowFilterConfigurations()
				                                        : _filterConfigRepository
					                                        .Get<RowFilterConfiguration>(category));

			return allFilters.Where(filter => IsApplicable(filter, forDataset))
			                 .OrderBy(d => d.Name);
			;
		}

		#endregion

		private bool IsApplicable(RowFilterConfiguration rowFilter,
		                          Dataset forDataset)
		{
			// TODO: Ask the RowConfig if it can filter the provieded dataset geometry
			return true;
		}
	}
}
