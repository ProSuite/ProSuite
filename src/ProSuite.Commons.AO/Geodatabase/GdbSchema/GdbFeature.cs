using System;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Geom;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	/// <inheritdoc cref="GdbRow" />
	public abstract class GdbFeature : GdbRow, IFeature, IFeatureBuffer, IFeatureChanges,
	                                   IReadOnlyFeature, IDbFeature
	{
		private class PolycurveFeature : GdbFeature, IIndexedPolycurveFeature
		{
			private IndexedPolycurve _indexedPolycurve;

			public PolycurveFeature(long oid, GdbFeatureClass featureClass, IValueList valueList)
				: base(oid, featureClass, valueList) { }

			bool IIndexedSegmentsFeature.AreIndexedSegmentsLoaded => _indexedPolycurve == null;

			IIndexedSegments IIndexedSegmentsFeature.IndexedSegments
				=> _indexedPolycurve ??
				   (_indexedPolycurve = new IndexedPolycurve((IPointCollection4) Shape));
		}

		private class MultiPatchFeature : GdbFeature, IIndexedMultiPatchFeature
		{
			private IndexedMultiPatch _indexedMultiPatch;

			public MultiPatchFeature(long oid, GdbFeatureClass featureClass, IValueList valueList)
				: base(oid, featureClass, valueList) { }

			bool IIndexedSegmentsFeature.AreIndexedSegmentsLoaded => true;

			IIndexedSegments IIndexedSegmentsFeature.IndexedSegments => IndexedMultiPatch;

			public IIndexedMultiPatch IndexedMultiPatch
				=> _indexedMultiPatch ??
				   (_indexedMultiPatch = new IndexedMultiPatch((IMultiPatch) Shape));
		}

		private class AnyFeature : GdbFeature
		{
			public AnyFeature(long oid, GdbFeatureClass featureClass, IValueList valueList)
				: base(oid, featureClass, valueList) { }
		}

		public static GdbFeature Create(long oid, [NotNull] GdbFeatureClass featureClass,
		                                [CanBeNull] IValueList valueList = null)
		{
			esriGeometryType geometryType = featureClass.ShapeType;

			GdbFeature result;

			switch (geometryType)
			{
				case esriGeometryType.esriGeometryMultiPatch:
					result = new MultiPatchFeature(oid, featureClass, valueList);
					break;

				case esriGeometryType.esriGeometryPolygon:
				case esriGeometryType.esriGeometryPolyline:
					result = new PolycurveFeature(oid, featureClass, valueList);
					break;

				default:
					result = new AnyFeature(oid, featureClass, valueList);
					break;
			}

			return result;
		}

		private readonly int _shapeFieldIndex;

		[NotNull] private readonly IFeatureClass _featureClass;

		private IGeometry _originalShape;

		#region Constructors

		protected GdbFeature(long oid, [NotNull] GdbFeatureClass featureClass,
		                     [CanBeNull] IValueList valueList = null)
			: base(oid, featureClass, valueList)
		{
			_featureClass = featureClass;
			_shapeFieldIndex = featureClass.ShapeFieldIndex;
		}

		#endregion

		#region IFeature implementation

#if Server11 || ARCGIS_12_0_OR_GREATER
		long IFeature.OID => OID;
#else
		int IFeature.OID => (int) OID;
#endif

		public override IGeometry ShapeCopy => Shape != null ? GeometryFactory.Clone(Shape) : null;
		ITable IFeature.Table => Table;

		IReadOnlyFeatureClass IReadOnlyFeature.FeatureClass =>
			(IReadOnlyFeatureClass) ReadOnlyTable;

		public override IGeometry Shape
		{
			get
			{
				try
				{
					// TODO: The performance overhead of increaseRcwRefCount: true is very large,
					// but only in x64!
					object shapeProperty = ValueSet.GetValue(_shapeFieldIndex, true);

					if (shapeProperty == DBNull.Value)
					{
						return null;
					}

					return (IGeometry) shapeProperty;
				}
				catch (COMException)
				{
					// E_Fail occurs if the value has never been set and does not exist in the property set.
					return null;
				}
			}
			set
			{
				esriGeometryType? shapeType = value?.GeometryType;

				if (shapeType != null)
				{
					// Allow null Shape
					Assert.AreEqual(_featureClass.ShapeType, shapeType.Value,
					                "Invalid geometry type: {0}", shapeType);
				}

				_originalShape = Shape;
				set_Value(_shapeFieldIndex, value);
			}
		}

		public override esriFeatureType FeatureType => _featureClass.FeatureType;

		#endregion

		#region Implementation of IDbFeature

		object IDbFeature.Shape => Shape;

		IBoundedXY IDbFeature.Extent => GeometryConversionUtils.CreateEnvelopeXY(base.Extent);

		public IFeatureClassData FeatureClass => (IFeatureClassData) DbTable;

		#endregion

		public bool ShapeChanged { get; private set; }

		IGeometry IFeatureChanges.OriginalShape => _originalShape;

		protected override void RecycleCore()
		{
			_originalShape = null;
		}

		protected override void StoreCore()
		{
			base.StoreCore();

			// TODO: consider snapping Shape to dataset's spatial reference

			ShapeChanged = ! GeometryUtils.AreEqual(_originalShape, Shape);
		}
	}
}
