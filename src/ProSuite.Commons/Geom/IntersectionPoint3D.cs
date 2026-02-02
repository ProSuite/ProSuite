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

		private bool? TargetDeviatesToLeftOfSource { get; set; }

		private bool? _linearIntersectionInOppositeDirection;

		public bool? LinearIntersectionInOppositeDirection
		{
			get
			{
				if (_linearIntersectionInOppositeDirection.HasValue)
				{
					return _linearIntersectionInOppositeDirection;
				}

				if (SegmentIntersection.IsSegmentZeroLength2D)
				{
					return null;
				}

				return SegmentIntersection.LinearIntersectionInOppositeDirection;
			}
			set => _linearIntersectionInOppositeDirection = value;
		}

		// TODO: These properties should probably be determined on the fly by the
		// IntersectionPointNavigator or by the IntersectionClusters class
		public bool DisallowTargetForward { get; set; }
		public bool DisallowTargetBackward { get; set; }

		public bool DisallowSourceForward { get; set; }
		public bool DisallowSourceBackward { get; set; }

		public IntersectionPoint3D(
			[NotNull] Pnt3D point,
			double virtualSourceVertexIdx,
			SegmentIntersection segmentIntersection = null,
			IntersectionPointType type = IntersectionPointType.Unknown)
		{
			Point = point;
			VirtualSourceVertex = virtualSourceVertexIdx;
			SegmentIntersection = segmentIntersection;
			Type = type;
		}

		#region Factory methods

		[NotNull]
		public static IntersectionPoint3D CreateAreaInteriorIntersection(
			[NotNull] Pnt3D sourcePoint,
			int sourceVertexIndex,
			int sourcePartIndex)
		{
			return new IntersectionPoint3D(sourcePoint, sourceVertexIndex)
			       {
				       Type = IntersectionPointType.AreaInterior,
				       SourcePartIndex = sourcePartIndex
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
		/// Create an intersection point originating from a point that intersects a segment.
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
		/// Create an intersection point originating from a line that intersects a point. The intersection
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

		public bool IsTargetVertex(out int localVertexIndex)
		{
			double remainder = VirtualTargetVertex % 1;

			bool result = MathUtils.AreEqual(remainder, 0);

			if (result)
			{
				localVertexIndex = (int) VirtualTargetVertex;
			}
			else
			{
				localVertexIndex = -1;
			}

			return result;
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

		public bool ReferencesSameSourceVertex([CanBeNull] IntersectionPoint3D other,
		                                       ISegmentList source,
		                                       double tolerance = double.NaN)
		{
			if (other == null)
			{
				return false;
			}

			if (other.SourcePartIndex != SourcePartIndex)
			{
				return false;
			}

			if (VirtualSourceVertex == 0 &&
			    other.IsAtSourceRingEndPoint(source))
			{
				return true;
			}

			if (other.VirtualSourceVertex == 0 &&
			    IsAtSourceRingEndPoint(source))
			{
				return true;
			}

			double delta = Math.Abs(VirtualSourceVertex - other.VirtualSourceVertex);

			// Quick test: if they are more than 2 vertices away from each other -> false
			if (delta > 2)
			{
				return false;
			}

			if (delta <= double.Epsilon)
			{
				// It does not make a difference whether measuring the ratio or absolute distance:
				return true;
			}

			// Proper test:
			Linestring sourcePart = source.GetPart(SourcePartIndex);

			// This might not be optimal in a ring where one is 0 and the other just below segment count
			int lowerSegmentIdx = (int) Math.Min(other.VirtualSourceVertex, VirtualSourceVertex);

			double distanceAlongBetween =
				GetDistanceAlong(sourcePart, VirtualSourceVertex, lowerSegmentIdx) -
				GetDistanceAlong(sourcePart, other.VirtualSourceVertex, lowerSegmentIdx);

			double alongDistance = Math.Abs(distanceAlongBetween);

			if (double.IsNaN(tolerance))
			{
				tolerance = MathUtils.GetDoubleSignificanceEpsilon(Point.X, Point.Y);
			}

			return alongDistance < tolerance;
		}

		public bool ReferencesSameTargetVertex([CanBeNull] IntersectionPoint3D other,
		                                       ISegmentList target,
		                                       double tolerance = double.Epsilon)
		{
			if (other == null)
			{
				return false;
			}

			if (other.TargetPartIndex != TargetPartIndex)
			{
				return false;
			}

			if (VirtualTargetVertex == 0 &&
			    other.IsAtTargetRingEndPoint(target))
			{
				return true;
			}

			if (other.VirtualTargetVertex == 0 &&
			    IsAtTargetRingEndPoint(target))
			{
				return true;
			}

			double delta = Math.Abs(VirtualTargetVertex - other.VirtualTargetVertex);

			// Quick test: if they are more than 2 vertices away from each other -> false
			if (delta > 2)
			{
				return false;
			}

			if (delta <= double.Epsilon)
			{
				// It does not make a difference whether measuring the ratio or absolute distance:
				return true;
			}

			// Proper test:
			Linestring targetPart = target.GetPart(TargetPartIndex);

			// This might not be optimal in a ring where one is 0 and the other just below segment count
			int lowerSegmentIdx = (int) Math.Min(other.VirtualTargetVertex, VirtualTargetVertex);

			double distanceAlongBetween =
				GetDistanceAlong(targetPart, VirtualTargetVertex, lowerSegmentIdx) -
				GetDistanceAlong(targetPart, other.VirtualTargetVertex, lowerSegmentIdx);

			double alongDistance = Math.Abs(distanceAlongBetween);

			return alongDistance < tolerance;
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
		public Pnt3D GetTargetPoint([NotNull] ISegmentList target)
		{
			Linestring targetPart = target.GetPart(TargetPartIndex);

			int segmentIndex = GetLocalTargetIntersectionSegmentIdx(targetPart, out double factor);

			return targetPart.GetSegment(segmentIndex).GetPointAlong(factor, true);
		}

		public IPnt GetTargetPoint(IPointList targetPoints)
		{
			Assert.True(IsTargetVertex(out _), "");

			int targetVertex = (int) VirtualTargetVertex;

			return targetPoints.GetPoint(targetVertex);
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
				deviationIsBackward = Type == IntersectionPointType.LinearIntersectionStart;

				if (LinearIntersectionInOppositeDirection == true)
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

		//public bool? TargetDeviatesToLeftOfSourceRing([NotNull] Linestring sourceRing,
		//                                              [NotNull] Linestring targetRing,
		//                                              double tolerance = 0)
		//{
		//	if (TargetDeviatesToLeftOfSource != null)
		//	{
		//		return TargetDeviatesToLeftOfSource;
		//	}

		//	// TODO:
		//	// This is completely wrong!!! -> Make sure the non-intersecting target point 
		//	Pnt3D nonIntersectingTargetPnt =
		//		Assert.NotNull(GetNonIntersectingTargetPoint(targetRing, 0.5));

		//	TargetDeviatesToLeftOfSource =
		//		! IsOnTheRightSide(sourceRing, nonIntersectingTargetPnt, tolerance);

		//	return TargetDeviatesToLeftOfSource;
		//}

		/// <summary>
		/// Attempts to get the target segment index that has a start- or endpoint which does
		/// not intersect the source (from the local perspective of this segment intersection).
		/// </summary>
		/// <param name="target">The target linestring</param>
		/// <param name="forwardAlongTarget">Whether the non-intersecting point should be found
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

				// If it cannot be determined if the segments run in opposite direction there are
				// potentially several zero-length segments that should probably be handled by
				// the caller.
				if (LinearIntersectionInOppositeDirection != null)
				{
					if (LinearIntersectionInOppositeDirection == true)
					{
						deviationIsBackward = ! deviationIsBackward;
					}

					if (deviationIsBackward && forwardAlongTarget ||
					    ! deviationIsBackward && ! forwardAlongTarget)
					{
						return null;
					}
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

		/// <summary>
		/// Determines whether this intersection point represents a true 3D intersection, i.e.
		/// whether the source and target geometries not only intersect in the XY projection
		/// but the vertical distance is also within the tolerance (cylinder).
		/// </summary>
		/// <param name="target">The target geometry</param>
		/// <param name="zTolerance">The Z tolerance for comparison</param>
		/// <returns>True if the Z values at the intersection are within tolerance.
		/// Null if one of the Z values is NaN</returns>
		public bool? Is3dIntersection(ISegmentList target, double zTolerance)
		{
			// Source Z is already in Point.Z
			double sourceZ = Point.Z;

			// Get target Z at the intersection location
			Pnt3D targetPoint = GetTargetPoint(target);
			double targetZ = targetPoint.Z;

			if (double.IsNaN(sourceZ) || double.IsNaN(targetZ))
			{
				// Undefined Z, probably not Z-aware data
				return null;
			}

			// Check if Z values are within tolerance
			return Math.Abs(sourceZ - targetZ) <= zTolerance;
		}

		/// <summary>
		/// Classifies the source trajectory with respect to this intersection.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="target"></param>
		/// <param name="sourceContinuesToRightSide"></param>
		/// <param name="sourceArrivesFromRightSide"></param>
		/// <param name="tolerance"></param>
		public void ClassifySourceTrajectory([NotNull] ISegmentList source,
		                                     [NotNull] ISegmentList target,
		                                     out bool? sourceContinuesToRightSide,
		                                     out bool? sourceArrivesFromRightSide,
		                                     double tolerance)
		{
			Assert.False(Type == IntersectionPointType.Unknown,
			             "Cannot classify unknown intersection type.");

			sourceContinuesToRightSide = SourceContinuesToRightSide(source, target, tolerance);

			sourceArrivesFromRightSide = SourceArrivesFromRightSide(source, target, tolerance);
		}

		/// <summary>
		/// Classifies the target trajectory with respect to this intersection.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="target"></param>
		/// <param name="targetContinuesToRightSide"></param>
		/// <param name="targetArrivesFromRightSide"></param>
		/// <param name="tolerance"></param>
		public void ClassifyTargetTrajectory([NotNull] ISegmentList source,
		                                     [NotNull] ISegmentList target,
		                                     out bool? targetContinuesToRightSide,
		                                     out bool? targetArrivesFromRightSide,
		                                     double tolerance = 0)
		{
			Assert.False(Type == IntersectionPointType.Unknown,
			             "Cannot classify unknown intersection type.");

			targetContinuesToRightSide = TargetContinuesToRightSide(source, target, tolerance);

			targetArrivesFromRightSide = TargetArrivesFromRightSide(source, target, tolerance);
		}

		public bool? TargetContinuesToRightSide([NotNull] ISegmentList source,
		                                        [NotNull] ISegmentList target,
		                                        double tolerance = 0)
		{
			Assert.False(Type == IntersectionPointType.Unknown,
			             "Cannot classify unknown intersection type.");

			if (Type == IntersectionPointType.LinearIntersectionIntermediate)
			{
				return null;
			}

			int? nextTargetSegment =
				GetNonIntersectingTargetSegmentIndex(target, forwardAlongTarget: true);

			Pnt3D nextPntAlongTarget = nextTargetSegment == null
				                           ? null
				                           : target[nextTargetSegment.Value].EndPoint;

			bool? targetContinuesToRightSide = null;

			if (nextPntAlongTarget != null)
			{
				targetContinuesToRightSide =
					IsOnTheRightSideOfSource(source, nextPntAlongTarget, tolerance);
			}

			return targetContinuesToRightSide;
		}

		public bool? TargetArrivesFromRightSide([NotNull] ISegmentList source,
		                                        [NotNull] ISegmentList target,
		                                        double tolerance = 0)
		{
			Assert.False(Type == IntersectionPointType.Unknown,
			             "Cannot classify unknown intersection type.");

			if (Type == IntersectionPointType.LinearIntersectionIntermediate)
			{
				return null;
			}

			int? previousTargetSegment =
				GetNonIntersectingTargetSegmentIndex(target, forwardAlongTarget: false);

			Pnt3D previousPntAlongTarget = previousTargetSegment == null
				                               ? null
				                               : target[previousTargetSegment.Value].StartPoint;

			bool? targetArrivesFromRightSide = null;
			if (previousPntAlongTarget != null)
			{
				targetArrivesFromRightSide =
					IsOnTheRightSideOfSource(source, previousPntAlongTarget, tolerance);
			}

			return targetArrivesFromRightSide;
		}

		public bool? SourceContinuesInbound([NotNull] ISegmentList source,
		                                    [NotNull] ISegmentList targetArea)
		{
			bool? result = SourceContinuesToRightSide(source, targetArea);

			Linestring targetRing = targetArea.GetPart(TargetPartIndex);

			if (targetRing.ClockwiseOriented == false)
			{
				return ! result;
			}

			return result;
		}

		public bool? SourceContinuesToRightSide([NotNull] ISegmentList source,
		                                        [NotNull] ISegmentList targetArea,
		                                        double tolerance = 0)
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

			int sourceSegmentIdx =
				GetLocalSourceIntersectionSegmentIdx(sourceLinestring, out double factor);

			Line3D segment = factor < 1
				                 ? sourceLinestring[sourceSegmentIdx]
				                 : sourceLinestring.NextSegment(sourceSegmentIdx);

			Pnt3D sourceContinuationPoint = Assert.NotNull(segment).EndPoint;

			return IsOnTheRightSideOfTarget(targetRing, sourceContinuationPoint, tolerance);
		}

		public bool? SourceArrivesFromRightSide([NotNull] ISegmentList source,
		                                        [NotNull] ISegmentList target,
		                                        double tolerance = 0)
		{
			Linestring sourceLinestring = source.GetPart(SourcePartIndex);
			Linestring targetLinestring = target.GetPart(TargetPartIndex);

			if (VirtualSourceVertex == 0 && ! sourceLinestring.IsClosed)
			{
				// First point does not arrive from anywhere
				return null;
			}

			if (Type == IntersectionPointType.LinearIntersectionEnd ||
			    Type == IntersectionPointType.LinearIntersectionIntermediate)
			{
				// Comes along target
				return null;
			}

			if (Type == IntersectionPointType.AreaInterior)
			{
				// Hard to know but assuming simple geometry, the interior is on the right:
				return true;
			}

			int sourceSegmentIdx =
				GetLocalSourceIntersectionSegmentIdx(sourceLinestring, out double factor);

			Line3D segment;
			if (factor > 0)
			{
				segment = sourceLinestring[sourceSegmentIdx];
			}
			else
			{
				int? previousIdx = sourceLinestring.PreviousSegmentIndex(sourceSegmentIdx);
				segment = previousIdx == null ? null : sourceLinestring[previousIdx.Value];
			}

			Pnt3D sourcePreviousPoint = Assert.NotNull(segment).StartPoint;

			return IsOnTheRightSideOfTarget(targetLinestring, sourcePreviousPoint, tolerance);
		}

		private bool? IsOnTheRightSideOfSource([NotNull] ISegmentList source,
		                                       [NotNull] IPnt testPoint,
		                                       double tolerance = 0)
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
				return IsOnTheRightOfSegment(testPoint, sourceSegment, tolerance);
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

			return IsOnTheRightOfVertex(testPoint, previousSegment, nextSegment, tolerance);
		}

		private bool? IsOnTheRightOfVertex(IPnt testPoint, Line3D previousSegment,
		                                   Line3D nextSegment,
		                                   double tolerance)
		{
			if (tolerance > 0)
			{
				// Test if the ring point is within the tolerance -> null
				return GeomTopoOpUtils.IsOnTheRightSide(
					previousSegment, nextSegment, testPoint, tolerance);
			}

			return GeomTopoOpUtils.IsOnTheRightSide(previousSegment.StartPoint, Point,
			                                        nextSegment.EndPoint, testPoint);
		}

		private static bool? IsOnTheRightOfSegment(IPnt testPoint, Line3D segment, double tolerance)
		{
			if (tolerance == 0)
			{
				return segment.IsLeftXY(testPoint) < 0;
			}

			double perpDistance = segment.GetDistanceXYPerpendicularSigned(testPoint);

			return Math.Abs(perpDistance) < tolerance ? (bool?) null : perpDistance < 0;
		}

		private bool? IsOnTheRightSideOfTarget(ISegmentList target,
		                                       IPnt testPoint,
		                                       double tolerance = 0)
		{
			Linestring targetRing = target.GetPart(TargetPartIndex);

			double targetRatio;
			int targetSegmentIdx =
				GetLocalTargetIntersectionSegmentIdx(targetRing, out targetRatio);

			Line3D targetSegment = targetRing[targetSegmentIdx];

			if (targetRatio > 0 && targetRatio < 1)
			{
				// The intersection is on the target segment's interior
				return IsOnTheRightOfSegment(testPoint, targetSegment, tolerance);
			}

			Line3D previousSegment, nextSegment;

			// Intersection at target vertex 0 or 1 -> get the 2 adjacent segments
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (targetRatio == 0)
			{
				previousSegment =
					targetRing.PreviousSegment(targetSegmentIdx, true);

				nextSegment = SegmentIntersection.IsTargetZeroLength2D
					              ? targetRing.NextSegment(targetSegmentIdx, true)
					              : targetSegment;
			}
			else // sourceRatio == 1
			{
				previousSegment = SegmentIntersection.IsTargetZeroLength2D
					                  ? targetRing.PreviousSegment(
						                  targetSegmentIdx, true)
					                  : targetSegment;
				nextSegment = targetRing.NextSegment(targetSegmentIdx, true);
			}

			bool? result = IsOnTheRightOfVertex(testPoint, previousSegment, nextSegment, tolerance);

			return result;
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

		private static double GetDistanceAlong([NotNull] Linestring linestring,
		                                       double virtualVertexIdx,
		                                       int startVertex = 0)
		{
			if (startVertex == linestring.SegmentCount)
			{
				startVertex = 0;
			}

			int segmentIdx = GetLocalIntersectionSegmentIdx(linestring, virtualVertexIdx,
			                                                out double distanceAlongSegmentRatio);

			double result = linestring.GetDistanceAlong2D(segmentIdx, startVertex);

			result += distanceAlongSegmentRatio * linestring.GetSegment(segmentIdx).Length2D;

			return result;
		}

		public override string ToString()
		{
			return $"Point: {Point}, " +
			       $"Type: {Type}, " +
			       $"RingVertex: {VirtualSourceVertex}, " +
			       $"TargetVertex: {VirtualTargetVertex}";
		}

		public IntersectionPoint3D Clone()
		{
			var result = new IntersectionPoint3D(Point.ClonePnt3D(), VirtualSourceVertex,
			                                     SegmentIntersection, Type)
			             {
				             SourcePartIndex = SourcePartIndex,
				             TargetPartIndex = TargetPartIndex,
				             VirtualTargetVertex = VirtualTargetVertex,
				             TargetDeviatesToLeftOfSource = TargetDeviatesToLeftOfSource
			             };

			return result;
		}
	}
}
