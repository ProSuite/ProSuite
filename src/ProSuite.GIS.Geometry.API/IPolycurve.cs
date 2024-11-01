using System;
using System.Collections.Generic;

namespace ProSuite.GIS.Geometry.API
{
	public interface IPolycurve : ICurve, IGeometryCollection
	{
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

		IEnumerable<KeyValuePair<int, ISegment>> FindSegments(
			double xMin, double yMin, double xMax, double yMax,
			double tolerance, bool allowIndexing = true,
			Predicate<int> predicate = null);

		bool HasNonLinearSegments();
	}
}
