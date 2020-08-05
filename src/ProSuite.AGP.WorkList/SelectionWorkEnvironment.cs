using ArcGIS.Desktop.Mapping;

namespace ProSuite.AGP.WorkList
{
	public class SelectionWorkEnvironment
		: WorkEnvironmentBase
	{
		protected override BasicFeatureLayer EnsureFeatureLayerCore(BasicFeatureLayer featureLayer)
		{
			return featureLayer;
		}
	}
}
