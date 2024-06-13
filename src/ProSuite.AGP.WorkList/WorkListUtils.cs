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
using ProSuite.AGP.WorkList.Domain.Persistence;
using ProSuite.AGP.WorkList.Domain.Persistence.Xml;
using ProSuite.Commons.Ado;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.IO;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Text;
using ProSuite.Commons.Xml;
using ProSuite.DomainModel.Core;

namespace ProSuite.AGP.WorkList
{
	public static class WorkListUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private const string WorklistsFolder = "Worklists";
		private const string PluginIdentifier = "ProSuite_WorkListDatasource";

		[NotNull]
		public static string GetLocalWorklistsFolder(string homeFolderPath)
		{
			return Path.Combine(homeFolderPath, WorklistsFolder);
		}

		[NotNull]
		public static string GetDatasource([NotNull] string homeFolderPath,
		                                   [NotNull] string workListName,
		                                   [NotNull] string fileSuffix)
		{
			//var baseUri = new Uri("worklist://localhost/");
			string folder = GetLocalWorklistsFolder(homeFolderPath);

			if (! FileSystemUtils.EnsureDirectoryExists(folder))
			{
				Assert.True(Directory.Exists(homeFolderPath), $"{homeFolderPath} does not exist");
				return homeFolderPath;
			}

			return Path.Combine(folder, $"{workListName}{fileSuffix}");
		}

		[NotNull]
		public static IWorkList Create([NotNull] XmlWorkListDefinition definition,
		                               [NotNull] string displayName)
		{
			Assert.ArgumentNotNull(definition, nameof(definition));
			Assert.ArgumentNotNullOrEmpty(displayName, nameof(displayName));

			try
			{
				var descriptor = new ClassDescriptor(definition.TypeName, definition.AssemblyName);

				IWorkItemRepository workItemRepository = CreateWorkItemRepository(definition);

				return descriptor.CreateInstance<IWorkList>(workItemRepository,
				                                            definition.Name, displayName);
			}
			catch (Exception e)
			{
				_msg.Error("Cannot create work list", e);
				throw;
			}
		}

		public static IWorkItemRepository CreateWorkItemRepository(
			[NotNull] XmlWorkListDefinition xmlWorkListDefinition)
		{
			Assert.ArgumentNotNull(xmlWorkListDefinition, nameof(xmlWorkListDefinition));

			var descriptor = new ClassDescriptor(xmlWorkListDefinition.TypeName,
			                                     xmlWorkListDefinition.AssemblyName);

			Type type = descriptor.GetInstanceType();

			IWorkItemStateRepository stateRepository =
				CreateItemStateRepository(xmlWorkListDefinition, type);

			List<Table> tables = GetDistinctTables(
				xmlWorkListDefinition.Workspaces, xmlWorkListDefinition.Name,
				xmlWorkListDefinition.Path, out NotificationCollection notifications);

			if (tables.Count == 0)
			{
				return EmptyWorkItemRepository(type, stateRepository);
			}

			IWorkItemRepository repository;

			var sourceClasses = new List<Tuple<Table, string>>();

			if (type == typeof(IssueWorkList))
			{
				// Issue source classes: table/definition query pairs
				foreach (XmlWorkListWorkspace xmlWorkspace in xmlWorkListDefinition.Workspaces)
				{
					foreach (XmlTableReference tableReference in xmlWorkspace.Tables)
					{
						Table table =
							tables.FirstOrDefault(t => t.GetName() == tableReference.Name);

						if (table == null)
						{
							continue;
						}

						// TODO: Get Status Schema from XML too
						sourceClasses.Add(
							new Tuple<Table, string>(table, tableReference.DefinitionQuery));
					}
				}

				repository =
					new IssueItemRepository(sourceClasses, stateRepository);
			}
			else if (type == typeof(SelectionWorkList))
			{
				// Selection source classes: tables/oids pairs
				Dictionary<long, Table> tablesById = new Dictionary<long, Table>();
				foreach (Table table in tables)
				{
					var gdbTableIdentity = new GdbTableIdentity(table);

					long uniqueTableId = GetUniqueTableIdAcrossWorkspaces(gdbTableIdentity);

					tablesById.Add(uniqueTableId, table);
				}

				Dictionary<Table, List<long>> oidsByTable =
					GetOidsByTable(xmlWorkListDefinition.Items, tablesById);

				if (oidsByTable.Count == 0)
				{
					_msg.Warn(
						"No items in selection work list or they could not be associated with an existing table.");
					return EmptyWorkItemRepository(type, stateRepository);
				}

				repository =
					new SelectionItemRepository(tables, oidsByTable, stateRepository);
			}
			else
			{
				throw new ArgumentException("Unknown work list type");
			}

			return repository;
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
		public static string GetIssueGeodatabasePath([NotNull] string worklistDefinitionFile)
		{
			Assert.ArgumentNotNullOrEmpty(worklistDefinitionFile, nameof(worklistDefinitionFile));

			if (! File.Exists(worklistDefinitionFile))
			{
				_msg.Debug($"{worklistDefinitionFile} does not exist");
				return null;
			}

			string extension = Path.GetExtension(worklistDefinitionFile);

			if (! string.Equals(extension, ".iwl"))
			{
				_msg.Debug($"{worklistDefinitionFile} is no issue work list");
				return null;
			}

			var helper = new XmlSerializationHelper<XmlWorkListDefinition>();

			XmlWorkListDefinition definition = helper.ReadFromFile(worklistDefinitionFile);
			List<XmlWorkListWorkspace> workspaces = definition.Workspaces;

			Assert.True(workspaces.Count > 0, $"no workspaces in {worklistDefinitionFile}");

			string result = workspaces[0].ConnectionString;

			if (workspaces.Count > 0)
			{
				_msg.Info(
					$"There are many issue geodatabases in {worklistDefinitionFile} but only one is expected. Taking the first one {result}");
			}
			else
			{
				_msg.Debug($"Found issue geodatabase {result} in {worklistDefinitionFile}");
			}

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
				                  Name = alias
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

		public static Dictionary<Datastore, List<Table>> GetDistinctTables(
			[NotNull] IEnumerable<Table> tables)
		{
			var result = new Dictionary<Datastore, SimpleSet<Table>>(new DatastoreComparer());

			foreach (Table table in tables.Distinct())
			{
				var datastore = table.GetDatastore();

				if (! result.ContainsKey(datastore))
				{
					result.Add(datastore, new SimpleSet<Table> { table });
				}
				else
				{
					result[datastore].TryAdd(table);
				}
			}

			return result.ToDictionary(pair => pair.Key, pair => pair.Value.ToList());
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

		private static IWorkItemStateRepository CreateItemStateRepository(
			[NotNull] XmlWorkListDefinition xmlWorkListDefinition,
			[NotNull] Type type)
		{
			string name = xmlWorkListDefinition.Name;
			string filePath = xmlWorkListDefinition.Path;
			int currentIndex = xmlWorkListDefinition.CurrentIndex;

			if (type == typeof(IssueWorkList))
			{
				return new XmlWorkItemStateRepository(filePath, name, type, currentIndex);
			}

			if (type == typeof(SelectionWorkList))
			{
				return new XmlSelectionItemStateRepository(filePath, name, type, currentIndex);
			}

			throw new ArgumentException($"Unknown work list type: {type.Name}");
		}

		private static IWorkItemRepository EmptyWorkItemRepository([NotNull] Type type,
			[NotNull] IWorkItemStateRepository itemStateRepository)
		{
			if (type == typeof(IssueWorkList))
			{
				return new IssueItemRepository(new List<Tuple<Table, string>>(0),
				                               itemStateRepository);
			}

			if (type == typeof(SelectionWorkList))
			{
				return new SelectionItemRepository(new List<Table>(),
				                                   new Dictionary<Table, List<long>>(),
				                                   itemStateRepository);
			}

			throw new ArgumentException($"Unknown work list type: {type.Name}");
		}

		[NotNull]
		private static List<Table> GetDistinctTables(
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
								new Uri(workspace.ConnectionString, UriKind.Absolute)));

					case WorkspaceFactory.SDE:
						var builder = new ConnectionStringBuilder(workspace.ConnectionString);

						Assert.True(
							Enum.TryParse(builder["dbclient"], ignoreCase: true,
							              out EnterpriseDatabaseType databaseType),
							$"Cannot parse {nameof(EnterpriseDatabaseType)} from connection string {workspace.ConnectionString}");

						Assert.True(
							Enum.TryParse(builder["authentication_mode"], ignoreCase: true,
							              out AuthenticationMode authMode),
							$"Cannot parse {nameof(AuthenticationMode)} from connection string {workspace.ConnectionString}");

						string instance = builder["instance"];

						// Typically the instance is saved as "sde:oracle11g:TOPGIST:SDE"
						if (databaseType == EnterpriseDatabaseType.Oracle)
						{
							// Real-world examples:
							// - "sde:oracle11g:TOPGIST:SDE"
							// - "sde:oracle$sde:oracle11g:gdzh"

							// NOTE: Sometimes the DB_CONNECTION_PROPERTIES contains the single instance name,
							//       but it can also contain the colon-separated components.

							string[] strings = instance?.Split(':');

							if (strings?.Length > 1)
							{
								string lastItem = strings[^1];

								if (lastItem.Equals("SDE", StringComparison.OrdinalIgnoreCase))
								{
									// Take the second last item
									instance = strings[^2];
								}
								else
								{
									instance = lastItem;
								}
							}
						}

						var connectionProperties =
							new DatabaseConnectionProperties(databaseType)
							{
								AuthenticationMode = authMode,
								ProjectInstance = builder["project_instance"],
								Database =
									builder[
										"server"], // is always null in CIMFeatureDatasetDataConnection
								Instance = instance,
								Version = builder["version"],
								Branch = builder["branch"], // ?
								Password = builder["encrypted_password"],
								User = builder["user"]
							};

						_msg.Debug(
							$"Opening workspace from connection string {workspace.ConnectionString} converted to {connectionProperties}");

						return new Geodatabase(connectionProperties);

					case WorkspaceFactory.Shapefile:
						return new FileSystemDatastore(
							new FileSystemConnectionPath(
								new Uri(workspace.ConnectionString, UriKind.Absolute),
								FileSystemDatastoreType.Shapefile));
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			catch (Exception e)
			{
				string message =
					$"Cannot open {workspace.WorkspaceFactory} workspace from connection string {workspace.ConnectionString} ({e.Message})";

				_msg.Debug(message, e);

				NotificationUtils.Add(notifications, $"{workspace.ConnectionString}");
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

			foreach (XmlWorkItemState item in xmlItems)
			{
				if (! tablesById.TryGetValue(item.Row.TableId, out Table table))
				{
					_msg.Warn(
						$"Table {item.Row.TableName} (UniqueID={item.Row.TableId}) not found in the " +
						$"list of available tables ({StringUtils.Concatenate(tablesById.Values, t => t.GetName(), ", ")}).");
					continue;
				}

				if (! result.ContainsKey(table))
				{
					result.Add(table, new List<long> { item.Row.OID });
				}
				else
				{
					List<long> oids = result[table];
					oids.Add(item.Row.OID);
				}
			}

			return result;
		}

		#endregion
	}
}
