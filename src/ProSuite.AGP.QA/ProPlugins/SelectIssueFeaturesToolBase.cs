using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.Selection;
using ProSuite.Commons.Notifications;
using ProSuite.DomainModel.AGP.QA;

namespace ProSuite.AGP.QA.ProPlugins
{
	public abstract class SelectIssueFeaturesToolBase : SelectionToolBase
	{
		protected override bool CanSelectFromLayerCore(BasicFeatureLayer basicFeatureLayer,
		                                               NotificationCollection notifications)
		{
			if (basicFeatureLayer is FeatureLayer featureLayer)
			{
				FeatureClass featureClass = featureLayer.GetFeatureClass();

				return featureClass != null &&
				       IssueGdbSchema.IssueFeatureClassNames.Contains(featureClass.GetName());
			}

			return false;
		}
	}
}
