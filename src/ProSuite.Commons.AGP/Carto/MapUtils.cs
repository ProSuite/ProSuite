using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Carto
{
	public static class MapUtils
	{
		public static Dictionary<GdbWorkspaceReference, HashSet<GdbTableReference>>
			GeDistinctTablesByWorkspace(IEnumerable<GdbTableReference> tables)
		{
			var result = new Dictionary<GdbWorkspaceReference, HashSet<GdbTableReference>>();

			foreach (GdbTableReference table in tables)
			{
				if (result.TryGetValue(table.WorkspaceReference,
				                       out HashSet<GdbTableReference> distinctTables))
				{
					if (! distinctTables.Contains(table))
					{
						distinctTables.Add(table);
					}
				}
				else
				{
					result.Add(table.WorkspaceReference, new HashSet<GdbTableReference> {table});
				}
			}

			return result;
		}

		public static Dictionary<MapMember, HashSet<long>> GetDistinctSelectionByTable(
			IEnumerable<KeyValuePair<MapMember, List<long>>> oidsByMapMember)
		{
			var distinctProxys = new SimpleSet<GdbTableReference>();

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
					var proxy = new GdbTableReference(table);

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
		public static Dictionary<GdbTableReference, IEnumerable<long>> GetDistinctSelectionByTable(
			[NotNull] IEnumerable<BasicFeatureLayer> featureLayers)
		{
			var result = new Dictionary<GdbTableReference, HashSet<long>>();

			foreach (BasicFeatureLayer featureLayer in featureLayers.Where(HasSelection))
			{
				IReadOnlyList<long> selectedFeatures = featureLayer.GetSelection().GetObjectIDs();

				using (Table table = featureLayer.GetTable())
				{
					var proxy = new GdbTableReference(table);

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


		// todo daro: out IEnumerable<IWorkspaceContext> workspaces?
		[NotNull]
		public static Dictionary<GdbTableReference, List<long>> GetDistinctSelectionByTable(
			[NotNull] IEnumerable<BasicFeatureLayer> featureLayers, out IEnumerable<GdbWorkspaceReference> distinctWorkspaces)
		{
			var workspaces = new SimpleSet<GdbWorkspaceReference>();
			var result = new Dictionary<GdbTableReference, SimpleSet<long>>();

			foreach (BasicFeatureLayer featureLayer in featureLayers.Where(HasSelection))
			{
				IReadOnlyList<long> selectedFeatures = featureLayer.GetSelection().GetObjectIDs();

				using (Table table = featureLayer.GetTable())
				{
					var proxy = new GdbTableReference(table);

					if (result.TryGetValue(proxy, out SimpleSet<long> objectIDs))
					{
						foreach (long oid in selectedFeatures)
						{
							objectIDs.TryAdd(oid);
						}
					}
					else
					{
						result.Add(proxy, new SimpleSet<long>(selectedFeatures));
					}

					workspaces.TryAdd(proxy.WorkspaceReference);
				}
			}

			distinctWorkspaces = workspaces.AsEnumerable();
			return result.ToDictionary(p => p.Key, p => p.Value.ToList());
		}

		public static bool HasSelection(BasicFeatureLayer featureLayer)
		{
			return featureLayer.SelectionCount > 0;
		}

		public static IEnumerable<BasicFeatureLayer> Distinct(this IEnumerable<BasicFeatureLayer> layers)
		{
			return layers.Distinct(new BasicFeatureLayerComparer());
		}

		public static IEnumerable<GdbWorkspaceReference> GetDistinctWorkspaceProxys(IEnumerable<BasicFeatureLayer> layers)
		{
			var result = new SimpleSet<GdbWorkspaceReference>();

			foreach (BasicFeatureLayer layer in layers)
			{
				using (Table table = layer.GetTable())
				{
					using (Datastore datastore = table.GetDatastore())
					{
						result.TryAdd(new GdbWorkspaceReference(datastore));
					}
				}
			}

			return result.AsEnumerable();
		}
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

			var left = new GdbTableReference(x.GetTable());
			var right = new GdbTableReference(y.GetTable());

			return Equals(left, right);

		}

		public int GetHashCode(BasicFeatureLayer obj)
		{
			return obj.GetHashCode();
		}
	}
}
