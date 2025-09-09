using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Domain.Persistence.Xml
{
	public class XmlWorkItemStateRepository
		: WorkItemStateRepository<XmlWorkItemState, XmlWorkListDefinition>
	{
		public XmlWorkItemStateRepository(string filePath, string name, string displayName,
		                                  Type type, int? currentItemIndex = null)
			: base(name, displayName, type, currentItemIndex)
		{
			WorkListDefinitionFilePath = filePath;

			ReadStatesByRow();
		}

		public static XmlWorkListDefinition Import(string xmlFilePath)
		{
			XmlWorkListDefinition definition = WorkListUtils.Read(xmlFilePath);
			definition.Path = xmlFilePath;
			return definition;
		}

		protected override void Store(XmlWorkListDefinition definition)
		{
			WorkListUtils.Save(definition, WorkListDefinitionFilePath);
		}

		protected override XmlWorkListDefinition CreateDefinition(
			IDictionary<GdbWorkspaceIdentity, SimpleSet<GdbTableIdentity>> tablesByWorkspace,
			IList<ISourceClass> sourceClasses,
			IEnumerable<XmlWorkItemState> states,
			Envelope extent)
		{
			int index = -1;
			if (CurrentIndex.HasValue)
			{
				index = CurrentIndex.Value;
			}

			var definition = new XmlWorkListDefinition
			                 {
				                 Name = Name,
				                 DisplayName = DisplayName,
				                 TypeName = Type.FullName,
				                 AssemblyName = Type.Assembly.GetName().Name,
				                 CurrentIndex = index
			                 };

			definition.Extent = extent?.ToXml();

			definition.Items = new List<XmlWorkItemState>();

			var xmlWorkspaces = new List<XmlWorkListWorkspace>(tablesByWorkspace.Count);

			PopulateXmlWorkspaceList(xmlWorkspaces, sourceClasses);

			// NOTE: During upgrade from 1.2.x to 1.3.x, there can be duplicate OIDs of which one
			// has an invalid TableId.
			HashSet<long> tableIds = new HashSet<long>();

			foreach (XmlTableReference table in xmlWorkspaces.SelectMany(
				         workspace => workspace.Tables))
			{
				tableIds.Add(table.Id);
			}

			definition.Workspaces = xmlWorkspaces;

			foreach (XmlWorkItemState state in states)
			{
				if (tableIds.Contains(state.Row.TableId))
				{
					definition.Items.Add(state);
				}
			}

			return definition;
		}

		protected override void ReadStatesByRowCore()
		{
			if (! File.Exists(WorkListDefinitionFilePath))
			{
				base.ReadStatesByRowCore();
				return;
			}

			XmlWorkListDefinition definition = Import(WorkListDefinitionFilePath);

			// Challenge: Re-create the GdbRowIdentity from the XmlWorkItemState:
			// NOTE: Either we re-open the workspaces or we also maintain the workspaces here
			//       and associate them with the connection string from the XmlWorkItemState.
			//       It is probably easier to just make sure the TableId is unique across all
			//       SourceClasses and workspaces and use GdbObjectReference instead of GdbRowIdentity.
			//       The table Id could be defined as follows:
			//     - If Table.GetId() < 0 -> unregistered table, such as shapefile. Use Hash of Name as Id.
			//       Hash collisions should be detected at the first creation and result in an error (extremely unlikely).
			//       Possibly just use the hash of the table name (without the full path) to support moving the data (TODO: proper support, relative paths etc.).
			//     - If Table.GetId() >= 0 -> registered table, such as feature class. Use Table.GetId() hashed with
			//       workspace (connection string?) Or just the hash of the folder/Fgdb name to support moving the data
			//       while maintaining a stable ID.
			// Current state: Use the TableId, and do not support multiple workspaces (with potentially duplicate table IDs).
			// TODO: Adapt XML/JSON format to a structure with less duplication.

			foreach (XmlWorkItemState itemState in definition.Items)
			{
				// We cannot easily re-create the WorkspaceIdentity from the XmlWorkItemState
				// with the current information in the Xml
				var objectReference =
					new GdbObjectReference(itemState.Row.TableId, itemState.Row.OID);

				StatesByRow.Add(objectReference, itemState);
			}
		}

		protected override XmlWorkItemState CreateState(IWorkItem item)
		{
			// Persist the unique and stable table ID instead of the standard TableId.
			var xmlGdbRowIdentity = new XmlGdbRowIdentity(item.GdbRowProxy, item.UniqueTableId);

			var state = new XmlWorkItemState(item.OID, item.Visited, WorkItemStatus.Unknown,
			                                 xmlGdbRowIdentity);

			if (item.Extent != null)
			{
				state.XMin = item.Extent.XMin;
				state.XMax = item.Extent.XMax;
				state.YMin = item.Extent.YMin;
				state.YMax = item.Extent.YMax;
			}

			state.ConnectionString = item.GdbRowProxy.Table.Workspace.ConnectionString;

			return state;
		}

		protected override void RefreshCore(IWorkItem item, XmlWorkItemState state) { }

		protected override void UpdateStateCore(XmlWorkItemState state, IWorkItem item)
		{
			state.Status = item.Status;
		}

		public override void Rename(string name)
		{
			string directoryName = Path.GetDirectoryName(WorkListDefinitionFilePath);
			Assert.NotNull(directoryName);

			string extension = Path.GetExtension(WorkListDefinitionFilePath);
			Assert.NotNull(extension);

			string path = Path.Combine(directoryName, $"{name}{extension}");

			WorkListDefinitionFilePath = path;
		}

		private static void PopulateXmlWorkspaceList(
			[NotNull] ICollection<XmlWorkListWorkspace> resultList,
			[NotNull] IList<ISourceClass> sourceClasses)
		{
			var tablesByWorkspace =
				new Dictionary<GdbWorkspaceIdentity, SimpleSet<XmlTableReference>>();

			foreach (ISourceClass sourceClass in sourceClasses)
			{
				GdbTableIdentity tableIdentity = sourceClass.TableIdentity;
				GdbWorkspaceIdentity workspaceIdentity = tableIdentity.Workspace;

				if (! tablesByWorkspace.TryGetValue(workspaceIdentity,
				                                    out SimpleSet<XmlTableReference> tables))
				{
					tables = new SimpleSet<XmlTableReference>();
					tablesByWorkspace.Add(workspaceIdentity, tables);
				}

				XmlTableReference xmlTableReference =
					CreateXmlTableReference(tableIdentity, sourceClass);

				tables.TryAdd(xmlTableReference);
			}

			foreach (var pair in tablesByWorkspace)
			{
				GdbWorkspaceIdentity workspace = pair.Key;
				SimpleSet<XmlTableReference> tables = pair.Value;

				var xmlWorkspace = new XmlWorkListWorkspace();
				xmlWorkspace.ConnectionString = workspace.ConnectionString;
				xmlWorkspace.WorkspaceFactory = workspace.WorkspaceFactory.ToString();

				xmlWorkspace.Tables = tables.ToList();
				resultList.Add(xmlWorkspace);
			}
		}

		private static XmlTableReference CreateXmlTableReference(GdbTableIdentity tableIdentity,
		                                                         ISourceClass sourceClass)
		{
			var xmlTableReference =
				new XmlTableReference
				{
					Id = sourceClass.GetUniqueTableId(),
					Name = tableIdentity.Name,
					DefinitionQuery = sourceClass.DefinitionQuery
				};

			if (sourceClass is DatabaseSourceClass dbStatusSourceClass)
			{
				xmlTableReference.StatusFieldName = dbStatusSourceClass.StatusField;
				xmlTableReference.StatusValueTodo = (int) dbStatusSourceClass.TodoValue;
				xmlTableReference.StatusValueDone = (int) dbStatusSourceClass.DoneValue;
			}

			return xmlTableReference;
		}
	}
}
