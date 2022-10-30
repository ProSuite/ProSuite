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

	public abstract class TransformedBackingDataset : TransformedBackingData
	{
		private readonly Dictionary<long, VirtualRow> _rowsCache;

		private readonly TransformedFeatureClass _resulting;

		public IDataContainer DataContainer { get; set; }
		public TransformedFeatureClass Resulting => _resulting;

		protected int BaseRowsFieldIndex { get; }

		public void AddToCache(VirtualRow row)
		{
			_rowsCache[row.OID] = row;
		}

		public bool RemoveFromCache(long oid)
		{
			return _rowsCache.Remove(oid);
		}

		public sealed override VirtualRow GetRow(long id)
		{
			if (_rowsCache.TryGetValue(id, out VirtualRow row))
			{
				return row;
			}

			return GetUncachedRow(id);
		}

		public abstract VirtualRow GetUncachedRow(long id);

		protected TransformedBackingDataset([NotNull] TransformedFeatureClass gdbTable,
		                                    IList<IReadOnlyTable> involvedTables)
			: base(involvedTables)
		{
			BaseRowsFieldIndex =
				gdbTable.AddFieldT(FieldUtils.CreateBlobField(InvolvedRowUtils.BaseRowField));

			_resulting = gdbTable;
			_rowsCache = new Dictionary<long, VirtualRow>();
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
						InvolvedRowUtils.EnumInvolved(new[] {entry.Value}).First();
					involvedDict.Add(entry.Value, knownInvolved);
				}

				yield return knownInvolved;
			}
		}
	}
}
