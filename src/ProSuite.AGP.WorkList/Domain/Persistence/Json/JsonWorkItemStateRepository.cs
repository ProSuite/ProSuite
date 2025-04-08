using System;
using System.Collections.Generic;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Core.Geodatabase;
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
			IEnumerable<JsonWorkItemState> states)
		{
			throw new NotImplementedException();
		}

		protected override JsonWorkItemState CreateState(IWorkItem item)
		{
			throw new NotImplementedException();
		}

		protected virtual List<JsonWorkItemState> ReadStates()
		{
			throw new NotImplementedException();
		}

		protected override void ReadStatesByRowCore()
		{
			throw new NotImplementedException();
		}

		public JsonWorkItemStateRepository(string name, Type type, int? currentItemIndex = null) :
			base(name, type, currentItemIndex) { }

		public string WorkListDefinitionFilePath { get; set; }
	}
}
