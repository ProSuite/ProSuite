using System;
using System.Collections.Generic;
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
		public static InvolvedRows GetInvolvedRows(params IRow[] rows)
		{
			return GetInvolvedRows((IEnumerable<IRow>) rows);
		}

		[NotNull]
		public static InvolvedRows GetInvolvedRows<T>([NotNull] IEnumerable<T> rows)
			where T : IRow
		{
			Assert.ArgumentNotNull(rows, nameof(rows));

			InvolvedRows involvedRows = new InvolvedRows();
			foreach (T row in rows)
			{
				involvedRows.AddRange(GetInvolvedRowsCore(row));
				involvedRows.TestedRows.Add(row);
			}

			return involvedRows;
		}

		public const string BaseRowField = "__BaseRows__";

		private static IEnumerable<InvolvedRow> GetInvolvedRowsCore(IRow row)
		{
			int baseRowsField = row.Fields.FindField(BaseRowField);
			if (baseRowsField >= 0 && row.get_Value(baseRowsField) is IList<IRow> baseRows)
			{
				foreach (var baseRow in baseRows)
				{
					foreach (var involvedRow in GetInvolvedRowsCore(baseRow))
					{
						yield return involvedRow;
					}
				}
			}
			else
			{
				if (((IDataset) row.Table).FullName is IQueryName qn)
				{
					foreach (string table in qn.QueryDef.Tables.Split(','))
					{
						string t = table.Trim();
						string oidField = $"{t}.OBJECTID";
						int iOid = row.Table.FindField(oidField);
						if (iOid >= 0)
						{
							yield return new InvolvedRow(t, (int) row.Value[iOid]);
						}
					}
				}
				else
				{
					yield return new InvolvedRow(row);
				}
			}
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
