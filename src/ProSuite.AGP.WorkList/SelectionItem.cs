using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;

namespace ProSuite.AGP.WorkList
{
	internal class SelectionItem : WorkItem
	{
		public SelectionItem(int id, Row row,
		                     IAttributeReader reader,
		                     double extentExpansionFactor = 1.1,
		                     double minimumSizeDegrees = 15,
		                     double minimumSizeProjected = 0.001) : base(
			id, row, extentExpansionFactor, minimumSizeDegrees, minimumSizeProjected)
		{
			ObjectID = reader.GetValue<int>(row, Attributes.ObjectID);
		}

		public long ObjectID { get; }
	}
}
