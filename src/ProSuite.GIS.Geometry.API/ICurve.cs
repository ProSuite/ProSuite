using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.GIS.Geometry.API
{
	public interface ICurve : IGeometry
	{
		double Length { get; }

		IPoint FromPoint { get; set; }

		void QueryFromPoint([NotNull] IPoint result);

		IPoint ToPoint { get; set; }

		void QueryToPoint([NotNull] IPoint result);

		ICurve GetSubcurve(double fromDistance,
		                   double toDistance);

		void ReverseOrientation();

		bool IsClosed { get; }

		IPoint GetPointAlong(double distanceAlong2d,
		                     bool asRatio);

		double GetDistancePerpendicular2d(IPoint ofPoint,
		                                  out double distanceAlongRatio,
		                                  out IPoint pointOnLine);
	}
}
