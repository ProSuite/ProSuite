using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public interface IIssueFeatureClass : IIssueDataset
	{
		[NotNull]
		IFeatureClass FeatureClass { get; }
	}
}
