using System;
using System.Diagnostics.CodeAnalysis;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
{
	/// <summary>
	/// Encapsulates information about the XY-intersection between two segments and provides
	/// functionality to create derived geometries such as intersection points.
	/// </summary>
	public class SegmentIntersection
	{
		// The intersection point distance of source points along the target (as ratio):
		private double _source1Factor;
		private double _source2Factor;

		/// <summary>
		/// The source segment index.
		/// </summary>
		public int SourceIndex { get; }

		/// <summary>
		/// The target segment index.
		/// </summary>
		public int TargetIndex { get; }

		/// <summary>
		/// Whether or not the start point of the source segment intersects the target segment.
		/// </summary>
		public bool SourceStartIntersects { get; private set; }

		/// <summary>
		/// Whether or not the end point of the source segment intersects the target segment.
		/// </summary>
		public bool SourceEndIntersects { get; private set; }

		/// <summary>
		/// The distance (as ratio) along the source of the target start point. If the target
		/// start is within the tolerance of a source vertex, it is snapped to 0 / 1.
		/// </summary>
		public double? TargetStartFactor { get; private set; }

		public bool TargetStartIntersects => TargetStartFactor >= 0 && TargetStartFactor <= 1;

		public bool TargetStartIsOnSourceInterior => TargetStartFactor > 0 &&
		                                             TargetStartFactor < 1;

		[SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
		public bool TargetStartIntersectsSourceVertex =>
			TargetStartFactor == 0 || TargetStartFactor == 1;

		/// <summary>
		/// The distance (as ratio) along the source of the target end point.  If the target
		/// end is within the tolerance of a source vertex, it is snapped to 0 / 1.
		/// </summary>
		public double? TargetEndFactor { get; private set; }

		public bool TargetEndIntersects => TargetEndFactor >= 0 && TargetEndFactor <= 1;

		public bool TargetEndIsOnSourceInterior =>
			TargetEndFactor > 0 && TargetEndFactor < 1;

		[SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
		public bool TargetEndIntersectsSourceVertex =>
			TargetEndFactor == 0 || TargetEndFactor == 1;

		/// <summary>
		/// In case a linear intersection exists: Whether the source and the target segments
		/// run in the opposite direction or not.
		/// NOTE: This property is unreliable for zero-length segments!
		/// </summary>
		public bool LinearIntersectionInOppositeDirection { get; set; }

		/// <summary>
		/// The factor (distance along the source segment as ratio) of the intersection point
		/// that is both on the source's interior and the target's interior.
		/// Null, if one of the source vertices or one of the target vertices are part of the
		/// intersection.
		/// </summary>
		public double? SingleInteriorIntersectionFactor { get; }

		public int TargetIntersectionCount
		{
			get
			{
				if (SingleInteriorIntersectionFactor != null)
				{
					return 1;
				}

				var result = 0;

				if (TargetStartIntersects)
				{
					result++;
				}

				if (TargetEndIntersects)
				{
					result++;
				}

				return result;
			}
		}

		public bool HasIntersection => SourceStartIntersects || SourceEndIntersects ||
		                               SingleInteriorIntersectionFactor != null ||
		                               TargetStartIntersects || TargetEndIntersects;

		/// <summary>
		/// Whether or not the intersection between the source and the target segment
		/// is a line. In case of a 0-length source segment the intersection is also
		/// considered a (0-length) line.
		/// </summary>
		public bool HasLinearIntersection
		{
			get
			{
				if (SourceStartIntersects && SourceEndIntersects)
				{
					return true;
				}

				int intersectionCount = TargetIntersectionCount;

				if (intersectionCount == 2)
				{
					return true;
				}

				if (intersectionCount == 0)
				{
					return false;
				}

				if (SingleInteriorIntersectionFactor != null)
				{
					return false;
				}

				double nonNullTarget =
					GetTargetStartIntersectionFactor() ??
					Assert.NotNull(GetTargetEndIntersectionFactor()).Value;

				if (SourceStartIntersects)
				{
					return ! MathUtils.AreEqual(0, nonNullTarget);
				}

				if (SourceEndIntersects)
				{
					return ! MathUtils.AreEqual(1.0, nonNullTarget);
				}

				return false;
			}
		}

		public bool SegmentsAreEqualInXy =>
			SourceStartIntersects && SourceEndIntersects &&
			TargetStartIntersectsSourceVertex &&
			TargetEndIntersectsSourceVertex;

		public bool IsSourceZeroLength2D
		{
			get
			{
				double? sourceStart = GetSourceStartOrSingleIntersectionFactor();

				if (sourceStart == null)
				{
					return false;
				}

				double? sourceEnd = GetSourceEndIntersectionFactor();

				if (sourceEnd == null)
				{
					return false;
				}

				return MathUtils.AreEqual(sourceStart.Value, sourceEnd.Value);
			}
		}

		public bool IsTargetZeroLength2D
		{
			get
			{
				double? targetStart = GetTargetStartIntersectionFactor();

				if (targetStart == null)
				{
					return false;
				}

				double? targetEnd = GetTargetEndIntersectionFactor();

				if (targetEnd == null)
				{
					return false;
				}

				return MathUtils.AreEqual(targetStart.Value, targetEnd.Value);
			}
		}

		public bool IsSegmentZeroLength2D => IsSourceZeroLength2D || IsTargetZeroLength2D;

		/// <summary>
		/// Whether the source interior is intersected by the target.
		/// </summary>
		public bool HasSourceInteriorIntersection
			=> HasLinearIntersection ||
			   SingleInteriorIntersectionFactor != null ||
			   TargetStartIsOnSourceInterior || TargetEndIsOnSourceInterior;

		#region Static factory methods

		/// <summary>
		/// Calculates intersection for two given segments.
		/// </summary>
		/// <param name="sourceLineIndex"></param>
		/// <param name="targetLineIndex"></param>
		/// <param name="sourceLine"></param>
		/// <param name="targetLine"></param>
		/// <param name="tolerance"></param>
		/// <returns></returns>
		public static SegmentIntersection CalculateIntersectionXY(
			int sourceLineIndex,
			int targetLineIndex,
			[NotNull] Line3D sourceLine,
			[NotNull] Line3D targetLine,
			double tolerance)
		{
			var intersection = new SegmentIntersection(
				sourceLineIndex, targetLineIndex, sourceLine,
				targetLine, tolerance);

			return intersection;
		}

		/// <summary>
		/// Creates a segment intersection for two coincident segments.
		/// </summary>
		/// <param name="sourceLineIndex"></param>
		/// <param name="targetLineIndex"></param>
		/// <param name="inverted"></param>
		/// <returns></returns>
		public static SegmentIntersection CreateCoincidenceIntersectionXY(
			int sourceLineIndex,
			int targetLineIndex,
			bool inverted)
		{
			var intersection = new SegmentIntersection(
				                   sourceLineIndex, targetLineIndex, 0, 1, 1, 0)
			                   {
				                   SourceStartIntersects = true,
				                   SourceEndIntersects = true,
				                   LinearIntersectionInOppositeDirection = inverted
			                   };

			return intersection;
		}

		#endregion

		#region Constructors

		private SegmentIntersection(int sourceLineIndex,
		                            int targetLineIndex,
		                            double source1Factor,
		                            double source2Factor,
		                            double targetStartFactor,
		                            double targetEndFactor)
		{
			SourceIndex = sourceLineIndex;
			TargetIndex = targetLineIndex;

			_source1Factor = source1Factor;
			_source2Factor = source2Factor;

			TargetStartFactor = targetStartFactor;
			TargetEndFactor = targetEndFactor;
		}

		private SegmentIntersection(int sourceLineIndex,
		                            int targetLineIndex,
		                            [NotNull] Line3D sourceLine,
		                            [NotNull] Line3D targetLine,
		                            double tolerance)
		{
			SourceIndex = sourceLineIndex;
			TargetIndex = targetLineIndex;

			CollectVertexIntersectionInfos(sourceLine, targetLine, tolerance);

			if (GetIntersectingVerticesCount() == 0)
			{
				// No linear intersection between the 2 lines, no intersection at vertices:
				// -> Search accurate intersection point on the segments' interior
				double thisFactor, otherFactor;
				if (sourceLine.TryGetIntersectionPointFactorsXY(
					    targetLine, out thisFactor, out otherFactor))
				{
					// TOP-5165: Must be really interior-interior intersection (0..1).
					// Intersections just outside have been handled in CollectVertexIntersectionInfos
					if (thisFactor > 0 && thisFactor < 1 &&
					    otherFactor > 0 && otherFactor < 1)
					{
						SingleInteriorIntersectionFactor = thisFactor;
						_source1Factor = otherFactor;
					}
				}
			}
		}

		#endregion

		/// <summary>
		/// The factor (distance along the source segment as ratio) of the target's start point
		/// or null, if the target's start point does not intersect the source.
		/// </summary>
		private double? GetTargetStartIntersectionFactor()
			=> TargetStartIntersects ? TargetStartFactor : null;

		/// <summary>
		/// The factor (distance along the source segment as ratio) of the target's end point
		/// or null, if the target's end point does not intersect the source.
		/// </summary>
		private double? GetTargetEndIntersectionFactor()
		{
			return TargetEndIntersects ? TargetEndFactor : null;
		}

		/// <summary>
		/// Gets the intersection factor (along the target) of the source's start point if
		/// the source start point intersects.
		/// If there is a single interior-interior intersection, its intersection factor (along
		/// the target) is returned.
		/// </summary>
		/// <returns></returns>
		private double? GetSourceStartOrSingleIntersectionFactor()
		{
			return GetIntersectionFactor(_source1Factor);
		}

		/// <summary>
		/// Gets the intersection factor (along the target) of the source's end point.
		/// </summary>
		/// <returns></returns>
		private double? GetSourceEndIntersectionFactor()
		{
			return GetIntersectionFactor(_source2Factor);
		}

		private static double? GetIntersectionFactor(double factor)
		{
			if (double.IsNaN(factor))
			{
				return null;
			}

			// The factors should have been snapped to 0..1 previously if the point was within the tolerance
			return factor >= 0 && factor <= 1
				       ? (double?) factor
				       : null;
		}

		public double GetLinearIntersectionStartFactor(bool closestToSourceStart = false)
		{
			if (SourceStartIntersects)
			{
				return 0;
			}

			if (closestToSourceStart)
			{
				// the target could have the opposite direction of the source, get the smaller
				double? targetStartFactor = GetTargetStartIntersectionFactor();
				double? targetEndFactor = GetTargetEndIntersectionFactor();

				// One of the two must not be null!
				if (targetStartFactor < targetEndFactor ||
				    targetEndFactor == null)
				{
					return Assert.NotNull(targetStartFactor).Value;
				}

				return Assert.NotNull(targetEndFactor).Value;
			}

			return GetTargetStartIntersectionFactor() ??
			       Assert.NotNull(GetTargetEndIntersectionFactor()).Value;
		}

		public double GetLinearIntersectionEndFactor(bool closestToSourceEnd = false)
		{
			if (SourceEndIntersects)
			{
				return 1.0;
			}

			if (closestToSourceEnd)
			{
				// the target could have the opposite direction of the source, get the larger
				double? targetStartFactor = GetTargetStartIntersectionFactor();
				double? targetEndFactor = GetTargetEndIntersectionFactor();

				// One of the two must not be null!
				if (targetEndFactor > targetStartFactor ||
				    targetStartFactor == null)
				{
					return Assert.NotNull(targetEndFactor).Value;
				}

				return Assert.NotNull(targetStartFactor).Value;
			}

			return GetTargetEndIntersectionFactor() ??
			       Assert.NotNull(GetTargetStartIntersectionFactor()).Value;
		}

		/// <summary>
		/// Creates the start point of the linear intersection along the source segment.
		/// </summary>
		/// <param name="sourceSegment">The source segment of this intersection.</param>
		/// <param name="startFactorAlongSource">The ratio along the source.</param>
		/// <returns></returns>
		public Pnt3D GetLinearIntersectionStart(Line3D sourceSegment,
		                                        out double startFactorAlongSource)
		{
			Pnt3D start;

			if (SourceStartIntersects)
			{
				start = sourceSegment.StartPoint;
				startFactorAlongSource = 0;
			}
			else
			{
				startFactorAlongSource = GetLinearIntersectionStartFactor(
					closestToSourceStart: true);

				start = sourceSegment.GetPointAlong(startFactorAlongSource, true);
			}

			return start;
		}

		/// <summary>
		/// Gets the start point of the linear intersection, corresponding with
		/// <see cref="GetLinearIntersectionStart"/>, but on the target.
		/// </summary>
		/// <param name="targetSegment"></param>
		/// <returns></returns>
		[CanBeNull]
		public Pnt3D GetLinearIntersectionStartOnTarget([NotNull] Line3D targetSegment)
		{
			return GetLinearIntersectionStartOnTarget(targetSegment, out double _);
		}

		/// <summary>
		/// Gets the start point of the linear intersection, corresponding with
		/// <see cref="GetLinearIntersectionStart"/>, but on the target.
		/// </summary>
		/// <param name="targetSegment"></param>
		/// <param name="factorAlongTarget">The ratio of the point along the target.</param>
		/// <returns></returns>
		[CanBeNull]
		public Pnt3D GetLinearIntersectionStartOnTarget([NotNull] Line3D targetSegment,
		                                                out double factorAlongTarget)
		{
			if (LinearIntersectionInOppositeDirection)
			{
				if (TargetEndIntersects)
				{
					factorAlongTarget = 1;
					return targetSegment.EndPoint;
				}
			}
			else
			{
				if (TargetStartIntersects)
				{
					factorAlongTarget = 0;
					return targetSegment.StartPoint;
				}
			}

			if (SourceStartIntersects)
			{
				factorAlongTarget = _source1Factor;

				return targetSegment.GetPointAlong(factorAlongTarget, true);
			}

			factorAlongTarget = double.NaN;
			return null;
		}

		/// <summary>
		/// Creates the end point of the linear intersection along the source segment.
		/// </summary>
		/// <param name="sourceSegment">The source segment of this intersection.</param>
		/// <param name="endFactorAlongSource">The ratio along the source.</param>
		/// <returns></returns>
		public Pnt3D GetLinearIntersectionEnd([NotNull] Line3D sourceSegment,
		                                      out double endFactorAlongSource)
		{
			Pnt3D end;
			if (SourceEndIntersects)
			{
				end = sourceSegment.EndPoint;
				endFactorAlongSource = 1;
			}
			else
			{
				endFactorAlongSource =
					GetLinearIntersectionEndFactor(closestToSourceEnd: true);

				end = sourceSegment.GetPointAlong(endFactorAlongSource, true);
			}

			return end;
		}

		/// <summary>
		/// Gets the end point of the linear intersection, corresponding with
		/// <see cref="GetLinearIntersectionEnd"/>, but on the target.
		/// </summary>
		/// <param name="targetSegment"></param>
		/// <returns></returns>
		[CanBeNull]
		public Pnt3D GetLinearIntersectionEndOnTarget([NotNull] Line3D targetSegment)
		{
			return GetLinearIntersectionEndOnTarget(targetSegment, out double _);
		}

		/// <summary>
		/// Gets the end point of the linear intersection, corresponding with
		/// <see cref="GetLinearIntersectionEnd"/>, but on the target.
		/// </summary>
		/// <param name="targetSegment"></param>
		/// <param name="factorAlongTarget"></param>
		/// <returns></returns>
		[CanBeNull]
		public Pnt3D GetLinearIntersectionEndOnTarget([NotNull] Line3D targetSegment,
		                                              out double factorAlongTarget)
		{
			if (LinearIntersectionInOppositeDirection)
			{
				if (TargetStartIntersects)
				{
					factorAlongTarget = 0;
					return targetSegment.StartPoint;
				}
			}
			else
			{
				if (TargetEndIntersects)
				{
					factorAlongTarget = 1;
					return targetSegment.EndPoint;
				}
			}

			if (SourceEndIntersects)
			{
				factorAlongTarget = _source2Factor;
				return targetSegment.GetPointAlong(factorAlongTarget, true);
			}

			factorAlongTarget = double.NaN;
			return null;
		}

		/// <summary>
		/// Gets the intersection factor along the source of any, typically the only intersection point.
		/// </summary>
		/// <returns></returns>
		public double GetIntersectionPointFactorAlongSource()
		{
			if (SingleInteriorIntersectionFactor != null)
			{
				return SingleInteriorIntersectionFactor.Value;
			}

			if (SourceStartIntersects)
			{
				return 0;
			}

			if (SourceEndIntersects)
			{
				return 1;
			}

			double? targetStartIntersectionFactor = GetTargetStartIntersectionFactor();

			if (targetStartIntersectionFactor != null)
			{
				return targetStartIntersectionFactor.Value;
			}

			double? targetEndIntersectionFactor = GetTargetEndIntersectionFactor();

			if (targetEndIntersectionFactor != null)
			{
				return targetEndIntersectionFactor.Value;
			}

			throw new InvalidOperationException("No intersection");
		}

		/// <summary>
		/// Gets the intersection factor along the target of any, typically the only intersection point.
		/// </summary>
		/// <returns></returns>
		public double GetIntersectionPointFactorAlongTarget()
		{
			if (TargetStartIntersects)
			{
				return 0;
			}

			double? sourceFirstIntersectionFactor =
				GetSourceStartOrSingleIntersectionFactor();

			if (sourceFirstIntersectionFactor != null)
			{
				return sourceFirstIntersectionFactor.Value;
			}

			double? sourceEndIntersectionFactor = GetSourceEndIntersectionFactor();

			if (sourceEndIntersectionFactor != null)
			{
				return _source2Factor;
			}

			if (TargetEndIntersects)
			{
				return 1;
			}

			throw new InvalidOperationException("No intersection");
		}

		public double GetFirstIntersectionAlongSource()
		{
			if (SingleInteriorIntersectionFactor != null)
			{
				return SingleInteriorIntersectionFactor.Value;
			}

			if (SourceStartIntersects)
			{
				return 0;
			}

			if (GetTargetStartIntersectionFactor() == null &&
			    GetTargetEndIntersectionFactor() == null)
			{
				return SourceEndIntersects ? 1.0 : double.NaN;
			}

			return Math.Min(GetTargetStartIntersectionFactor() ?? double.MaxValue,
			                GetTargetEndIntersectionFactor() ?? double.MaxValue);
		}

		/// <summary>
		/// Determines whether the two paths cross (i.e. the intersection is a point) in this intersection.
		/// </summary>
		/// <param name="sourceSegments"></param>
		/// <param name="targetSegments"></param>
		/// <param name="tolerance"></param>
		/// <param name="targetDeviatesToLeft">If there is no crossing: Whether the target segment deviates 
		/// to the left of the source.</param>
		/// <returns></returns>
		public bool IsCrossingInPoint([NotNull] ISegmentList sourceSegments,
		                              [NotNull] ISegmentList targetSegments,
		                              double tolerance,
		                              out bool? targetDeviatesToLeft)
		{
			targetDeviatesToLeft = null;

			if (HasLinearIntersection)
			{
				return false;
			}

			if (SingleInteriorIntersectionFactor != null)
			{
				// interior/interior
				targetDeviatesToLeft = true;

				return true;
			}

			Line3D sourceLine = sourceSegments[SourceIndex];

			// Target start/end on source interior: check previous/next target segments
			if (TargetStartIsOnSourceInterior)
			{
				Line3D thisTargetSegment = targetSegments[TargetIndex];
				Line3D previousTargetSegment = targetSegments.PreviousSegment(TargetIndex);

				if (previousTargetSegment == null)
				{
					return false;
				}

				if (ArePointsOnDifferentSide(sourceLine,
				                             previousTargetSegment.StartPoint,
				                             thisTargetSegment.EndPoint, tolerance,
				                             out targetDeviatesToLeft))
				{
					return true;
				}
			}

			if (TargetEndIsOnSourceInterior)
			{
				// check the sides of the target segment and the next target segment
				Line3D thisTargetSegment = targetSegments[TargetIndex];
				Line3D nextTargetSegment = targetSegments.NextSegment(TargetIndex);

				if (nextTargetSegment == null)
				{
					return false;
				}

				if (ArePointsOnDifferentSide(sourceLine, thisTargetSegment.StartPoint,
				                             nextTargetSegment.EndPoint, tolerance,
				                             out targetDeviatesToLeft))
				{
					return true;
				}
			}

			if (SourceStartIntersects)
			{
				Line3D previousSource =
					sourceSegments.PreviousSegment(SourceIndex, true);

				return TargetCrossesBetweenSourceSegments(previousSource, sourceLine,
				                                          targetSegments,
				                                          tolerance,
				                                          out targetDeviatesToLeft);
			}

			if (SourceEndIntersects)
			{
				Line3D nextSource =
					sourceSegments.NextSegment(SourceIndex, true);

				return TargetCrossesBetweenSourceSegments(sourceLine, nextSource,
				                                          targetSegments,
				                                          tolerance,
				                                          out targetDeviatesToLeft);
			}

			return false;
		}

		/// <summary>
		/// Calculates the XY intersection line between the source and the target.
		/// The result has the Z-values of the source segment.
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		[CanBeNull]
		public Line3D TryGetIntersectionLine([NotNull] ISegmentList source)
		{
			if (! HasLinearIntersection)
			{
				return null;
			}

			Line3D sourceSegment = source[SourceIndex];

			return TryGetIntersectionLine(sourceSegment);
		}

		/// <summary>
		/// Calculates the XY intersection line between the source and the target.
		/// The result has the Z-values of the source segment.
		/// </summary>
		/// <param name="sourceSegment"></param>
		/// <returns></returns>
		[CanBeNull]
		public Line3D TryGetIntersectionLine(Line3D sourceSegment)
		{
			if (SegmentsAreEqualInXy)
			{
				return sourceSegment;
			}

			Pnt3D fromPoint = GetLinearIntersectionStart(sourceSegment, out double _);
			Pnt3D toPoint = GetLinearIntersectionEnd(sourceSegment, out double _);

			var segment = new Line3D(fromPoint, toPoint);

			if (segment.Length3D <= 0)
			{
				return null;
			}

			return segment;
		}

		public double GetRatioAlongTargetLinearStart()
		{
			if (TargetStartIntersects && ! LinearIntersectionInOppositeDirection)
			{
				return 0;
			}

			if (TargetEndIntersects && LinearIntersectionInOppositeDirection)
			{
				return 1;
			}

			double? sourceStartFactorAlongTarget =
				GetSourceStartOrSingleIntersectionFactor();

			if (sourceStartFactorAlongTarget != null)
			{
				return sourceStartFactorAlongTarget.Value;
			}

			double? sourceEndFactorAlongTarget = GetSourceEndIntersectionFactor();

			if (sourceEndFactorAlongTarget != null)
			{
				return sourceEndFactorAlongTarget.Value;
			}

			Assert.CantReach(
				"No along-target factor for linear start intersection found.");
			return -1;
		}

		public double GetRatioAlongTargetLinearEnd()
		{
			if (TargetEndIntersects && ! LinearIntersectionInOppositeDirection)
			{
				return 1;
			}

			if (TargetStartIntersects && LinearIntersectionInOppositeDirection)
			{
				return 0;
			}

			double? sourceEndFactorAlongTarget = GetSourceEndIntersectionFactor();

			if (sourceEndFactorAlongTarget != null)
			{
				return sourceEndFactorAlongTarget.Value;
			}

			double? sourceStartFactorAlongTarget =
				GetSourceStartOrSingleIntersectionFactor();

			if (sourceStartFactorAlongTarget != null)
			{
				return sourceStartFactorAlongTarget.Value;
			}

			Assert.CantReach("No along-target factor for linear end intersection found.");
			return -1;
		}

		public bool IsPotentialPseudoLinearIntersection([NotNull] Line3D sourceLine,
		                                                [NotNull] Line3D targetLine,
		                                                double tolerance)
		{
			// NOTE: Acute intersections can have 'pseudo-linear' intersections:
			//       These can result in incorrect touches calculation (same as ArcObjects).

			//   |
			//   |
			//   |\
			//     \
			//      \

			//   |\   this piece will be considered a linear intersecting if the tolerance
			//        is smaller than the lenght of | but large enough that the bottom point
			//        of the vertical line is within the tolerance of the \ line's interior!
			// 
			// The condition is (tolerance / sin(alpha)) > dist(intersection point along source) - source start/end)
			// that indicates if this might be a 'pseudo-linear' intersection.
			// -> If the adjacent segment has a (proper) linear intersection along the same
			//    stretch, the pseudo-linear intersection can be safely ignored. In TouchesXY() 
			//    probably all pseudo-linear intersections should be ignored?!

			if (SourceStartIntersects ^ SourceEndIntersects &&
			    TargetStartIntersects ^ TargetEndIntersects)
			{
				Pnt3D sourcePoint = SourceStartIntersects
					                    ? sourceLine.StartPoint
					                    : sourceLine.EndPoint;

				Pnt3D nonIntersectingTargetPnt =
					TargetStartIntersects ? targetLine.EndPoint : targetLine.StartPoint;

				if (SourceStartIntersects && _source1Factor > 0 && _source1Factor < 1)
				{
					Pnt3D touchPoint = GetLinearIntersectionEnd(sourceLine, out _);

					return IsBelowThreshold(sourcePoint, touchPoint, nonIntersectingTargetPnt,
					                        tolerance);
				}

				if (SourceEndIntersects && _source2Factor > 0 && _source2Factor < 1)
				{
					Pnt3D touchPoint = GetLinearIntersectionStart(sourceLine, out _);

					return IsBelowThreshold(sourcePoint, touchPoint, nonIntersectingTargetPnt,
					                        tolerance);
				}
			}

			return false;
		}

		private static bool IsBelowThreshold(Pnt3D sourcePoint, Pnt3D touchPoint,
		                                     Pnt3D nonIntersectingTargetPnt, double tolerance)
		{
			// Target starts (just?) after source, target touches source interior
			//Pnt3D touchPoint = targetLine.GetPointAlong(_source1Factor, true);
			double distanceFromStartToTouchPoint =
				touchPoint.GetDistance(sourcePoint, true);

			double alpha =
				GeomUtils.GetAngle2DInRad(sourcePoint, touchPoint, nonIntersectingTargetPnt);

			if (alpha < Math.PI / 2) // smaller 90°
			{
				double threshold = tolerance / Math.Sin(alpha);

				if (distanceFromStartToTouchPoint < threshold)
				{
					return true;
				}
			}

			return false;
		}

		public override string ToString()
		{
			return
				$"SourceIndex: {SourceIndex}; TargetIndex: {TargetIndex}; SourceStartIntersects: {SourceStartIntersects}; SourceEndIntersects: {SourceEndIntersects}; TargetIntersectionCount: {TargetIntersectionCount}";
		}

		private bool TargetCrossesBetweenSourceSegments([CanBeNull] Line3D sourceLine1,
		                                                [CanBeNull] Line3D sourceLine2,
		                                                [NotNull] ISegmentList targetSegments,
		                                                double tolerance,
		                                                out bool? targetDeviatesToLeft)
		{
			targetDeviatesToLeft = null;

			if (sourceLine1 == null || sourceLine2 == null)
			{
				return false;
			}

			Pnt3D targetBefore;
			Pnt3D targetAfter;
			GetNonIntersectingTargetPoints(targetSegments, sourceLine1, sourceLine2,
			                               tolerance,
			                               out targetBefore, out targetAfter);

			if (targetBefore == null && targetAfter == null)
			{
				return false;
			}

			bool isRightTurn = sourceLine1.IsLeftXY(sourceLine2.EndPoint) < 0;

			if (targetBefore != null && targetAfter != null)
			{
				bool beforeIsRight = IsRightOfVertex(isRightTurn, sourceLine1,
				                                     sourceLine2,
				                                     targetBefore);
				bool afterIsRight =
					IsRightOfVertex(isRightTurn, sourceLine1, sourceLine2, targetAfter);

				if (beforeIsRight != afterIsRight)
				{
					return true;
				}

				targetDeviatesToLeft = ! beforeIsRight;
			}
			else
			{
				if (targetBefore != null)
				{
					targetDeviatesToLeft =
						! IsRightOfVertex(isRightTurn, sourceLine1, sourceLine2,
						                  targetBefore);
				}
				else
				{
					targetDeviatesToLeft =
						! IsRightOfVertex(isRightTurn, sourceLine1, sourceLine2,
						                  targetAfter);
				}
			}

			return false;
		}

		private static bool IsRightOfVertex(bool isRightTurn,
		                                    [NotNull] Line3D lineBeforeVertex,
		                                    [NotNull] Line3D lineAfterVertex,
		                                    [NotNull] Pnt3D testPoint)
		{
			bool beforeIsRightOfL1 = lineBeforeVertex.IsLeftXY(testPoint) < 0;
			bool beforeIsRightOfL2 = lineAfterVertex.IsLeftXY(testPoint) < 0;

			bool beforeIsRight =
				IsRightOfVertex(isRightTurn, beforeIsRightOfL1, beforeIsRightOfL2);
			return beforeIsRight;
		}

		private static bool IsRightOfVertex(bool isRightTurn,
		                                    bool isRightOfPreviousSegment,
		                                    bool isRightOfNextSegment)
		{
			var toTheInside = true;

			if (isRightTurn)
			{
				if (! isRightOfPreviousSegment || ! isRightOfNextSegment)
				{
					// clockwise convex: both must be on the right
					toTheInside = false;
				}
			}
			else
			{
				// clockwise concave: must not be on the left of either
				if (! isRightOfPreviousSegment && ! isRightOfNextSegment)
				{
					toTheInside = false;
				}
			}

			return toTheInside;
		}

		private static bool ArePointsOnDifferentSide(Line3D ofLine, Pnt3D point1,
		                                             Pnt3D point2,
		                                             double tolerance,
		                                             out bool? anyPointAtLeft)
		{
			anyPointAtLeft = null;

			if (point1 == null || point2 == null)
			{
				return false;
			}

			double d1 = ofLine.GetDistanceXYPerpendicularSigned(point1);

			if (Math.Abs(d1) < tolerance)
			{
				d1 = 0;
			}

			double d2 = ofLine.GetDistanceXYPerpendicularSigned(point2);

			if (Math.Abs(d2) < tolerance)
			{
				d2 = 0;
			}

			anyPointAtLeft = d1 > 0 || d2 > 0;

			return d1 * d2 < 0;
		}

		/// <summary>
		/// Returns the points from the target that deviate from the source, i.e. that are on neither of the sourceLines.
		/// </summary>
		/// <param name="targetSegments"></param>
		/// <param name="sourceLine1"></param>
		/// <param name="sourceLine2"></param>
		/// <param name="xyTolerance"></param>
		/// <param name="before"></param>
		/// <param name="after"></param>
		private void GetNonIntersectingTargetPoints(ISegmentList targetSegments,
		                                            Line3D sourceLine1,
		                                            Line3D sourceLine2,
		                                            double xyTolerance,
		                                            [CanBeNull] out Pnt3D before,
		                                            [CanBeNull] out Pnt3D after)
		{
			Line3D thisTargetSegment = targetSegments[TargetIndex];

			before = null;
			after = null;
			double? targetStartIntersectionFactor = GetTargetStartIntersectionFactor();
			double? targetEndIntersectionFactor = GetTargetEndIntersectionFactor();

			if (targetStartIntersectionFactor == null &&
			    targetEndIntersectionFactor == null)
			{
				// The target interior intersects the source start
				before = thisTargetSegment.StartPoint;
				after = thisTargetSegment.EndPoint;
			}
			else if (targetStartIntersectionFactor != null)
			{
				Line3D previousTargetSegment = targetSegments.PreviousSegment(TargetIndex);

				if (previousTargetSegment != null)
				{
					before = previousTargetSegment.StartPoint;
					after = thisTargetSegment.EndPoint;
				}
			}
			else // targetEndIntersectionFactor != null
			{
				Line3D nextTargetSegment = targetSegments.NextSegment(TargetIndex);

				if (nextTargetSegment != null)
				{
					before = thisTargetSegment.StartPoint;
					after = nextTargetSegment.EndPoint;
				}
			}

			if (before != null)
			{
				if (sourceLine1.IntersectsPointXY(before, xyTolerance) ||
				    sourceLine2.IntersectsPointXY(before, xyTolerance))
				{
					before = null;
				}
			}

			if (after != null)
			{
				if (sourceLine1.IntersectsPointXY(after, xyTolerance) ||
				    sourceLine2.IntersectsPointXY(after, xyTolerance))
				{
					after = null;
				}
			}
		}

		private int GetIntersectingVerticesCount()
		{
			var result = 0;

			if (SourceStartIntersects)
				result++;

			if (SourceEndIntersects)
				result++;

			if (TargetStartIntersects)
				result++;

			if (TargetEndIntersects)
				result++;

			return result;
		}

		#region SegmentIntersection creation

		/// <summary>
		/// Collects the relevant information of the line vertices that intersect (i.e. are within the tolerance) of
		/// the other line.
		/// </summary>
		/// <param name="thisLine"></param>
		/// <param name="otherLine"></param>
		/// <param name="tolerance"></param>
		/// <param name="knownTargetEndFactor"></param>
		private void CollectVertexIntersectionInfos(
			[NotNull] Line3D thisLine,
			[NotNull] Line3D otherLine,
			double tolerance,
			double? knownTargetEndFactor = null)
		{
			_source1Factor = SegmentIntersectionUtils.GetPointFactorWithinLine(
				otherLine, thisLine.StartPoint, tolerance);

			// Consider removing extra property SourceStartIntersects and compute like this:
			//SingleInteriorIntersectionFactor == null && _source1Factor >= 0 && _source1Factor <= 1;
			SourceStartIntersects = _source1Factor >= 0 && _source1Factor <= 1;

			_source2Factor = SegmentIntersectionUtils.GetPointFactorWithinLine(
				otherLine, thisLine.EndPoint, tolerance);
			SourceEndIntersects = _source2Factor >= 0 && _source2Factor <= 1;

			double targetStartFactor = knownTargetEndFactor ??
			                           SegmentIntersectionUtils.GetPointFactorWithinLine(
				                           thisLine, otherLine.StartPoint, tolerance);
			if (! double.IsNaN(targetStartFactor))
			{
				TargetStartFactor = targetStartFactor;
			}

			double targetEndFactor = SegmentIntersectionUtils.GetPointFactorWithinLine(
				thisLine, otherLine.EndPoint, tolerance);

			if (! double.IsNaN(targetEndFactor))
			{
				TargetEndFactor = targetEndFactor;
			}

			// Determine if the linear intersection has opposite line direction
			if (! double.IsNaN(_source1Factor) && ! double.IsNaN(_source2Factor))
			{
				LinearIntersectionInOppositeDirection = _source1Factor > _source2Factor;
			}
			else if (! double.IsNaN(targetStartFactor) && ! double.IsNaN(targetEndFactor))
			{
				LinearIntersectionInOppositeDirection = targetStartFactor >
				                                        targetEndFactor;
			}
			else
			{
				if (! (double.IsNaN(targetStartFactor) && double.IsNaN(targetEndFactor)))
				{
					if (SourceStartIntersects)
					{
						LinearIntersectionInOppositeDirection =
							GetTargetStartIntersectionFactor() > 0;
					}
					else if (SourceEndIntersects)
					{
						LinearIntersectionInOppositeDirection =
							GetTargetEndIntersectionFactor() < 1;
					}
				}
			}
		}

		#endregion
	}
}
