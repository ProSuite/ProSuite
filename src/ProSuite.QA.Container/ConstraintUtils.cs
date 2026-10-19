using System;
using System.Collections.Generic;
using System.Data;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	internal static class ConstraintUtils
	{
		/// <summary>
		/// Adds a column to <paramref name="dataTable"/> for each available column handler
		/// that is referenced by the constraint expression, and returns those handlers.
		/// </summary>
		[NotNull]
		public static List<ColumnHandler<TSubject, TCache>> AddColumns<TSubject, TCache>(
			[NotNull] DataTable dataTable,
			[NotNull] string constraint,
			[NotNull] IEnumerable<ColumnHandler<TSubject, TCache>> availableHandlers)
		{
			var columnHandlers = new List<ColumnHandler<TSubject, TCache>>();

			DataColumnCollection columns = dataTable.Columns;

			foreach (ColumnHandler<TSubject, TCache> columnHandler in availableHandlers)
			{
				if (! UsesField(constraint, columnHandler.ColumnName))
				{
					continue;
				}

				columns.Add(columnHandler.CreateColumn());
				columnHandlers.Add(columnHandler);
			}

			return columnHandlers;
		}

		private static bool UsesField([NotNull] string constraint,
		                              [NotNull] string fieldName)
		{
			return constraint.IndexOf(fieldName, StringComparison.OrdinalIgnoreCase) >= 0;
		}
	}
}
