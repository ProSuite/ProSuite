using ESRI.ArcGIS.esriSystem;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geometry.Proxy
{
	internal class WksSegmentProxy : WksSegmentProxyBase
	{
		public WksSegmentProxy([NotNull] IWKSPointCollection wksPointCollection,
		                       int partIndex, int index)
			: base(wksPointCollection, partIndex, index)
		{
			Assert.ArgumentCondition(index >= 0,
			                         "Invalid segment index: {0}", index);
			int pointCount = wksPointCollection.Points.Count;
			Assert.ArgumentCondition(index < pointCount - 1,
			                         "Invalid segment index for point count {0}: {1}",
			                         pointCount, index);
		}

		protected override WKSPointZ FromPoint => _wksPointCollection.Points[SegmentIndex];

		protected override WKSPointZ ToPoint => _wksPointCollection.Points[SegmentIndex + 1];
	}
}
