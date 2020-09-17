using System.Collections.Generic;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain.Persistence.Json;

namespace ProSuite.AGP.WorkList.Domain.Persistence.Xml
{
	public class JsonWorkItemStateRepository : WorkItemStateRepository<JsonWorkItemState, JsonBasedWorkListDefinition>
	{
		protected override void Store(JsonBasedWorkListDefinition definition)
		{
			throw new System.NotImplementedException();
		}

		protected override JsonBasedWorkListDefinition CreateDefinition(List<JsonWorkItemState> states)
		{
			throw new System.NotImplementedException();
		}

		protected override JsonWorkItemState CreateState(IWorkItem item)
		{
			throw new System.NotImplementedException();
		}

		protected override List<JsonWorkItemState> ReadStates()
		{
			throw new System.NotImplementedException();
		}
	}
}
