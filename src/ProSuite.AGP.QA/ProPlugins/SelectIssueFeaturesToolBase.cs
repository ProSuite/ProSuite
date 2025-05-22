using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.Selection;
using ProSuite.Commons.Essentials.CodeAnnotations;
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

				return featureClass != null && IsIssueTable(featureClass);
			}

			return false;
		}

		protected virtual bool IsIssueTable([NotNull] Table candidate)
		{
			// This is very hacky and should be improved:
			return IssueGdbSchema.IssueFeatureClassNames.Contains(candidate.GetName());
		}
	}
}
