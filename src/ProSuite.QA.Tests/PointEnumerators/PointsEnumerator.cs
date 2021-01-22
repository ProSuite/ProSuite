using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry;
using Pnt = ProSuite.Commons.Geometry.Pnt;

namespace ProSuite.QA.Tests.PointEnumerators
{
	internal abstract class PointsEnumerator : IPointsEnumerator
	{
		protected PointsEnumerator([NotNull] IFeature feature)
		{
			Assert.ArgumentNotNull(feature, nameof(feature));

			Feature = feature;
			SpatialReference = ((IGeoDataset) feature.Class).SpatialReference;

			const bool bStandardUnits = true;
			XYResolution =
				((ISpatialReferenceResolution) SpatialReference).XYResolution[bStandardUnits];
			XYTolerance = ((ISpatialReferenceTolerance) SpatialReference).XYTolerance;
		}

		public IFeature Feature { get; }

		public ISpatialReference SpatialReference { get; }

		public double XYResolution { get; }

		public double XYTolerance { get; }

		public abstract IEnumerable<Pnt> GetPoints();

		public abstract IEnumerable<Pnt> GetPoints(IBox box);
	}
}
