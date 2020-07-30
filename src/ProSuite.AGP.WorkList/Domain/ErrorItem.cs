using ArcGIS.Core.Data;

namespace ProSuite.AGP.WorkList.Domain
{
	// todo daro: find correct folder and namespace for this class
	public class ErrorItem : WorkItem
	{
		public ErrorItem(Row row,
		                 double extentExpansionFactor = 1.1,
		                 double minimumSizeDegrees = 15,
		                 double minimumSizeProjected = 0.001) : base(
			row, extentExpansionFactor, minimumSizeDegrees, minimumSizeProjected) { }
	}
}
