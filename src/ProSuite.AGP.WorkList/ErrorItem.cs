using System;
using ArcGIS.Core.Data;

namespace ProSuite.AGP.WorkList
{
	// todo daro: find correct folder and namespace for this class
	[CLSCompliant(false)]
	public class ErrorItem : WorkItem
	{
		public ErrorItem(Row row) : base(row) { }
	}
}
