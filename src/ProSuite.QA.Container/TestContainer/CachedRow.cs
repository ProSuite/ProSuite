using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry;
using ProSuite.QA.Container.Geometry;

namespace ProSuite.QA.Container.TestContainer
{
	public class CachedRow : BaseRow
	{
		private FeatureProxy _feature;

		private int _pointCount = -1;

		public CachedRow([NotNull] IFeature feature,
		                 [CanBeNull] UniqueIdProvider uniqueIdProvider = null)
			: this(GetFeatureProxy(feature, uniqueIdProvider)) { }

		private CachedRow([NotNull] FeatureProxy feature)
			: base(feature)
		{
			_feature = feature;
		}

		public override string ToString()
		{
			return $"CachedRow: {_feature}";
		}

		[NotNull]
		private static FeatureProxy GetFeatureProxy(
			[NotNull] IFeature feature,
			[CanBeNull] UniqueIdProvider uniqueIdProvider)
		{
			FeatureProxy result = feature as FeatureProxy;
			if (result != null)
			{
				return result;
			}

			result = FeatureProxyFactory.Create(feature, uniqueIdProvider);

			GeometryUtils.AllowIndexing(feature.Shape);

			return result;
		}

		public void UpdateFeature([NotNull] IFeature feature,
		                          [CanBeNull] UniqueIdProvider uniqueIdProvider)
		{
			if (_feature == null)
			{
				_feature = GetFeatureProxy(feature, uniqueIdProvider);
			}
		}

		public void ReleaseFeature()
		{
			_feature = null;
		}

		public bool HasFeatureCached()
		{
			return _feature != null;
		}

		[NotNull]
		public IGeometry Geometry => _feature.Shape;

		[NotNull]
		public IFeature Feature => _feature;

		protected override Box GetExtent()
		{
			Box extent = QaGeometryUtils.CreateBox(_feature.Shape);
			return extent;
		}

		protected override IList<int> GetOidList()
		{
			IList<int> oidList = GetOidList(_feature);
			return oidList;
		}

		public int PointCount
		{
			get
			{
				if (_pointCount < 0)
				{
					_pointCount = GeometryUtils.GetPointCount(Geometry);
				}

				return _pointCount;
			}
		}

		public int CachedPointCount => _feature != null
			                               ? PointCount
			                               : 0;

		#region Nested type: FeatureProxy

		private abstract class FeatureProxy : IFeature, IRowSubtypes, IFeatureSimplify2,
		                                      IFeatureProxy, IUniqueIdObject
		{
			[NotNull] private readonly IFeature _feature;
			[NotNull] private readonly IGeometry _shape;

			[CanBeNull] private readonly UniqueId _uniqueId;

			[CanBeNull] private IFields _fields;
			[CanBeNull] private ITable _table;

			/// <summary>
			/// Initializes a new instance of the <see cref="FeatureProxy"/> class.
			/// </summary>
			protected FeatureProxy([NotNull] IFeature feature,
			                       [NotNull] IGeometry shape,
			                       [CanBeNull] UniqueId uniqueId)
			{
				Assert.ArgumentNotNull(feature, nameof(feature));
				Assert.ArgumentNotNull(shape, nameof(shape));

				_feature = feature;
				_shape = shape;

				_uniqueId = uniqueId;
				// TODO cache the extent also? IFeature.Extent always creates copy otherwise
			}

			public override string ToString()
			{
				return $"OID: {_feature.OID}; UniqueID: {_uniqueId}";
			}

			IFeature IFeatureProxy.Inner => _feature;

			UniqueId IUniqueIdObject.UniqueId => _uniqueId;

			#region IFeature Members

			public void Store()
			{
				_feature.Store();
			}

			public void Delete()
			{
				_feature.Delete();
			}

			public IFields Fields => _fields ?? (_fields = _feature.Fields);

			public bool HasOID => _feature.HasOID;

			public int OID => _feature.OID;

			public ITable Table => _table ?? (_table = _feature.Table);

			public IObjectClass Class => (IObjectClass) Table;

			public IGeometry ShapeCopy => _feature.ShapeCopy;

			public IGeometry Shape
			{
				get { return _shape; }
				set { throw new NotImplementedException(); }
			}

			public IEnvelope Extent => _feature.Extent;

			public esriFeatureType FeatureType => _feature.FeatureType;

			public object get_Value(int index)
			{
				return _feature.Value[index];
			}

			public void set_Value(int index, object value)
			{
				_feature.set_Value(index, value);
			}

			#endregion

			#region IRowSubtypes

			public void InitDefaultValues()
			{
				((IRowSubtypes) _feature).InitDefaultValues();
			}

			public int SubtypeCode
			{
				get { return ((IRowSubtypes) _feature).SubtypeCode; }
				set { ((IRowSubtypes) _feature).SubtypeCode = value; }
			}

			#endregion

			#region IFeatureSimplify2

			public void SimplifyGeometry(IGeometry geometry)
			{
				((IFeatureSimplify2) _feature).SimplifyGeometry(geometry);
			}

			public bool get_IsSimpleGeometry(IGeometry geometry,
			                                 out esriNonSimpleReasonEnum reason)
			{
				return ((IFeatureSimplify2) _feature).get_IsSimpleGeometry(geometry, out reason);
			}

			#endregion
		}

		private class MultiPatchFeatureProxy : FeatureProxy, IIndexedMultiPatchFeature
		{
			private IndexedMultiPatch _indexedMultiPatch;

			public MultiPatchFeatureProxy([NotNull] IFeature feature,
			                              [NotNull] IMultiPatch shape,
			                              [CanBeNull] UniqueId uniqueIdProvider)
				: base(feature, shape, uniqueIdProvider) { }

			bool IIndexedSegmentsFeature.AreIndexedSegmentsLoaded => true;

			IIndexedSegments IIndexedSegmentsFeature.IndexedSegments => IndexedMultiPatch;

			public IIndexedMultiPatch IndexedMultiPatch
				=> _indexedMultiPatch ??
				   (_indexedMultiPatch = new IndexedMultiPatch((IMultiPatch) Shape));
		}

		private class AnyFeatureProxy : FeatureProxy
		{
			public AnyFeatureProxy([NotNull] IFeature feature, IGeometry shape,
			                       [CanBeNull] UniqueId uniqueId)
				: base(feature, shape, uniqueId) { }
		}

		private class PolycurveFeatureProxy : FeatureProxy, IIndexedPolycurveFeature
		{
			private IndexedPolycurve _indexedPolycurve;

			public PolycurveFeatureProxy(IFeature feature, IPolycurve shape,
			                             [CanBeNull] UniqueId uniqueId)
				: base(feature, shape, uniqueId) { }

			bool IIndexedSegmentsFeature.AreIndexedSegmentsLoaded => _indexedPolycurve == null;

			IIndexedSegments IIndexedSegmentsFeature.IndexedSegments
				=> _indexedPolycurve ??
				   (_indexedPolycurve = new IndexedPolycurve((IPointCollection4) Shape));
		}

		private static class FeatureProxyFactory
		{
			[NotNull]
			public static FeatureProxy Create([NotNull] IFeature feature,
			                                  [CanBeNull] UniqueIdProvider uniqueIdProvider)
			{
				UniqueId uniqueId = uniqueIdProvider != null
					                    ? new UniqueId(feature, uniqueIdProvider)
					                    : null;

				esriGeometryType geometryType = feature.Shape.GeometryType;

				FeatureProxy result;

				switch (geometryType)
				{
					case esriGeometryType.esriGeometryMultiPatch:
						result = new MultiPatchFeatureProxy(feature, (IMultiPatch) feature.Shape,
						                                    uniqueId);
						break;

					case esriGeometryType.esriGeometryPolygon:
					case esriGeometryType.esriGeometryPolyline:
						result = new PolycurveFeatureProxy(feature, (IPolycurve) feature.Shape,
						                                   uniqueId);
						break;

					default:
						result = new AnyFeatureProxy(feature, feature.Shape, uniqueId);
						break;
				}

				return result;
			}
		}

		#endregion
	}
}
