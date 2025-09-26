using System;
using System.Collections.Generic;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	public class RowComparer : IComparer<IReadOnlyRow>
	{
		private readonly IRelatedTablesProvider _relatedTablesProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="RowComparer"/> class.
		/// </summary>
		/// <param name="relatedTablesProvider">The related tables provider.</param>
		public RowComparer([NotNull] IRelatedTablesProvider relatedTablesProvider)
		{
			Assert.ArgumentNotNull(relatedTablesProvider, nameof(relatedTablesProvider));

			_relatedTablesProvider = relatedTablesProvider;
		}

		#region IComparer<IRow> Members

		public int Compare(IReadOnlyRow row0, IReadOnlyRow row1)
		{
			if (row0 == row1)
			{
				return 0;
			}

			int oidDifference = row0.OID.CompareTo(row1.OID);
			if (oidDifference != 0)
			{
				return oidDifference;
			}

			// oids are equal
			IReadOnlyTable table0 = row0.Table;
			IReadOnlyTable table1 = row1.Table;

			if (table0.Equals(table1))
			{
				return CompareRelatedRows(row0, row1);
			}

			// TODO names might not be unique (if multiple workspaces are involved)
			string name0 = table0.Name;
			string name1 = table1.Name;

			return string.Compare(name0, name1, StringComparison.Ordinal);
		}

		private static int CompareRelatedRows([NotNull] IReadOnlyRow row0,
		                                      [NotNull] IReadOnlyRow row1)
		{
			IList<InvolvedRow> relatedList0 = InvolvedRowUtils.GetInvolvedRows(row0);
			IList<InvolvedRow> relatedList1 = InvolvedRowUtils.GetInvolvedRows(row1);

			int relatedCount = relatedList0.Count;
			Assert.AreEqual(relatedCount, relatedList1.Count, "Invalid involved rows");

			for (var idxRelated = 0; idxRelated < relatedCount; idxRelated++)
			{
				InvolvedRow relatedRow0 = relatedList0[idxRelated];
				InvolvedRow relatedRow1 = relatedList1[idxRelated];

				Assert.AreEqual(relatedRow0.TableName, relatedRow1.TableName,
				                "Involved Rows not sorted");

				int relatedRowCompare = relatedRow0.OID.CompareTo(relatedRow1.OID);
				if (relatedRowCompare != 0)
				{
					return relatedRowCompare;
				}
			}

			return 0;
		}

		#endregion
	}
}
