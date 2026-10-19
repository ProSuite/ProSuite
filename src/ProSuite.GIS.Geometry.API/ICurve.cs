using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.GIS.Geometry.API
{
	public interface ICurve : IGeometry
	{
		double Length { get; }

		/// <summary>
		/// Returns a clone of the curve's start point.
		/// </summary>
		IPoint FromPoint { get; set; }

		void QueryFromPoint([NotNull] IPoint result);

		/// <summary>
		/// Returns a clone of the curve's end point.
		/// </summary>
		IPoint ToPoint { get; set; }

		void QueryToPoint([NotNull] IPoint result);

		ICurve GetSubcurve(double fromDistance,
		                   double toDistance);

		void ReverseOrientation();

		bool IsClosed { get; }

		IPoint GetPointAlong(double distanceAlong2d,
		                     bool asRatio);

		/// <summary>
		/// The 2D distance between the specified point and the closest point on this curve.
		/// </summary>
		/// <param name="toPoint">The point to determine the distance for.</param>
		/// <param name="location">The location on this curve that is closest to the specified
		/// point, or <see cref="CurveLocation.None"/> if this curve has no segments. The
		/// returned distance is the distance to the point at that location.</param>
		double GetDistance2d(IPoint toPoint, out CurveLocation location);

		/// <summary>
		/// The 2D distance from the start of this curve to the specified location on it,
		/// measured along the curve. For a curve with several parts the gaps between the
		/// parts are not counted, consistent with <see cref="Length"/>.
		/// </summary>
		double GetDistanceAlongCurve2d(CurveLocation location);
	}
}
