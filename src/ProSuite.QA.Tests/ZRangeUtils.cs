using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	[CLSCompliant(false)]
	public static class ZRangeUtils
	{
		public static double GetSplitRatio(double fromZ, double toZ,
		                                   double splitAtZ)
		{
			double zAtSplitPoint;
			return GetSplitRatio(fromZ, toZ, splitAtZ,
			                     null, out zAtSplitPoint);
		}

		public static double GetSplitRatio(double fromZ, double toZ,
		                                   double splitAtZ,
		                                   [CanBeNull] Predicate<double> isAllowedZ,
		                                   out double zAtSplitPoint)
		{
			const double defaultRatio = 0.5;

			if (! ((fromZ >= splitAtZ && toZ <= splitAtZ) ||
			       (fromZ <= splitAtZ && toZ >= splitAtZ)))
			{
				// The splitAtZ value is *not* between fromZ and toZ: 
				// This may be the case when one of the bounds is outside the z range but allowed.
				// In this case, just split the segment in the middle

				if (isAllowedZ != null)
				{
					bool fromZIsAllowed = isAllowedZ(fromZ);
					bool toZIsAllowed = isAllowedZ(toZ);

					if (fromZIsAllowed && toZIsAllowed)
					{
						throw new ArgumentException(
							$"GetSplitRatio() called for segment point Z values that are both allowed ({fromZ}, {toZ})");
					}

					if (fromZIsAllowed)
					{
						zAtSplitPoint = toZ;
						return defaultRatio;
					}

					if (toZIsAllowed)
					{
						zAtSplitPoint = fromZ;
						return defaultRatio;
					}
				}

				// neither fromZ nor toZ is allowed -> the split Z value *must* be between fromZ and toZ
				throw new ArgumentException(
					$"Z value ({splitAtZ}) to split at must be between segment point Z values ({fromZ}, {toZ})");
			}

			double ratio = fromZ > toZ
				               ? (fromZ - splitAtZ) / (fromZ - toZ)
				               : (splitAtZ - fromZ) / (toZ - fromZ);

			Assert.ArgumentCondition((ratio >= 0 && ratio <= 1),
			                         "Split ratio must be between 0 and 1, actual value is {0}",
			                         ratio);

			zAtSplitPoint = splitAtZ;
			return ratio;
		}

		[NotNull]
		public static IEnumerable<ZRangeErrorSegments> GetErrorSegments(
			[NotNull] IPath path,
			double minimumZ,
			double maximumZ,
			[CanBeNull] Predicate<double> isAllowed = null)
		{
			Assert.ArgumentNotNull(path, nameof(path));

			return GetErrorSegments((ISegmentCollection) path, minimumZ, maximumZ, isAllowed);
		}

		[NotNull]
		public static IEnumerable<ZRangeErrorSegments> GetErrorSegments(
			[NotNull] IRing ring,
			double minimumZ,
			double maximumZ,
			[CanBeNull] Predicate<double> isAllowed = null)
		{
			Assert.ArgumentNotNull(ring, nameof(ring));

			var segments = (ISegmentCollection) ring;

			ZRangeErrorSegments errorSegmentsAtStart = null;
			foreach (ZRangeErrorSegments errorSegments in GetErrorSegments(
				segments, minimumZ, maximumZ, isAllowed))
			{
				if (errorSegments.StartsOnFirstSegment && ! errorSegments.EndsOnLastSegment)
				{
					// don't report yet, will be merged with another sequence at end point
					Assert.Null(errorSegmentsAtStart,
					            "error segments at start point already assigned");
					errorSegmentsAtStart = errorSegments;
				}
				else
				{
					if (errorSegments.EndsOnLastSegment && errorSegmentsAtStart != null &&
					    errorSegments.ZRangeRelation == errorSegmentsAtStart.ZRangeRelation)
					{
						// merge the error segment sequences
						yield return MergeErrorSegments(errorSegments, errorSegmentsAtStart);
						errorSegmentsAtStart = null; // merged and reported
					}
					else
					{
						yield return errorSegments;
					}
				}
			}

			if (errorSegmentsAtStart != null)
			{
				yield return errorSegmentsAtStart;
			}
		}

		public static void SplitSegmentAtZValue([NotNull] ISegment segment,
		                                        double fromZ, double toZ,
		                                        double splitAtZ,
		                                        [NotNull] out ISegment fromSegment,
		                                        [NotNull] out ISegment toSegment)
		{
			double zAtSplitPoint;
			SplitSegmentAtZValue(segment, fromZ, toZ, splitAtZ, null,
			                     out fromSegment, out toSegment,
			                     out zAtSplitPoint);
		}

		public static void SplitSegmentAtZValue([NotNull] ISegment segment,
		                                        double fromZ, double toZ,
		                                        double splitAtZ,
		                                        [CanBeNull] Predicate<double> isAllowedZ,
		                                        [NotNull] out ISegment fromSegment,
		                                        [NotNull] out ISegment toSegment,
		                                        out double zAtSplitPoint)
		{
			Assert.ArgumentNotNull(segment, nameof(segment));

			// TODO test behavior with ratios very near end points -> empty segment?

			double ratio = GetSplitRatio(fromZ, toZ, splitAtZ,
			                             isAllowedZ, out zAtSplitPoint);

			const bool asRatio = true;
			segment.SplitAtDistance(ratio, asRatio, out fromSegment, out toSegment);
		}

		[NotNull]
		private static ZRangeErrorSegments MergeErrorSegments(
			[NotNull] ZRangeErrorSegments errorSegmentsAtEnd,
			[NotNull] ZRangeErrorSegments errorSegmentsAtStart)
		{
			const bool startsOnFirstSegment = false;
			var result = new ZRangeErrorSegments(errorSegmentsAtEnd.ZRangeRelation,
			                                     errorSegmentsAtEnd.SpatialReference,
			                                     startsOnFirstSegment)
			             {EndsOnLastSegment = false};

			result.AddSegments(errorSegmentsAtEnd);
			result.AddSegments(errorSegmentsAtStart);

			return result;
		}

		[NotNull]
		private static IEnumerable<ZRangeErrorSegments> GetErrorSegments(
			[NotNull] ISegmentCollection segments,
			double minimumZ,
			double maximumZ,
			[CanBeNull] Predicate<double> isAllowed)
		{
			Assert.ArgumentNotNull(segments, nameof(segments));

			IEnumSegment enumSegments = segments.EnumSegments;
			bool recycling = enumSegments.IsRecycling;

			enumSegments.Reset();

			var partIndex = 0;
			var segmentIndex = 0;
			ISegment segment;
			enumSegments.Next(out segment, ref partIndex, ref segmentIndex);

			ISpatialReference spatialReference = ((IGeometry) segments).SpatialReference;

			// this will be null when no open sequence of error segments exist, 
			// and non-null while such a sequence exists and has not yet been reported
			ZRangeErrorSegments currentErrorSegments = null;

			var firstSegment = true;

			while (segment != null)
			{
				double fromZ;
				double toZ;
				segment.QueryVertexAttributes(esriGeometryAttributes.esriAttributeZ,
				                              out fromZ, out toZ);

				ISegment fromSegment;
				ISegment toSegment;

				double splitPointZ;
				if (fromZ < minimumZ && ! (isAllowed != null && isAllowed(fromZ)))
				{
					if (toZ < minimumZ && ! (isAllowed != null && isAllowed(toZ)))
					{
						// fromZ below, toZ below

						// add whole segment (below)
						if (currentErrorSegments == null)
						{
							currentErrorSegments = new ZRangeErrorSegments(
								ZRangeRelation.BelowZMin, spatialReference, firstSegment);
						}

						currentErrorSegments.AddSegment(recycling
							                                ? GeometryFactory.Clone(segment)
							                                : segment,
						                                fromZ, toZ);
					}
					else if (toZ > maximumZ && ! (isAllowed != null && isAllowed(toZ)))
					{
						// fromZ below, toZ above

						// interpolate from (below)
						SplitSegmentAtZValue(segment, fromZ, toZ, minimumZ, isAllowed,
						                     out fromSegment, out toSegment,
						                     out splitPointZ);
						if (currentErrorSegments == null)
						{
							currentErrorSegments = new ZRangeErrorSegments(
								ZRangeRelation.BelowZMin, spatialReference, firstSegment);
						}

						currentErrorSegments.AddSegment(fromSegment, fromZ, splitPointZ);
						yield return currentErrorSegments;

						//interpolate to (above)
						SplitSegmentAtZValue(segment, fromZ, toZ, maximumZ, isAllowed,
						                     out fromSegment, out toSegment, out splitPointZ);

						currentErrorSegments = new ZRangeErrorSegments(
							ZRangeRelation.AboveZMax, spatialReference);
						currentErrorSegments.AddSegment(toSegment, splitPointZ, toZ);
					}
					else // to z ok
					{
						// fromZ below, toZ OK

						// interpolate from (below)
						SplitSegmentAtZValue(segment, fromZ, toZ, minimumZ, isAllowed,
						                     out fromSegment, out toSegment, out splitPointZ);

						if (currentErrorSegments == null)
						{
							currentErrorSegments = new ZRangeErrorSegments(
								ZRangeRelation.BelowZMin, spatialReference, firstSegment);
						}

						currentErrorSegments.AddSegment(fromSegment, fromZ, splitPointZ);

						yield return currentErrorSegments;
						currentErrorSegments = null;
					}
				}
				else if (fromZ > maximumZ && ! (isAllowed != null && isAllowed(fromZ)))
				{
					if (toZ < minimumZ)
					{
						// fromZ above, toZ below

						// interpolate from (above)
						SplitSegmentAtZValue(segment, fromZ, toZ, maximumZ, isAllowed,
						                     out fromSegment, out toSegment, out splitPointZ);
						if (currentErrorSegments == null)
						{
							currentErrorSegments = new ZRangeErrorSegments(
								ZRangeRelation.AboveZMax, spatialReference, firstSegment);
						}

						currentErrorSegments.AddSegment(fromSegment, fromZ, splitPointZ);

						yield return currentErrorSegments;

						// interpolate to (below)
						SplitSegmentAtZValue(segment, fromZ, toZ, minimumZ, isAllowed,
						                     out fromSegment, out toSegment, out splitPointZ);

						currentErrorSegments = new ZRangeErrorSegments(
							ZRangeRelation.BelowZMin, spatialReference);
						currentErrorSegments.AddSegment(toSegment, splitPointZ, toZ);
					}
					else if (toZ > maximumZ && ! (isAllowed != null && isAllowed(toZ)))
					{
						// fromZ above, toZ above

						// add whole segment
						if (currentErrorSegments == null)
						{
							currentErrorSegments = new ZRangeErrorSegments(
								ZRangeRelation.AboveZMax, spatialReference, firstSegment);
						}

						currentErrorSegments.AddSegment(segment, fromZ, toZ);
					}
					else
					{
						// fromZ above, toZ OK

						// interpolate from (above)
						SplitSegmentAtZValue(segment, fromZ, toZ, maximumZ, isAllowed,
						                     out fromSegment, out toSegment, out splitPointZ);
						if (currentErrorSegments == null)
						{
							currentErrorSegments = new ZRangeErrorSegments(
								ZRangeRelation.AboveZMax, spatialReference, firstSegment);
						}

						currentErrorSegments.AddSegment(fromSegment, fromZ, splitPointZ);

						yield return currentErrorSegments;
						currentErrorSegments = null;
					}
				}
				else //from Z ok
				{
					if (toZ < minimumZ && ! (isAllowed != null && isAllowed(toZ)))
					{
						// fromZ OK, toZ below

						// interpolate to (below)
						SplitSegmentAtZValue(segment, fromZ, toZ, minimumZ, isAllowed,
						                     out fromSegment, out toSegment, out splitPointZ);
						if (currentErrorSegments == null)
						{
							currentErrorSegments = new ZRangeErrorSegments(
								ZRangeRelation.BelowZMin, spatialReference);
						}

						currentErrorSegments.AddSegment(toSegment, splitPointZ, toZ);
					}
					else if (toZ > maximumZ && ! (isAllowed != null && isAllowed(toZ)))
					{
						// fromZ OK, toZ above

						// interpolate to (above)
						SplitSegmentAtZValue(segment, fromZ, toZ, maximumZ, isAllowed,
						                     out fromSegment, out toSegment, out splitPointZ);
						if (currentErrorSegments == null)
						{
							currentErrorSegments = new ZRangeErrorSegments(
								ZRangeRelation.AboveZMax, spatialReference);
						}

						currentErrorSegments.AddSegment(toSegment, splitPointZ, toZ);
					}
				}

				if (recycling)
				{
					// release the segment, otherwise "pure virtual function call" occurs 
					// when there are certain circular arcs (IsLine == true ?)
					Marshal.ReleaseComObject(segment);
				}

				enumSegments.Next(out segment, ref partIndex, ref segmentIndex);
				firstSegment = false;
			}

			if (currentErrorSegments != null)
			{
				currentErrorSegments.EndsOnLastSegment = true;
				yield return currentErrorSegments;
			}
		}
	}
}
