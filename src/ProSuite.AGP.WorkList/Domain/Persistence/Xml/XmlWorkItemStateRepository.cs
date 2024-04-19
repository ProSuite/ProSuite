using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Xml;

namespace ProSuite.AGP.WorkList.Domain.Persistence.Xml
{
	public class XmlWorkItemStateRepository
		: WorkItemStateRepository<XmlWorkItemState, XmlWorkListDefinition>
	{
		private string _filePath;

		public string FilePath
		{
			get => _filePath;
			set => _filePath = value;
		}

		public XmlWorkItemStateRepository(string filePath, string name, Type type,
		                                  int? currentItemIndex = null) : base(
			name, type, currentItemIndex)
		{
			_filePath = filePath;
		}

		public static XmlWorkListDefinition Import(string xmlFilePath)
		{
			var helper = new XmlSerializationHelper<XmlWorkListDefinition>();

			XmlWorkListDefinition definition = helper.ReadFromFile(xmlFilePath);
			definition.Path = xmlFilePath;
			return definition;
		}

		protected override void Store(XmlWorkListDefinition definition)
		{
			var helper = new XmlSerializationHelper<XmlWorkListDefinition>();
			helper.SaveToFile(definition, _filePath);
		}

		protected override XmlWorkListDefinition CreateDefinition(
			IDictionary<GdbWorkspaceIdentity, SimpleSet<GdbTableIdentity>> tablesByWorkspace,
			IList<ISourceClass> sourceClasses,
			List<XmlWorkItemState> states)
		{
			int index = -1;
			if (CurrentIndex.HasValue)
			{
				index = CurrentIndex.Value;
			}

			var definition = new XmlWorkListDefinition
			                 {
				                 Name = Name, TypeName = Type.FullName,
				                 AssemblyName = Type.Assembly.GetName().Name, Items = states,
				                 CurrentIndex = index
			                 };

			definition.Items = states;

			var xmlWorkspaces = new List<XmlWorkListWorkspace>(tablesByWorkspace.Count);

			Populate(tablesByWorkspace, xmlWorkspaces, sourceClasses);

			definition.Workspaces = xmlWorkspaces;

			return definition;
		}

		protected override List<XmlWorkItemState> ReadStates()
		{
			if (! File.Exists(_filePath))
			{
				return new List<XmlWorkItemState>();
			}

			XmlWorkListDefinition definition = Import(_filePath);

			return definition.Items.ToList();
		}

		protected override XmlWorkItemState CreateState(IWorkItem item)
		{
			var state = new XmlWorkItemState(item.OID, item.Visited, WorkItemStatus.Unknown,
			                                 new XmlGdbRowIdentity(item.Proxy));

			state.ConnectionString = item.Proxy.Table.Workspace.ConnectionString;

			return state;
		}

		protected override IWorkItem RefreshCore(IWorkItem item, XmlWorkItemState state)
		{
			return item;
		}

		protected override void UpdateCore(XmlWorkItemState state, IWorkItem item)
		{
			state.Status = item.Status;
		}

		private static void Populate(
			IDictionary<GdbWorkspaceIdentity, SimpleSet<GdbTableIdentity>> tablesByWorkspace,
			ICollection<XmlWorkListWorkspace> list, IList<ISourceClass> sourceClasses)
		{
			foreach (KeyValuePair<GdbWorkspaceIdentity, SimpleSet<GdbTableIdentity>> pair in
			         tablesByWorkspace)
			{
				GdbWorkspaceIdentity workspace = pair.Key;
				SimpleSet<GdbTableIdentity> tables = pair.Value;

				var xmlWorkspace = new XmlWorkListWorkspace();
				xmlWorkspace.ConnectionString = workspace.ConnectionString;
				xmlWorkspace.WorkspaceFactory = workspace.WorkspaceFactory.ToString();

				xmlWorkspace.Tables = tables
				                      .Select(table => new XmlTableReference(table.Id, table.Name))
				                      .ToList();

				foreach (XmlTableReference xmlTableReference in xmlWorkspace.Tables)
				{
					ISourceClass sourceClass =
						sourceClasses.FirstOrDefault(s => s.Name == xmlTableReference.Name);

					if (sourceClass != null)
					{
						xmlTableReference.DefinitionQuery = sourceClass.DefinitionQuery;
					}
				}

				list.Add(xmlWorkspace);
			}
		}
	}
}
