using System.Collections.Generic;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.WorkList.Test;

public class WorkItemStateRepositoryMock : IWorkItemStateRepository
{
	public void Refresh(IWorkItem item) { }

	public void UpdateState(IWorkItem item) { }

	public void Commit(IList<ISourceClass> sourceClasses, Envelope extent) { }

	public int? CurrentIndex { get; set; }

	public void Rename(string name) { }

	public string WorkListDefinitionFilePath { get; set; }
}
