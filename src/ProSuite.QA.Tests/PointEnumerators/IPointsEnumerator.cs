using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using Pnt = ProSuite.Commons.Geom.Pnt;

namespace ProSuite.QA.Tests.PointEnumerators
{
	public interface IPointsEnumerator
	{
		[NotNull]
		IEnumerable<Pnt> GetPoints();

		[NotNull]
		IEnumerable<Pnt> GetPoints(IBox box);

		[NotNull]
		IReadOnlyFeature Feature { get; }

		[NotNull]
		ISpatialReference SpatialReference { get; }

		double XYResolution { get; }

		double XYTolerance { get; }
	}
}
