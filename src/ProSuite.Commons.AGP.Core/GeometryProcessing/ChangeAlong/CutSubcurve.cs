using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.GeometryProcessing.ChangeAlong
{
	/// <summary>
	///     A subcurve that is typically created by the intersection of the reshaping geometry
	///     with the geometry to be reshaped. The shortest possible line to reshape.
	/// </summary>
	public class CutSubcurve
	{
		#region Field declarations

		private readonly bool _isReshapeMemberCandidate;
		private readonly bool? _touchingDifferentParts;

		private readonly SubcurveNode _fromNode;
		private readonly SubcurveNode _toNode;

		private double? _lineAngleAtFrom;
		private double? _lineAngleAtTo;

		#endregion

		#region Constructors

		public CutSubcurve([NotNull] Polyline path,
		                   bool touchAtFromPoint,
		                   bool touchAtToPoint,
		                   [CanBeNull] SubcurveNode fromNode = null,
		                   [CanBeNull] SubcurveNode toNode = null,
		                   bool? touchingDifferentParts = false)
		{
			Path = path;

			TouchAtToPoint = touchAtToPoint;
			TouchAtFromPoint = touchAtFromPoint;

			_fromNode = fromNode;
			_toNode = toNode;
			_touchingDifferentParts = touchingDifferentParts;
		}

		public bool TouchAtFromPoint { get; }

		public bool TouchAtToPoint { get; }

		private SubcurveNode FromNode => _fromNode;

		public SubcurveNode ToNode => _toNode;

		public CutSubcurve([NotNull] Polyline path,
		                   bool canReshape,
		                   bool isCandidate,
		                   bool isFiltered,
		                   [CanBeNull] Polyline targetSegmentAtFrom,
		                   [CanBeNull] Polyline targetSegmentAtTo,
		                   [CanBeNull] IEnumerable<MapPoint> extraTargetInsertPoints)
		{
			Path = path;
			CanReshape = canReshape;
			_isReshapeMemberCandidate = isCandidate;
			IsFiltered = isFiltered;

			if (targetSegmentAtFrom != null)
			{
				FromPointIsStitchPoint = true;
				TargetSegmentAtFromPoint = targetSegmentAtFrom;
			}

			if (targetSegmentAtTo != null)
			{
				ToPointIsStitchPoint = true;
				TargetSegmentAtToPoint = targetSegmentAtTo;
			}

			ExtraTargetInsertPoints = extraTargetInsertPoints?.ToList();
		}

		#endregion

		public bool CanReshape { get; }

		public bool IsReshapeMemberCandidate
		{
			get
			{
				if (IsFiltered) return false;

				return _isReshapeMemberCandidate;
			}
		}

		public double LineAngleAtFrom
		{
			get
			{
				if (_lineAngleAtFrom == null)
				{
					_lineAngleAtFrom = GetLineAngle(this, _fromNode);
				}

				return (double) _lineAngleAtFrom;
			}
		}

		public double LineAngleAtTo
		{
			get
			{
				if (_lineAngleAtTo == null)
				{
					_lineAngleAtTo = GetLineAngle(this, _toNode);
				}

				return (double) _lineAngleAtTo;
			}
		}

		private static double GetLineAngle([NotNull] CutSubcurve subcurve,
		                                   SubcurveNode atNode)
		{
			//var segments = (ISegmentCollection)subcurve.Path;
			//var segments = subcurve.Path.Parts;
			ICollection<Segment> segmentCol = new List<Segment>();
			subcurve.Path.GetAllSegments(ref segmentCol);
			var segments = segmentCol.ToList();
			Segment segment;

			var reverseOrientation = false;

			if (atNode == subcurve.ToNode)
			{
				// use last segment and the line needs to be inverted
				//segment = segments.Segment[segments.SegmentCount - 1];
				segment = segments[segments.Count - 1];
				reverseOrientation = true;
			}
			else
			{
				segment = segments[0];
			}

			//var line = segment as ILine;
			//LineSegment line = LineBuilderEx.CreateLineSegment(segment);
			LineSegment line = segment as LineSegment;

			if (line == null)
			{
				//line = new LineClass();
				//segment.QueryTangent(esriSegmentExtension.esriNoExtension, 1, true, 10, line);
				line = GeometryEngine.Instance.QueryTangent(
					segment, SegmentExtensionType.NoExtension, 1, AsRatioOrLength.AsRatio, 10);
			}

			double angle = line.Angle;

			if (reverseOrientation)
			{
				angle = angle >= Math.PI
					        ? angle - Math.PI
					        : angle + Math.PI;
			}

			return angle;
		}

		public Polyline Path { get; set; }

		protected virtual MapPoint FromPointOnTarget => FromPoint;

		protected virtual MapPoint ToPointOnTarget => ToPoint;

		[CanBeNull]
		//public Geometry ExtraTargetInsertPoints { get; set; }
		public List<MapPoint> ExtraTargetInsertPoints { get; set; }

		public bool IsFiltered { get; set; }

		public MapPoint FromPoint => Path.Points[0];
		public MapPoint ToPoint => Path.Points[Path.PointCount - 1];

		/// <summary>
		///     Whether or not the to-point of the subcurve is a stitch point, i.e. an intersection point
		///     with no vertex in the target.
		///     <seealso cref="FromPointIsStitchPoint" />
		/// </summary>
		public bool ToPointIsStitchPoint { get; set; }

		/// <summary>
		///     Whether or not the from-point of the subcurve is a stitch point, i.e. an intersection point
		///     with no vertex in the target. If yes it can/should be removed in case several adjacent subcurves
		///     are merged together. The removal of this point before reshaping is necessary to avoid inserting
		///     artificial vertices into the reshape source.
		/// </summary>
		public bool FromPointIsStitchPoint { get; set; }

		/// <summary>
		///     The segment in the target at the location of the from-point in case FromPointIsStitchPoint equals true.
		///     This can be used to easily and exactly reconstruct the course of the target in the reshape line.
		/// </summary>
		[CanBeNull]
		public Polyline TargetSegmentAtFromPoint { get; set; }

		/// <summary>
		///     The segment in the target at the location of the to-point in case ToPointIsStitchPoint equals true.
		///     This can be used to easily and exactly reconstruct the course of the target in the reshape line.
		/// </summary>
		[CanBeNull]
		public Polyline TargetSegmentAtToPoint { get; set; }

		/// <summary>
		///     Link to the source feature this subcurve applies to. This can be used in the case where multiple
		///     sources are treated individually to identify for which feature a specific reshape curve is applicable.
		/// </summary>
		[CanBeNull]
		public GdbObjectReference? Source { get; set; }

		protected bool Equals([NotNull] CutSubcurve other)
		{
			return IsReshapeMemberCandidate == other.IsReshapeMemberCandidate &&
			       IsFiltered == other.IsFiltered &&
			       CanReshape == other.CanReshape &&
			       Equals(other.Source, Source) &&
			       ToPointIsStitchPoint == other.ToPointIsStitchPoint &&
			       FromPointIsStitchPoint == other.FromPointIsStitchPoint &&
			       Path.IsEqual(other.Path);
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

			if (obj.GetType() != GetType())
			{
				return false;
			}

			return Equals((CutSubcurve) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = _isReshapeMemberCandidate.GetHashCode();
				hashCode = (hashCode * 397) ^ CanReshape.GetHashCode();
				hashCode = (hashCode * 397) ^ (Path != null ? Path.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ IsFiltered.GetHashCode();
				hashCode = (hashCode * 397) ^ ToPointIsStitchPoint.GetHashCode();
				hashCode = (hashCode * 397) ^ FromPointIsStitchPoint.GetHashCode();
				return hashCode;
			}
		}

		//public override bool Equals(object obj)
		//{
		//	if (ReferenceEquals(null, obj)) return false;

		//	if (ReferenceEquals(this, obj)) return true;

		//	if (obj.GetType() != typeof(CutSubcurve)) return false;

		//	return Equals((CutSubcurve) obj);
		//}

		public override string ToString()
		{
			string pathInfo;
			if (Path == null)
			{
				pathInfo = "<path is null>";
			}
			else if (Path.IsEmpty)
			{
				pathInfo = "<path is empty>";
			}
			else
			{
				var from = FromPoint;
				var to = ToPoint;

				pathInfo = string.Format("path from {0}|{1} to {2}|{3} with length {4}",
				                         from.X, from.Y, to.X, to.Y, Path.Length);
			}

			return string.Format("CutSubcurve with {0}", pathInfo);
		}

		public IEnumerable<MapPoint> GetPotentialTargetInsertPoints()
		{
			//TODO: wegen overrides FromPointOnTarget, ToPointOnTarget PointCount nicht ohne weiteres ermittelbar
			//<see cref="AdjustedCutSubcurve.FromPointOnTarget"/>, throws an exception if PointCount = 0
			//<see cref="CutSubcurve.FromPointOnTarget"/>, throws an exception if PointCount = 0

			var list = new List<MapPoint>();
			try
			{
				list.Add(FromPointOnTarget);
			}
			catch (Exception exception)
			{
				//nothing to do				
			}

			try
			{
				list.Add(ToPointOnTarget);
			}
			catch (Exception exception)
			{
				//nothing to do
			}

			foreach (MapPoint mapPoint in list)
			{
				yield return mapPoint;
			}

			if (ExtraTargetInsertPoints != null)
			{
				foreach (MapPoint point in ExtraTargetInsertPoints)
				{
					yield return point;
				}
			}
		}

		#region Private and protected members

		private void AddExtraPotentialTargetInsertPoint(MapPoint point)
		{
			if (ExtraTargetInsertPoints == null)
			{
				ExtraTargetInsertPoints = new List<MapPoint>();
			}

			ExtraTargetInsertPoints.Add(point);
		}

		#endregion

		// TODO: implement IDispose to release COM objects?
	}
}
