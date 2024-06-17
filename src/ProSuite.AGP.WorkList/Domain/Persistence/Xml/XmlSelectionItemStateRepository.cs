using System;
using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.WorkList.Domain.Persistence.Xml
{
	public class XmlSelectionItemStateRepository : XmlWorkItemStateRepository
	{
		public XmlSelectionItemStateRepository(string filePath, string name, Type type,
		                                       int? currentItemIndex = null) : base(
			filePath, name, type, currentItemIndex) { }

		protected override IWorkItem RefreshCore(IWorkItem item, XmlWorkItemState state)
		{
			item.Status = state.Status;

			return item;
		}
	}
}
