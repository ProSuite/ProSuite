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
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.IO;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;
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

			if (! FileSystemUtils.EnsureFolderExists(folder))
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

				return descriptor.CreateInstance<IWorkList>(CreateWorkItemRepository(definition),
				                                            definition.Name, displayName);
			}
			catch (Exception e)
			{
				_msg.Error("Cannot create work list", e);
				throw;
			}
		}

		public static IWorkItemRepository CreateWorkItemRepository(
			[NotNull] XmlWorkListDefinition definition)
		{
			Assert.ArgumentNotNull(definition, nameof(definition));

			var descriptor = new ClassDescriptor(definition.TypeName, definition.AssemblyName);

			Type type = descriptor.GetInstanceType();

			Dictionary<Geodatabase, List<Table>> tablesByGeodatabase =
				GetTablesByGeodatabase(definition.Workspaces);

			IRepository stateRepository;
			IWorkItemRepository repository;

			if (type == typeof(IssueWorkList))
			{
				stateRepository =
					new XmlWorkItemStateRepository(definition.Path, definition.Name, type,
					                               definition.CurrentIndex);
				repository = new IssueItemRepository(tablesByGeodatabase, stateRepository);
			}
			else if (type == typeof(SelectionWorkList))
			{
				stateRepository =
					new XmlWorkItemStateRepository(definition.Path, definition.Name, type,
					                               definition.CurrentIndex);

				Dictionary<long, Table> tablesById =
					tablesByGeodatabase.Values
					                   .SelectMany(table => table)
					                   .ToDictionary(table => new GdbTableIdentity(table).Id,
					                                 table => table);

				Dictionary<Table, List<long>> oidsByTable =
					GetOidsByTable(definition.Items, tablesById);

				repository =
					new SelectionItemRepository(tablesByGeodatabase, oidsByTable, stateRepository);
			}
			else
			{
				throw new ArgumentException("Unkown work list type");
			}

			return repository;
		}

		[NotNull]
		private static Dictionary<Geodatabase, List<Table>> GetTablesByGeodatabase(
			ICollection<XmlWorkListWorkspace> workspaces)
		{
			var result = new Dictionary<Geodatabase, List<Table>>(workspaces.Count);

			var notifications = new NotificationCollection();

			foreach (XmlWorkListWorkspace workspace in workspaces)
			{
				var geodatabase = GetGeodatabase(workspace, notifications);

				if (geodatabase == null)
				{
					continue;
				}

				if (result.ContainsKey(geodatabase))
				{
					_msg.Debug($"Duplicate workspace {workspace.ConnectionString}");
					continue;
				}

				List<Table> tables = GetDistinctTables(workspace, geodatabase);
				result.Add(geodatabase, tables);
			}

			if (notifications.Count <= 0)
			{
				return result;
			}

			_msg.Info(string.Format(
				          "Cannot open work item workspaces from connection strings:{0}{1}",
				          Environment.NewLine, notifications.Concatenate(Environment.NewLine)));

			return result;
		}

		[CanBeNull]
		private static Geodatabase GetGeodatabase([NotNull] XmlWorkListWorkspace workspace,
		                                          [NotNull] NotificationCollection notifications)
		{
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

						var connectionProperties =
							new DatabaseConnectionProperties(databaseType)
							{
								AuthenticationMode = authMode,
								ProjectInstance = builder["project_instance"],
								Database =
									builder[
										"server"], // is always null in CIMFeatureDatasetDataConnection
								Instance = builder["instance"],
								Version = builder["version"],
								Branch = builder["branch"], // ?
								Password = builder["encrypted_password"],
								User = builder["user"]
							};

						return new Geodatabase(connectionProperties);
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			catch (Exception e)
			{
				string message = $"Cannot open workspace from connection string {workspace.ConnectionString}";

				_msg.Debug(message, e);

				NotificationUtils.Add(notifications, $"{workspace.ConnectionString}");
				return null;
			}
		}

		private static List<Table> GetDistinctTables(XmlWorkListWorkspace workspace,
		                                             Geodatabase geodatabase)
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

		private static Dictionary<Table, List<long>> GetOidsByTable(
			IEnumerable<XmlWorkItemState> xmlItems, IDictionary<long, Table> tablesById)
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

		public static Dictionary<Geodatabase, List<Table>> GetDistinctTables(
			[NotNull] IEnumerable<Table> tables)
		{
			var result = new Dictionary<Geodatabase, SimpleSet<Table>>(new DatastoreComprarer());

			foreach (Table table in tables.Distinct())
			{
				var geodatabase = (Geodatabase) table.GetDatastore();

				if (! result.ContainsKey(geodatabase))
				{
					result.Add(geodatabase, new SimpleSet<Table> { table });
				}
				else
				{
					result[geodatabase].TryAdd(table);
				}
			}

			return result.ToDictionary(pair => pair.Key, pair => pair.Value.ToList());
		}
	}
}
