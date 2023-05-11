using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.TableBased;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;

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
		public static InvolvedRows GetInvolvedRows(params IReadOnlyRow[] rows)
		{
			return GetInvolvedRows((IEnumerable<IReadOnlyRow>) rows);
		}

		[NotNull]
		public static InvolvedRows GetInvolvedRows<T>([NotNull] IEnumerable<T> rows)
			where T : IReadOnlyRow
		{
			Assert.ArgumentNotNull(rows, nameof(rows));

			InvolvedRows involvedRows = new InvolvedRows();
			foreach (T row in rows)
			{
				involvedRows.AddRange(GetInvolvedCore(row).EnumInvolvedRows());
				involvedRows.TestedRows.Add(row);
			}

			return involvedRows;
		}

		public static IEnumerable<Involved> EnumInvolved<T>([NotNull] IEnumerable<T> rows)
			where T : IReadOnlyRow
		{
			foreach (T row in rows)
			{
				yield return GetInvolvedCore(row);
			}
		}

		public const string BaseRowField = "__BaseRows__";

		private static Involved GetInvolvedCore(IReadOnlyRow row)
		{
			if (row.Table is ITableBased tableBased)
			{
				List<Involved> involveds = tableBased.GetInvolvedRows(row).ToList();

				IEnumerable<IReadOnlyTable> readOnlyTables = tableBased.GetInvolvedTables();

				Assert.True(involveds.Count > 0,
				            $"No involved rows for {row.Table.Name} with tables " +
				            $"{StringUtils.Concatenate(readOnlyTables, t => t.Name, ", ")}");

				return new InvolvedNested(row.Table.Name, involveds);
			}

			// TODO: Consider putting this also behind the ITableBased interface
			int baseRowsField = row.Table.Fields.FindField(BaseRowField);
			if (baseRowsField >= 0 && row.get_Value(baseRowsField) is IList<IReadOnlyRow> baseRows)
			{
				List<Involved> involveds = new List<Involved>();
				foreach (var baseRow in baseRows)
				{
					involveds.Add(GetInvolvedCore(baseRow));
				}

				return new InvolvedNested(row.Table.Name, involveds);
			}

			return new InvolvedRow(row);
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
