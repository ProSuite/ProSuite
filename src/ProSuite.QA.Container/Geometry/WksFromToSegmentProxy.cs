using ESRI.ArcGIS.esriSystem;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.Geometry
{
	internal class WksFromToSegmentProxy : WksSegmentProxyBase
	{
		private readonly int _fromIndex;
		private readonly int _toIndex;

		public WksFromToSegmentProxy([NotNull] IWKSPointCollection wksPointCollection,
		                             int partIndex, int segmentIndex, int fromIndex,
		                             int toIndex)
			: base(wksPointCollection, partIndex, segmentIndex)
		{
			Assert.ArgumentCondition(fromIndex >= 0, "Invalid segment index: {0}", fromIndex);
			Assert.ArgumentCondition(toIndex >= 0, "Invalid segment index: {0}", toIndex);

			int pointCount = wksPointCollection.Points.Count;
			Assert.ArgumentCondition(fromIndex < pointCount,
			                         "Invalid fromIndex for point count {0}: {1}",
			                         pointCount, fromIndex);
			Assert.ArgumentCondition(toIndex < pointCount,
			                         "Invalid toIndex for point count {0}: {1}",
			                         pointCount, toIndex);
			_fromIndex = fromIndex;
			_toIndex = toIndex;
		}

		protected override WKSPointZ FromPoint
		{
			get { return _wksPointCollection.Points[_fromIndex]; }
		}

		protected override WKSPointZ ToPoint
		{
			get { return _wksPointCollection.Points[_toIndex]; }
		}
	}
}
