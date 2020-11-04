using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Notifications;

namespace ProSuite.Commons.AO.Geometry.ChangeAlong
{
	[CLSCompliant(false)]
	public class ReshapeResultFilter
	{
		private readonly List<IEnvelope> _allowedExtents;
		private readonly IPolygon _targetUnionPoly;

		public ReshapeResultFilter(
			[CanBeNull] List<IEnvelope> allowedExtents,
			[CanBeNull] IEnumerable<IFeature> unallowedOverlapGeometries,
			bool useMinimalTolerance)
		{
			_allowedExtents = allowedExtents;

			if (unallowedOverlapGeometries != null)
			{
				_targetUnionPoly = ReshapeUtils.CreateUnionPolygon(
					GdbObjectUtils.GetGeometries(unallowedOverlapGeometries),
					useMinimalTolerance);
			}
		}

		public ReshapeResultFilter(bool useNonDefaultReshapeSide)
		{
			UseNonDefaultReshapeSide = useNonDefaultReshapeSide;
		}

		/// <summary>
		/// Whether the user specified that the non-default side should be used.
		/// </summary>
		public bool UseNonDefaultReshapeSide { get; set; }

		/// <summary>
		/// Determines whether the proposed reshape side and result is allowed regarding other
		/// criteria than the geometric situation. These could be determined by the current reshape 
		/// options, such as allowed target overlaps or updates to the geometry outside the visible
		/// extent.
		/// This method is called after the default reshape side has been determined
		/// and the ReshapeSide property of the reshape info is set. It takes precedence over the 
		/// boolean parameter useNonDefaultReshapeSide available in the reshape methods.
		/// </summary>
		/// <param name="reshapeInfo"></param>
		/// <param name="proposedSide"></param>
		/// <param name="notifications"></param>
		/// <returns></returns>
		public bool IsReshapeSideAllowed([NotNull] ReshapeInfo reshapeInfo,
		                                 RingReshapeSideOfLine proposedSide,
		                                 [CanBeNull] NotificationCollection notifications)
		{
			if (proposedSide == RingReshapeSideOfLine.Undefined)
			{
				// Special case, such as zig-zag 
				return true;
			}

			if (_allowedExtents != null &&
			    ! ChangesVisibleInAnyExtent(_allowedExtents, GetChangedSegments(
				                                reshapeInfo,
				                                proposedSide)))
			{
				NotificationUtils.Add(notifications,
				                      "Reshape side was swapped because otherwise the geometry would change outside the allowed (visible) extents. This is prevented by the option Exclude reshapes that are not completely within main map extent'.");

				return false;
			}

			if (_targetUnionPoly != null)
			{
				IPolygon proposedResult =
					proposedSide == RingReshapeSideOfLine.Left
						? reshapeInfo.LeftReshapePolygon
						: reshapeInfo.RightReshapePolygon;

				if (ReshapeUtils.ResultsInOverlapWithTarget(reshapeInfo, proposedResult,
				                                            _targetUnionPoly))
				{
					NotificationUtils.Add(notifications,
					                      "Reshape side was swapped because otherwise the result would overlap a target. This is prevented by the option 'Exclude reshape lines that result in overlaps with target features'.");
					return false;
				}
			}

			return true;
		}

		public bool IsResultAllowed(ReshapeInfo reshapeInfo,
		                            NotificationCollection notifications)
		{
			if (_allowedExtents != null &&
			    ! ChangesVisibleInAnyExtent(
				    _allowedExtents, GetChangedSegments(
					    reshapeInfo, reshapeInfo.RingReshapeSide)))
			{
				NotificationUtils.Add(notifications,
				                      "Some of the updated segments are outside the visible extent. The reshape is not allowed due to the option 'Exclude reshapes that are not completely within main map extent'");
				return false;
			}

			return true;
		}

		private static IPath GetChangedSegments(ReshapeInfo reshapeInfo,
		                                        RingReshapeSideOfLine proposedSide)
		{
			IPath replacedSegments;

			if (reshapeInfo.ReplacedSegments == null)
			{
				var ringToReshape = (IRing) reshapeInfo.GetGeometryPartToReshape();
				IPath cutReshapePath = Assert.NotNull(reshapeInfo.CutReshapePath).Path;

				replacedSegments = SegmentReplacementUtils.GetSegmentsToReplace(
					ringToReshape, cutReshapePath.FromPoint, cutReshapePath.ToPoint,
					proposedSide);
			}
			else
			{
				replacedSegments = reshapeInfo.ReplacedSegments;
			}

			return replacedSegments;
		}

		private static bool ChangesVisibleInAnyExtent(
			[NotNull] IEnumerable<IEnvelope> extents,
			[NotNull] IPath replacedSegments)
		{
			IGeometry highLevelChanges = null;
			try
			{
				highLevelChanges =
					GeometryUtils.GetHighLevelGeometry(replacedSegments, true);

				foreach (IEnvelope extent in extents)
				{
					if (extent.IsEmpty)
					{
						continue;
					}

					if (GeometryUtils.Contains(extent, highLevelChanges))
					{
						return true;
					}
				}
			}
			finally
			{
				if (highLevelChanges != null)
					Marshal.ReleaseComObject(highLevelChanges);
			}

			return false;
		}
	}
}
