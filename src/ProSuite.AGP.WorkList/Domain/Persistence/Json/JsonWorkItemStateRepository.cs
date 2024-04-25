using System;
using System.Collections.Generic;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Collections;

namespace ProSuite.AGP.WorkList.Domain.Persistence.Json
{
	public class JsonWorkItemStateRepository
		: WorkItemStateRepository<JsonWorkItemState, JsonBasedWorkListDefinition>
	{
		protected override void Store(JsonBasedWorkListDefinition definition)
		{
			throw new NotImplementedException();
		}

		protected override JsonBasedWorkListDefinition CreateDefinition(
			IDictionary<GdbWorkspaceIdentity, SimpleSet<GdbTableIdentity>> tablesByWorkspace,
			IList<ISourceClass> sourceClasses,
			List<JsonWorkItemState> states)
		{
			throw new NotImplementedException();
		}

		protected override JsonWorkItemState CreateState(IWorkItem item)
		{
			throw new NotImplementedException();
		}

		protected override List<JsonWorkItemState> ReadStates()
		{
			throw new NotImplementedException();
		}

		public JsonWorkItemStateRepository(string name, Type type, int? currentItemIndex = null) :
			base(name, type, currentItemIndex) { }
	}
}
