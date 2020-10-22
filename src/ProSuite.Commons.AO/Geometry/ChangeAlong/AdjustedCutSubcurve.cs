using System;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geometry.ChangeAlong
{
	public class AdjustedCutSubcurve : CutSubcurve
	{
		private readonly IPath _connectLineAtFromPoint;
		private readonly IPath _connectLineAtToPoint;

		private readonly IPath _pathOnTarget;

		[CLSCompliant(false)]
		public AdjustedCutSubcurve([NotNull] IPath pathOnTarget,
		                           [CanBeNull] IPath connectionLineAtFrom,
		                           [CanBeNull] IPath connectionLineAtTo)
			: base(pathOnTarget,
			       connectionLineAtFrom != null && connectionLineAtFrom.Length > 0,
			       connectionLineAtTo != null && connectionLineAtTo.Length > 0)
		{
			_connectLineAtFromPoint = connectionLineAtFrom;
			_connectLineAtToPoint = connectionLineAtTo;

			_pathOnTarget = pathOnTarget;
			Path = GetFullReshapePath(pathOnTarget);
		}

		[CLSCompliant(false)]
		public IPath ConnectLineAtFromPoint => _connectLineAtFromPoint;

		[CLSCompliant(false)]
		public IPath ConnectLineAtToPoint => _connectLineAtToPoint;

		[NotNull]
		[CLSCompliant(false)]
		public IPath PathOnTarget => _pathOnTarget;

		[CLSCompliant(false)]
		protected override IPoint FromPointOnTarget => PathOnTarget.FromPoint;

		[CLSCompliant(false)]
		protected override IPoint ToPointOnTarget => PathOnTarget.ToPoint;

		protected override bool CanReshapeCore()
		{
			return true;
		}

		private IPath GetFullReshapePath(IPath pathOnTarget)
		{
			IPath fullReshapePath;

			var pathClone = (ISegmentCollection) GeometryFactory.Clone(pathOnTarget);

			// NOTE: sometimes it's not empty but the length is 0!
			if (_connectLineAtFromPoint != null && _connectLineAtFromPoint.Length > 0)
			{
				if (GeometryUtils.AreEqualInXY(_connectLineAtFromPoint.FromPoint,
				                               pathOnTarget.FromPoint))
				{
					_connectLineAtFromPoint.ReverseOrientation();
				}

				fullReshapePath = GeometryFactory.Clone(_connectLineAtFromPoint);

				((ISegmentCollection) fullReshapePath).AddSegmentCollection(pathClone);
			}
			else
			{
				fullReshapePath = (IPath) pathClone;
			}

			if (_connectLineAtToPoint != null && _connectLineAtToPoint.Length > 0)
			{
				if (GeometryUtils.AreEqualInXY(_connectLineAtToPoint.ToPoint,
				                               pathOnTarget.ToPoint))
				{
					_connectLineAtToPoint.ReverseOrientation();
				}

				var connectLineAtToPointClone =
					(ISegmentCollection) GeometryFactory.Clone(_connectLineAtToPoint);

				((ISegmentCollection) fullReshapePath).AddSegmentCollection(
					connectLineAtToPointClone);
			}

			// NOTE regarding the fullReshapePath segment collection:
			// Despite the corrected orientation there can be very small gaps between the segments of the
			// segment collection. If a reference to this path is later added to another geometry (with 
			// reduced resolution) using GeometryCollection.AddSegmentCollection (now it is cloned) a subsequent
			// simplify on that collection deletes all segments after the small gap in THIS segement collection!

			// Therefore:
			fullReshapePath.SnapToSpatialReference();

			return fullReshapePath;
		}
	}
}
