using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.AGP.WorkList.Domain.Persistence.Xml;
using ProSuite.Commons.Ado;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.Commons.Xml;

namespace ProSuite.AGP.WorkList
{
	public static class WorkListUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private const string PluginIdentifier = "ProSuite_WorkListDatasource";

		public static PluginDatastore GetPluginDatastore([NotNull] Uri dataSource)
		{
			Assert.ArgumentNotNull(dataSource, nameof(dataSource));

			return new PluginDatastore(
				new PluginDatasourceConnectionPath(PluginIdentifier, dataSource));
		}

		public static IEnumerable<ISourceClass> CreateSourceClasses([NotNull] Map map)
		{
			if (map is null)
			{
				throw new ArgumentNullException(nameof(map));
			}

			Dictionary<MapMember, List<long>> oidsByLayer = SelectionUtils.GetSelection(map);

			foreach ((Table table, List<long> oids) in MapUtils.GetDistinctSelectionByTable(
				         oidsByLayer))
			{
				using TableDefinition tableDefinition = table.GetDefinition();

				SourceClassSchema schema;

				if (tableDefinition is FeatureClassDefinition featureClassDefinition)
				{
					schema = new SourceClassSchema(featureClassDefinition.GetObjectIDField(),
					                               featureClassDefinition.GetShapeField());
				}
				else
				{
					schema = new SourceClassSchema(tableDefinition.GetObjectIDField());
				}

				yield return new SelectionSourceClass(new GdbTableIdentity(table), schema, oids);
			}
		}

		public static IEnumerable<ISourceClass> CreateSourceClasses(
			[NotNull] Map map, [NotNull] XmlWorkListDefinition definition)
		{
			if (map is null)
			{
				throw new ArgumentNullException(nameof(map));
			}

			if (definition is null)
			{
				throw new ArgumentNullException(nameof(definition));
			}

			var tablesById = new Dictionary<long, Table>();

			List<BasicFeatureLayer> featureLayers =
				MapUtils.GetFeatureLayers<BasicFeatureLayer>(map).ToList();

			IEnumerable<XmlTableReference> tableReferences =
				definition.Workspaces.SelectMany(w => w.Tables);

			foreach (XmlTableReference tableReference in tableReferences)
			{
				foreach (BasicFeatureLayer layer in featureLayers)
				{
					Table table = layer.GetTable();

					long id = GetUniqueTableIdAcrossWorkspaces(new GdbTableIdentity(table));
					if (id == tableReference.Id)
					{
						tablesById.TryAdd(id, table);
					}
				}
			}

			Dictionary<Table, List<long>>
				oidsByTable = GetOidsByTable(definition.Items, tablesById);

			if (oidsByTable.Count == 0)
			{
				_msg.Debug($"There are no referenced table from '{definition.Path}' in the map");

				var message =
					$"There are no referenced table from '{Path.GetFileName(definition.Path)}' in the map";
				var caption = "Cannot open work List";

				Gateway.ShowMessage(message, caption,
				                    MessageBoxButton.OK,
				                    MessageBoxImage.Information);

				yield break;
			}

			foreach ((Table table, List<long> oids) in oidsByTable)
			{
				using TableDefinition tableDefinition = table.GetDefinition();

				SourceClassSchema schema;

				if (tableDefinition is FeatureClassDefinition featureClassDefinition)
				{
					schema = new SourceClassSchema(featureClassDefinition.GetObjectIDField(),
					                               featureClassDefinition.GetShapeField());
				}
				else
				{
					schema = new SourceClassSchema(tableDefinition.GetObjectIDField());
				}

				yield return new SelectionSourceClass(new GdbTableIdentity(table), schema, oids);
			}
		}

		[NotNull]
		public static string ParseName([CanBeNull] string uri)
		{
			if (string.IsNullOrEmpty(uri))
			{
				return string.Empty;
			}

			int index = uri.LastIndexOf('/');
			if (index >= 0)
				uri = uri.Substring(index + 1);
			index = uri.LastIndexOf('\\');
			if (index >= 0)
				uri = uri.Substring(index + 1);

			// scheme://Host:Port/AbsolutePath?Query#Fragment
			// worklist://localhost/workListName?unused&for#now

			return Path.GetFileNameWithoutExtension(uri);
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

			XmlWorkListDefinition definition = Read(worklistDefinitionFile);
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

			XmlWorkListDefinition definition = Read(worklistDefinitionFile);
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

			XmlWorkListDefinition definition = Read(worklistDefinitionFile);
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

		private static Dictionary<Table, List<long>> GetOidsByTable(
			IEnumerable<XmlWorkItemState> xmlItems,
			IDictionary<long, Table> tablesById)
		{
			var result = new Dictionary<Table, List<long>>();

			foreach (XmlWorkItemState item in xmlItems)
			{
				if (! tablesById.TryGetValue(item.Row.TableId, out Table table))
				{
					// Not found by table ID. For backward compatibility, try Id:
					table = tablesById.Values.FirstOrDefault(t => t.GetID() == item.Row.TableId);

					if (table == null)
					{
						_msg.Warn(
							$"Table {item.Row.TableName} (UniqueID={item.Row.TableId}) not found in the " +
							$"list of available tables ({StringUtils.Concatenate(tablesById.Values, t => t.GetName(), ", ")}).");
						continue;
					}
				}

				if (result.TryGetValue(table, out List<long> oids))
				{
					if (oids.Contains(item.Row.OID))
					{
						continue;
					}

					oids.Add(item.Row.OID);
				}
				else
				{
					result.Add(table, new List<long> { item.Row.OID });
				}
			}

			return result;
		}

		public static void Save(XmlWorkListDefinition definition, string workListDefinitionFilePath)
		{
			var helper = new XmlSerializationHelper<XmlWorkListDefinition>();
			helper.SaveToFile(definition, workListDefinitionFilePath);
		}

		public static XmlWorkListDefinition Read(string workListDefinitionFilePath)
		{
			var helper = new XmlSerializationHelper<XmlWorkListDefinition>();
			return helper.ReadFromFile(workListDefinitionFilePath);
		}

		public static string Format(IWorkList worklist)
		{
			return $"{worklist.DisplayName}: {worklist.Name}";
		}

		public static void LoadItemsInBackground([NotNull] IWorkList workList)
		{
			if (workList == null) throw new ArgumentNullException();

			try
			{
				var thread = new Thread(() =>
				{
					try
					{
						_msg.VerboseDebug(() => $"{Format(workList)} load items.");

						workList.LoadItems(new QueryFilter());

						// The thread terminates once its work is done.
					}
					catch (OperationCanceledException oce)
					{
						_msg.Debug("Cancel service", oce);
					}
					catch (Exception ex)
					{
						_msg.Debug(ex.Message, ex);
					}
				});

				// TODO: (daro) implement feedback for navigator?
				thread.TrySetApartmentState(ApartmentState.STA);
				thread.IsBackground = true;
				thread.Start();
			}
			catch (Exception ex)
			{
				_msg.Debug(ex.Message, ex);
			}
		}

		public static void CountItemsInBackground([NotNull] IWorkList workList)
		{
			if (workList == null) throw new ArgumentNullException();

			try
			{
				IProgress<int> progress = new Progress<int>();
				var thread = new Thread(() =>
				{
					try
					{
						_msg.VerboseDebug(() => $"{Format(workList)} count items.");

						workList.Count();

						// The thread terminates once its work is done.
					}
					catch (OperationCanceledException oce)
					{
						_msg.Debug("Cancel service", oce);
					}
					catch (Exception ex)
					{
						_msg.Debug(ex.Message, ex);
					}
				});

				thread.TrySetApartmentState(ApartmentState.STA);
				thread.IsBackground = true;
				thread.Start();
			}
			catch (Exception ex)
			{
				_msg.Debug(ex.Message, ex);
			}
		}

		public static IEnumerable<Layer> GetWorklistLayersByPath(
			[NotNull] ILayerContainer container,
			[NotNull] string workListFile)
		{
			IReadOnlyList<Layer> layers = container.GetLayersAsFlattenedList();

			foreach (Layer layer in layers)
			{
				if (layer.GetDataConnection() is not CIMStandardDataConnection connection)
				{
					continue;
				}

				string connectionString = connection.WorkspaceConnectionString;
				var builder = new ConnectionStringBuilder(connectionString);

				string database = builder["database"];

				if (string.Equals(database, workListFile, StringComparison.OrdinalIgnoreCase))
				{
					yield return layer;
				}
			}
		}

		public static IEnumerable<Layer> GetWorklistLayers(
			[NotNull] ILayerContainer container,
			[NotNull] string worklistName)
		{
			IReadOnlyList<Layer> layers = container.GetLayersAsFlattenedList();

			return GetWorklistLayers(layers, worklistName);
		}

		public static IEnumerable<Layer> GetWorklistLayers([NotNull] IEnumerable<Layer> layers,
		                                                   [NotNull] IWorkList workList)
		{
			return GetWorklistLayers(layers, workList.Name);
		}

		public static IEnumerable<Layer> GetWorklistLayers([NotNull] IEnumerable<Layer> layers,
		                                                   [NotNull] string worklistName)
		{
			foreach (Layer layer in layers)
			{
				var connection = layer.GetDataConnection() as CIMStandardDataConnection;

				if (! string.Equals(worklistName, connection?.Dataset,
				                    StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				_msg.VerboseDebug(
					() => $"'work list layer {layer.Name} is loaded: work list {worklistName}");

				yield return layer;
			}
		}

		public static IEnumerable<IWorkList> GetLoadedWorklistsByPath(
			[NotNull] IWorkListRegistry registry,
			[NotNull] ILayerContainer container,
			[NotNull] string workListFile)
		{
			IEnumerable<Layer> layers = GetWorklistLayersByPath(container, workListFile);

			return GetLoadedWorklists(registry, layers);
		}

		[NotNull]
		public static IEnumerable<IWorkList> GetLoadedWorklists(
			[NotNull] IWorkListRegistry registry,
			[NotNull] ILayerContainer container)
		{
			return GetLoadedWorklists(registry, container.GetLayersAsFlattenedList());
		}

		[NotNull]
		public static IEnumerable<IWorkList> GetLoadedWorklists(
			[NotNull] IWorkListRegistry registry,
			[NotNull] IEnumerable<Layer> layers)
		{
			return layers.Select(lyr => GetLoadedWorklist(registry, lyr))
			             .Where(worklist => worklist != null);
		}

		[CanBeNull]
		public static IWorkList GetLoadedWorklist(
			[NotNull] IWorkListRegistry registry,
			[NotNull] Layer layer)
		{
			IWorkList loadedWorklist = null;

			if (layer.GetDataConnection() is CIMStandardDataConnection connection &&
			    layer.ConnectionStatus == ConnectionStatus.Connected)
			{
				loadedWorklist = registry.Get(connection.Dataset);
			}

			return loadedWorklist;
		}

		public static async Task RemoveWorkListLayersAsync(MapView mapView, IWorkList workList)
		{
			await QueuedTask.Run(() =>
			{
				Map map = mapView.Map;
				IReadOnlyList<Layer> layers = map.GetLayersAsFlattenedList();

				var worklistLayers =
					GetWorklistLayers(layers, workList).ToList();

				Assert.True(MapUtils.RemoveLayers(map, worklistLayers),
				            "map doesn't contain work list layer");

				if (workList is not IssueWorkList)
				{
					return;
				}

				// NOTE: magic string!!!!
				map.RemoveLayers(
					MapUtils.GetLayers<GroupLayer>(
						map,
						l => string.Equals(l.Name, "QA", StringComparison.OrdinalIgnoreCase)));
			});
		}
	}
}
