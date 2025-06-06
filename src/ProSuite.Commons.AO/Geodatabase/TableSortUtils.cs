using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Geodatabase
{
	public static class TableSortUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Creates a table sort object for the provided table and sort field name.
		/// </summary>
		/// <param name="table"></param>
		/// <param name="fieldName"></param>
		/// <param name="selection"></param>
		/// <returns></returns>
		[NotNull]
		public static ITableSort CreateTableSort([NotNull] ITable table,
		                                         [NotNull] string fieldName,
		                                         [CanBeNull] ISelectionSet selection = null)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNullOrEmpty(fieldName, nameof(fieldName));

			int fieldIdx = table.FindField(fieldName);

			Assert.ArgumentCondition(fieldIdx >= 0, "Field {0} not found in table", fieldName);

			return new TableSortClass
			       {
				       Table = table,
				       Fields = fieldName,
				       SelectionSet = selection
			       };
		}

		/// <summary>
		/// Creates a table sort object for the provided table and sort field name.
		/// </summary>
		/// <param name="table"></param>
		/// <param name="fieldName"></param>
		/// <param name="queryFilter"></param>
		/// <returns></returns>
		[NotNull]
		public static ITableSort CreateFilteredTableSort(
			[NotNull] ITable table,
			[NotNull] string fieldName,
			[CanBeNull] IQueryFilter queryFilter = null)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNullOrEmpty(fieldName, nameof(fieldName));

			int fieldIdx = table.FindField(fieldName);

			Assert.ArgumentCondition(fieldIdx >= 0, "Field {0} not found in table", fieldName);

			return new TableSortClass
			       {
				       Table = table,
				       Fields = fieldName,
				       QueryFilter = queryFilter
			       };
		}

		/// <summary>
		/// Creates a table sort object that uses string-based guid comparison. This is the
		/// default behaviour on oracle. This method can be used for compatibility on tables 
		/// from File-based GDBs or other DBMS. This method is considerably slower than
		/// the default sort algorithm.
		/// </summary>
		/// <param name="table"></param>
		/// <param name="guidFieldName"></param>
		/// <param name="selection"></param>
		/// <returns></returns>
		[NotNull]
		public static ITableSort CreateGuidFieldTableSort(
			[NotNull] ITable table,
			[NotNull] string guidFieldName,
			[CanBeNull] ISelectionSet selection)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNullOrEmpty(guidFieldName, nameof(guidFieldName));

			int fieldIdx = table.FindField(guidFieldName);
			esriFieldType fieldType = table.Fields.Field[fieldIdx].Type;

			Assert.ArgumentCondition(fieldType == esriFieldType.esriFieldTypeGUID ||
			                         fieldType == esriFieldType.esriFieldTypeGlobalID,
			                         "Field type of {0} must be Guid or Global ID.",
			                         fieldType);

			ITableSort result = CreateTableSort(table, guidFieldName, selection);

			result.Compare = new GuidFieldSortCallback();

			return result;
		}

		/// <summary>
		/// Creates a table sort object that uses string-based guid comparison. This is the
		/// default behaviour on oracle. This method can be used for compatibility on tables 
		/// from File-based GDBs or other DBMS. This method is considerably slower than
		/// the default sort algorithm.
		/// </summary>
		/// <param name="table"></param>
		/// <param name="guidFieldName"></param>
		/// <param name="queryFilter"></param>
		/// <returns></returns>
		[NotNull]
		public static ITableSort CreateGuidFieldTableSort(
			[NotNull] ITable table,
			[NotNull] string guidFieldName,
			[CanBeNull] IQueryFilter queryFilter)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNullOrEmpty(guidFieldName, nameof(guidFieldName));

			ITableSort result = CreateFilteredTableSort(table, guidFieldName, queryFilter);

			result.Compare = new GuidFieldSortCallback();

			return result;
		}

		[NotNull]
		public static ICursor GetSortedTableCursor([NotNull] ITable table,
		                                           [NotNull] string fieldName,
		                                           [CanBeNull] IQueryFilter queryFilter,
		                                           [CanBeNull] ITrackCancel trackCancel = null)
		{
			ITableSort tableSort = CreateFilteredTableSort(table, fieldName, queryFilter);

			return GetSortedCursor(tableSort, trackCancel);
		}

		[NotNull]
		public static ICursor GetSortedTableCursor([NotNull] ISelectionSet selection,
		                                           [NotNull] string fieldName,
		                                           [CanBeNull] ITrackCancel trackCancel = null)
		{
			ITableSort tableSort = CreateTableSort(selection.Target, fieldName, selection);

			return GetSortedCursor(tableSort, trackCancel);
		}

		[NotNull]
		public static ICursor GetGuidFieldSortedCursor([NotNull] ITable table,
		                                               [NotNull] string guidFieldName,
		                                               [CanBeNull] IQueryFilter queryFilter,
		                                               [CanBeNull] ITrackCancel trackCancel = null)
		{
			ITableSort tableSort = CreateGuidFieldTableSort(table, guidFieldName, queryFilter);

			return GetSortedCursor(tableSort, trackCancel);
		}

		[NotNull]
		public static ICursor GetGuidFieldSortedCursor([NotNull] ISelectionSet selection,
		                                               [NotNull] string guidFieldName,
		                                               [CanBeNull] ITrackCancel trackCancel = null)
		{
			ITableSort tableSort =
				CreateGuidFieldTableSort(selection.Target, guidFieldName, selection);

			return GetSortedCursor(tableSort, trackCancel);
		}

		[NotNull]
		private static ICursor GetSortedCursor([NotNull] ITableSort tableSort,
		                                       [CanBeNull] ITrackCancel trackCancel)
		{
			ITable table = tableSort.Table;

			if (table != null)
			{
				_msg.DebugFormat("Sorting table {0}...", DatasetUtils.GetTableName(table));
			}

			tableSort.Sort(trackCancel);

			return tableSort.Rows;
		}
	}
}
