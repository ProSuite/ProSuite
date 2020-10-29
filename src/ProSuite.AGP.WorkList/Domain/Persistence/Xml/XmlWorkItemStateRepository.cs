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
	public class XmlWorkItemStateRepository : WorkItemStateRepository<XmlWorkItemState, XmlWorkListDefinition>
	{
		private readonly string _xmlFilePath;

		public XmlWorkItemStateRepository(string xmlPath, string name, Type type,
		                                  int? currentItemIndex = null) : base(name, type, currentItemIndex)
		{
			_xmlFilePath = xmlPath;
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
			helper.SaveToFile(definition, _xmlFilePath);
		}

		protected override XmlWorkListDefinition CreateDefinition(
			Dictionary<GdbWorkspaceIdentity, SimpleSet<GdbTableIdentity>> tablesByWorkspace,
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

			Populate(tablesByWorkspace, xmlWorkspaces);

			definition.Workspaces = xmlWorkspaces;

			return definition;
		}

		protected override List<XmlWorkItemState> ReadStates()
		{
			if (! File.Exists(_xmlFilePath))
			{
				return new List<XmlWorkItemState>();
			}

			XmlWorkListDefinition definition = Import(_xmlFilePath);

			return definition.Items.ToList();
		}

		protected override XmlWorkItemState CreateState(IWorkItem item)
		{
			var state = new XmlWorkItemState(item.OID, item.Visited, WorkItemStatus.Unknown,
			                            new XmlGdbRowIdentity(item.Proxy));

			state.Path = item.Proxy.Table.Workspace.Path;

			return state;
		}

		protected override IWorkItem RefreshCore(IWorkItem item, XmlWorkItemState state)
		{
			item.Status = state.Status;

			return item;
		}

		protected override void UpdateCore(XmlWorkItemState state, IWorkItem item)
		{
			state.Status = item.Status;
		}

		private static void Populate(
			Dictionary<GdbWorkspaceIdentity, SimpleSet<GdbTableIdentity>> tablesByWorkspace,
			ICollection<XmlWorkListWorkspace> list)
		{
			foreach (KeyValuePair<GdbWorkspaceIdentity, SimpleSet<GdbTableIdentity>> pair in
				tablesByWorkspace)
			{
				GdbWorkspaceIdentity workspace = pair.Key;
				SimpleSet<GdbTableIdentity> tables = pair.Value;

				var xmlWorkspace = new XmlWorkListWorkspace();
				xmlWorkspace.Path = workspace.Path;
				xmlWorkspace.Tables = tables
				                      .Select(table => new XmlTableReference(table.Id, table.Name))
				                      .ToList();

				list.Add(xmlWorkspace);
			}
		}
	}
}
