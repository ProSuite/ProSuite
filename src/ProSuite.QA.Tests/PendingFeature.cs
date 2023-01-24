using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests
{
	public class PendingFeature
	{
		private readonly long _oid;
		private readonly double _xMin;
		private readonly double _yMin;
		private readonly double _xMax;
		private readonly double _yMax;

		public PendingFeature(long oid, double xMin, double yMin, double xMax, double yMax)
		{
			_oid = oid;
			_xMin = xMin;
			_yMin = yMin;
			_xMax = xMax;
			_yMax = yMax;
		}

		public long OID => _oid;

		public bool IsFullyChecked([NotNull] IEnvelope tileEnvelope,
		                           [CanBeNull] IEnvelope testRunEnvelope)
		{
			return TestUtils.IsFeatureFullyChecked(_xMin, _yMin, _xMax, _yMax,
			                                       tileEnvelope, testRunEnvelope);
		}
	}
}
