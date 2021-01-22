using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.EdgeMatch
{
	internal abstract class EdgeMatchSingleBorderConnection : EdgeMatchBorderConnection
	{
		protected EdgeMatchSingleBorderConnection([NotNull] IFeature feature,
		                                          int classIndex,
		                                          [NotNull] IFeature borderFeature,
		                                          int borderClassIndex,
		                                          [NotNull] IPolyline geometryAlongBoundary)
			: base(feature, classIndex, borderClassIndex)
		{
			Assert.ArgumentNotNull(borderFeature, nameof(borderFeature));
			Assert.ArgumentNotNull(geometryAlongBoundary, nameof(geometryAlongBoundary));
			BorderFeature = borderFeature;
			GeometryAlongBoundary = geometryAlongBoundary;
		}

		[NotNull]
		public IFeature BorderFeature { get; private set; }

		[NotNull]
		public IPolyline GeometryAlongBoundary { get; private set; }
	}
}
