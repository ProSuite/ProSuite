using System;
using System.Collections.Generic;

namespace ProSuite.Commons.AGP.Core.GeometryProcessing.ChangeAlong;

public class SubcurveNode
{
	private readonly double _x;
	private readonly double _y;

	private readonly int _digitsForCoordinateComparison;

	private readonly List<CutSubcurve> _connectedSubcurves;

	public SubcurveNode(double x, double y, int digitsForCoordinateComparison = 7)
	{
		_digitsForCoordinateComparison = digitsForCoordinateComparison;

		_x = Math.Round(x, _digitsForCoordinateComparison);
		_y = Math.Round(y, _digitsForCoordinateComparison);

		_connectedSubcurves = new List<CutSubcurve>();
	}

	public double X
	{
		get { return _x; }
	}

	public double Y
	{
		get { return _y; }
	}

	public int DigitsForCoordinateComparison
	{
		get { return _digitsForCoordinateComparison; }
	}

	public IList<CutSubcurve> ConnectedSubcurves
	{
		get { return _connectedSubcurves; }
	}

	public IEnumerable<CutSubcurve> ConnectedSubcurvesFromRightToLeft(
		CutSubcurve startCurve)
	{
		_connectedSubcurves.Sort(new CutSubcurveComparer(startCurve, this, true));

		return _connectedSubcurves;
	}

	public IEnumerable<CutSubcurve> ConnectedSubcurvesFromLeftToRight(
		CutSubcurve startCurve)
	{
		_connectedSubcurves.Sort(new CutSubcurveComparer(startCurve, this, false));

		return _connectedSubcurves;
	}

	public bool Equals(SubcurveNode other)
	{
		if (ReferenceEquals(null, other))
		{
			return false;
		}

		if (ReferenceEquals(this, other))
		{
			return true;
		}

		return other._x.Equals(_x) && other._y.Equals(_y);
	}

	public override bool Equals(object obj)
	{
		if (ReferenceEquals(null, obj))
		{
			return false;
		}

		if (ReferenceEquals(this, obj))
		{
			return true;
		}

		if (obj.GetType() != typeof(SubcurveNode))
		{
			return false;
		}

		return Equals((SubcurveNode) obj);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			return (_x.GetHashCode() * 397) ^ _y.GetHashCode();
		}
	}

	private class CutSubcurveComparer : IComparer<CutSubcurve>
	{
		private readonly double _comingFromAngle;
		private readonly SubcurveNode _atNode;
		private readonly bool _righToLeft;

		public CutSubcurveComparer(CutSubcurve startCurve, SubcurveNode atNode,
		                           bool righToLeft)
		{
			_comingFromAngle = GetAngle(startCurve, atNode);
			_atNode = atNode;
			_righToLeft = righToLeft;
		}

		#region Implementation of IComparer<CutSubcurve>

		public int Compare(CutSubcurve x, CutSubcurve y)
		{
			double angleX = GetAngle(x, _atNode);
			double angleY = GetAngle(y, _atNode);

			double deltaX = angleX - _comingFromAngle;
			double deltaY = angleY - _comingFromAngle;

			if (deltaX == deltaY)
			{
				return 0;
			}

			if (_righToLeft)
			{
				return deltaX > deltaY
					       ? 1
					       : -1;
			}

			return deltaX < deltaY
				       ? -1
				       : 1;
		}

		private static double GetAngle(CutSubcurve subcurve, SubcurveNode atNode)
		{
			if (atNode == subcurve.ToNode)
			{
				return subcurve.LineAngleAtTo;
			}
			else
			{
				return subcurve.LineAngleAtFrom;
			}
		}

		//private static double GetLineAngle(CutSubcurve subcurve, SubcurveNode atNode)
		//{
		//    ISegmentCollection segments = (ISegmentCollection)subcurve.Path;

		//    ISegment segment;

		//    bool reverseOrientation = false;

		//    if (atNode == subcurve.ToNode)
		//    {
		//        // use last segment and the line needs to be inverted
		//        segment = segments.get_Segment(segments.SegmentCount - 1);

		//        reverseOrientation = true;
		//    }
		//    else
		//    {
		//        segment = segments.get_Segment(0);
		//    }

		//    var line = segment as ILine;

		//    if (line == null)
		//    {
		//        line = new LineClass();
		//        segment.QueryTangent(esriSegmentExtension.esriNoExtension, 1, true, 10, line);

		//    }

		//    double angle = line.Angle;

		//    if (reverseOrientation)
		//    {
		//        angle = angle >= Math.PI
		//                    ? angle - Math.PI
		//                    : angle + Math.PI;
		//    }

		//    return angle;
		//}

		#endregion
	}

	//List<CutSubcurve> list = (List<CutSubcurve>) atNode.ConnectedSubcurves;
	//list.Sort(new Comparison<CutSubcurve>(delegate(CutSubcurve x, CutSubcurve y)
	//                                      {
	//                                        if (x.Path...)
	//                                        return -1;
	//                                      }));
}
