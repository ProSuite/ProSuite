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
		private IReadOnlyFeature _feature;

		private int _pointCount = -1;

		public CachedRow([NotNull] IReadOnlyFeature feature,
		                 [CanBeNull] IUniqueIdProvider uniqueIdProvider = null)
			: this(feature)
		{
			if (uniqueIdProvider is IUniqueIdProvider<IReadOnlyFeature> up)
			{
				UniqueId uniqueId = new UniqueId(feature, up);
				((IUniqueIdObjectEdit) feature).UniqueId = uniqueId;
			}
		}

		private CachedRow([NotNull] IReadOnlyFeature feature)
			: base(feature)
		{
			_feature = feature;
			GeometryUtils.AllowIndexing(feature.Shape);
		}

		public override string ToString()
		{
			return $"CachedRow: {_feature}";
		}

		public void UpdateFeature([NotNull] IReadOnlyFeature feature,
		                          [CanBeNull] IUniqueIdProvider uniqueIdProvider)
		{
			if (_feature == null)
			{
				_feature = feature;
				if (uniqueIdProvider is IUniqueIdProvider<IReadOnlyFeature> up)
				{
					UniqueId uniqueId = new UniqueId(feature, up);
					((IUniqueIdObjectEdit) feature).UniqueId = uniqueId;
				}
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
			Box extent = ProxyUtils.CreateBox(_feature.Shape);
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
	}
}
