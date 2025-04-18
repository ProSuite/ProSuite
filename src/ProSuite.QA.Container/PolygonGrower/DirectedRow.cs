using System.Collections.Generic;
using System.Text;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.PolygonGrower
{
	public class DirectedRow : NetElement, IPolygonDirectedRow
	{
		#region nested classes

		public class RowByLineAngleComparer : IComparer<DirectedRow>
		{
			#region IComparer<DirectedRow> Members

			public int Compare(DirectedRow x, DirectedRow y)
			{
				return x.FromAngle.CompareTo(y.FromAngle);
			}

			#endregion
		}

		#endregion

		public static DirectedRow Reverse(DirectedRow row)
		{
			return row.Reverse();
		}

		private readonly bool _isBackward;
		private readonly TopologicalLine _topologicalLine;

		public DirectedRow([NotNull] TableIndexRow row, int partIndex, bool isBackward)
			: base(row)
		{
			_isBackward = isBackward;
			_topologicalLine = new TopologicalLine(row, partIndex);
		}

		public DirectedRow([NotNull] TopologicalLine line, bool isBackward)
			: base(line.Row)
		{
			_isBackward = isBackward;
			_topologicalLine = line;
		}

		public override string ToString()
		{
			var s = new StringBuilder();
			s.AppendFormat("OID:{0};", _topologicalLine.Row.Row.OID);
			if (_topologicalLine.PartIndex >= 0)
			{
				s.AppendFormat("Part:{0};", PartIndex);
			}
			else
			{
				s.Append("(all parts);");
			}

			s.Append($"Reverse:{_isBackward}; FromAngle:{FromAngle:N3}; FromPt:[{FromPoint.X:N0},{FromPoint.Y:N0}]");
			return s.ToString();
		}

		public bool IsBackward
		{
			get { return _isBackward; }
		}

		public int PartIndex
		{
			get { return _topologicalLine.PartIndex; }
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

		void ILineDirectedRow.QueryEnvelope(IEnvelope queryEnvelope)
		{
			TopologicalLine.Path.QueryEnvelope(queryEnvelope);
		}

		int ILineDirectedRow.Orientation
		{
			get { return TopologicalLine.Orientation; }
		}

		ITableIndexRow IDirectedRow.Row
		{
			get { return Row; }
		}

		protected override NetPoint_ NetPoint__
		{
			get { return new NetPoint_(FromPoint); }
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

		public LineListPolygon RightPoly
		{
			get
			{
				return _isBackward
					       ? _topologicalLine.LeftPoly
					       : _topologicalLine.RightPoly;
			}
			set
			{
				if (_isBackward)
				{
					_topologicalLine.LeftPoly = value;
				}
				else
				{
					_topologicalLine.RightPoly = value;
				}
			}
		}

		public IReadOnlyRow RightCentroid
		{
			get
			{
				return _isBackward
					       ? _topologicalLine.LeftCentroid
					       : _topologicalLine.RightCentroid;
			}
			set
			{
				if (_isBackward)
				{
					_topologicalLine.LeftCentroid = value;
				}
				else
				{
					_topologicalLine.RightCentroid = value;
				}
			}
		}

		public LineListPolygon LeftPoly
		{
			get
			{
				return _isBackward == false
					       ? _topologicalLine.LeftPoly
					       : _topologicalLine.RightPoly;
			}
			set
			{
				if (_isBackward == false)
				{
					_topologicalLine.LeftPoly = value;
				}
				else
				{
					_topologicalLine.RightPoly = value;
				}
			}
		}

		public IReadOnlyRow LeftCentroid
		{
			get
			{
				return _isBackward == false
					       ? _topologicalLine.LeftCentroid
					       : _topologicalLine.RightCentroid;
			}
			set
			{
				if (_isBackward == false)
				{
					_topologicalLine.LeftCentroid = value;
				}
				else
				{
					_topologicalLine.RightCentroid = value;
				}
			}
		}

		ITopologicalLine IDirectedRow.TopoLine
		{
			get { return TopologicalLine; }
		}

		public TopologicalLine TopologicalLine
		{
			get { return _topologicalLine; }
		}

		public void QueryFromPoint(IPoint queryPoint)
		{
			if (_isBackward)
			{
				_topologicalLine.QueryToPoint(queryPoint);
			}
			else
			{
				_topologicalLine.QueryFromPoint(queryPoint);
			}
		}

		protected override NetPoint_ QueryNetPoint(NetPoint_ queryPoint)
		{
			QueryFromPoint(queryPoint.Point);
			return queryPoint;
		}

		public void QueryToPoint(IPoint queryPoint)
		{
			if (_isBackward)
			{
				_topologicalLine.QueryFromPoint(queryPoint);
			}
			else
			{
				_topologicalLine.QueryToPoint(queryPoint);
			}
		}

		public DirectedRow Reverse()
		{
			return new DirectedRow(_topologicalLine, ! _isBackward);
		}

		public ISegmentCollection GetDirectedSegmentCollection()
		{
			var line = (ICurve) ((IClone) _topologicalLine.Path).Clone();

			if (_isBackward)
			{
				line.ReverseOrientation();
			}

			return (ISegmentCollection) line;
		}

		public ICurve GetBaseLine()
		{
			return _topologicalLine.Path;
		}
	}
}
