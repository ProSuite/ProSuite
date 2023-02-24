using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	public static class MeasureUtils
	{
		private static readonly ThreadLocal<IPoint> _pointTemplate =
			new ThreadLocal<IPoint>(() => new PointClass());

		[NotNull]
		public static ICollection<IPoint> GetPointsWithInvalidM(
			[NotNull] IPointCollection points,
			[NotNull] IPoint pointTemplate,
			double invalidValue)
		{
			Assert.ArgumentNotNull(points, nameof(points));
			Assert.ArgumentNotNull(pointTemplate, nameof(pointTemplate));

			IEnumVertex enumVertex = points.EnumVertices;
			int partIndex;
			int vertexIndex;

			enumVertex.QueryNext(pointTemplate, out partIndex, out vertexIndex);

			var result = new List<IPoint>();

			while (partIndex >= 0 && vertexIndex >= 0)
			{
				if (IsInvalidValue(pointTemplate.M, invalidValue))
				{
					result.Add(GeometryFactory.Clone(pointTemplate));
				}

				enumVertex.QueryNext(pointTemplate, out partIndex, out vertexIndex);
			}

			return result;
		}

		public static bool HasInvalidMValue([NotNull] ISegment segment,
		                                    [NotNull] IPoint pointTemplate,
		                                    double invalidValue)
		{
			Assert.ArgumentNotNull(segment, nameof(segment));
			Assert.ArgumentNotNull(pointTemplate, nameof(pointTemplate));

			segment.QueryFromPoint(pointTemplate);

			if (IsInvalidValue(pointTemplate.M, invalidValue))
			{
				return true;
			}

			segment.QueryToPoint(pointTemplate);

			return IsInvalidValue(pointTemplate.M, invalidValue);
		}

		public static bool IsInvalidValue(double m, double invalidValue)
		{
			return double.IsNaN(m) && double.IsNaN(invalidValue) ||
			       Math.Abs(m - invalidValue) < double.Epsilon;
		}

		[NotNull]
		public static IPolyline GetErrorGeometry(
			[NotNull] ICollection<ISegment> invalidMSegments,
			bool cloneSegments)
		{
			Assert.ArgumentNotNull(invalidMSegments, nameof(invalidMSegments));

			IPolyline result = ProxyUtils.CreatePolyline(invalidMSegments);

			var segments = (ISegmentCollection) result;

			object emptyRef = Type.Missing;

			foreach (ISegment segment in invalidMSegments)
			{
				segments.AddSegment(cloneSegments
					                    ? GeometryFactory.Clone(segment)
					                    : segment,
				                    ref emptyRef, ref emptyRef);
			}

			return result;
		}

		[CanBeNull]
		public static IPolyline GetSubcurves([NotNull] IPolyline polyline,
		                                     double mMin, double mMax,
		                                     [NotNull] out IList<IPoint> points)
		{
			Assert.ArgumentNotNull(polyline, nameof(polyline));

			points = new List<IPoint>();

			if (polyline.IsEmpty)
			{
				return null;
			}

			IMultipoint multipoint = GetPointsAtMs(polyline, mMin, mMax);

			var mSegmentation = (IMSegmentation3) polyline;
			var subcurves = (IPolyline) mSegmentation.GetSubcurveBetweenMs(mMin, mMax);
			GeometryUtils.AllowIndexing(subcurves);

			IRelationalOperator subcurvesRelOp = subcurves.IsEmpty
				                                     ? null
				                                     : (IRelationalOperator) subcurves;

			var pointCollection = (IPointCollection) multipoint;
			int pointCount = pointCollection.PointCount;

			for (var i = 0; i < pointCount; i++)
			{
				IPoint point = pointCollection.Point[i];

				if (subcurvesRelOp == null || subcurvesRelOp.Disjoint(point))
				{
					points.Add(point);
				}
			}

			return subcurves.IsEmpty
				       ? null
				       : subcurves;
		}

		public static bool ContainsAllMonotonicityTypes(
			[NotNull] IPolyline polyline,
			params esriMonotinicityEnum[] monotonicityTypes)
		{
			Assert.ArgumentNotNull(polyline, nameof(polyline));

			int monotonicity = ((IMSegmentation3) polyline).MMonotonicity;

			return ContainsAllMonotonicityTypes(monotonicity, monotonicityTypes);
		}

		public static bool ContainsAllMonotonicityTypes(
			int monotonicity,
			params esriMonotinicityEnum[] monotonicityTypes)
		{
			return monotonicityTypes.All(
				monotonicityType => (monotonicity & (int) monotonicityType) != 0);
		}

		[NotNull]
		public static IEnumerable<MMonotonicitySequence> GetMonotonicitySequences(
			[NotNull] ISegmentCollection segments,
			params esriMonotinicityEnum[] monotonicityTypes)
		{
			return GetMonotonicitySequences(segments,
			                                (IEnumerable<esriMonotinicityEnum>)
			                                monotonicityTypes);
		}

		[NotNull]
		public static IEnumerable<MMonotonicitySequence> GetMonotonicitySequences(
			[NotNull] IPolyline polyline,
			[NotNull] IEnumerable<esriMonotinicityEnum> monotonicityTypes)
		{
			return GetMonotonicitySequences((ISegmentCollection) polyline, monotonicityTypes);
		}

		[NotNull]
		public static IEnumerable<MMonotonicitySequence> GetMonotonicitySequences(
			[NotNull] ISegmentCollection segments,
			[NotNull] IEnumerable<esriMonotinicityEnum> monotonicityTypes)
		{
			MMonotonicitySequence currentSequence = null;
			var types = new HashSet<esriMonotinicityEnum>(monotonicityTypes);

			if (types.Count == 0)
			{
				yield break;
			}

			IEnumSegment enumSegments = segments.EnumSegments;
			enumSegments.Reset();

			ISegment segment;
			var partIndex = 0;
			var segmentIndex = 0;

			enumSegments.Next(out segment, ref partIndex, ref segmentIndex);
			bool recycling = enumSegments.IsRecycling;

			while (segment != null)
			{
				esriMonotinicityEnum currentMonotonicity = GetMonotonicityType(segment);

				if (types.Contains(currentMonotonicity))
				{
					if (currentSequence == null)
					{
						currentSequence = new MMonotonicitySequence(currentMonotonicity,
							segment.SpatialReference);
					}

					if (currentSequence.MonotonicityType != currentMonotonicity)
					{
						yield return currentSequence;

						currentSequence = new MMonotonicitySequence(currentMonotonicity,
							segment.SpatialReference);
					}

					currentSequence.Add(recycling
						                    ? GeometryFactory.Clone(segment)
						                    : segment);
				}
				else
				{
					if (currentSequence != null)
					{
						yield return currentSequence;
						currentSequence = null;
					}
				}

				if (recycling)
				{
					Marshal.ReleaseComObject(segment);
				}

				enumSegments.Next(out segment, ref partIndex, ref segmentIndex);
			}

			if (currentSequence != null)
			{
				yield return currentSequence;
			}
		}

		public static esriMonotinicityEnum GetMonotonicityType([NotNull] ISegment segment)
		{
			double fromM;
			double toM;
			segment.QueryVertexAttributes(esriGeometryAttributes.esriAttributeM,
			                              out fromM, out toM);

			double dM = toM - fromM;

			return GetMonotonicityType(dM);
		}

		public static esriMonotinicityEnum GetMonotonicityType(double diff)
		{
			if (double.IsNaN(diff))
			{
				return esriMonotinicityEnum.esriValuesEmpty;
			}

			if (Math.Abs(diff) < double.Epsilon)
			{
				return esriMonotinicityEnum.esriValueLevel;
			}

			return diff > 0
				       ? esriMonotinicityEnum.esriValueIncreases
				       : esriMonotinicityEnum.esriValueDecreases;
		}

		public static esriMonotinicityEnum GetMonotonicityTrend(
			[NotNull] IPolyline polyline)
		{
			var mSegmentation = (IMSegmentation3) polyline;

			double firstM;
			double lastM;
			mSegmentation.QueryFirstLastM(out firstM, out lastM);

			if (double.IsNaN(firstM) || double.IsNaN(lastM))
			{
				return esriMonotinicityEnum.esriValuesEmpty;
			}

			if (firstM > lastM)
			{
				return esriMonotinicityEnum.esriValueDecreases;
			}

			if (firstM < lastM)
			{
				return esriMonotinicityEnum.esriValueIncreases;
			}

			//First and last are equal, compare summed length of in- and decreasing segments

			IEnumerable<MMonotonicitySequence> sequences =
				GetMonotonicitySequences((ISegmentCollection) polyline,
				                         esriMonotinicityEnum.esriValueDecreases,
				                         esriMonotinicityEnum.esriValueIncreases);

			ICollection<MMonotonicitySequence> sequenceCollection =
				CollectionUtils.GetCollection(sequences);

			double lengthIncreasing = GetTotalLengthForMonotonicity(
				sequenceCollection, esriMonotinicityEnum.esriValueIncreases);
			double lengthDecreasing = GetTotalLengthForMonotonicity(
				sequenceCollection, esriMonotinicityEnum.esriValueDecreases);

			// only return level if completely level, only one non NaN m-value or same distance increasing and decreasing
			if (Math.Abs(lengthDecreasing - lengthIncreasing) < double.Epsilon)
			{
				return esriMonotinicityEnum.esriValueLevel;
			}

			return lengthDecreasing > lengthIncreasing
				       ? esriMonotinicityEnum.esriValueDecreases
				       : esriMonotinicityEnum.esriValueIncreases;
		}

		private static double GetTotalLengthForMonotonicity(
			[NotNull] IEnumerable<MMonotonicitySequence> sequences,
			esriMonotinicityEnum monotinicity)
		{
			return sequences.Where(sequence => sequence.MonotonicityType == monotinicity)
			                .Sum(sequence => sequence.Length);
		}

		[NotNull]
		public static IEnumerable<CurveMeasureRange> GetMeasureRanges(
			[NotNull] IReadOnlyFeature feature,
			int tableIndex)
		{
			Assert.ArgumentNotNull(feature, nameof(feature));

			IGeometry geometry = feature.Shape;

			if (geometry == null || geometry.IsEmpty)
			{
				yield break;
			}

			var polyline = geometry as IPolyline;
			if (polyline == null)
			{
				yield break;
			}

			long oid = feature.OID;

			foreach (CurveMeasureRange range in GetMeasureRanges(polyline, oid, tableIndex))
			{
				yield return range;
			}
		}

		[NotNull]
		public static IEnumerable<CurveMeasureRange> GetMeasureRanges(
			[NotNull] IPolyline polyline,
			long oid,
			int tableIndex)
		{
			Assert.ArgumentNotNull(polyline, nameof(polyline));

			var parts = polyline as IGeometryCollection;

			if (parts == null || parts.GeometryCount == 1)
			{
				CurveMeasureRange range = GetMeasureRange(polyline, oid, tableIndex);

				if (range != null)
				{
					yield return range;
				}
			}
			else
			{
				int partCount = parts.GeometryCount;
				for (var partIndex = 0; partIndex < partCount; partIndex++)
				{
					IGeometry part = parts.Geometry[partIndex];

					var partPolyline = (IPolyline) GeometryUtils.GetHighLevelGeometry(part);

					CurveMeasureRange range = GetMeasureRange(
						partPolyline, oid, tableIndex, partIndex);

					if (range != null)
					{
						yield return range;
					}
				}
			}
		}

		[NotNull]
		private static IMultipoint GetPointsAtMs([NotNull] IPolyline polyline,
		                                         params double[] mValues)
		{
			Assert.ArgumentNotNull(polyline, nameof(polyline));

			var result = new MultipointClass {SpatialReference = polyline.SpatialReference};

			GeometryUtils.MakeMAware(result);
			if (GeometryUtils.IsZAware(polyline))
			{
				GeometryUtils.MakeZAware(result);
			}

			var mSegmentation = (IMSegmentation3) polyline;

			object emptyRef = Type.Missing;

			foreach (double mValue in mValues)
			{
				IGeometryCollection points = mSegmentation.GetPointsAtM(mValue, 0);

				int pointCount = points.GeometryCount;

				for (var i = 0; i < pointCount; i++)
				{
					result.AddPoint((IPoint) points.Geometry[i], ref emptyRef, ref emptyRef);
				}
			}

			// eliminate duplicates
			GeometryUtils.Simplify(result);

			return result;
		}

		[CanBeNull]
		private static CurveMeasureRange GetMeasureRange(
			[NotNull] IPolyline polyline, long oid, int tableIndex)
		{
			return GetMeasureRange(polyline, oid, tableIndex, -1);
		}

		[CanBeNull]
		private static CurveMeasureRange GetMeasureRange(
			[NotNull] IPolyline polyline, long oid, int tableIndex, int partIndex)
		{
			Assert.ArgumentNotNull(polyline, nameof(polyline));

			var mCollection = (IMCollection) polyline;

			double mMin = mCollection.MMin;
			double mMax = mCollection.MMax;

			if (double.IsNaN(mMin) || double.IsNaN(mMax))
			{
				return null;
			}

			return CreateMeasureRange(polyline, mMin, mMax, oid, tableIndex, partIndex);
		}

		[NotNull]
		private static CurveMeasureRange CreateMeasureRange([NotNull] IPolyline polyline,
		                                                    double mMin, double mMax,
		                                                    long oid, int tableIndex,
		                                                    int partIndex)
		{
			Assert.ArgumentNotNull(polyline, nameof(polyline));

			var result = new CurveMeasureRange(oid, tableIndex, mMin, mMax, partIndex);

			polyline.QueryFromPoint(_pointTemplate.Value);

			AssignEndPoint(result, _pointTemplate.Value, mMin, mMax);

			polyline.QueryToPoint(_pointTemplate.Value);

			AssignEndPoint(result, _pointTemplate.Value, mMin, mMax);

			return result;
		}

		private static void AssignEndPoint([NotNull] CurveMeasureRange curveMeasureRange,
		                                   [NotNull] IPoint point, double mMin, double mMax)
		{
			double m = point.M;

			if (Math.Abs(m - mMin) < double.Epsilon)
			{
				curveMeasureRange.MMinEndPoint = GetLocation(point);
			}

			if (Math.Abs(m - mMax) < double.Epsilon)
			{
				curveMeasureRange.MMaxEndPoint = GetLocation(point);
			}
		}

		[NotNull]
		private static Location GetLocation([NotNull] IPoint point)
		{
			double x;
			double y;
			point.QueryCoords(out x, out y);
			return new Location(x, y);
		}

		[NotNull]
		public static IEnumerable<MMonotonicitySequence> GetErrorSequences(
			[NotNull] IPolyline polyline,
			MonotonicityDirection expectedMonotonicity,
			[NotNull] Func<bool> isFeatureFlipped,
			bool allowConstantValues)
		{
			var geometryCollection = (IGeometryCollection) polyline;

			int partCount = geometryCollection.GeometryCount;

			if (partCount <= 1)
			{
				return GetErrorSequencesFromSinglePart(polyline,
				                                       expectedMonotonicity,
				                                       isFeatureFlipped, allowConstantValues);
			}

			var result = new List<MMonotonicitySequence>();

			foreach (IPath path in GeometryUtils.GetPaths(polyline))
			{
				var pathPolyline = (IPolyline) GeometryUtils.GetHighLevelGeometry(path);

				result.AddRange(GetErrorSequencesFromSinglePart(pathPolyline,
				                                                expectedMonotonicity,
				                                                isFeatureFlipped,
				                                                allowConstantValues));
			}

			return result;
		}

		[NotNull]
		private static IEnumerable<MMonotonicitySequence> GetErrorSequencesFromSinglePart(
			[NotNull] IPolyline singlePartPolyline,
			MonotonicityDirection expectedMonotonicity,
			[NotNull] Func<bool> isFeatureFlipped,
			bool allowConstantValues)
		{
			bool? featureFlipped;
			esriMonotinicityEnum? monotoicityTrend;

			ICollection<esriMonotinicityEnum> errorMonotonicities =
				GetActualErrorMonotonicities(singlePartPolyline,
				                             expectedMonotonicity,
				                             isFeatureFlipped,
				                             allowConstantValues,
				                             out featureFlipped,
				                             out monotoicityTrend);

			if (errorMonotonicities.Count == 0)
			{
				yield break;
			}

			foreach (
				MMonotonicitySequence sequence in
				GetMonotonicitySequences((ISegmentCollection) singlePartPolyline,
				                         errorMonotonicities))
			{
				sequence.FeatureMonotonicityTrend = monotoicityTrend;
				sequence.FeatureIsFlipped = featureFlipped;

				yield return sequence;
			}
		}

		[NotNull]
		private static ICollection<esriMonotinicityEnum> GetActualErrorMonotonicities(
			[NotNull] IPolyline singlePartPolyline,
			MonotonicityDirection expectedMonotonicity,
			[NotNull] Func<bool> isFeatureFlipped,
			bool allowConstantValues,
			out bool? featureFlipped,
			out esriMonotinicityEnum? monotonicityTrend)
		{
			var mSegmentation = (IMSegmentation3) singlePartPolyline;
			int actualMonotonicityTypes = mSegmentation.MMonotonicity;

			monotonicityTrend = null;
			featureFlipped = null;

			var result = new HashSet<esriMonotinicityEnum>();

			if (! allowConstantValues)
			{
				if (ContainsMonotonicityType(actualMonotonicityTypes,
				                             esriMonotinicityEnum.esriValueLevel))

				{
					result.Add(esriMonotinicityEnum.esriValueLevel);
				}
			}

			if (expectedMonotonicity == MonotonicityDirection.Any)
			{
				if (ContainsAllMonotonicityTypes(actualMonotonicityTypes,
				                                 esriMonotinicityEnum.esriValueIncreases,
				                                 esriMonotinicityEnum.esriValueDecreases))
				{
					monotonicityTrend = GetMonotonicityTrend(singlePartPolyline);

					result.Add(
						monotonicityTrend == esriMonotinicityEnum.esriValueDecreases
							? esriMonotinicityEnum.esriValueIncreases
							: esriMonotinicityEnum.esriValueDecreases);
				}

				return result;
			}

			esriMonotinicityEnum violatingMonotonicityType =
				GetViolatingMonotonicityType(expectedMonotonicity, isFeatureFlipped,
				                             out featureFlipped);

			if (ContainsMonotonicityType(actualMonotonicityTypes,
			                             violatingMonotonicityType))
			{
				result.Add(violatingMonotonicityType);
			}

			return result;
		}

		private static esriMonotinicityEnum GetViolatingMonotonicityType(
			MonotonicityDirection expectedMonotonicity,
			[NotNull] Func<bool> isFeatureFlipped,
			out bool? featureFlipped)
		{
			MonotonicityDirection expectedMonotonicityDirectionForFeature =
				GetExpectedMonotonicityDirectionForFeature(expectedMonotonicity,
				                                           isFeatureFlipped,
				                                           out featureFlipped);

			return GetViolatingMonotonicityType(expectedMonotonicityDirectionForFeature);
		}

		private static esriMonotinicityEnum GetViolatingMonotonicityType(
			MonotonicityDirection expectedMonotonicity)
		{
			switch (expectedMonotonicity)
			{
				case MonotonicityDirection.Any:
					throw new ArgumentException("Unexpected monotonicity direction (Any)");

				case MonotonicityDirection.Increasing:
					return esriMonotinicityEnum.esriValueDecreases;

				case MonotonicityDirection.Decreasing:
					return esriMonotinicityEnum.esriValueIncreases;

				default:
					throw new ArgumentOutOfRangeException(nameof(expectedMonotonicity));
			}
		}

		private static MonotonicityDirection GetExpectedMonotonicityDirectionForFeature(
			MonotonicityDirection expectedMonotonicity,
			[NotNull] Func<bool> isFeatureFlipped,
			out bool? featureFlipped)
		{
			featureFlipped = null;

			switch (expectedMonotonicity)
			{
				case MonotonicityDirection.Any:
					return MonotonicityDirection.Any;

				case MonotonicityDirection.Increasing:
					featureFlipped = isFeatureFlipped();
					return (bool) featureFlipped
						       ? MonotonicityDirection.Decreasing
						       : MonotonicityDirection.Increasing;

				case MonotonicityDirection.Decreasing:
					featureFlipped = isFeatureFlipped();
					return (bool) featureFlipped
						       ? MonotonicityDirection.Increasing
						       : MonotonicityDirection.Decreasing;

				default:
					throw new ArgumentOutOfRangeException(nameof(expectedMonotonicity));
			}
		}

		private static bool ContainsMonotonicityType(int monotonicityTypes,
		                                             esriMonotinicityEnum monotonicity)
		{
			return (monotonicityTypes & (int) monotonicity) == (int) monotonicity;
		}
	}
}
