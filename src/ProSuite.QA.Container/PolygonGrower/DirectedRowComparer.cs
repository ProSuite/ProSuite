using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.PolygonGrower
{
	public class DirectedRowComparer : IComparer<IDirectedRow>,
	                                   IEqualityComparer<IDirectedRow>
	{
		private readonly TableIndexRowComparer _rowComparer;

		public DirectedRowComparer([NotNull] TableIndexRowComparer rowComparer)
		{
			_rowComparer = rowComparer;
		}

		public bool Equals(IDirectedRow x, IDirectedRow y)
		{
			return Compare(x, y) == 0;
		}

		public int GetHashCode(IDirectedRow row)
		{
			return 37 * (2 * row.Row.RowOID.GetHashCode() + (row.IsBackward ? 1 : 0)) +
			       row.TopoLine.PartIndex;
		}

		public int Compare(IDirectedRow x, IDirectedRow y)
		{
			if (x.IsBackward != y.IsBackward)
			{
				return x.IsBackward
					       ? -1
					       : 1;
			}

			int d = x.TopoLine.PartIndex.CompareTo(y.TopoLine.PartIndex);
			if (d != 0)
			{
				return d;
			}

			return _rowComparer.Compare(x.Row, y.Row);
		}
	}
}
