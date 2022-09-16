using ProSuite.Commons.AO.Geodatabase;

namespace ProSuite.QA.Tests
{
	public class SimpleCrossTileFeatureState : CrossTileFeatureState<PendingFeature>
	{
		protected override PendingFeature CreatePendingFeature(IReadOnlyFeature feature,
		                                                       double xMin, double yMin,
		                                                       double xMax, double yMax)
		{
			return new PendingFeature(feature.OID, xMin, yMin, xMax, yMax);
		}
	}
}
