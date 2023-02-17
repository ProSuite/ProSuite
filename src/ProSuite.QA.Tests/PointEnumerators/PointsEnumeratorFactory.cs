using System;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.PointEnumerators
{
	public static class PointsEnumeratorFactory
	{
		[NotNull]
		public static IPointsEnumerator Create([NotNull] IReadOnlyFeature feature,
		                                       [CanBeNull] IEnvelope tileEnvelope)
		{
			var indexedSegmentsFeature = feature as IIndexedSegmentsFeature;

			if (indexedSegmentsFeature != null)
			{
				return new IndexedSegmentsFeaturePointEnumerator(indexedSegmentsFeature,
				                                                 tileEnvelope);
			}

			if (feature.Shape is IPointCollection4)
			{
				return new PointCollectionFeaturePointEnumerator(feature, tileEnvelope);
			}

			if (feature.Shape is IPoint)
			{
				return new PointFeaturePointEnumerator(feature);
			}

			throw new NotImplementedException("Unhandled geometry type " +
			                                  feature.Shape.GeometryType);
		}
	}
}
