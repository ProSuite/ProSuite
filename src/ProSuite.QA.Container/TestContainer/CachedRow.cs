using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.QA.Container.Geometry;

namespace ProSuite.QA.Container.TestContainer
{
	public class CachedRow : BaseRow
	{
		private FeatureProxy _feature;

		private int _pointCount = -1;

		public CachedRow([NotNull] IReadOnlyFeature feature,
		                 [CanBeNull] IUniqueIdProvider uniqueIdProvider = null)
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
			[NotNull] IReadOnlyFeature feature,
			[CanBeNull] IUniqueIdProvider uniqueIdProvider)
		{
			FeatureProxy result = feature as FeatureProxy;
			if (result != null)
			{
				return result;
			}

			result = FeatureProxyFactory.Create(
				feature, uniqueIdProvider as IUniqueIdProvider<IReadOnlyFeature>);

			GeometryUtils.AllowIndexing(feature.Shape);

			return result;
		}

		public void UpdateFeature([NotNull] IReadOnlyFeature feature,
		                          [CanBeNull] IUniqueIdProvider uniqueIdProvider)
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
		public IReadOnlyFeature Feature => _feature;

		protected override Box GetExtent()
		{
			Box extent = Commons.AO.Geometry.Proxy.ProxyUtils.CreateBox(_feature.Shape);
			return extent;
		}

		protected override IList<long> GetOidList()
		{
			IList<long> oidList = GetOidList(_feature);
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

		private abstract class FeatureProxy : VirtualRow,
		                                      IReadOnlyFeature, IRowSubtypes, IFeatureSimplify2,
		                                      IFeatureProxy, IUniqueIdObject
		{
			[NotNull] private readonly IReadOnlyFeature _feature;
			[NotNull] private readonly IGeometry _shape;

			[CanBeNull] private readonly UniqueId _uniqueId;

			[CanBeNull] private IReadOnlyTable _table;

			/// <summary>
			/// Initializes a new instance of the <see cref="FeatureProxy"/> class.
			/// </summary>
			protected FeatureProxy([NotNull] IReadOnlyFeature feature,
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

			public static explicit operator ReadOnlyRow(FeatureProxy featureProxy)
				=> featureProxy._feature as ReadOnlyRow;
			public override string ToString()
			{
				return $"OID: {_feature.OID}; UniqueID: {_uniqueId}";
			}

			IReadOnlyFeature IFeatureProxy.Inner => _feature;

			UniqueId IUniqueIdObject.UniqueId => _uniqueId;

			#region IFeature Members

			public override long OID => _feature.OID;

			public override IReadOnlyTable ReadOnlyTable => _table ?? (_table = _feature.Table);

			public override IObjectClass Class
			{
				get
				{
					IObjectClass cls = _feature.Table as IObjectClass;
					if (cls == null)
					{
						cls = (IObjectClass) (_feature.Table as ReadOnlyFeatureClass)?.BaseTable;
					}

					return cls;
				}
			}

			public IReadOnlyFeatureClass FeatureClass => (IReadOnlyFeatureClass) ReadOnlyTable;

			public override IGeometry Shape
			{
				get { return _shape; }
				set { throw new NotImplementedException(); }
			}

			public override void Store() { }

			public override esriFeatureType FeatureType => _feature.FeatureType;

			public override object get_Value(int index)
			{
				return _feature.get_Value(index);
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

			bool IFeatureSimplify2.get_IsSimpleGeometry(IGeometry geometry,
			                                            out esriNonSimpleReasonEnum reason)
			{
				return ((IFeatureSimplify2) _feature).get_IsSimpleGeometry(geometry, out reason);
			}

			#endregion
		}

		private class MultiPatchFeatureProxy : FeatureProxy, IIndexedMultiPatchFeature
		{
			private IIndexedMultiPatch _indexedMultiPatch;

			public MultiPatchFeatureProxy([NotNull] IReadOnlyFeature feature,
										  [NotNull] IMultiPatch shape,
										  [CanBeNull] UniqueId uniqueIdProvider)
				: base(feature, shape, uniqueIdProvider) { }

			bool IIndexedSegmentsFeature.AreIndexedSegmentsLoaded => true;

			IIndexedSegments IIndexedSegmentsFeature.IndexedSegments => IndexedMultiPatch;

			public IIndexedMultiPatch IndexedMultiPatch
				=> _indexedMultiPatch ??
				   (_indexedMultiPatch = new IndexedMultiPatch((IMultiPatch)Shape));
		}

		private class AnyFeatureProxy : FeatureProxy
		{
			public AnyFeatureProxy([NotNull] IReadOnlyFeature feature, IGeometry shape,
								   [CanBeNull] UniqueId uniqueId)
				: base(feature, shape, uniqueId) { }
		}

		private class PolycurveFeatureProxy : FeatureProxy, IIndexedPolycurveFeature
		{
			private IndexedPolycurve _indexedPolycurve;

			public PolycurveFeatureProxy(IReadOnlyFeature feature, IPolycurve shape,
										 [CanBeNull] UniqueId uniqueId)
				: base(feature, shape, uniqueId) { }

			bool IIndexedSegmentsFeature.AreIndexedSegmentsLoaded => _indexedPolycurve == null;

			IIndexedSegments IIndexedSegmentsFeature.IndexedSegments
				=> _indexedPolycurve ??
				   (_indexedPolycurve = new IndexedPolycurve((IPointCollection4)Shape));
		}

		private static class FeatureProxyFactory
		{
			[NotNull]
			public static FeatureProxy Create(
				[NotNull] IReadOnlyFeature feature,
				[CanBeNull] IUniqueIdProvider<IReadOnlyFeature> uniqueIdProvider)
			{
				UniqueId uniqueId = uniqueIdProvider != null
										? new UniqueId(feature, uniqueIdProvider)
										: null;

				esriGeometryType geometryType = feature.Shape.GeometryType;

				FeatureProxy result;

				switch (geometryType)
				{
					case esriGeometryType.esriGeometryMultiPatch:
						result = new MultiPatchFeatureProxy(feature, (IMultiPatch)feature.Shape,
															uniqueId);
						break;

					case esriGeometryType.esriGeometryPolygon:
					case esriGeometryType.esriGeometryPolyline:
						result = new PolycurveFeatureProxy(feature, (IPolycurve)feature.Shape,
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
