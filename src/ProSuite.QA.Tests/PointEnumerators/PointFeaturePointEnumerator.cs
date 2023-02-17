using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;

namespace ProSuite.QA.Tests.PointEnumerators
{
	internal class PointFeaturePointEnumerator : PointsEnumerator
	{
		private readonly Pnt _point;

		public PointFeaturePointEnumerator([NotNull] IReadOnlyFeature feature)
			: base(feature)
		{
			var point = (IPoint) feature.Shape;

			_point = QaGeometryUtils.CreatePoint3D(point);
		}

		public override IEnumerable<Pnt> GetPoints()
		{
			yield return _point;
		}

		public override IEnumerable<Pnt> GetPoints(IBox box)
		{
			if (box.Contains((IPnt) _point))
			{
				yield return _point;
			}
		}
	}
}
