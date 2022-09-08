using System;
using System.Collections.Generic;
using System.Data;

namespace ProSuite.QA.Container
{
	public static class FilterUtils
	{
		public static DataView GetFiltersView<T>(string filtersExpression, IEnumerable<T> filters)
			where T : INamedFilter
		{
			if (string.IsNullOrWhiteSpace(filtersExpression))
			{
				return null;
			}

			DataTable tbl = new DataTable();
			foreach (T filter in filters)
			{
				tbl.Columns.Add(filter.Name, typeof(bool));
			}

			DataView filtersView = new DataView(tbl);
			filtersView.RowFilter = filtersExpression;
			return filtersView;
		}

		public static bool IsFulfilled<T>(DataView filtersView, IEnumerable<T> filters,
		                                  Func<T, bool> fulfilledFunc)
			where T : INamedFilter
		{
			DataRow filterRow = null;
			foreach (T filter in filters)
			{
				bool fulfilled = fulfilledFunc(filter);
				if (fulfilled && string.IsNullOrWhiteSpace(filtersView?.RowFilter))
				{
					return true;
				}

				if (filtersView != null)
				{
					filterRow = filterRow ?? filtersView.Table.NewRow();
					filterRow[filter.Name] = fulfilled;
				}
			}

			filterRow?.Table.Rows.Add(filterRow);
			filterRow?.AcceptChanges();

			bool allFulfilled = filtersView?.Count == 1;
			filtersView?.Table.Clear();
			filtersView?.Table.AcceptChanges();

			return allFulfilled;
		}
	}
}
