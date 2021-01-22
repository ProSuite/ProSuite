using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry;
using IPnt = ProSuite.Commons.Geometry.IPnt;
using Pnt = ProSuite.Commons.Geometry.Pnt;

namespace ProSuite.QA.Tests.PointEnumerators
{
	internal class PointFeaturePointEnumerator : PointsEnumerator
	{
		private readonly Pnt _point;

		public PointFeaturePointEnumerator([NotNull] IFeature feature)
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
