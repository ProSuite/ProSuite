using System;
using System.Reflection;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Geodatabase
{
	[CLSCompliant(false)]
	public static class TableSortUtils
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Creates a table sort object for the provided table and sort field name.
		/// </summary>
		/// <param name="table"></param>
		/// <param name="fieldName"></param>
		/// <returns></returns>
		[NotNull]
		public static ITableSort CreateTableSort([NotNull] ITable table,
		                                         [NotNull] string fieldName)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNullOrEmpty(fieldName, nameof(fieldName));

			int fieldIdx = table.FindField(fieldName);

			Assert.ArgumentCondition(fieldIdx >= 0, "Field {0} not found in table",
			                         fieldName);

			return new TableSortClass
			       {
				       Table = table,
				       Fields = fieldName,
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
		/// <returns></returns>
		[NotNull]
		public static ITableSort CreateGuidFieldTableSort([NotNull] ITable table,
		                                                  [NotNull] string guidFieldName)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNullOrEmpty(guidFieldName, nameof(guidFieldName));

			int fieldIdx = table.FindField(guidFieldName);
			esriFieldType fieldType = table.Fields.Field[fieldIdx].Type;

			Assert.ArgumentCondition(fieldType == esriFieldType.esriFieldTypeGUID ||
			                         fieldType == esriFieldType.esriFieldTypeGlobalID,
			                         "Field type of {0} must be Guid or Gobal ID.");

			ITableSort result = CreateTableSort(table, guidFieldName);

			result.Compare = new GuidFieldSortCallback();

			return result;
		}

		[NotNull]
		public static ICursor GetSortedTableCursor([NotNull] ITable table,
		                                           [NotNull] string fieldName,
		                                           [CanBeNull] ITrackCancel trackCancel =
			                                           null)
		{
			ITableSort tableSort = CreateTableSort(table, fieldName);

			return GetSortedCursor(tableSort, trackCancel);
		}

		[NotNull]
		public static ICursor GetGuidFieldSortedCursor([NotNull] ITable table,
		                                               [NotNull] string guidFieldName,
		                                               [CanBeNull] ITrackCancel trackCancel = null)
		{
			ITableSort tableSort = CreateGuidFieldTableSort(table, guidFieldName);

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
