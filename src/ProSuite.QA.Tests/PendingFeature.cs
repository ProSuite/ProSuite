using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	public class PendingFeature
	{
		private readonly int _oid;
		private readonly double _xMin;
		private readonly double _yMin;
		private readonly double _xMax;
		private readonly double _yMax;

		public PendingFeature(int oid, double xMin, double yMin, double xMax, double yMax)
		{
			_oid = oid;
			_xMin = xMin;
			_yMin = yMin;
			_xMax = xMax;
			_yMax = yMax;
		}

		public int OID => _oid;

		public bool IsFullyChecked([NotNull] IEnvelope tileEnvelope,
		                           [CanBeNull] IEnvelope testRunEnvelope)
		{
			return TestUtils.IsFeatureFullyChecked(_xMin, _yMin, _xMax, _yMax,
			                                       tileEnvelope, testRunEnvelope);
		}
	}
}
