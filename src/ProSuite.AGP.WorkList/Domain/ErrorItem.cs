using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.WorkList.Domain
{
	// todo daro: find correct folder and namespace for this class
	public class ErrorItem : WorkItem
	{
		public ErrorItem(int id, Row row,
		                 IAttributeReader reader,
		                 double extentExpansionFactor = 1.1,
		                 double minimumSizeDegrees = 15,
		                 double minimumSizeProjected = 0.001) : base(
			id, row, extentExpansionFactor, minimumSizeDegrees, minimumSizeProjected)
		{
			Description = reader.GetValue<string>(row, Attributes.IssueCodeDescription);
		}

		public string Description { get; }
	}
}
