using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Carto
{
	// todo daro: refactor common lines of code
	public static class MapUtils
	{
		[NotNull]
		public static Dictionary<GdbTableIdentity, List<long>> GetDistinctSelectionByTable(
			[NotNull] IEnumerable<BasicFeatureLayer> layers, out IEnumerable<GdbWorkspaceIdentity> distinctWorkspaces)
		{
			var workspaces = new SimpleSet<GdbWorkspaceIdentity>();
			var selectionByTable = new Dictionary<GdbTableIdentity, SimpleSet<long>>();

			foreach (BasicFeatureLayer layer in layers.Where(HasSelection))
			{
				IReadOnlyList<long> selection = layer.GetSelection().GetObjectIDs();

				GdbTableIdentity table = GetTableIdentity(layer);

				if (selectionByTable.TryGetValue(table, out SimpleSet<long> objectIDs))
				{
					foreach (long oid in selection)
					{
						objectIDs.TryAdd(oid);
					}
				}
				else
				{
					selectionByTable.Add(table, new SimpleSet<long>(selection));
				}

				workspaces.TryAdd(table.Workspace);
			}

			distinctWorkspaces = workspaces.AsEnumerable();
			return selectionByTable.ToDictionary(p => p.Key, p => p.Value.ToList());
		}

		public static IEnumerable<GdbTableIdentity> GetDistinctTables(
			[NotNull] IEnumerable<BasicFeatureLayer> layers,
			out IEnumerable<GdbWorkspaceIdentity> distinctWorkspaces)
		{
			var workspaces = new SimpleSet<GdbWorkspaceIdentity>();
			var tables = new SimpleSet<GdbTableIdentity>();

			foreach (GdbTableIdentity table in layers.Select(GetTableIdentity))
			{
				tables.TryAdd(table);
				workspaces.TryAdd(table.Workspace);
			}

			distinctWorkspaces = workspaces.AsEnumerable();
			return tables.ToList();
		}

		public static GdbTableIdentity GetTableIdentity(IDisplayTable layer)
		{
			using (Table table = layer.GetTable())
			{
				return new GdbTableIdentity(table);
			}
		}

		#region unused

		public static Dictionary<GdbWorkspaceIdentity, HashSet<GdbTableIdentity>>
			GeDistinctTablesByWorkspace(IEnumerable<GdbTableIdentity> tables)
		{
			var result = new Dictionary<GdbWorkspaceIdentity, HashSet<GdbTableIdentity>>();

			foreach (GdbTableIdentity table in tables)
			{
				if (result.TryGetValue(table.Workspace,
				                       out HashSet<GdbTableIdentity> distinctTables))
				{
					if (! distinctTables.Contains(table))
					{
						distinctTables.Add(table);
					}
				}
				else
				{
					result.Add(table.Workspace, new HashSet<GdbTableIdentity> {table});
				}
			}

			return result;
		}

		public static Dictionary<MapMember, HashSet<long>> GetDistinctSelectionByTable(
			IEnumerable<KeyValuePair<MapMember, List<long>>> oidsByMapMember)
		{
			var distinctProxys = new SimpleSet<GdbTableIdentity>();

			var result = new Dictionary<MapMember, HashSet<long>>();

			foreach (KeyValuePair<MapMember, List<long>> pair in oidsByMapMember)
			{
				MapMember mapMember = pair.Key;
				List<long> selectedFeatures = pair.Value;

				if (! (mapMember is BasicFeatureLayer basicFeatureLayer))
				{
					continue;
				}

				using (Table table = basicFeatureLayer.GetTable())
				{
					var proxy = new GdbTableIdentity(table);

					if (distinctProxys.Contains(proxy))
					{
						HashSet<long> oids = result[mapMember];

						foreach (long oid in selectedFeatures)
						{
							if (! oids.Contains(oid))
							{
								oids.Add(oid);
							}
						}
					}
					else
					{
						distinctProxys.Add(proxy);
						result.Add(mapMember, selectedFeatures.ToHashSet());
					}
				}
			}

			return result;
		}
		
		// todo daro: out IEnumerable<IWorkspaceContext> workspaces?
		[NotNull]
		public static Dictionary<GdbTableIdentity, IEnumerable<long>> GetDistinctSelectionByTable(
			[NotNull] IEnumerable<BasicFeatureLayer> layers)
		{
			var result = new Dictionary<GdbTableIdentity, HashSet<long>>();

			foreach (BasicFeatureLayer featureLayer in layers.Where(HasSelection))
			{
				IReadOnlyList<long> selectedFeatures = featureLayer.GetSelection().GetObjectIDs();

				using (Table table = featureLayer.GetTable())
				{
					var proxy = new GdbTableIdentity(table);

					if (result.TryGetValue(proxy, out HashSet<long> oids))
					{
						foreach (long oid in selectedFeatures)
						{
							if (!oids.Contains(oid))
							{
								oids.Add(oid);
							}
						}
					}
					else
					{
						result.Add(proxy, selectedFeatures.ToHashSet());
					}
				}
			}

			return result.ToDictionary(pair => pair.Key, pair => pair.Value.AsEnumerable());
		}

		public static bool HasSelection(BasicFeatureLayer featureLayer)
		{
			return featureLayer.SelectionCount > 0;
		}

		public static IEnumerable<BasicFeatureLayer> Distinct(this IEnumerable<BasicFeatureLayer> layers)
		{
			return layers.Distinct(new BasicFeatureLayerComparer());
		}

		public static IEnumerable<GdbWorkspaceIdentity> GetDistinctWorkspaceProxys(IEnumerable<BasicFeatureLayer> layers)
		{
			var result = new SimpleSet<GdbWorkspaceIdentity>();

			foreach (BasicFeatureLayer layer in layers)
			{
				using (Table table = layer.GetTable())
				{
					using (Datastore datastore = table.GetDatastore())
					{
						result.TryAdd(new GdbWorkspaceIdentity(datastore));
					}
				}
			}

			return result.AsEnumerable();
		}

		#endregion
	}

	public class BasicFeatureLayerComparer : IEqualityComparer<BasicFeatureLayer>
	{
		public bool Equals(BasicFeatureLayer x, BasicFeatureLayer y)
		{
			if (ReferenceEquals(x, y))
			{
				// both null or reference equal
				return true;
			}

			if (x == null || y == null)
			{
				return false;
			}

			var left = new GdbTableIdentity(x.GetTable());
			var right = new GdbTableIdentity(y.GetTable());

			return Equals(left, right);

		}

		public int GetHashCode(BasicFeatureLayer obj)
		{
			return obj.GetHashCode();
		}
	}
}
