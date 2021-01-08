using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.PolygonGrower
{
	public abstract class NetNode
	{
		public abstract int RowsCount { get; }
	}

	[CLSCompliant(false)]
	public class NetNode<TDirectedRow> : NetNode
		where TDirectedRow : class, INodesDirectedRow
	{
		private readonly List<TDirectedRow> _rows;

		public NetNode([NotNull] IEnumerable<TDirectedRow> rows)
		{
			_rows = new List<TDirectedRow>(rows);
			foreach (TDirectedRow row in _rows)
			{
				row.FromNode = this;
			}
		}

		public List<TDirectedRow> Rows
		{
			get { return _rows; }
		}

		public override int RowsCount
		{
			get { return _rows.Count; }
		}
	}
}
