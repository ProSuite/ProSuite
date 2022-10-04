using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.EdgeMatch
{
	internal abstract class EdgeMatchSingleBorderConnection : EdgeMatchBorderConnection
	{
		protected EdgeMatchSingleBorderConnection([NotNull] IReadOnlyFeature feature,
		                                          int classIndex,
		                                          [NotNull] IReadOnlyFeature borderFeature,
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
		public IReadOnlyFeature BorderFeature { get; }

		[NotNull]
		public IPolyline GeometryAlongBoundary { get; }
	}
}
