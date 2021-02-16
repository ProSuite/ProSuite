using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public interface IIssueTable : IIssueDataset
	{
		[NotNull]
		ITable Table { get; }
	}
}
