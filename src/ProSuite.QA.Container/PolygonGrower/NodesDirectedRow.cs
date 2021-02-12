using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.PolygonGrower
{
	public class NodesDirectedRow : SimpleDirectedRow, INodesDirectedRow
	{
		#region nested classes

		public class NodesDirectedRowComparer : IComparer<NodesDirectedRow>,
		                                        IEqualityComparer<NodesDirectedRow>
		{
			private readonly DirectedRowComparer _rowComparer;

			public NodesDirectedRowComparer([NotNull] DirectedRowComparer rowComparer)
			{
				_rowComparer = rowComparer;
			}

			public bool Equals(NodesDirectedRow x, NodesDirectedRow y)
			{
				return Compare(x, y) == 0;
			}

			public int GetHashCode(NodesDirectedRow row)
			{
				return _rowComparer.GetHashCode(row);
			}

			public int Compare(NodesDirectedRow x, NodesDirectedRow y)
			{
				return _rowComparer.Compare(x, y);
			}
		}

		#endregion

		public NodesDirectedRow([NotNull] ITopologicalLine line,
		                        [NotNull] ITableIndexRow row,
		                        bool isBackward)
			: base(line, row, isBackward) { }

		public NetNode FromNode { get; set; }

		public NetNode ToNode { get; set; }

		public new NodesDirectedRow Reverse()
		{
			return (NodesDirectedRow) ReverseCore();
		}

		protected override SimpleDirectedRow ReverseCore()
		{
			return new NodesDirectedRow(TopoLine, Row, ! IsBackward)
			       {
				       FromNode = ToNode,
				       ToNode = FromNode
			       };
		}
	}
}
