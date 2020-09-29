using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.AGP.WorkList.Domain.Persistence.Xml;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.DomainModel.Core;

namespace ProSuite.AGP.WorkList
{
	public static class WorkListUtils
	{
		public static IWorkList Create(XmlWorkListDefinition definition)
		{
			Dictionary<Geodatabase, List<Table>> tablesByGeodatabase = GetTablesByGeodatabase(definition.Workspaces);

			Dictionary<long, Table> tablesById =
				tablesByGeodatabase.Values
				                   .SelectMany(table => table)
				                   .ToDictionary(table => new GdbTableIdentity(table).Id,  table => table);

			Dictionary<Table, List<long>> oidsByTable = GetOidsByTable(definition.Items, tablesById);

			var descriptor = new ClassDescriptor(definition.TypeName, definition.AssemblyName);

			var stateRepository = new XmlWorkItemStateRepository(definition.Path, definition.Name, descriptor.GetInstanceType(), definition.CurrentIndex);
			var repository = new SelectionItemRepository(tablesByGeodatabase, oidsByTable, stateRepository);

			return new SelectionWorkList(repository, definition.Name);
		}

		private static Dictionary<Geodatabase, List<Table>> GetTablesByGeodatabase(ICollection<XmlWorkListWorkspace> workspaces)
		{
			var result = new Dictionary<Geodatabase, List<Table>>(workspaces.Count);

			foreach (XmlWorkListWorkspace workspace in workspaces)
			{
				var geodatabase =
					new Geodatabase(
						new FileGeodatabaseConnectionPath(new Uri(workspace.Path, UriKind.Absolute)));

				if (result.ContainsKey(geodatabase))
				{
					// todo daro
				}

				List<Table> tables = GetDistinctTables(workspace, geodatabase);
				result.Add(geodatabase, tables);
			}

			return result;
		}

		private static List<Table> GetDistinctTables(XmlWorkListWorkspace workspace, Geodatabase geodatabase)
		{
			var distinctTables = new Dictionary<GdbTableIdentity, Table>();
			foreach (XmlTableReference tableReference in workspace.Tables)
			{
				var table = geodatabase.OpenDataset<Table>(tableReference.Name);
				var id = new GdbTableIdentity(table);
				if (! distinctTables.ContainsKey(id))
				{
					distinctTables.Add(id, table);
				}
			}

			return distinctTables.Values.ToList();
		}

		private static Dictionary<Table, List<long>> GetOidsByTable(IEnumerable<XmlWorkItemState> xmlItems, IDictionary<long, Table> tablesById)
		{
			var result = new Dictionary<Table, List<long>>();

			foreach (XmlWorkItemState item in xmlItems)
			{
				if (! tablesById.TryGetValue(item.Row.TableId, out Table table))
				{
					continue;
				}

				if (! result.ContainsKey(table))
				{
					result.Add(table, new List<long> {item.Row.OID});
				}
				else
				{
					List<long> oids = result[table];
					oids.Add(item.Row.OID);
				}
			}

			return result;
		}
	}
}
