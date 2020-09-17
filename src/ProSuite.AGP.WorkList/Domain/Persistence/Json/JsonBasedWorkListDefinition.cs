using System.Collections.Generic;

namespace ProSuite.AGP.WorkList.Domain.Persistence.Json
{
	public class JsonBasedWorkListDefinition : IWorkListDefinition<JsonWorkItemState>
	{
		public string Path { get; set; }
		public List<JsonWorkItemState> Items { get; set; }
	}
}
