using System;
using System.Collections.Generic;
using System.IO;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain.Persistence.Xml;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Xml;

namespace ProSuite.AGP.WorkList.Domain.Persistence;

public class EmptyWorkItemStateRepository : IWorkItemStateRepository
{
	public EmptyWorkItemStateRepository(string filePath, string name, Type type)
	{
		WorkListDefinitionFilePath = filePath;
		Name = name;
		Type = type;
	}

	public string Name { get; }
	public Type Type { get; }

	public string WorkListDefinitionFilePath { get; set; }

	public void Refresh(IWorkItem item) { }

	public void UpdateState(IWorkItem item) { }

	public void Commit(IList<ISourceClass> sourceClasses)
	{
		int index = -1;
		if (CurrentIndex.HasValue)
		{
			index = CurrentIndex.Value;
		}

		var definition = new XmlWorkListDefinition
		                 {
			                 Name = Name,
			                 TypeName = Type.FullName,
			                 AssemblyName = Type.Assembly.GetName().Name,
			                 CurrentIndex = index
		                 };

		definition.Items = new List<XmlWorkItemState>(0);
		definition.Workspaces = new List<XmlWorkListWorkspace>(0);

		WorkListUtils.Save(definition, WorkListDefinitionFilePath);
	}

	public int? CurrentIndex { get; set; }

	public void Rename(string name)
	{
		string directoryName = Path.GetDirectoryName(WorkListDefinitionFilePath);
		Assert.NotNull(directoryName);

		string extension = Path.GetExtension(WorkListDefinitionFilePath);
		Assert.NotNull(extension);

		string path = Path.Combine(directoryName, $"{name}{extension}");

		WorkListDefinitionFilePath = path;
	}
}
