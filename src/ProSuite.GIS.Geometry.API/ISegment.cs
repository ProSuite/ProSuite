namespace ProSuite.GIS.Geometry.API
{
	public interface ISegment : ICurve
	{
		double GetDistanceAlong3D(IPoint ofPoint, bool asRatio);

		double GetDistanceAlong2D(IPoint ofPoint, bool asRatio);

		double GetDistancePerpendicular3D(IPoint ofPoint, out double distanceAlongRatio,
		                                  out IPoint pointOnLine);

		void QueryTangent(
			double distanceAlongCurve,
			bool asRatio,
			double length,
			ILine tangent);

		ISegment GetSubcurve(
			double fromDistance,
			double toDistance,
			bool asRatio);

		//void QueryVertexAttributes(
		//	esriGeometryAttributes attributeType,
		//	out double fromAttribute,
		//	out double toAttribute);

		//void PutVertexAttributes(
		//	esriGeometryAttributes attributeType,
		//	double fromAttribute,
		//	double toAttribute);

		//double GetVertexAttributeAtDistance(
		//	esriGeometryAttributes attributeType,
		//	double distance,
		//	bool asRatio);

		//double GetDistanceAtVertexAttribute(
		//	esriGeometryAttributes attributeType,
		//	double attributeValue,
		//	bool asRatio);

		//void SplitAtVertexAttribute(
		//	esriGeometryAttributes attributeType,
		//	double attributeValue,
		//	[MarshalAs(UnmanagedType.Interface)] out ISegment fromSegment,
		//	[MarshalAs(UnmanagedType.Interface)] out ISegment toSegment);

		//void GetPointsAtVertexAttribute(
		//	esriGeometryAttributes attributeType,
		//	double attributeValue,
		//	double lateralOffset,
		//	[MarshalAs(UnmanagedType.Interface)] out IPointCollection outPoints);

		//void GetSubSegmentBetweenVertexAttributes(
		//	esriGeometryAttributes attributeType,
		//	double fromAttribute,
		//	double toAttribute,
		//	out ISegment outSegment);

		//void InterpolateVertexAttributes(double distanceAlongSegment, bool asRatio,
		//                                IPoint atPoint);

		//void SynchronizeEmptyAttributes(ISegment toSegment);

		//void QueryCurvature(
		//	double DistanceAlongCurve,
		//	bool asRatio,
		//	out double curvature,
		//	ILine unitVector);

		//int ReturnTurnDirection(ISegment otherSegment);

		//void EnvelopeIntersection(
		//	IEnvelope intersectionEnvelope,
		//	bool boundaryOverlap,
		//	ref double segmentParams,
		//	ref double envelopeDistances,
		//	ref int numIntersections,
		//	out int outcode);

		//void QueryAreaCorrection(out double areaCorrection);

		//void QueryCentroidCorrection(
		//	ref double weightedCentroidX,
		//	ref double weightedCentroidY,
		//	ref double areaCorrection);

		void QueryWksEnvelope(ref WKSEnvelope envelope);

		//int HorizontalIntersectionCount(ref WKSPoint p,
		//                                out bool pointOnLine);

		void SplitAtDistance(
			double distanceAlong2D,
			bool asRatio,
			out ISegment fromSegment,
			out ISegment toSegment);

		//void SplitDivideLength(
		//	double Offset,
		//	double Length,
		//	bool asRatio,
		//	ref int numSplitSegments,
		//	ref ISegment splitSegments);

		//void Densify(int cInSlots, double maxDeviation, out int pcOutSegments,
		//             out ILine segments);

		//void MaxDistanceFromLine(
		//	ref WKSPoint baseFrom,
		//	ref WKSPoint baseTo,
		//	double minOffset,
		//	double fromArcDistance,
		//	double toArcDistance,
		//	ref double maxOffset,
		//	ref double atArcDistance,
		//	ref WKSPoint farPoint);

		//void ConvertDistanceMeasureToRatio(double distanceMeasure, ref double distanceRatio);

		//void QueryWKSFromPoint( out WKSPoint p);

		//void QueryWKSToPoint( out WKSPoint p);

		//void GeographicShift(double splitLongitude);

		// TODO: Rename to IsShortIn2d()
		bool IsVertical();

		bool ExtentIntersectsXY(double xMin, double yMin, double xMax, double yMax,
		                        double tolerance);
	}
}
