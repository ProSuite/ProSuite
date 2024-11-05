namespace ProSuite.GIS.Geometry.API
{
	public interface IPath : ICurve
	{
		//new void QueryPointAndDistance(
		//  esriSegmentExtension extension,
		//  IPoint inPoint,
		//  bool asRatio,
		//  IPoint outPoint,
		//  ref double DistanceAlongCurve,
		//  ref double distanceFromCurve,
		//  ref bool bRightSide);
		//new void QueryTangent(
		//  esriSegmentExtension extension,
		//  double DistanceAlongCurve,
		//  bool asRatio,
		//  double Length,
		//  ILine tangent);

		//new void QueryNormal(
		//  esriSegmentExtension extension,
		//  double DistanceAlongCurve,
		//  bool asRatio,
		//  double Length,
		//  ILine normal);

		//void QueryChordLengthTangents(
		//  int pointIndex,
		//  IPoint prevTangent,
		//  ref bool prevSetByUser,
		//  IPoint nextTangent,
		//  ref bool nextSetByUser);

		//void SetChordLengthTangents(int pointIndex, IPoint prevTangent, IPoint nextTangent);

		double GetDistanceAlong2D(int vertexIndex,
		                          int startVertexIndex = 0);

		bool HasNonLinearSegments();
	}
}
