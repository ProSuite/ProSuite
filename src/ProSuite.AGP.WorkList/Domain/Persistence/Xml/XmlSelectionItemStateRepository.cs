using System;
using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.WorkList.Domain.Persistence.Xml
{
	public class XmlSelectionItemStateRepository : XmlWorkItemStateRepository
	{
		public XmlSelectionItemStateRepository(string filePath, string name, string displayName,
		                                       Type type, int? currentItemIndex = null)
			: base(filePath, name, displayName, type, currentItemIndex) { }

		protected override void RefreshCore(IWorkItem item, XmlWorkItemState state)
		{
			item.Status = state.Status;
		}
	}
}
