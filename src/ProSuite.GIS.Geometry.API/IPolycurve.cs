namespace ProSuite.GIS.Geometry.API
{
	public interface IPolycurve : IGeometry, IGeometryCollection
	{
		double Length { get; }

		IPoint FromPoint { get; }

		//void QueryFromPoint(IPoint from);

		IPoint ToPoint { get; }

		//void QueryToPoint(IPoint to);

		//void QueryPoint(
		//	esriSegmentExtension extension,
		//	double DistanceAlongCurve,
		//	bool asRatio,
		//	IPoint outPoint);

		//void QueryPointAndDistance(
		//	esriSegmentExtension extension,
		//	IPoint inPoint,
		//	bool asRatio,
		//	IPoint outPoint,
		//	ref double DistanceAlongCurve,
		//	ref double distanceFromCurve,
		//	ref bool bRightSide);

		//void QueryTangent(
		//	esriSegmentExtension extension,
		//	double DistanceAlongCurve,
		//	bool asRatio,
		//	double Length,
		//	ILine tangent);

		//void QueryNormal(
		//	esriSegmentExtension extension,
		//	double DistanceAlongCurve,
		//	bool asRatio,
		//	double Length,
		//	ILine normal);

		//void GetSubcurve(
		//	double fromDistance,
		//	double toDistance,
		//	bool asRatio,
		//	out ICurve outSubcurve);

		bool IsClosed { get; }

		//void SplitAtPoint(
		//	IPoint splitPoint,
		//	bool projectOnto,
		//	bool createPart,
		//	out bool SplitHappened,
		//	out int newPartIndex,
		//	out int newSegmentIndex);

		//void SplitAtDistance(
		//	double distance,
		//	bool asRatio,
		//	bool createPart,
		//	out bool SplitHappened,
		//	out int newPartIndex,
		//	out int newSegmentIndex);

		//void SimplifyNetwork();
	}
}
