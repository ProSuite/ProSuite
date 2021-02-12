using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry;
using IPnt = ProSuite.Commons.Geometry.IPnt;
using Pnt = ProSuite.Commons.Geometry.Pnt;

namespace ProSuite.QA.Tests.PointEnumerators
{
	internal class PointCollectionFeaturePointEnumerator : PointsEnumerator
	{
		private readonly WKSPointZ[] _wksPoints;

		public PointCollectionFeaturePointEnumerator([NotNull] IFeature feature,
		                                             [CanBeNull] IEnvelope envelope)
			: base(feature)
		{
			var points = (IPointCollection4) feature.Shape;

			var wksPoints = new WKSPointZ[points.PointCount];
			GeometryUtils.QueryWKSPointZs(points, wksPoints);

			if (envelope == null || (((IRelationalOperator) envelope).Contains(feature.Shape)))
			{
				_wksPoints = wksPoints;
			}
			else
			{
				_wksPoints = GetFilteredPoints(wksPoints, envelope, XYResolution);
			}
		}

		[NotNull]
		private static WKSPointZ[] GetFilteredPoints(
			[NotNull] ICollection<WKSPointZ> wksPoints,
			[NotNull] IEnvelope envelope,
			double tolerance)
		{
			double xMin;
			double yMin;
			double xMax;
			double yMax;
			envelope.QueryCoords(out xMin, out yMin, out xMax, out yMax);

			xMin = xMin - tolerance;
			yMin = yMin - tolerance;
			xMax = xMax + tolerance;
			yMax = yMax + tolerance;

			var filteredPoints = new List<WKSPointZ>(wksPoints.Count);

			foreach (WKSPointZ wksPoint in wksPoints)
			{
				if (wksPoint.X >= xMin &&
				    wksPoint.X <= xMax &&
				    wksPoint.Y >= yMin &&
				    wksPoint.Y <= yMax)
				{
					filteredPoints.Add(wksPoint);
				}
			}

			return filteredPoints.ToArray();
		}

		public override IEnumerable<Pnt> GetPoints()
		{
			foreach (WKSPointZ wksPoint in _wksPoints)
			{
				yield return QaGeometryUtils.CreatePoint3D(wksPoint);
			}
		}

		public override IEnumerable<Pnt> GetPoints(IBox searchBox)
		{
			foreach (WKSPointZ wksPoint in _wksPoints)
			{
				Pnt point = QaGeometryUtils.CreatePoint3D(wksPoint);
				if (searchBox.Contains((IPnt) point))
				{
					yield return point;
				}
			}
		}
	}
}
