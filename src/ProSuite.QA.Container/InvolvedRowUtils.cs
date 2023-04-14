using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
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
			if (row.Table is ITransformedTableBasedOnTables transformedTable)
			{
				List<Involved> involveds = new List<Involved>();
				foreach (Involved involvedRow in transformedTable.GetBaseRowReferences(row))
				{
					involveds.Add(involvedRow);
				}

				return new InvolvedNested(row.Table.Name, involveds);
			}

			// TODO: Consider putting this also behind the ITransformedTableBasedOnTables interface
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

			// TODO: Consider putting this also behind the ITransformedTableBasedOnTables interface
			if (row.Table.FullName is IQueryName qn)
			{
				List<Involved> involveds = new List<Involved>();
				foreach (string table in qn.QueryDef.Tables.Split(','))
				{
					string t = table.Trim();
					string oidField = $"{t}.OBJECTID";
					int oidFieldIdx = row.Table.FindField(oidField);
					if (oidFieldIdx < 0)
					{
						continue;
					}

					long? oidValue = GdbObjectUtils.ReadRowOidValue(row, oidFieldIdx);

					if (oidValue.HasValue)
					{
						involveds.Add(new InvolvedRow(t, oidValue.Value));
					}
				}

				Assert.True(involveds.Count > 0,
				            $"Only NULL OIDs found for a record of IQueryName {row.Table.Name} with tables {qn.QueryDef.Tables}");
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

		public static IEnumerable<Involved> GetInvolvedRowsFromJoinedRow(
			[NotNull] IReadOnlyRow joinedRow,
			[NotNull] IEnumerable<IReadOnlyTable> baseTables)
		{
			foreach (IReadOnlyTable baseTable in baseTables)
			{
				string oidFieldName = baseTable.OIDFieldName;

				string oidFieldQualified =
					string.IsNullOrEmpty(oidFieldName)
						? null // no OID field
						: DatasetUtils.QualifyFieldName(baseTable, oidFieldName);

				if (oidFieldName == null)
				{
					continue;
				}

				int oidFieldIdx = joinedRow.Table.FindField(oidFieldQualified);

				if (oidFieldIdx == -1)
				{
					continue;
				}

				long? oidValue =
					GdbObjectUtils.ReadRowOidValue(joinedRow, oidFieldIdx);

				if (oidValue != null)
				{
					yield return new InvolvedRow(baseTable.Name, oidValue.Value);
				}
			}
		}
	}
}
