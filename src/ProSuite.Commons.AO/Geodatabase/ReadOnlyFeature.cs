using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	public abstract class ReadOnlyFeature : ReadOnlyRow, IReadOnlyFeature
	{
		private class PolycurveFeature : ReadOnlyFeature, IIndexedPolycurveFeature
		{
			private IndexedPolycurve _indexedPolycurve;

			public PolycurveFeature(ReadOnlyFeatureClass featureClass, IFeature feature)
				: base(featureClass, feature) { }

			bool IIndexedSegmentsFeature.AreIndexedSegmentsLoaded => _indexedPolycurve == null;

			IIndexedSegments IIndexedSegmentsFeature.IndexedSegments
				=> _indexedPolycurve ??
				   (_indexedPolycurve = new IndexedPolycurve((IPointCollection4)Shape));
		}

		private class MultiPatchFeature : ReadOnlyFeature, IIndexedMultiPatchFeature
		{
			private IndexedMultiPatch _indexedMultiPatch;

			public MultiPatchFeature(ReadOnlyFeatureClass featureClass, IFeature feature)
				: base(featureClass, feature) { }

			bool IIndexedSegmentsFeature.AreIndexedSegmentsLoaded => true;

			IIndexedSegments IIndexedSegmentsFeature.IndexedSegments => IndexedMultiPatch;

			public IIndexedMultiPatch IndexedMultiPatch
				=> _indexedMultiPatch ??
				   (_indexedMultiPatch = new IndexedMultiPatch((IMultiPatch)Shape));
		}

		private class AnyFeature : ReadOnlyFeature
		{
			public AnyFeature(ReadOnlyFeatureClass featureClass, IFeature feature)
				: base(featureClass, feature) { }
		}

		public new static ReadOnlyFeature Create([NotNull] IFeature feature)
		{
			return Create(ReadOnlyTableFactory.Create((IFeatureClass)feature.Table), feature);
		}

		public static ReadOnlyFeature Create(
			[NotNull] ReadOnlyFeatureClass owner, [NotNull] IFeature feature)
		{
			Assert.AreEqual(owner.BaseTable, feature.Table, "FeatureClasses differ");
			esriGeometryType geometryType = owner.ShapeType;

			ReadOnlyFeature result;

			switch (geometryType)
			{
				case esriGeometryType.esriGeometryMultiPatch:
					result = new MultiPatchFeature(owner, feature);
					break;

				case esriGeometryType.esriGeometryPolygon:
				case esriGeometryType.esriGeometryPolyline:
					result = new PolycurveFeature(owner, feature);
					break;

				default:
					result = new AnyFeature(owner, feature);
					break;
			}

			return result;
		}

		private ReadOnlyFeature(ReadOnlyFeatureClass featureClass, IFeature feature)
			: base(featureClass, feature) { }

		protected IFeature Feature => (IFeature) Row;
		public IEnvelope Extent => Feature.Extent;
		public IGeometry Shape => Feature.Shape;
		public IGeometry ShapeCopy => Feature.ShapeCopy;
		IReadOnlyFeatureClass IReadOnlyFeature.FeatureClass => FeatureClass;
		public ReadOnlyFeatureClass FeatureClass => (ReadOnlyFeatureClass) Table;
		public esriFeatureType FeatureType => Feature.FeatureType;
	}
}
