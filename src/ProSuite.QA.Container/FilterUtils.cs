using System;
using System.Collections.Generic;
using System.Data;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	public static class FilterUtils
	{
		[NotNull]
		public static IList<string> GetFilterNames([CanBeNull] string filterExpression)
		{
			if (string.IsNullOrEmpty(filterExpression))
			{
				return Array.Empty<string>();
			}

			IList<string> filterNames = new List<string>();
			foreach (string token in ExpressionUtils.GetExpressionTokens(filterExpression))
			{
				const StringComparison ii = StringComparison.InvariantCultureIgnoreCase;
				if (token.Equals("AND", ii) || token.Equals("OR") || token.Equals("NOT"))
				{
					continue;
				}

				filterNames.Add(token);
			}

			return filterNames;
		}

		[CanBeNull]
		public static DataView GetFiltersView<T>([CanBeNull] string filtersExpression,
		                                         [NotNull] IEnumerable<T> filters)
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

		public static bool IsFulfilled<T>([CanBeNull] DataView filtersView,
		                                  [NotNull] IEnumerable<T> filters,
		                                  [NotNull] Func<T, bool> fulfilledFunc)
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
