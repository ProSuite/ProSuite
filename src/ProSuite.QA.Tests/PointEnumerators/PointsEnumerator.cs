using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;

namespace ProSuite.QA.Tests.PointEnumerators
{
	internal abstract class PointsEnumerator : IPointsEnumerator
	{
		protected PointsEnumerator([NotNull] IReadOnlyFeature feature)
		{
			Assert.ArgumentNotNull(feature, nameof(feature));

			Feature = feature;
			SpatialReference = ((IReadOnlyGeoDataset) feature.Table).SpatialReference;

			const bool bStandardUnits = true;
			XYResolution =
				((ISpatialReferenceResolution) SpatialReference).XYResolution[bStandardUnits];
			XYTolerance = ((ISpatialReferenceTolerance) SpatialReference).XYTolerance;
		}

		public IReadOnlyFeature Feature { get; }

		public ISpatialReference SpatialReference { get; }

		public double XYResolution { get; }

		public double XYTolerance { get; }

		public abstract IEnumerable<Pnt> GetPoints();

		public abstract IEnumerable<Pnt> GetPoints(IBox box);
	}
}
