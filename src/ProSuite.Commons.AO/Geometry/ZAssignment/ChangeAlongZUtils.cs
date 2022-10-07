using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Geometry.ZAssignment
{
	public static class ChangeAlongZUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

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
	}
}
