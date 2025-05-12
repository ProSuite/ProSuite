using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.AGP.WorkList.Domain.Persistence.Xml;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Text;
using ProSuite.Commons.Xml;

namespace ProSuite.AGP.WorkList
{
	public static class WorkListUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private const string PluginIdentifier = "ProSuite_WorkListDatasource";

		// TODO: (daro) really needed?
		public static IWorkItemRepository CreateSelectionItemRepository(
			List<Table> tables,
			IWorkItemStateRepository stateRepository,
			XmlWorkListDefinition definition)
		{
			Dictionary<long, Table> tablesById = new Dictionary<long, Table>();

			foreach (Table table in tables)
			{
				var gdbTableIdentity = new GdbTableIdentity(table);

				long uniqueTableId = GetUniqueTableIdAcrossWorkspaces(gdbTableIdentity);

				tablesById.TryAdd(uniqueTableId, table);
			}

			Dictionary<Table, List<long>> oidsByTable =
				GetOidsByTable(definition.Items, tablesById);

			if (oidsByTable.Count == 0)
			{
				_msg.Warn(
					"No items in selection work list or they could not be associated with an existing table.");
				return new SelectionItemRepository(new List<SelectionSourceClass>(), stateRepository);
			}

			IList<SelectionSourceClass> sourceClasses = new List<SelectionSourceClass>(oidsByTable.Count);

			foreach ((Table table, List<long> oids) in oidsByTable)
			{
				using TableDefinition tableDefinition = table.GetDefinition();

				string objectIDField = tableDefinition.GetObjectIDField();

				string shapeField = null;

				if (tableDefinition is FeatureClassDefinition featureClassDefinition)
				{
					shapeField = featureClassDefinition.GetShapeField();
				}

				var schema = new SourceClassSchema(objectIDField, shapeField);

				// todo: daro inline
				Datastore datastore = table.GetDatastore();
				var sourceClass = new SelectionSourceClass(new GdbTableIdentity(table), datastore, schema, oids, null);
				sourceClasses.Add(sourceClass);
			}

			return new SelectionItemRepository(sourceClasses, stateRepository);
		}

		[NotNull]
		public static string GetName([CanBeNull] string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return string.Empty;
			}

			int index = path.LastIndexOf('/');
			if (index >= 0)
				path = path.Substring(index + 1);
			index = path.LastIndexOf('\\');
			if (index >= 0)
				path = path.Substring(index + 1);

			// scheme://Host:Port/AbsolutePath?Query#Fragment
			// worklist://localhost/workListName?unused&for#now

			// work list file => WORKLISTNAME.xml.wl
			string temp = Path.GetFileNameWithoutExtension(path);
			return Path.GetFileNameWithoutExtension(temp);
		}

		// todo daro rename GetNameFromUri?
		public static string ParseName(string layerUri)
		{
			int index = layerUri.LastIndexOf('/');
			if (index < 0)
			{
				throw new ArgumentException($"{layerUri} is not a valid layer URI");
			}

			string name = layerUri.Substring(index + 1);
			return Path.GetFileNameWithoutExtension(name);
		}

		[CanBeNull]
		public static string GetIssueGeodatabasePath([NotNull] string worklistDefinitionFile,
		                                             out string message)
		{
			Assert.ArgumentNotNullOrEmpty(worklistDefinitionFile, nameof(worklistDefinitionFile));

			if (! File.Exists(worklistDefinitionFile))
			{
				message = $"{worklistDefinitionFile} does not exist";
				_msg.Debug(message);
				return null;
			}

			string extension = Path.GetExtension(worklistDefinitionFile);

			if (! string.Equals(extension, ".iwl"))
			{
				message = $"{worklistDefinitionFile} is no issue work list";
				_msg.Debug(message);
				return null;
			}

			var helper = new XmlSerializationHelper<XmlWorkListDefinition>();

			XmlWorkListDefinition definition = helper.ReadFromFile(worklistDefinitionFile);
			List<XmlWorkListWorkspace> workspaces = definition.Workspaces;

			Assert.True(workspaces.Count > 0,
			            $"No workspaces referenced in {worklistDefinitionFile}. The work list might be empty.");

			string result = workspaces[0].ConnectionString;

			if (workspaces.Count > 1)
			{
				_msg.Info(
					$"There are several issue geodatabases in {worklistDefinitionFile} but only one is expected. Taking the first one {result}");
			}
			else if (result != null &&
			         ! result.EndsWith(".gdb", StringComparison.InvariantCultureIgnoreCase))
			{
				message = $"{result} is no issue FileGeodatabase";
				_msg.Debug(message);
				return null;
			}
			else
			{
				_msg.Debug($"Found issue geodatabase {result} in {worklistDefinitionFile}");
			}

			message = null;
			return result;
		}

		[CanBeNull]
		public static string GetWorklistName([NotNull] string worklistDefinitionFile)
		{
			Assert.ArgumentNotNullOrEmpty(worklistDefinitionFile, nameof(worklistDefinitionFile));

			if (! File.Exists(worklistDefinitionFile))
			{
				_msg.Debug($"{worklistDefinitionFile} does not exist");
				return null;
			}

			var helper = new XmlSerializationHelper<XmlWorkListDefinition>();
			XmlWorkListDefinition definition = helper.ReadFromFile(worklistDefinitionFile);
			return definition.Name;
		}

		[CanBeNull]
		public static string GetWorklistName([NotNull] string worklistDefinitionFile,
		                                     [CanBeNull] out string typeName)
		{
			Assert.ArgumentNotNullOrEmpty(worklistDefinitionFile, nameof(worklistDefinitionFile));

			typeName = null;

			if (! File.Exists(worklistDefinitionFile))
			{
				_msg.Debug($"{worklistDefinitionFile} does not exist");
				return null;
			}

			var helper = new XmlSerializationHelper<XmlWorkListDefinition>();
			XmlWorkListDefinition definition = helper.ReadFromFile(worklistDefinitionFile);
			typeName = definition.TypeName;

			return definition.Name;
		}

		public static void MoveTo([NotNull] List<IWorkItem> items,
		                          [NotNull] IWorkItem movingItem,
		                          int insertIndex)
		{
			Assert.ArgumentNotNull(items, nameof(items));
			Assert.ArgumentNotNull(movingItem, nameof(movingItem));
			Assert.ArgumentCondition(insertIndex >= 0 && insertIndex < items.Count,
			                         "insert index out of range: {0}", insertIndex);

			CollectionUtils.MoveTo(items, movingItem, insertIndex);
		}

		public static PluginDatastore GetPluginDatastore([NotNull] Uri dataSource)
		{
			Assert.ArgumentNotNull(dataSource, nameof(dataSource));

			return new PluginDatastore(
				new PluginDatasourceConnectionPath(PluginIdentifier, dataSource));
		}

		[NotNull]
		public static FeatureLayerCreationParams CreateLayerParams(
			[NotNull] FeatureClass featureClass, string alias = null)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			if (string.IsNullOrEmpty(alias))
			{
				alias = featureClass.GetName();
			}

			var layerParams = new FeatureLayerCreationParams(featureClass)
			                  {
				                  IsVisible = true,
				                  Name = alias,
				                  MapMemberPosition = MapMemberPosition.AddToTop
			                  };

			// todo daro: apply renderer here from template

			// LayerDocument is null!
			//LayerDocument template
			//CIMDefinition layerDefinition = layerParams.LayerDocument.LayerDefinitions[0];

			//var uniqueValueRenderer = GetRenderer<CIMUniqueValueRenderer>(template);

			//if (uniqueValueRenderer != null)
			//{
			//	((CIMFeatureLayer) layerDefinition).Renderer = uniqueValueRenderer;
			//}

			return layerParams;
		}

		public static long GetUniqueTableIdAcrossWorkspaces(GdbTableIdentity tableIdentity)
		{
			// NOTE: Do not use string.GetHashCode() because it is not guaranteed to be stable!

			if (tableIdentity.Id < 0)
			{
				// Un-registered table: Use hash of the table name (without the workspace name to support changed relative paths)
				// Uniqueness will be checked by the repository
				return HashString(tableIdentity.Name);
			}

			// Registered table: Use hash of the table name combined with the table ID
			long result = HashString(tableIdentity.Name);
			result = result * 31 + tableIdentity.Id;

			return result;
		}

		private static long HashString([NotNull] string text)
		{
			unchecked
			{
				long hash = 23;
				foreach (char c in text)
				{
					hash = hash * 31 + c;
				}

				return hash;
			}
		}

		#region Repository creation

		public static IWorkItemStateRepository CreateItemStateRepository(
			string path, string workListName, Type workListType, int currentIndex)
		{
			if (typeof(DbStatusWorkList).IsAssignableFrom(workListType))
			{
				return new XmlWorkItemStateRepository(path, workListName, workListType, currentIndex);
			}

			if (typeof(SelectionWorkList).IsAssignableFrom(workListType))
			{
				return new XmlSelectionItemStateRepository(path, workListName, workListType, currentIndex);
			}

			throw new ArgumentException($"Unknown work list type: {workListType.Name}");
		}

		[NotNull]
		public static List<Table> GetDistinctTables(
			ICollection<XmlWorkListWorkspace> workspaces,
			string worklistName, string workListPath,
			out NotificationCollection dataStoreNotifications)
		{
			var result = new Dictionary<Datastore, List<Table>>(workspaces.Count);

			dataStoreNotifications = new NotificationCollection();
			var tableNotifications = new NotificationCollection();

			foreach (XmlWorkListWorkspace workspace in workspaces)
			{
				var datastore = GetDatastore(workspace, dataStoreNotifications);

				if (datastore == null)
				{
					continue;
				}

				if (result.ContainsKey(datastore))
				{
					_msg.Debug($"Duplicate workspace {workspace.ConnectionString}");
					continue;
				}

				// TODO: Same behaviour as for GetDatastore if a table cannot be opened
				List<Table> tables = GetDistinctTables(workspace, datastore, tableNotifications);
				result.Add(datastore, tables);
			}

			if (dataStoreNotifications.Count == 0 && tableNotifications.Count == 0)
			{
				return result.SelectMany(pair => pair.Value).ToList();
			}

			// Something went wrong, make the work list unusable

			if (dataStoreNotifications.Count > 0)
			{
				_msg.Warn(
					$"{worklistName}: Cannot open work item workspace(s) from connection strings specified in work list file:" +
					Environment.NewLine + workListPath +
					Environment.NewLine + "No items will be loaded." +
					Environment.NewLine +
					$"{dataStoreNotifications.Concatenate(Environment.NewLine)}");
			}

			if (tableNotifications.Count > 0)
			{
				_msg.Warn(
					$"{worklistName}: Cannot open work item table(s) specified in work list file:" +
					Environment.NewLine + workListPath +
					Environment.NewLine + "No items will be loaded." +
					Environment.NewLine + $"{tableNotifications.Concatenate(Environment.NewLine)}");
			}

			return new List<Table>(0);
		}

		[CanBeNull]
		private static Datastore GetDatastore([NotNull] XmlWorkListWorkspace workspace,
		                                      [NotNull] NotificationCollection notifications)
		{
			// TODO: Find a solution for SDE files. The original SDE files are not provided by the
			// workspace! The geodatabase path is always a local temp file, such as
			// ...AppData\\Local\\Temp\\ArcGISProTemp55352\\84864a323a7c4bd2802815271f9afaa3.sde
			// We would need to go through the Project Items, find the connection files and compare
			// the connection properties of each SDE file with the current connection!
			// This behaviour should probably be an option only if we find no better way of re-opening
			// the connection using the encrypted password.
			// Other work-around (to be tested!): Delay the opening of the referenced tables and hope the workspace
			// becomes valid if any of the other layers in the map reference the exact same workspace.

			// TODO: In case of FGDB/Shapefile, support relative path to worklist file

			// DBCLIENT = oracle
			// AUTHENTICATION_MODE = DBMS
			// PROJECT_INSTANCE = sde
			// ENCRYPTED_PASSWORD = 00022e684d4b4235766e4b6e324833335277647064696e734e586f584269575652504534653763387763674876504d3d2a00
			// SERVER = topgist
			// INSTANCE = sde:oracle11g: topgist
			// VERSION = SDE.DEFAULT
			// DB_CONNECTION_PROPERTIES = topgist
			// USER = topgis_tlm

			string connectionString = workspace.ConnectionString;

			try
			{
				Assert.True(
					Enum.TryParse(workspace.WorkspaceFactory, ignoreCase: true,
					              out WorkspaceFactory factory),
					$"Cannot parse {nameof(WorkspaceFactory)} from string {workspace.WorkspaceFactory}");

				switch (factory)
				{
					case WorkspaceFactory.FileGDB:
						return new Geodatabase(
							new FileGeodatabaseConnectionPath(
								new Uri(connectionString, UriKind.Absolute)));

					case WorkspaceFactory.SDE:
						DatabaseConnectionProperties connectionProperties =
							WorkspaceUtils.GetConnectionProperties(connectionString);

						_msg.Debug(
							$"Opening workspace from connection string {connectionString} " +
							$"converted to {WorkspaceUtils.ConnectionPropertiesToString(connectionProperties)}");

						return new Geodatabase(connectionProperties);

					case WorkspaceFactory.Shapefile:
						return new FileSystemDatastore(
							new FileSystemConnectionPath(
								new Uri(connectionString, UriKind.Absolute),
								FileSystemDatastoreType.Shapefile));
					case WorkspaceFactory.Custom:
						return new PluginDatastore(
							new PluginDatasourceConnectionPath(
								PluginIdentifier, new Uri(connectionString, UriKind.Absolute)));
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			catch (Exception e)
			{
				string message =
					$"Cannot open {workspace.WorkspaceFactory} workspace from connection string {connectionString} ({e.Message})";

				_msg.Debug(message, e);

				NotificationUtils.Add(notifications, $"{connectionString}");
				return null;
			}
		}

		private static List<Table> GetDistinctTables([NotNull] XmlWorkListWorkspace workspace,
		                                             [NotNull] Datastore datastore,
		                                             [NotNull] NotificationCollection notifications)
		{
			var distinctTables = new Dictionary<GdbTableIdentity, Table>();

			foreach (XmlTableReference tableReference in workspace.Tables)
			{
				try
				{
					Table table = DatasetUtils.OpenDataset<Table>(datastore, tableReference.Name);

					var id = new GdbTableIdentity(table);

					distinctTables.TryAdd(id, table);
				}
				catch (Exception e)
				{
					string message =
						$"{tableReference.Name}: {e.Message} (Workspace {workspace.ConnectionString})";

					_msg.Debug(message, e);

					NotificationUtils.Add(notifications, message);
				}
			}

			return notifications.Count > 0 ? new List<Table>(0) : distinctTables.Values.ToList();
		}

		private static Dictionary<Table, List<long>> GetOidsByTable(
			IEnumerable<XmlWorkItemState> xmlItems, IDictionary<long, Table> tablesById)
		{
			var result = new Dictionary<Table, List<long>>();

			// TODO: Delete backward compatibility at ca. 1.5
			bool backwardCompatibleLoading = false;
			foreach (XmlWorkItemState item in xmlItems)
			{
				if (! tablesById.TryGetValue(item.Row.TableId, out Table table))
				{
					// Not found by table ID. For backward compatibility, try Id:
					table =
						tablesById.Values.FirstOrDefault(t => t.GetID() == item.Row.TableId);

					if (table == null)
					{
						_msg.Warn(
							$"Table {item.Row.TableName} (UniqueID={item.Row.TableId}) not found in the " +
							$"list of available tables ({StringUtils.Concatenate(tablesById.Values, t => t.GetName(), ", ")}).");
						continue;
					}

					// Found by legacy (version 1.2.x) table ID
					backwardCompatibleLoading = true;
				}

				if (! result.ContainsKey(table))
				{
					result.Add(table, new List<long> { item.Row.OID });
				}
				else
				{
					List<long> oids = result[table];

					// Prevent duplicates (duplicates would happen on upgrading)
					if (! backwardCompatibleLoading || ! oids.Contains(item.Row.OID))
					{
						oids.Add(item.Row.OID);
					}
				}
			}

			return result;
		}

		#endregion
	}
}
