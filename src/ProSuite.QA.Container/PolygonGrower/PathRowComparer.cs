using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.PolygonGrower
{
	public class PathRowComparer : IComparer<IDirectedRow>,
	                               IEqualityComparer<IDirectedRow>
	{
		private readonly TableIndexRowComparer _rowComparer;

		public PathRowComparer([NotNull] TableIndexRowComparer rowComparer)
		{
			_rowComparer = rowComparer;
		}

		public TableIndexRowComparer RowComparer
		{
			get { return _rowComparer; }
		}

		[CLSCompliant(false)]
		public bool Equals(IDirectedRow x, IDirectedRow y)
		{
			return Compare(x, y) == 0;
		}

		[CLSCompliant(false)]
		public int GetHashCode(IDirectedRow row)
		{
			return 37 * row.Row.RowOID + row.TopoLine.PartIndex;
		}

		[CLSCompliant(false)]
		public int Compare(IDirectedRow x, IDirectedRow y)
		{
			int d = _rowComparer.Compare(x.Row, y.Row);
			if (d != 0)
			{
				return d;
			}

			d = x.TopoLine.PartIndex.CompareTo(y.TopoLine.PartIndex);

			return d;
		}
	}
}
