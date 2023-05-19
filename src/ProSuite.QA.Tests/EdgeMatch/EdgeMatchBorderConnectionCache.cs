using System;
using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Db;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestSupport;

namespace ProSuite.QA.Tests.EdgeMatch
{
	internal abstract class EdgeMatchBorderConnectionCache<T>
		where T : EdgeMatchSingleBorderConnection
	{
		private readonly Dictionary<FeatureKey, Dictionary<FeatureKey, T>>
			_cache =
				new Dictionary<FeatureKey, Dictionary<FeatureKey, T>>(
					new FeatureKeyComparer());

		public void Clear()
		{
			_cache.Clear();
		}

		public void Clear(WKSEnvelope tileWksBox, WKSEnvelope allWksBox)
		{
			RemoveHandledObjects(tileWksBox, allWksBox);
		}

		private void RemoveHandledObjects(WKSEnvelope tileWksBox, WKSEnvelope allWksBox)
		{
			var areaFeaturesToRemove = new List<FeatureKey>(_cache.Count);

			foreach (
				KeyValuePair<FeatureKey, Dictionary<FeatureKey, T>> cachedPair in
				_cache)
			{
				var allRemoved = true;
				Dictionary<FeatureKey, T> borderConnections = cachedPair.Value;

				var borderFeaturesToRemove = new List<FeatureKey>(borderConnections.Count);

				foreach (
					KeyValuePair<FeatureKey, T> borderPair in borderConnections)
				{
					T borderConnection = borderPair.Value;

					if (VerifyHandled(borderConnection, tileWksBox, allWksBox))
					{
						borderFeaturesToRemove.Add(borderPair.Key);
					}
					else
					{
						allRemoved = false;
					}
				}

				if (! allRemoved)
				{
					foreach (FeatureKey borderKey in borderFeaturesToRemove)
					{
						borderConnections.Remove(borderKey);
					}
				}

				if (borderConnections.Count <= 0)
				{
					areaFeaturesToRemove.Add(cachedPair.Key);
				}
			}

			foreach (FeatureKey areaFeatureKey in areaFeaturesToRemove)
			{
				_cache.Remove(areaFeatureKey);
			}
		}

		protected abstract bool VerifyHandled(T borderConnection, WKSEnvelope tileBox,
		                                      WKSEnvelope allBox);

		[NotNull]
		public IEnumerable<T> GetBorderConnections<TG>(
			[NotNull] TG geometry,
			[NotNull] IReadOnlyFeature geometryFeature,
			int geometryClassIndex,
			int borderClassIndex,
			IReadOnlyTable borderClass,
			IFeatureClassFilter spatialFilter,
			QueryFilterHelper filterHelper,
			Func<IReadOnlyTable, ITableFilter, QueryFilterHelper, IEnumerable<IReadOnlyRow>> search,
			RowPairCondition borderMatchCondition)
			where TG : IGeometry
		{
			var geometryKey = new FeatureKey(geometryFeature.OID, geometryClassIndex);

			Dictionary<FeatureKey, T> borderConnections;
			if (! _cache.TryGetValue(geometryKey, out borderConnections))
			{
				borderConnections = new Dictionary<FeatureKey, T>(new FeatureKeyComparer());

				_cache.Add(geometryKey, borderConnections);
			}

			IPolyline geometryLine = geometry is IPolyline
				                         ? (IPolyline) geometry
				                         : (IPolyline) ((ITopologicalOperator) geometry).Boundary;

			IEnumerable<IReadOnlyFeature> borderFeatures =
				GetConnectedBorderFeatures(geometry, geometryFeature, geometryClassIndex,
				                           borderClassIndex, search, borderClass, spatialFilter,
				                           filterHelper, borderMatchCondition);

			foreach (IReadOnlyFeature borderFeature in borderFeatures)
			{
				var borderKey = new FeatureKey(borderFeature.OID, borderClassIndex);

				T borderConnection;
				if (! borderConnections.TryGetValue(borderKey, out borderConnection))
				{
					IPolyline geometryAlongBorder = GetGeometryAlongBorder(borderFeature,
						geometryLine);

					borderConnection = CreateBorderConnection(geometryFeature, geometryClassIndex,
					                                          borderFeature,
					                                          borderClassIndex,
					                                          geometryAlongBorder,
					                                          geometryAlongBorder);

					borderConnections.Add(borderKey, borderConnection);
				}
			}

			return new List<T>(borderConnections.Values);
		}

		[NotNull]
		private IEnumerable<IReadOnlyFeature> GetConnectedBorderFeatures(
			[NotNull] IGeometry geometry,
			[NotNull] IReadOnlyFeature geometryFeature, int geometryClassIndex,
			int borderClassIndex,
			[NotNull]
			Func<IReadOnlyTable, ITableFilter, QueryFilterHelper, IEnumerable<IReadOnlyRow>> search,
			IReadOnlyTable borderClass,
			IFeatureClassFilter spatialFilter,
			QueryFilterHelper filterHelper,
			RowPairCondition borderMatchCondition)
		{
			spatialFilter.FilterGeometry = geometry;

			var result = new List<IReadOnlyFeature>(5);

			foreach (IReadOnlyRow borderRow in search(borderClass,
			                                          spatialFilter,
			                                          filterHelper))
			{
				if (! borderMatchCondition.IsFulfilled(geometryFeature, geometryClassIndex,
				                                       borderRow, borderClassIndex))
				{
					continue;
				}

				result.Add((IReadOnlyFeature) borderRow);
			}

			return result;
		}

		protected abstract T CreateBorderConnection([NotNull] IReadOnlyFeature feature,
		                                            int featureClassIndex,
		                                            [NotNull] IReadOnlyFeature borderFeature,
		                                            int borderClassIndex,
		                                            [NotNull] IPolyline lineAlongBorder,
		                                            [NotNull] IPolyline uncoveredLine);

		[NotNull]
		protected static IPolyline GetGeometryAlongBorder(
			[NotNull] IReadOnlyFeature borderFeature,
			[NotNull] IPolyline line)
		{
			IGeometry border = borderFeature.Shape;
			if (border.GeometryType == esriGeometryType.esriGeometryPolygon)
			{
				border = ((ITopologicalOperator) border).Boundary;
			}

			return (IPolyline) IntersectionUtils.Intersect(
				line, border,
				esriGeometryDimension.esriGeometry1Dimension);
		}
	}
}
