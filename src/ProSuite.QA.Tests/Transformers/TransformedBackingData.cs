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


			_queryHelpers = InvolvedTables
			                .Select(t => new QueryFilterHelper(t, null, false))
			                .ToList();
		}

		public IList<IReadOnlyTable> InvolvedTables { get; }

		public IDataContainer DataSearchContainer { get; set; }

		protected IReadOnlyList<QueryFilterHelper> QueryHelpers => _queryHelpers;

		public void SetConstraint(int tableIndex, string condition)
		{
			if (tableIndex >= 0 && tableIndex < InvolvedTables.Count)
			{
				QueryFilterHelper current = _queryHelpers[tableIndex];
				_queryHelpers[tableIndex] =
					new QueryFilterHelper(
						InvolvedTables[tableIndex], condition,
						current?.TableView?.CaseSensitive ?? true)
					{
						FullGeometrySearch = current?.FullGeometrySearch ?? false
					};
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
				QueryFilterHelper current = _queryHelpers[tableIndex];
				_queryHelpers[tableIndex] =
					new QueryFilterHelper(
						InvolvedTables[tableIndex], current?.TableView?.Constraint,
						useCaseSensitiveQaSql)
					{
						FullGeometrySearch = current?.FullGeometrySearch ?? false
					};
			}
			else
			{
				throw new InvalidOperationException(
					$"Invalid table index {tableIndex}");
			}
		}
	}
}
