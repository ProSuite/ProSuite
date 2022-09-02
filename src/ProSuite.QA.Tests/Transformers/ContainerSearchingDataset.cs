using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
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

		public override IEnvelope Extent => throw new NotImplementedException();

		public override VirtualRow GetRow(int id)
		{
			return CreateRow(_sourceTable.GetRow(id));
		}

		public override int GetRowCount(IQueryFilter queryFilter)
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

			// NOTE: The container does not check that the search geometry is actually covered by the cache
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
				// TODO: just create it with null constraint?
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

		private IEnumerable<VirtualRow> SearchSourceTable(IQueryFilter filter, bool recycling)
		{
			_msg.DebugFormat("Searching {0} rows in database...", _sourceTable.Name);

			// TODO: Consider changing interface to IReadOnlyRow and return row directly
			foreach (IReadOnlyRow readOnlyRow in _sourceTable.EnumRows(filter, recycling))
			{
				if (FilterHelper == null ||
				    FilterHelper.MatchesConstraint(readOnlyRow))
				{
					yield return CreateRow(readOnlyRow);
				}
			}
		}

		private VirtualRow CreateRow(IReadOnlyRow baseRow)
		{
			var rowValueList = new ReadOnlyRowBasedValues(baseRow);

			int oid = baseRow.HasOID ? baseRow.OID : -1;

			return _gdbTable.CreateObject(oid, rowValueList);
		}
	}
}
