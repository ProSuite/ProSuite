using System;
using System.Collections.Generic;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Collections;

namespace ProSuite.AGP.WorkList.Domain.Persistence.Json
{
	public class JsonWorkItemStateRepository : WorkItemStateRepository<JsonWorkItemState, JsonBasedWorkListDefinition>
	{
		protected override void Store(JsonBasedWorkListDefinition definition)
		{
			throw new System.NotImplementedException();
		}

		protected override JsonBasedWorkListDefinition CreateDefinition(
			Dictionary<GdbWorkspaceIdentity, SimpleSet<GdbTableIdentity>> tablesByWorkspace,
			List<JsonWorkItemState> states)
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

		public JsonWorkItemStateRepository(string name, Type type, int? currentItemIndex = null) : base(name, type, currentItemIndex){}
	}
}
