using System.ComponentModel;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList
{
	public class SelectionItem : WorkItem
	{
		public SelectionItem(int id, [NotNull] Row row, IAttributeReader reader) : base(id, row)
		{
			ObjectID = reader.GetValue<int>(row, Attributes.ObjectID);
		}

		public int ObjectID { get; }
	}
}
