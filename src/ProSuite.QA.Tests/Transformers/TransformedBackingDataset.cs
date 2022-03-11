using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom.SpatialIndex;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Geometry;

namespace ProSuite.QA.Tests.Transformers
{
	public abstract class TransformedBackingDataset<T> : TransformedBackingDataset
		where T : TransformedFeatureClass
	{
		protected TransformedBackingDataset([NotNull] T gdbTable,
		                                    IList<IReadOnlyTable> involvedTables)
			: base(gdbTable, involvedTables) { }

		public new T Resulting => (T) base.Resulting;
	}

	public abstract class TransformedBackingDataset : BackingDataset
	{
		private readonly IList<IReadOnlyTable> _involvedTables;
		private readonly List<QueryFilterHelper> _queryHelpers;
		private readonly Dictionary<int, VirtualRow> _rowsCache;

		private readonly TransformedFeatureClass _resulting;

		public ISearchable DataContainer { get; set; }
		public TransformedFeatureClass Resulting => _resulting;
		protected IReadOnlyList<QueryFilterHelper> QueryHelpers => _queryHelpers;

		public void AddToCache(VirtualRow row)
		{
			_rowsCache[row.OID] = row;
		}

		public bool RemoveFromCache(int oid)
		{
			return _rowsCache.Remove(oid);
		}

		public sealed override VirtualRow GetRow(int id)
		{
			if (_rowsCache.TryGetValue(id, out VirtualRow row))
			{
				return row;
			}

			return GetUncachedRow(id);
		}

		public abstract VirtualRow GetUncachedRow(int id);

		protected TransformedBackingDataset([NotNull] TransformedFeatureClass gdbTable,
		                                    IList<IReadOnlyTable> involvedTables)
		{
			_involvedTables = involvedTables;
			_queryHelpers = _involvedTables
			                .Select(t => new QueryFilterHelper(t, null, false)
			                             { RepeatCachedRows = true })
			                .ToList();

			gdbTable.AddField(FieldUtils.CreateBlobField(InvolvedRowUtils.BaseRowField));
			_resulting = gdbTable;
			_rowsCache = new Dictionary<int, VirtualRow>();
		}

		public void SetConstraint(int tableIndex, string condition)
		{
			if (tableIndex >= 0 && tableIndex < _involvedTables.Count)
			{
				_queryHelpers[tableIndex] =
					new QueryFilterHelper(
						_involvedTables[tableIndex], condition,
						_queryHelpers[tableIndex]?.TableView?.CaseSensitive ?? true)
					{ RepeatCachedRows = true };
			}
			else
			{
				throw new InvalidOperationException(
					$"Invalid table index {tableIndex}");
			}
		}

		public void SetSqlCaseSensitivity(int tableIndex, bool useCaseSensitiveQaSql)
		{
			if (tableIndex >= 0 && tableIndex < _involvedTables.Count)
			{
				_queryHelpers[tableIndex] = new QueryFilterHelper(
					_involvedTables[tableIndex], _queryHelpers[tableIndex]?.TableView?.Constraint,
					useCaseSensitiveQaSql);
			}
			else
			{
				throw new InvalidOperationException(
					$"Invalid table index {tableIndex}");
			}
		}

		protected IEnumerable<Involved> EnumKnownInvolveds(
			[NotNull] IReadOnlyFeature baseFeature,
			[CanBeNull] BoxTree<VirtualRow> knownRows,
			[NotNull] Dictionary<VirtualRow, Involved> involvedDict)
		{
			if (knownRows == null)
			{
				yield break;
			}

			foreach (BoxTree<VirtualRow>.TileEntry entry in
			         knownRows.Search(QaGeometryUtils.CreateBox(baseFeature.Extent)))
			{
				if (! involvedDict.TryGetValue(entry.Value, out Involved knownInvolved))
				{
					knownInvolved =
						InvolvedRowUtils.EnumInvolved(new[] { entry.Value }).First();
					involvedDict.Add(entry.Value, knownInvolved);
				}

				yield return knownInvolved;
			}
		}
	}
}
