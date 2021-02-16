using System.Collections.Generic;

namespace ProSuite.QA.Container.PolygonGrower
{
	public class TableIndexRowComparer : IComparer<ITableIndexRow>,
	                                     IEqualityComparer<ITableIndexRow>
	{
		#region IComparer<TableIndexRow> Members

		public int Compare(ITableIndexRow row0, ITableIndexRow row1)
		{
			if (row0 == null && row1 == null)
			{
				return 0;
			}

			if (row0 == null)
			{
				return -1;
			}

			if (row1 == null)
			{
				return 1;
			}

			if (row0 == row1)
			{
				return 0;
			}

			int tableIndexDifference = row0.TableIndex - row1.TableIndex;
			if (tableIndexDifference != 0)
			{
				return tableIndexDifference;
			}

			// NOTE in case of joined rows, the RowOID may not be the expected field. 
			// NOTE even apart from that bug, there are cases where the joined OID cannot be unique
			// TODO use the unique id
			int oidDifference = row0.RowOID - row1.RowOID;
			if (oidDifference != 0)
			{
				return oidDifference;
			}

			return 0;
		}

		#endregion

		public bool Equals(ITableIndexRow x, ITableIndexRow y)
		{
			return Compare(x, y) == 0;
		}

		public int GetHashCode(ITableIndexRow obj)
		{
			return obj.RowOID ^ 29 * obj.TableIndex;
		}
	}
}
