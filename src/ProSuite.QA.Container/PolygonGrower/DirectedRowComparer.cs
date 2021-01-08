using System;
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

		[CLSCompliant(false)]
		public bool Equals(IDirectedRow x, IDirectedRow y)
		{
			return Compare(x, y) == 0;
		}

		[CLSCompliant(false)]
		public int GetHashCode(IDirectedRow row)
		{
			return 37 * (2 * row.Row.RowOID + (row.IsBackward ? 1 : 0)) +
			       row.TopoLine.PartIndex;
		}

		[CLSCompliant(false)]
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
