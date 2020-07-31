using ArcGIS.Core.Data;

namespace ProSuite.AGP.WorkList.Domain
{
	// todo daro: find correct folder and namespace for this class
	public class ErrorItem : WorkItem
	{
		public ErrorItem(Row row,
		                 IAttributeReader reader,
		                 double extentExpansionFactor = 1.1,
		                 double minimumSizeDegrees = 15,
		                 double minimumSizeProjected = 0.001) : base(
			row, extentExpansionFactor, minimumSizeDegrees, minimumSizeProjected)
		{
			Description = reader.GetValue<string>(row, Attributes.IssueCodeDescription);
		}

		public string Description { get; set; }
	}
}
