using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests.Transformers
{
	/// <summary>
	/// A bare-bones base class for implementations of row transformation logic with the most
	/// fundamental functionality for accessing the data container and involved tables with
	/// constraints.It has no direct support for row caching but allows accessing other cached
	/// data in the container.
	/// </summary>
	public abstract class TransformedBackingData : BackingDataset
	{
		private readonly List<QueryFilterHelper> _queryHelpers;

		protected TransformedBackingData(IList<IReadOnlyTable> involvedTables)
		{
			InvolvedTables = involvedTables;

			// This seems to be some kind of optimization if a feature is already cached from a previous tile?
			// -> Switch off optimization by repeating search for previously cached rows:
			const bool repeatCachedRows = true;

			_queryHelpers = InvolvedTables
			                .Select(t => new QueryFilterHelper(t, null, false)
			                             {RepeatCachedRows = repeatCachedRows})
			                .ToList();
		}

		public IList<IReadOnlyTable> InvolvedTables { get; }

		public ISearchable DataSearchContainer { get; set; }

		protected IReadOnlyList<QueryFilterHelper> QueryHelpers => _queryHelpers;

		public void SetConstraint(int tableIndex, string condition)
		{
			if (tableIndex >= 0 && tableIndex < InvolvedTables.Count)
			{
				_queryHelpers[tableIndex] =
					new QueryFilterHelper(
						InvolvedTables[tableIndex], condition,
						_queryHelpers[tableIndex]?.TableView?.CaseSensitive ?? true)
					{RepeatCachedRows = true};
			}
			else
			{
				throw new InvalidOperationException(
					$"Invalid table index {tableIndex}");
			}
		}

		public void SetSqlCaseSensitivity(int tableIndex, bool useCaseSensitiveQaSql)
		{
			if (tableIndex >= 0 && tableIndex < InvolvedTables.Count)
			{
				_queryHelpers[tableIndex] = new QueryFilterHelper(
					InvolvedTables[tableIndex], _queryHelpers[tableIndex]?.TableView?.Constraint,
					useCaseSensitiveQaSql);
			}
			else
			{
				throw new InvalidOperationException(
					$"Invalid table index {tableIndex}");
			}
		}
	}
}
