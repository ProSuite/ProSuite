using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests.Transformers
{
	public class ContainerSearchingDataset : BackingDataset
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly IReadOnlyTable _sourceTable;
		private readonly GdbTable _gdbTable;

		public ContainerSearchingDataset(IReadOnlyTable tableToSearch,
		                                 GdbTable gdbTable)
		{
			_sourceTable = tableToSearch;
			_gdbTable = gdbTable;
		}

		public IDataContainer DataContainer { get; set; }

		public QueryFilterHelper FilterHelper { get; set; }

		public bool HasGeometry => _gdbTable is IReadOnlyFeatureClass;

		#region Overrides of BackingDataset

		public override IEnvelope Extent => (_sourceTable as IReadOnlyFeatureClass)?.Extent;

		public override VirtualRow GetRow(long id)
		{
			return CreateRow(_sourceTable.GetRow(id));
		}

		public override long GetRowCount(IQueryFilter queryFilter)
		{
			return _sourceTable.RowCount(queryFilter);
		}

		public override IEnumerable<VirtualRow> Search(IQueryFilter filter, bool recycling)
		{
			if (DataContainer == null)
			{
				return SearchSourceTable(filter, recycling);
			}

			ISpatialFilter spatialFilter = filter as ISpatialFilter;
			IGeometry spatialFilterGeometry = spatialFilter?.Geometry;

			if (spatialFilterGeometry == null)
			{
				// The container requires not-null spatial filter
				return SearchSourceTable(filter, recycling);
			}

			// NOTE: The container does not check that the search geometry is actually covered by the cache.
			//       Extra-tile-loading outside the current tile using TileAdmin can happen for transformers
			//       that use neighbor search All (filterHelper.FullGeometrySearch). However this is not
			//       propagated to upstream transformers probably because it would overwhelm the available
			//       memory for vast searches throughout several tiles.
			//       This very call could in fact be part of the loading procedure of an extra tile.
			if (! GeometryUtils.Contains(DataContainer.GetLoadedExtent(_sourceTable),
			                             spatialFilterGeometry))
			{
				// The search area exceeds the cached area
				return SearchSourceTable(filter, recycling);
			}

			if (! string.IsNullOrEmpty(filter.WhereClause))
			{
				// NOTE: The container ignores the where clause of the filter -> set it up here!
				if (FilterHelper != null)
				{
					// TODO: How to adapt the constraint or create a new filter helper with ... AND whereClause?
					return SearchSourceTable(filter, recycling);
				}

				FilterHelper = new QueryFilterHelper(_sourceTable, filter.WhereClause, false);
			}

			if (FilterHelper == null)
			{
				// The container requires not-null filter helper
				// TODO: just create it with null constraint? Assert not null?
				return SearchSourceTable(filter, recycling);
			}

			// If the features are not in the container, a different approach would be more suitable:
			// Get all intersecting, search by unioned envelopes, etc.
			IEnumerable<IReadOnlyRow> containerEnum = DataContainer.Search(
				_sourceTable, filter, FilterHelper);

			if (containerEnum == null)
			{
				// If the table is not cached
				return SearchSourceTable(filter, recycling);
			}

			return YieldContainerResults(containerEnum);
		}

		#endregion

		private IEnumerable<VirtualRow> YieldContainerResults(
			IEnumerable<IReadOnlyRow> containerEnum)
		{
			_msg.DebugFormat("Searching {0} rows in container...", _sourceTable.Name);

			// TODO: Do we really need to wrap again? -> Consider BackingDataset use IReadOnlyRow instead VirtualRow?
			foreach (IReadOnlyRow resultRow in containerEnum)
			{
				// No caching, just wrap it:
				yield return CreateRow(resultRow);
			}
		}

		private IEnumerable<VirtualRow> SearchSourceTable([CanBeNull] IQueryFilter filter,
		                                                  bool recycling)
		{
			_msg.DebugFormat("Searching {0} rows in database...", _sourceTable.Name);

			string originalSubfields = filter?.SubFields;

			// TOP-5639: Append the sub-fields from the FilterHelper, if not already "*" sub-fields.
			if (! string.IsNullOrEmpty(originalSubfields) &&
			    originalSubfields != "*" &&
			    ! string.IsNullOrEmpty(FilterHelper?.SubFields))
			{
				filter.SubFields =
					GdbQueryUtils.AppendToFieldList(originalSubfields, FilterHelper.SubFields);
			}

			// TODO: Consider changing interface to IReadOnlyRow and return row directly
			foreach (IReadOnlyRow readOnlyRow in _sourceTable.EnumRows(filter, recycling))
			{
				if (FilterHelper == null ||
				    FilterHelper.MatchesConstraint(readOnlyRow))
				{
					yield return CreateRow(readOnlyRow);
				}
			}

			if (filter != null)
			{
				filter.SubFields = originalSubfields;
			}
		}

		private VirtualRow CreateRow(IReadOnlyRow baseRow)
		{
			var rowValueList = new ReadOnlyRowBasedValues(baseRow);

			long oid = baseRow.HasOID ? baseRow.OID : -1;

			return _gdbTable.CreateObject(oid, rowValueList);
		}
	}
}
