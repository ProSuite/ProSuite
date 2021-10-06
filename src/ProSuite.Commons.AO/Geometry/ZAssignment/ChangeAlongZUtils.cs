using System;
using System.Collections.Generic;
using System.Reflection;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.AO.Geometry.ZAssignment
{
	public static class ChangeAlongZUtils
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Returns all ChangeAlongZSource values associated with a display string.
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<KeyValuePair<ChangeAlongZSource, string>> GetZSources()
		{
			foreach (ChangeAlongZSource zSource in Enum.GetValues(
				typeof(ChangeAlongZSource)))
			{
				yield return new KeyValuePair<ChangeAlongZSource, string>(
					zSource, GetDisplayText(zSource));
			}
		}

		public static string GetDisplayText(ChangeAlongZSource zSource)
		{
			switch (zSource)
			{
				case ChangeAlongZSource.Target: return "Target";
				case ChangeAlongZSource.InterpolatedSource: return "Interpolated source";
				case ChangeAlongZSource.SourcePlane: return "Source plane";
				default:
					throw new ArgumentOutOfRangeException(
						$"Unknown ChangeAlongZSource: {zSource}");
			}
		}

		public static T PrepareCutPolylineZs<T>([NotNull] T cutPolycurve,
		                                        ChangeAlongZSource zSource)
			where T : IPolycurve
		{
			// Copy from CutGeometryUtils
			if (zSource == ChangeAlongZSource.Target ||
			    ! GeometryUtils.IsZAware(cutPolycurve))
			{
				return cutPolycurve;
			}

			T result = GeometryFactory.Clone(cutPolycurve);

			// Do not make z-unaware to avoid the Z values to be restored in SegmentReplacementUtils.EnsureZs()
			((IZAware) result).DropZs();

			return result;
		}

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

		public static void AssignZ([NotNull] IPointCollection points,
		                           [NotNull] Plane3D fromPlane)
		{
			Assert.False(points is IMultiPatch,
			             "This method cannot be used on multipatch geometries.");

			IEnumVertex eVertex = points.EnumVertices;
			IPoint point = new PointClass();
			int part;
			int vertex;

			eVertex.QueryNext(point, out part, out vertex);
			while (part >= 0 && vertex >= 0)
			{
				// This crashes ArcMap (with AccessViolation) if called on multipatch geometry:
				eVertex.put_Z(fromPlane.GetZ(point.X, point.Y));
				eVertex.QueryNext(point, out part, out vertex);
			}
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
