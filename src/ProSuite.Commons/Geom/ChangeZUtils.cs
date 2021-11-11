using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.Geom
{
	public static class ChangeZUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[CanBeNull]
		public static Plane3D GetSourcePlane([NotNull] IList<Pnt3D> pntList,
		                                     double coplanarityTolerance,
		                                     bool warnIfNotPlanar = true)
		{
			Plane3D sourcePlane = Plane3D.TryFitPlane(pntList);

			if (sourcePlane == null)
			{
				return null;
			}

			bool? coplanar = AreCoplanar(
				pntList, sourcePlane, coplanarityTolerance, out double _, out string _);

			if (coplanar == null || ! coplanar.Value)
			{
				if (warnIfNotPlanar)
					_msg.WarnFormat(
						"Input geometry contains non-coplanar points. The result will not be co-planar either.");

				_msg.DebugFormat(
					"Input points are not planar w.r.t. tolerance {0}: {1}",
					coplanarityTolerance, StringUtils.Concatenate(pntList, ", "));
			}

			return sourcePlane;
		}

		public static bool? AreCoplanar([NotNull] IList<Pnt3D> points,
		                                double tolerance,
		                                out double maxDeviationFromPlane,
		                                out string message)
		{
			Assert.ArgumentCondition(points.Count > 0, "No points provided");

			Plane3D plane = Plane3D.FitPlane(points, true);

			return AreCoplanar(points, plane, tolerance, out maxDeviationFromPlane,
			                   out message);
		}

		public static bool? AreCoplanar([NotNull] IList<Pnt3D> points,
		                                [NotNull] Plane3D plane,
		                                double tolerance,
		                                out double maxDeviationFromPlane,
		                                out string message)
		{
			message = null;

			if (! plane.IsDefined)
			{
				message =
					$"The plane is not sufficiently defined by the input points {StringUtils.Concatenate(points, ", ")}.";
				maxDeviationFromPlane = double.NaN;
				return null;
			}

			if (MathUtils.AreEqual(
				0, GeomUtils.GetArea3D(points, new Pnt3D(plane.Normal))))
			{
				// Technically, the plane could be defined, but it is quite random
				// TODO: This is also the case if the input is 3 points that have been used to create the plane!
				message =
					$"The ring is degenerate without 3D area {StringUtils.Concatenate(points, ", ")}.";
				maxDeviationFromPlane = double.NaN;
				return null;
			}

			var coplanar = true;

			double maxDistance = 0;
			Pnt3D maxDistancePoint = null;
			foreach (Pnt3D pnt3D in points)
			{
				double d = plane.GetDistanceSigned(pnt3D);

				if (! MathUtils.AreEqual(d, 0, tolerance))
				{
					if (Math.Abs(d) > Math.Abs(maxDistance))
					{
						maxDistance = d;
						maxDistancePoint = pnt3D;
					}

					coplanar = false;
				}
			}

			if (! coplanar)
			{
				_msg.VerboseDebug(() =>
					                  $"Coplanarity of point {maxDistancePoint} with plane {plane} is violated: {maxDistance}m");
				message =
					$"Coplanarity of the plane is violated by {maxDistance} at point {maxDistancePoint}";
			}

			maxDeviationFromPlane = Math.Abs(maxDistance);

			return coplanar;
		}
	}
}
