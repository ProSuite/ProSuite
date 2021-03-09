using System.Collections.Generic;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.WorkList.Test
{
	public class GdbQueryWorkList : Domain.WorkList
	{
		public GdbQueryWorkList(IWorkItemRepository repository, string name) :
			base(repository, name) { }

		public override string DisplayName => $"{nameof(GdbQueryWorkList)}";

		public override IEnumerable<IWorkItem> GetItems(QueryFilter filter = null,
		                                                bool ignoreListSettings = false,
		                                                int startIndex = 0)
		{
			return Repository.GetItems(filter);
		}
	}
}
