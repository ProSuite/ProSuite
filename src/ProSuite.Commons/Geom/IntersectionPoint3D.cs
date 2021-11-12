using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
{
	public class IntersectionPoint3D
	{
		/// <summary>
		/// The intersection point taken from the source (i.e. with the source's Z value).
		/// </summary>
		public Pnt3D Point { get; }

		/// <summary>
		/// The source linestring index if the source was a multilinestring, or -1 if it was a linestring.
		/// </summary>
		public int SourcePartIndex { get; set; } = -1;

		/// <summary>
		/// The target linestring index if the target was a multilinestring, or -1 if it was a linestring.
		/// </summary>
		public int TargetPartIndex { get; set; } = -1;

		/// <summary>
		/// A virtual vertex index of the point in the source linestring on an ordinal scale.
		/// If the intersection point is between two vertices the ratio along the segment is added
		/// to the from-point's index.
		/// </summary>
		public double VirtualSourceVertex { get; set; }

		/// <summary>
		/// A virtual vertex index of the point in the target linestring on an ordinal scale.
		/// If the intersection point is between two vertices the ratio along the segment is added
		/// to the from-point's index.
		/// </summary>
		public double VirtualTargetVertex { get; set; } = double.NaN;

		public SegmentIntersection SegmentIntersection { get; }

		public IntersectionPointType Type { get; set; }

		public bool? TargetDeviatesToLeftOfSource { get; set; }

		public IntersectionPoint3D(
			[NotNull] Pnt3D point,
			double virtualRingVertexIndex,
			SegmentIntersection segmentIntersection = null,
			IntersectionPointType type = IntersectionPointType.Unknown)
		{
			Point = point;
			VirtualSourceVertex = virtualRingVertexIndex;
			SegmentIntersection = segmentIntersection;
			Type = type;
		}

		#region Factory methods

		[NotNull]
		public static IntersectionPoint3D CreateAreaInteriorIntersection(
			[NotNull] Pnt3D sourcePoint,
			int sourceIndex)
		{
			return new IntersectionPoint3D(sourcePoint, sourceIndex)
			       {
				       Type = IntersectionPointType.AreaInterior
			       };
		}

		[CanBeNull]
		public static IntersectionPoint3D CreatePointPointIntersection(
			[NotNull] IPnt sourcePoint,
			int sourceIndex,
			[NotNull] IPointList targetPoints,
			int targetIndex,
			double tolerance)
		{
			IPnt targetPoint = targetPoints.GetPoint(targetIndex);

			if (GeomUtils.GetDistanceSquaredXY(sourcePoint, targetPoint) > tolerance * tolerance)
			{
				return null;
			}

			IntersectionPoint3D result =
				new IntersectionPoint3D(new Pnt3D(sourcePoint), sourceIndex)
				{
					VirtualTargetVertex = targetIndex,
					Type = IntersectionPointType.TouchingInPoint
				};

			return result;
		}

		/// <summary>
		/// Create an intersection point originating from a point that intsersects a segment.
		/// </summary>
		/// <param name="sourcePoint">The source point</param>
		/// <param name="targetSegments">The target segment list</param>
		/// <param name="sourceIndex"></param>
		/// <param name="targetIndex">The (global) target segment index</param>
		/// <param name="tolerance"></param>
		/// <returns></returns>
		[CanBeNull]
		public static IntersectionPoint3D CreatePointLineIntersection(IPnt sourcePoint,
			ISegmentList targetSegments,
			int sourceIndex,
			int targetIndex,
			double tolerance)
		{
			Line3D targetSegment = targetSegments.GetSegment(targetIndex);

			int targetSegmentIndex = targetSegments.GetLocalSegmentIndex(
				targetIndex, out int targetPartIndex);

			double pointTargetFactor = SegmentIntersectionUtils.GetPointFactorWithinLine(
				targetSegment, sourcePoint, tolerance);

			if (double.IsNaN(pointTargetFactor))
			{
				return null;
			}

			// TODO: Clone also in other factory methods!?
			IntersectionPoint3D result =
				new IntersectionPoint3D(new Pnt3D(sourcePoint), sourceIndex)
				{
					VirtualTargetVertex = targetSegmentIndex + pointTargetFactor,
					Type = IntersectionPointType.TouchingInPoint
				};

			result.TargetPartIndex = targetPartIndex;

			return result;
		}

		/// <summary>
		/// Create an intersection point originating from a line that intsersects a point. The intersection
		/// point properties (XYZ) are taken from the source line.
		/// </summary>
		/// <param name="sourceSegments">The source segment list</param>
		/// <param name="targetPoint">The target point</param>
		/// <param name="sourceIndex">The (global) source segment index</param>
		/// <param name="targetIndex">The target point index</param>
		/// <param name="tolerance"></param>
		/// <returns></returns>
		[CanBeNull]
		public static IntersectionPoint3D CreateLinePointIntersection(
			ISegmentList sourceSegments,
			IPnt targetPoint,
			int sourceIndex,
			int targetIndex,
			double tolerance)
		{
			Line3D sourceSegment = sourceSegments.GetSegment(sourceIndex);

			int sourceSegmentIndex = sourceSegments.GetLocalSegmentIndex(
				sourceIndex, out int sourcePartIndex);

			double pointSourceFactor = SegmentIntersectionUtils.GetPointFactorWithinLine(
				sourceSegment, targetPoint, tolerance);

			if (double.IsNaN(pointSourceFactor))
			{
				return null;
			}

			Pnt3D sourcePoint = GetSourcePoint(sourceSegment, sourceSegmentIndex, pointSourceFactor,
			                                   out double virtualSourceIndex);

			// TODO: Clone also in other factory methods!?
			IntersectionPoint3D result = new IntersectionPoint3D(sourcePoint, virtualSourceIndex)
			                             {
				                             VirtualTargetVertex = targetIndex,
				                             Type = IntersectionPointType.TouchingInPoint
			                             };

			result.SourcePartIndex = sourcePartIndex;

			return result;
		}

		[NotNull]
		public static IntersectionPoint3D CreateSingleIntersectionPoint(
			[NotNull] SegmentIntersection intersection,
			[NotNull] ISegmentList sourceSegments,
			[NotNull] ISegmentList targetSegments,
			double tolerance)
		{
			Line3D sourceSegment = sourceSegments.GetSegment(intersection.SourceIndex);
			double? targetIntersectionFactorOnSource =
				intersection.GetIntersectionPointFactorAlongSource();

			int sourcePartIdx;
			int sourceSegmentIndex = sourceSegments.GetLocalSegmentIndex(
				intersection.SourceIndex, out sourcePartIdx);

			Pnt3D point;
			double sourceIndex;

			point = GetSourcePoint(sourceSegment, sourceSegmentIndex,
			                       targetIntersectionFactorOnSource.Value, out sourceIndex);

			IntersectionPoint3D result = new IntersectionPoint3D(point, sourceIndex, intersection)
			                             {
				                             SourcePartIndex = sourcePartIdx
			                             };

			bool? targetDeviatesToLeft;
			result.Type = intersection.IsCrossingInPoint(
				              sourceSegments, targetSegments, tolerance,
				              out targetDeviatesToLeft)
				              ? IntersectionPointType.Crossing
				              : IntersectionPointType.TouchingInPoint;

			result.TargetDeviatesToLeftOfSource = targetDeviatesToLeft;

			int targetPartIdx;

			result.VirtualTargetVertex = CalculateVirtualTargetVertex(
				targetSegments, result.Type, intersection, out targetPartIdx);

			result.TargetPartIndex = targetPartIdx;

			return result;
		}

		private static Pnt3D GetSourcePoint(Line3D sourceSegment, int sourceSegmentIndex,
		                                    double targetIntersectionFactorOnSource,
		                                    out double virtualSourceIndex)
		{
			Pnt3D point;
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (targetIntersectionFactorOnSource == 0)
			{
				point = sourceSegment.StartPointCopy;
				virtualSourceIndex = sourceSegmentIndex;
			}
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			else if (targetIntersectionFactorOnSource == 1)
			{
				point = sourceSegment.EndPointCopy;
				virtualSourceIndex = sourceSegmentIndex + 1;
			}
			else
			{
				point = sourceSegment.GetPointAlong(
					targetIntersectionFactorOnSource, true);
				virtualSourceIndex = sourceSegmentIndex +
				                     targetIntersectionFactorOnSource;
			}

			return point;
		}

		private static double CalculateVirtualTargetVertex(ISegmentList targetSegments,
		                                                   IntersectionPointType intersectionType,
		                                                   SegmentIntersection segmentIntersection,
		                                                   out int targetPartIndex)
		{
			int targetSegmentIndex = targetSegments.GetLocalSegmentIndex(
				segmentIntersection.TargetIndex, out targetPartIndex);

			double result;
			switch (intersectionType)
			{
				case IntersectionPointType.LinearIntersectionStart:
					result = targetSegmentIndex +
					         segmentIntersection.GetRatioAlongTargetLinearStart();
					break;
				case IntersectionPointType.LinearIntersectionEnd:
				case IntersectionPointType.LinearIntersectionIntermediate:
					result = targetSegmentIndex +
					         segmentIntersection.GetRatioAlongTargetLinearEnd();
					break;
				case IntersectionPointType.Crossing:
				case IntersectionPointType.TouchingInPoint:
					result = targetSegmentIndex +
					         segmentIntersection.GetIntersectionPointFactorAlongTarget();
					break;
				default:
					throw new InvalidOperationException(
						$"Unsupported type :{intersectionType}");
			}

			return result;
		}

		/// <summary>
		/// Creates the IntersectionPoint3D of type <see cref="IntersectionPointType.LinearIntersectionStart"/> 
		/// that corresponds to the first intersection along the source segment.
		/// </summary>
		/// <param name="intersection">The intersection</param>
		/// <param name="sourceSegments"></param>
		/// <param name="targetSegments">The target segment list.</param>
		/// <returns></returns>
		public static IntersectionPoint3D GetLinearIntersectionStart(
			[NotNull] SegmentIntersection intersection,
			[NotNull] ISegmentList sourceSegments,
			[NotNull] ISegmentList targetSegments)
		{
			Line3D sourceSegment = sourceSegments[intersection.SourceIndex];

			double startFactor;
			Pnt3D fromPoint =
				intersection.GetLinearIntersectionStart(sourceSegment, out startFactor);

			int sourcePartIdx;
			int sourceSegmentIndex = sourceSegments.GetLocalSegmentIndex(
				intersection.SourceIndex, out sourcePartIdx);

			var result = new IntersectionPoint3D(
				             fromPoint,
				             sourceSegmentIndex + startFactor,
				             intersection)
			             {
				             Type = IntersectionPointType.LinearIntersectionStart,
				             SourcePartIndex = sourcePartIdx
			             };

			int targetPartIdx;
			result.VirtualTargetVertex = CalculateVirtualTargetVertex(
				targetSegments, result.Type, intersection, out targetPartIdx);

			result.TargetPartIndex = targetPartIdx;

			return result;
		}

		/// <summary>
		/// Creates the IntersectionPoint3D of type <see cref="IntersectionPointType.LinearIntersectionEnd"/> 
		/// that corresponds to the second intersection along the source segment.
		/// </summary>
		/// <param name="intersection">The intersection</param>
		/// <param name="sourceSegments"></param>
		/// <param name="targetSegments"></param>
		/// <returns></returns>
		public static IntersectionPoint3D GetLinearIntersectionEnd(
			[NotNull] SegmentIntersection intersection,
			[NotNull] ISegmentList sourceSegments,
			[NotNull] ISegmentList targetSegments)
		{
			Line3D sourceSegment = sourceSegments[intersection.SourceIndex];

			double endFactor;
			Pnt3D toPoint =
				intersection.GetLinearIntersectionEnd(sourceSegment, out endFactor);

			int sourcePartIdx;
			int sourceSegmentIndex = sourceSegments.GetLocalSegmentIndex(
				intersection.SourceIndex, out sourcePartIdx);

			IntersectionPoint3D result =
				new IntersectionPoint3D(
					toPoint, sourceSegmentIndex + endFactor, intersection)
				{
					Type = IntersectionPointType.LinearIntersectionEnd,
					SourcePartIndex = sourcePartIdx
				};

			int targetPartIdx;
			result.VirtualTargetVertex = CalculateVirtualTargetVertex(
				targetSegments, result.Type, intersection, out targetPartIdx);

			result.TargetPartIndex = targetPartIdx;

			return result;
		}

		#endregion

		public bool IsSourceVertex()
		{
			return MathUtils.AreEqual(VirtualSourceVertex % 1, 0);
		}

		public bool IsTargetVertex()
		{
			return MathUtils.AreEqual(VirtualTargetVertex % 1, 0);
		}

		public bool IsLinearIntersectionStartAtStartPoint(Linestring source)
		{
			return Type == IntersectionPointType.LinearIntersectionStart &&
			       source.StartPoint.Equals(Point);
		}

		public bool IsLinearIntersectionEndAtEndPoint(Linestring source)
		{
			return Type == IntersectionPointType.LinearIntersectionEnd &&
			       source.EndPoint.Equals(Point);
		}

		public bool IsAtTargetRingEndPoint(ISegmentList target)
		{
			Linestring targetPart = target.GetPart(TargetPartIndex);

			// ReSharper disable once CompareOfFloatsByEqualityOperator
			return targetPart.PointCount - 1 == VirtualTargetVertex && target.IsClosed;
		}

		public bool IsAtSourceRingEndPoint(ISegmentList source)
		{
			Linestring sourcePart = source.GetPart(SourcePartIndex);

			// ReSharper disable once CompareOfFloatsByEqualityOperator
			return sourcePart.PointCount - 1 == VirtualSourceVertex && source.IsClosed;
		}

		public int GetLocalSourceIntersectionSegmentIdx(Linestring source,
		                                                out double distanceAlongAsRatio)
		{
			return GetLocalIntersectionSegmentIdx(source, VirtualSourceVertex,
			                                      out distanceAlongAsRatio);
		}

		public int GetLocalTargetIntersectionSegmentIdx(Linestring target,
		                                                out double distanceAlongAsRatio)
		{
			return GetLocalIntersectionSegmentIdx(target, VirtualTargetVertex,
			                                      out distanceAlongAsRatio);
		}

		/// <summary>
		/// Gets the intersection point taken from the target (i.e. with the target's Z value).
		/// The X,Y coordinates can be different to the <see cref="Point"/> by up to the tolerance.
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		public Pnt3D GetTargetPoint(ISegmentList target)
		{
			Linestring targetPart = target.GetPart(TargetPartIndex);

			int segmentIndex = GetLocalTargetIntersectionSegmentIdx(targetPart, out double factor);

			return targetPart.GetSegment(segmentIndex).GetPointAlong(factor, true);
		}

		public int GetNextRingVertexIndex(int vertexCount)
		{
			if (! IsSourceVertex())
			{
				return (int) Math.Ceiling(VirtualSourceVertex);
			}

			var vertexIndex = (int) VirtualSourceVertex;

			int nextIndex = vertexIndex == vertexCount - 1 ? 1 : vertexIndex + 1;

			return nextIndex;
		}

		public int GetPreviousRingVertexIndex(int vertexCount)
		{
			if (! IsSourceVertex())
			{
				return (int) Math.Floor(VirtualSourceVertex);
			}

			var vertexIndex = (int) VirtualSourceVertex;

			int previousIndex = vertexIndex == 0 ? vertexCount - 2 : vertexIndex - 1;

			return previousIndex;
		}

		public Pnt3D GetNonIntersectingSourcePoint([NotNull] Linestring sourceRing,
		                                           double distanceFromIntersectionAsRatio)
		{
			Assert.NotNull(SegmentIntersection);

			double factor;

			var searchForward = true;

			if (Type == IntersectionPointType.LinearIntersectionStart)
			{
				searchForward = false;
				factor = SegmentIntersection.GetLinearIntersectionStartFactor(true);
			}
			else if (Type == IntersectionPointType.LinearIntersectionEnd)
			{
				factor = SegmentIntersection.GetLinearIntersectionEndFactor(true);
			}
			else
			{
				factor = SegmentIntersection.GetIntersectionPointFactorAlongSource();
			}

			Line3D segment = sourceRing[SegmentIntersection.SourceIndex];

			if (searchForward)
			{
				factor += distanceFromIntersectionAsRatio;
			}
			else
			{
				factor -= distanceFromIntersectionAsRatio;
			}

			if (factor >= 1)
			{
				segment =
					sourceRing[
						sourceRing.NextIndexInRing(SegmentIntersection.SourceIndex)];
				factor -= 1;
			}
			else if (factor < 0)
			{
				segment =
					sourceRing[
						sourceRing.PreviousIndexInRing(SegmentIntersection.SourceIndex)];
				factor += 1;
			}

			return segment.GetPointAlong(factor, true);
		}

		[CanBeNull]
		public Pnt3D GetNonIntersectingTargetPoint(
			[NotNull] Linestring targetRing,
			double distanceFromIntersectionAsRatio)
		{
			if (Type == IntersectionPointType.LinearIntersectionIntermediate ||
			    Type == IntersectionPointType.Unknown)
			{
				throw new InvalidOperationException(
					$"Cannot get the non-intersecting target point for an intersection point type {Type}");
			}

			bool? deviationIsBackward = null;
			if (Type == IntersectionPointType.LinearIntersectionStart ||
			    Type == IntersectionPointType.LinearIntersectionEnd)
			{
				// the direction matters:
				deviationIsBackward =
					Type == IntersectionPointType.LinearIntersectionStart;

				if (SegmentIntersection.LinearIntersectionInOppositeDirection)
				{
					deviationIsBackward = ! deviationIsBackward;
				}
			}

			double targetFactor;
			int targetSegmentIdx =
				GetLocalTargetIntersectionSegmentIdx(targetRing, out targetFactor);

			double adjustedTargetFactor;
			if (deviationIsBackward == true)
			{
				adjustedTargetFactor = targetFactor - distanceFromIntersectionAsRatio;
			}
			else
			{
				// forward or null (i.e. does not matter)
				adjustedTargetFactor = targetFactor + distanceFromIntersectionAsRatio;

				if (adjustedTargetFactor > 1)
				{
					int? nextIdx = targetRing.NextSegmentIndex(targetSegmentIdx);
					if (nextIdx != null)
					{
						targetSegmentIdx = nextIdx.Value;
						adjustedTargetFactor -= 1;
					}
					else if (deviationIsBackward == null)
					{
						// cannot go forward, try backward:
						adjustedTargetFactor =
							targetFactor - distanceFromIntersectionAsRatio;
					}
				}
			}

			if (adjustedTargetFactor < 0)
			{
				int? previousIdx = targetRing.PreviousSegmentIndex(targetSegmentIdx);
				if (previousIdx == null)
				{
					return null;
				}

				targetSegmentIdx = previousIdx.Value;
				adjustedTargetFactor += 1;
			}

			return targetRing[targetSegmentIdx].GetPointAlong(adjustedTargetFactor, true);
		}

		public bool? TargetDeviatesToLeftOfSourceRing([NotNull] Linestring sourceRing,
		                                              [NotNull] Linestring targetRing)
		{
			if (TargetDeviatesToLeftOfSource != null)
			{
				return TargetDeviatesToLeftOfSource;
			}

			Pnt3D nonIntersectingTargetPnt =
				Assert.NotNull(GetNonIntersectingTargetPoint(targetRing, 0.5));

			TargetDeviatesToLeftOfSource =
				! IsOnTheRightSide(sourceRing, nonIntersectingTargetPnt, true);

			return TargetDeviatesToLeftOfSource;
		}

		/// <summary>
		/// Attempts to get the target segment index that has a start- or endpoint which does
		/// not intersect the source (from the local perspective of this segment intersection).
		/// </summary>
		/// <param name="target">The target linestring</param>
		/// <param name="forwardAlongTarget">Whether the non-intesecting point should be found
		/// by going forward along the target or backward.</param>
		/// <returns></returns>
		[CanBeNull]
		public int? GetNonIntersectingTargetSegmentIndex(
			[NotNull] ISegmentList target,
			bool forwardAlongTarget)
		{
			if (SegmentIntersection.HasLinearIntersection)
			{
				if (Type == IntersectionPointType.LinearIntersectionIntermediate)
				{
					return null;
				}

				// search the first non-intersecting point at the and of the linear intersection
				bool deviationIsBackward =
					Type == IntersectionPointType.LinearIntersectionStart;

				if (SegmentIntersection.LinearIntersectionInOppositeDirection)
				{
					deviationIsBackward = ! deviationIsBackward;
				}

				if (deviationIsBackward && forwardAlongTarget ||
				    ! deviationIsBackward && ! forwardAlongTarget)
				{
					return null;
				}

				// get the first non-intersecting point after the linear intersection, check its side:
				if (SegmentIntersection.TargetStartIntersects &&
				    SegmentIntersection.TargetEndIntersects)
				{
					// Target is within source (or equal to source)
					return forwardAlongTarget
						       ? target.NextSegmentIndex(SegmentIntersection.TargetIndex)
						       : target.PreviousSegmentIndex(SegmentIntersection.TargetIndex);
				}

				if (! SegmentIntersection.TargetStartIntersects &&
				    ! SegmentIntersection.TargetEndIntersects)
				{
					// Source is completely within target
					return SegmentIntersection.TargetIndex;
				}

				// Either the source start or the source end intersects but not both
				// The same must be the case for the target
				Assert.True(
					SegmentIntersection.TargetEndIntersects ^
					SegmentIntersection.TargetStartIntersects,
					"Either target start or end is expected to intersect");
			}

			if (SegmentIntersection.TargetStartIntersects)
			{
				return forwardAlongTarget
					       ? SegmentIntersection.TargetIndex
					       : target.PreviousSegmentIndex(SegmentIntersection.TargetIndex);
			}

			if (SegmentIntersection.TargetEndIntersects)
			{
				return forwardAlongTarget
					       ? target.NextSegmentIndex(SegmentIntersection.TargetIndex)
					       : SegmentIntersection.TargetIndex;
			}

			// By now all linear cases should have been handled
			Assert.False(SegmentIntersection.HasLinearIntersection,
			             "Not all linear cases were handled.");

			return SegmentIntersection.TargetIndex;
		}

		public bool IsOnTheRightSide([NotNull] ISegmentList source,
		                             [NotNull] Pnt3D testPoint,
		                             bool disregardOrientation = false)
		{
			Assert.True(source.IsClosed, "Source must be closed ring(s)");

			Linestring sourceRing = source.GetPart(SourcePartIndex);

			double sourceRatio;
			int sourceSegmentIdx =
				GetLocalSourceIntersectionSegmentIdx(sourceRing, out sourceRatio);

			Line3D sourceSegment = sourceRing[sourceSegmentIdx];

			if (sourceRatio > 0 && sourceRatio < 1)
			{
				// The intersection is on the source segment's interior
				return sourceSegment.IsLeftXY(testPoint) < 0;
			}

			Line3D previousSegment, nextSegment;

			// Intersection at source vertex 0 or 1 -> get the 2 adjacent segments
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (sourceRatio == 0)
			{
				previousSegment =
					sourceRing.PreviousSegmentInRing(sourceSegmentIdx, true);

				nextSegment = SegmentIntersection.IsSourceZeroLength2D
					              ? sourceRing.NextSegmentInRing(sourceSegmentIdx, true)
					              : sourceSegment;
			}
			else // sourceRatio == 1
			{
				previousSegment = SegmentIntersection.IsSourceZeroLength2D
					                  ? sourceRing.PreviousSegmentInRing(
						                  sourceSegmentIdx, true)
					                  : sourceSegment;
				nextSegment = sourceRing.NextSegmentInRing(sourceSegmentIdx, true);
			}

			bool result = GeomTopoOpUtils.IsOnTheRightSide(previousSegment.StartPoint, Point,
			                                               nextSegment.EndPoint, testPoint);

			if (! disregardOrientation && sourceRing.ClockwiseOriented == false)
			{
				result = ! result;
			}

			return result;
		}

		public bool? IsOnTheRightSideOfTarget(ISegmentList target,
		                                      IPnt testPoint,
		                                      bool disregardOrientation = false)
		{
			Assert.True(target.IsClosed, "Target must be closed ring(s)");

			Linestring targetRing = target.GetPart(TargetPartIndex);

			double targetRatio;
			int targetSegmentIdx =
				GetLocalTargetIntersectionSegmentIdx(targetRing, out targetRatio);

			Line3D targetSegment = targetRing[targetSegmentIdx];

			if (targetRatio > 0 && targetRatio < 1)
			{
				// The intersection is on the target segment's interior
				return targetSegment.IsLeftXY(testPoint) < 0;
			}

			Line3D previousSegment, nextSegment;

			// Intersection at source vertex 0 or 1 -> get the 2 adjacent segments
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (targetRatio == 0)
			{
				previousSegment =
					targetRing.PreviousSegmentInRing(targetSegmentIdx, true);

				nextSegment = SegmentIntersection.IsTargetZeroLength2D
					              ? targetRing.NextSegmentInRing(targetSegmentIdx, true)
					              : targetSegment;
			}
			else // sourceRatio == 1
			{
				previousSegment = SegmentIntersection.IsTargetZeroLength2D
					                  ? targetRing.PreviousSegmentInRing(
						                  targetSegmentIdx, true)
					                  : targetSegment;
				nextSegment = targetRing.NextSegmentInRing(targetSegmentIdx, true);
			}

			bool result = GeomTopoOpUtils.IsOnTheRightSide(previousSegment.StartPoint, Point,
			                                               nextSegment.EndPoint, testPoint);

			if (! disregardOrientation && targetRing.ClockwiseOriented == false)
			{
				result = ! result;
			}

			return result;
		}

		public bool? SourceContinuesInbound([NotNull] ISegmentList source,
		                                    [NotNull] ISegmentList targetArea)
		{
			Linestring sourceLinestring = source.GetPart(SourcePartIndex);
			Linestring targetRing = targetArea.GetPart(TargetPartIndex);

			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (VirtualSourceVertex == sourceLinestring.PointCount - 1 &&
			    ! sourceLinestring.IsClosed)
			{
				// Last point, cannot continue
				return null;
			}

			if (Type == IntersectionPointType.LinearIntersectionStart ||
			    Type == IntersectionPointType.LinearIntersectionIntermediate)
			{
				// Continues along target
				return null;
			}

			if (Type == IntersectionPointType.AreaInterior)
			{
				// Hard to know but assuming simple geometry, the interior is on the right:
				return true;
			}

			double factor;
			if (Type == IntersectionPointType.LinearIntersectionEnd)
			{
				factor = SegmentIntersection.GetLinearIntersectionEndFactor(true);
			}
			else
			{
				factor = SegmentIntersection.GetIntersectionPointFactorAlongSource();
			}

			Line3D segment;

			if (factor < 1)
			{
				segment = sourceLinestring[SegmentIntersection.SourceIndex];
			}
			else
			{
				segment = sourceLinestring.NextSegment(SegmentIntersection.SourceIndex);
			}

			Pnt3D sourceContinuationPoint = Assert.NotNull(segment).EndPoint;

			return IsOnTheRightSideOfTarget(targetRing, sourceContinuationPoint);
		}

		private static int GetLocalIntersectionSegmentIdx([NotNull] Linestring forSegments,
		                                                  double virtualVertexIndex,
		                                                  out double distanceAlongAsRatio)
		{
			Assert.ArgumentNotNull(forSegments, nameof(forSegments));
			Assert.ArgumentNotNaN(virtualVertexIndex, nameof(virtualVertexIndex));

			var localSegmentIdx = (int) Math.Truncate(virtualVertexIndex);

			distanceAlongAsRatio = virtualVertexIndex - localSegmentIdx;

			if (forSegments.IsLastPointInPart(localSegmentIdx) && distanceAlongAsRatio >= 0)
			{
				// out of segment bounds:
				if (forSegments.IsClosed)
				{
					localSegmentIdx = 0;
				}
				else
				{
					// last segment, end point:
					localSegmentIdx -= 1;
					distanceAlongAsRatio += 1;
				}
			}

			return localSegmentIdx;
		}

		public override string ToString()
		{
			return $"Point: {Point}, " +
			       $"Type: {Type}, " +
			       $"RingVertex: {VirtualSourceVertex}, " +
			       $"TargetVertex: {VirtualTargetVertex}";
		}
	}
}
