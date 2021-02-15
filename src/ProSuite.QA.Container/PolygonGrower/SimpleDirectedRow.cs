using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.PolygonGrower
{
	public class SimpleDirectedRow : IDirectedRow
	{
		private readonly bool _isBackward;
		private readonly ITopologicalLine _topologicalLine;
		private readonly ITableIndexRow _row;

		public SimpleDirectedRow([NotNull] ITopologicalLine line,
		                         [NotNull] ITableIndexRow row,
		                         bool isBackward)
		{
			_isBackward = isBackward;
			_topologicalLine = line; // new SimpleTopoLine(line);
			_row = row; // new SimpleTableIndexRow(row);
		}

		public override string ToString()
		{
			string s = string.Format("OID:{0}; Reverse:{1}, FromAngle:{2} ",
			                         _row.RowOID, _isBackward, FromAngle);
			return s;
		}

		public bool IsBackward
		{
			get { return _isBackward; }
		}

		public IPoint FromPoint
		{
			get
			{
				return _isBackward
					       ? _topologicalLine.ToPoint
					       : _topologicalLine.FromPoint;
			}
		}

		public ITableIndexRow Row
		{
			get { return _row; }
		}

		public IPoint ToPoint
		{
			get
			{
				return _isBackward
					       ? _topologicalLine.FromPoint
					       : _topologicalLine.ToPoint;
			}
		}

		public double FromAngle
		{
			get
			{
				return _isBackward
					       ? _topologicalLine.ToAngle
					       : _topologicalLine.FromAngle;
			}
		}

		public double ToAngle
		{
			get
			{
				return _isBackward
					       ? _topologicalLine.FromAngle
					       : _topologicalLine.ToAngle;
			}
		}

		public ITopologicalLine TopoLine
		{
			get { return _topologicalLine; }
		}

		public SimpleDirectedRow Reverse()
		{
			return ReverseCore();
		}

		protected virtual SimpleDirectedRow ReverseCore()
		{
			return new SimpleDirectedRow(_topologicalLine, _row, ! _isBackward);
		}

		public ISegmentCollection GetSegmentCollection()
		{
			var line = (ICurve) ((IClone) _topologicalLine.GetLine()).Clone();

			if (_isBackward)
			{
				line.ReverseOrientation();
			}

			return (ISegmentCollection) line;
		}
	}
}
