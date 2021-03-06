using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container.TestSupport;

namespace ProSuite.QA.Container
{
	public class RowComparer : IComparer<IRow>
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

		public int Compare(IRow row0, IRow row1)
		{
			if (row0 == row1)
			{
				return 0;
			}

			int oidDifference = row0.OID - row1.OID;
			if (oidDifference != 0)
			{
				return oidDifference;
			}

			// oids are equal
			ITable table0 = row0.Table;
			ITable table1 = row1.Table;

			if (table0 == table1)
			{
				RelatedTables relTables = _relatedTablesProvider.GetRelatedTables(row0);
				return relTables == null
					       ? 0
					       : CompareRelatedRows(row0, row1, relTables);
			}

			// TODO names might not be unique (if multiple workspaces are involved)
			string name0 = ((IDataset) table0).Name;
			string name1 = ((IDataset) table1).Name;

			return string.Compare(name0, name1, StringComparison.Ordinal);
		}

		private static int CompareRelatedRows([NotNull] IRow row0,
		                                      [NotNull] IRow row1,
		                                      [NotNull] RelatedTables relTables)
		{
			IList<InvolvedRow> relatedList0 = relTables.GetInvolvedRows(row0);
			IList<InvolvedRow> relatedList1 = relTables.GetInvolvedRows(row1);

			int relatedCount = relatedList0.Count;
			Assert.AreEqual(relatedCount, relatedList1.Count, "Invalid involved rows");

			for (var idxRelated = 0; idxRelated < relatedCount; idxRelated++)
			{
				InvolvedRow relatedRow0 = relatedList0[idxRelated];
				InvolvedRow relatedRow1 = relatedList1[idxRelated];

				Assert.AreEqual(relatedRow0.TableName, relatedRow1.TableName,
				                "Involved Rows not sorted");

				int relOidDifference = relatedRow0.OID - relatedRow1.OID;
				if (relOidDifference != 0)
				{
					return relOidDifference;
				}
			}

			return 0;
		}

		#endregion
	}
}
