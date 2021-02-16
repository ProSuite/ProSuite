using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.EdgeMatch
{
	internal abstract class EdgeMatchBorderConnection
	{
		protected EdgeMatchBorderConnection([NotNull] IFeature feature,
		                                    int classIndex,
		                                    int borderClassIndex)
		{
			Assert.ArgumentNotNull(feature, nameof(feature));

			Feature = feature;
			ClassIndex = classIndex;
			BorderClassIndex = borderClassIndex;
		}

		[NotNull]
		public IFeature Feature { get; }

		public int ClassIndex { get; }

		public int BorderClassIndex { get; }
	}
}
