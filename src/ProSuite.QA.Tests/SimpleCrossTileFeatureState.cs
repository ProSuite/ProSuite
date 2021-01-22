using System;
using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.QA.Tests
{
	[CLSCompliant(false)]
	public class SimpleCrossTileFeatureState : CrossTileFeatureState<PendingFeature>
	{
		protected override PendingFeature CreatePendingFeature(IFeature feature,
		                                                       double xMin, double yMin,
		                                                       double xMax, double yMax)
		{
			return new PendingFeature(feature.OID, xMin, yMin, xMax, yMax);
		}
	}
}
