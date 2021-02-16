using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	/// <summary>
	/// Helper methods to deal with involved rows
	/// </summary>
	public static class InvolvedRowUtils
	{
		public static void AddUniqueInvolvedRows(
			[NotNull] ICollection<InvolvedRow> existingInvolvedRows,
			[NotNull] IEnumerable<InvolvedRow> involvedRows)
		{
			var set = new HashSet<InvolvedRow>(existingInvolvedRows);

			foreach (InvolvedRow involvedRow in involvedRows)
			{
				if (set.Add(involvedRow))
				{
					existingInvolvedRows.Add(involvedRow);
				}
			}
		}

		[NotNull]
		public static IList<InvolvedRow> GetInvolvedRows(params IRow[] rows)
		{
			return GetInvolvedRows((IEnumerable<IRow>) rows);
		}

		[NotNull]
		public static IList<InvolvedRow> GetInvolvedRows<T>([NotNull] IEnumerable<T> rows)
			where T : IRow
		{
			Assert.ArgumentNotNull(rows, nameof(rows));

			return rows.Select(row => new InvolvedRow(row)).ToList();
		}

		[NotNull]
		public static IDictionary<string, List<InvolvedRow>> GroupByTableName(
			[NotNull] IEnumerable<InvolvedRow> involvedRows)
		{
			var result = new Dictionary<string, List<InvolvedRow>>(
				StringComparer.OrdinalIgnoreCase);

			foreach (InvolvedRow involvedRow in involvedRows)
			{
				List<InvolvedRow> rows;
				if (! result.TryGetValue(involvedRow.TableName, out rows))
				{
					rows = new List<InvolvedRow>();
					result.Add(involvedRow.TableName, rows);
				}

				rows.Add(involvedRow);
			}

			// TODO make sure that the first involved row is also the first row in the first table

			return result;
		}
	}
}
