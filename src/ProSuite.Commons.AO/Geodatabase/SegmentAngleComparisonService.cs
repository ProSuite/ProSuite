using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	public class SegmentAngleComparisonService : ISegmentAngleComparisonService
	{
		[NotNull] private readonly IIssueReporter _issueReporter;
		private readonly double _maximumSegmentAngleDifferenceRadians;
		private readonly double _minimumSegmentLength;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="SegmentAngleComparisonService"/> class.
		/// </summary>
		/// <param name="issueReporter">The issue reporter.</param>
		/// <param name="sourceFeatureClass">The source feature class.</param>
		/// <param name="maximumSegmentAngleDifferenceDegrees">The maximum segment angle difference in degrees.</param>
		/// <param name="maximumRoundingEffectRatio">The maximum rounding effect ratio.</param>
		public SegmentAngleComparisonService(
			[NotNull] IIssueReporter issueReporter,
			[NotNull] IFeatureClass sourceFeatureClass,
			double maximumSegmentAngleDifferenceDegrees,
			double maximumRoundingEffectRatio)
		{
			Assert.ArgumentNotNull(issueReporter, nameof(issueReporter));
			Assert.ArgumentNotNull(sourceFeatureClass, nameof(sourceFeatureClass));
			Assert.ArgumentCondition(maximumSegmentAngleDifferenceDegrees > 0,
			                         "maximum segment angle difference must be > 0");
			Assert.ArgumentCondition(maximumRoundingEffectRatio > 0,
			                         "maximum rounding effect ratio must be > 0");

			_issueReporter = issueReporter;
			_maximumSegmentAngleDifferenceRadians =
				MathUtils.ToRadians(maximumSegmentAngleDifferenceDegrees);

			_minimumSegmentLength = GetMinimumSegmentLength(
				sourceFeatureClass, _maximumSegmentAngleDifferenceRadians,
				maximumRoundingEffectRatio);
		}

		#endregion

		public void CompareSegmentAngles(IPolyline sourceShape,
		                                 IPolyline transformedShape,
		                                 IFeature transformedFeature)
		{
			CompareSegmentAnglesCore(sourceShape, transformedShape, transformedFeature);
		}

		public void CompareSegmentAngles(IPolygon sourceShape,
		                                 IPolygon transformedShape,
		                                 IFeature transformedFeature)
		{
			CompareSegmentAnglesCore(sourceShape, transformedShape, transformedFeature);
		}

		#region Non-public

		private void CompareSegmentAnglesCore([NotNull] IPolycurve sourceShape,
		                                      [NotNull] IPolycurve transformedShape,
		                                      [NotNull] IFeature transformedFeature)
		{
			var sourceParts = (IGeometryCollection) sourceShape;
			var transformedParts = (IGeometryCollection) transformedShape;

			int sourcePartCount = sourceParts.GeometryCount;

			for (var index = 0; index < sourcePartCount; index++)
			{
				CompareSegmentAngles((IPath) sourceParts.Geometry[index],
				                     (IPath) transformedParts.Geometry[index],
				                     transformedFeature);
			}
		}

		private static double GetMinimumSegmentLength(
			[NotNull] IFeatureClass featureClass,
			double maximumSegmentAngleDifferenceRadians,
			double maximumRoundingEffectRatio)
		{
			ISpatialReference spatialReference = ((IGeoDataset) featureClass).SpatialReference;

			var spatialReferenceResolution = spatialReference as ISpatialReferenceResolution;
			if (spatialReferenceResolution == null)
			{
				return 0;
			}

			double xyResolution = spatialReferenceResolution.XYResolution[true];

			return (xyResolution * Math.Sqrt(2)) /
			       Math.Tan(maximumSegmentAngleDifferenceRadians) /
			       maximumRoundingEffectRatio;
		}

		private void CompareSegmentAngles(
			[NotNull] IPath sourcePath,
			[NotNull] IPath transformedPath,
			[NotNull] IFeature transformedFeature)
		{
			ICollection<int> ignoredSegmentIndexes = GetShortSegmentIndexes(
				(ISegmentCollection) sourcePath, _minimumSegmentLength);

			var transformedPoints = (IPointCollection) transformedPath;

			const double ignoredAngleValue = 9999;

			double[] sourceAngles = GeometryUtils.GetLinearizedSegmentAngles(
				sourcePath, ignoredSegmentIndexes, ignoredAngleValue);

			double[] transformedAngles = GeometryUtils.GetLinearizedSegmentAngles(
				transformedPath, ignoredSegmentIndexes, ignoredAngleValue);

			CompareSegmentAngles(sourceAngles, transformedAngles,
			                     _maximumSegmentAngleDifferenceRadians,
			                     ignoredAngleValue,
			                     transformedFeature,
			                     transformedPoints,
			                     _issueReporter);
		}

		[NotNull]
		private static ICollection<int> GetShortSegmentIndexes(
			[NotNull] ISegmentCollection segments, double minSegmentLength)
		{
			var indexes = new List<int>();

			var index = 0;
			const bool allowRecycling = true;

			foreach (
				ISegment segment in
				GeometryUtils.GetSegments(segments.EnumSegments, allowRecycling, null))
			{
				if (segment.Length < minSegmentLength)
				{
					indexes.Add(index);
				}

				index++;
			}

			const int maxArraySize = 5;
			return indexes.Count <= maxArraySize
				       ? (ICollection<int>) indexes.ToArray()
				       : new HashSet<int>(indexes);
		}

		private static void CompareSegmentAngles(
			[NotNull] IList<double> sourceAngles,
			[NotNull] IList<double> transformedAngles,
			double maxSegmentAngleDifferenceRadians,
			double ignoredAngleValue,
			[NotNull] IFeature transformedFeature,
			[NotNull] IPointCollection transformedPoints,
			[NotNull] IIssueReporter issueReporter)
		{
			Assert.AreEqual(sourceAngles.Count, transformedAngles.Count,
			                "Differing number of segment angles. Source: {0} Transformed: {1}",
			                sourceAngles.Count, transformedAngles.Count);

			var ignoredVertices = new List<int>();
			var isClosedEvaluator = new IsClosedEvaluator(transformedPoints);

			int lastVertexIndex = sourceAngles.Count - 1;

			for (var vertexIndex = 0; vertexIndex <= lastVertexIndex; vertexIndex++)
			{
				double sourceAngle = sourceAngles[vertexIndex];
				double transformedAngle = transformedAngles[vertexIndex];

				if (Math.Abs(sourceAngle - ignoredAngleValue) < double.Epsilon)
				{
					ignoredVertices.Add(vertexIndex);
					continue;
				}

				double difference = Math.Abs(sourceAngle - transformedAngle);

				if (difference > maxSegmentAngleDifferenceRadians)
				{
					// ignore the angle difference if this is the last vertex, and its angle is 
					// equal to the first vertex, and the points form a closed loop
					bool ignoreDifference =
						vertexIndex == lastVertexIndex &&
						Math.Abs(transformedAngle - transformedAngles[0]) < double.Epsilon &&
						isClosedEvaluator.IsClosed;

					if (! ignoreDifference)
					{
						IGeometry errorGeometry = GetErrorGeometry(
							vertexIndex, ignoredVertices, transformedPoints,
							isClosedEvaluator, sourceAngles, ignoredAngleValue);

						string description = string.Format(
							"Segment angle difference exceeds limit: {0}°",
							MathUtils.ToDegrees(difference));

						issueReporter.Report(transformedFeature, errorGeometry, description);
					}
				}

				if (ignoredVertices.Count > 0)
				{
					ignoredVertices.Clear();
				}
			}
		}

		[NotNull]
		private static IGeometry GetErrorGeometry(
			int vertexIndex,
			[NotNull] ICollection<int> ignoredVertices,
			[NotNull] IPointCollection transformedPoints,
			[NotNull] IsClosedEvaluator isClosedEvaluator,
			IList<double> sourceAngles,
			double ignoredAngleValue)
		{
			if (ignoredVertices.Count <= 0)
			{
				return transformedPoints.Point[vertexIndex];
			}

			List<IPoint> points = ignoredVertices.Select(
				                                     index => transformedPoints.Point[index])
			                                     .ToList();

			if (ignoredVertices.Contains(0) && isClosedEvaluator.IsClosed)
			{
				// the first vertex is ignored, and the path is closed
				// --> check if there are ignored vertices at the end of the path which should be added

				int lastVertexIndexBeforeEndpoint = transformedPoints.PointCount - 2;

				if (Math.Abs(sourceAngles[lastVertexIndexBeforeEndpoint] - ignoredAngleValue) <
				    double.Epsilon)
				{
					// the vertex before the end vertex is also ignored

					// --> add the ending sequence of ignored vertices to the list
					for (int vertexIndexFromEnd = lastVertexIndexBeforeEndpoint;
					     vertexIndexFromEnd >= 0;
					     vertexIndexFromEnd--)
					{
						if (Math.Abs(sourceAngles[vertexIndexFromEnd] - ignoredAngleValue) >
						    double.Epsilon)
						{
							// the vertex is not ignored --> stop here
							break;
						}

						if (ignoredVertices.Contains(vertexIndexFromEnd))
						{
							// the end sequence overlaps the start sequence
							// --> apparently all vertices are ignored
							break;
						}

						// add the ignored vertex from the end sequence 
						points.Add(transformedPoints.Point[vertexIndexFromEnd]);
					}
				}
			}

			points.Add(transformedPoints.Point[vertexIndex]);

			return GeometryFactory.CreateMultipoint(points);
		}

		#region Nested types

		private class IsClosedEvaluator
		{
			private readonly IPointCollection _points;
			private bool? _isClosed;

			public IsClosedEvaluator([NotNull] IPointCollection points)
			{
				_points = points;
			}

			public bool IsClosed
			{
				get
				{
					if (! _isClosed.HasValue)
					{
						var curve = _points as ICurve;
						_isClosed = curve != null && curve.IsClosed;
					}

					return _isClosed.Value;
				}
			}
		}

		#endregion

		#endregion
	}
}
