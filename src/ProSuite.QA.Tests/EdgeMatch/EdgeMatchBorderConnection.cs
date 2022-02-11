using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.EdgeMatch
{
	internal abstract class EdgeMatchBorderConnection
	{
		protected EdgeMatchBorderConnection([NotNull] IReadOnlyFeature feature,
		                                    int classIndex,
		                                    int borderClassIndex)
		{
			Assert.ArgumentNotNull(feature, nameof(feature));

			Feature = feature;
			ClassIndex = classIndex;
			BorderClassIndex = borderClassIndex;
		}

		[NotNull]
		public IReadOnlyFeature Feature { get; }

		public int ClassIndex { get; }

		public int BorderClassIndex { get; }
	}
}
