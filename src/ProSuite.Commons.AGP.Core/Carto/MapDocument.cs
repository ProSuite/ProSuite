using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.AGP.Core.Carto
{
	/// <summary>
	/// Wraps a <see cref="CIM"/>, that is, a .mapx file,
	/// and provides convenient access to layers.
	/// </summary>
	public class MapDocument
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public CIMMapDocument CIM { get; }

		public MapDocument(CIMMapDocument cim)
		{
			CIM = cim ?? throw new ArgumentNullException(nameof(cim));
		}

		public static MapDocument Open(string mapxFilePath)
		{
			string json = File.ReadAllText(mapxFilePath, Encoding.UTF8);
			var cim = CIMMapDocument.FromJson(json);
			return new MapDocument(cim);
		}

		public double ReferenceScale => CIM.MapDefinition?.ReferenceScale ?? 0.0;

#if ARCGISPRO_GREATER_3_2
		public bool UseMasking => CIM.MapDefinition?.UseMasking ?? false;
#endif

		public Envelope DefaultExtent => CIM.MapDefinition?.DefaultExtent;

		public IReadOnlyList<CIMDefinition> RootLayers => GetRootLayers();

		public IEnumerable<T> GetLayers<T>([InstantHandle] Predicate<T> predicate = null)
			where T : CIMDefinition
		{
			// (CIMDefinition)
			//   CIMStandaloneTable
			//     CIMSubtypeGroupTable
			//   (CIMBaseLayer)
			//     CIMGroupLayer
			//     (CIMBasicFeatureLayer)
			//       CIMAnnotationLayer
			//       (CIMGeoFeatureLayerBase)
			//         CIMFeatureLayer

			var definitions = CIM.LayerDefinitions;

			if (definitions is null)
			{
				return Enumerable.Empty<T>();
			}

			var query = definitions.OfType<T>();

			if (predicate is not null)
			{
				query = query.Where(t => predicate(t));
			}

			return query;
		}

		private IReadOnlyList<CIMDefinition> GetRootLayers()
		{
			var rootUris = CIM.MapDefinition?.Layers;
			var rootSet = rootUris?.ToHashSet(StringComparer.OrdinalIgnoreCase);

			if (rootSet is null)
			{
				return Array.Empty<CIMDefinition>();
			}

			return GetLayers<CIMDefinition>()
			       .Where(l => rootSet.Contains(l.URI))
			       .ToList();
		}

		public void Traverse<T>(Func<CIMDefinition, Stack<CIMDefinition>, T, bool> visitor, T state)
		{
			if (visitor is null)
				throw new ArgumentNullException(nameof(visitor));

			// Within a CIMMapDocument, LayerDefinitions is a flat list
			// of CIMDefinitions (layers and/or standalone tables), and
			// MapDefinition.Layers is a list of URIs of the root layers.
			// Group layers refer to their child layers by URI.

			var layers = CIM.LayerDefinitions;
			if (layers is null || layers.Length < 1) return; // no layers
			var layersByUri = layers.ToDictionary(l => l.URI);

			var rootUris = CIM.MapDefinition?.Layers;
			if (rootUris is null) return;

			var parentStack = new Stack<CIMDefinition>();
			var workStack = new Stack<string>(rootUris.Reverse());

			while (workStack.Count > 0)
			{
				var uri = workStack.Pop();
				if (uri is null)
				{
					parentStack.Pop();
					continue;
				}

				if (layersByUri.TryGetValue(uri, out var definition))
				{
					if (! visitor(definition, parentStack, state))
					{
						return; // stop traversal
					}

					if (definition is CIMGroupLayer groupLayer)
					{
						parentStack.Push(groupLayer);
						workStack.Push(null); // marker that group is done

						foreach (var childUri in EmptyIfNull(groupLayer.StandaloneTables).Reverse())
						{
							workStack.Push(childUri);
						}

						foreach (var childUri in EmptyIfNull(groupLayer.Layers).Reverse())
						{
							workStack.Push(childUri);
						}
					}
				}
			}
		}

		private static T[] EmptyIfNull<T>(T[] array)
		{
			return array ?? Array.Empty<T>();
		}

		public List<T> FindLayers<T>(string name, string parent = null, string ancestor = null,
		                             bool wildMatch = false, bool ignoreCase = false)
			where T : CIMDefinition
		{
			var list = new List<T>();

			Traverse(Visitor, list);

			return list;

			bool Visitor(CIMDefinition member, Stack<CIMDefinition> parents, List<T> accumulator)
			{
				if (member is T t && Match(name, member.Name) &&
				    (parent is null || parents.Count > 0 && Match(parent, parents.Peek().Name)) &&
				    (ancestor is null || parents.Any(p => Match(ancestor, p.Name))))
				{
					accumulator.Add(t);
				}

				return true; // continue traversal
			}

			bool Match(string pattern, string candidate)
			{
				if (wildMatch)
				{
					return TextMatching.WildMatch(pattern, candidate, ignoreCase);
				}

				var comparison = ignoreCase
					                 ? StringComparison.OrdinalIgnoreCase
					                 : StringComparison.Ordinal;
				return string.Equals(pattern, candidate, comparison);
			}
		}

		public T FindLayer<T>(FeatureClass featureClass) where T : CIMBasicFeatureLayer
		{
			if (featureClass is null) return null;
			var tableName = featureClass.GetName();
			using var datastore = featureClass.GetDatastore();

			tableName = DatasetNameUtils.UnqualifyDatasetName(tableName);

			var list = GetLayers<T>(lyr => IsLayerSource(lyr, tableName, datastore)).ToList();

			return list.FirstOrDefault();
		}

		private static bool IsLayerSource(CIMBasicFeatureLayer layer, string unqualifiedTableName,
		                                  [InstantHandle] Datastore datastore)
		{
			if (layer is null)
				throw new ArgumentNullException(nameof(layer));

			//var connector = datastore.GetConnector();

			if (layer.FeatureTable?.DataConnection is CIMStandardDataConnection sdc)
			{
				// DatasetType: ...
				// WorkspaceFactory: SDE, FileGDB, Shapefile, Sql (query layer), SQLite, Custom (plugin datasource), etc.
				// Dataset (name)
				// WorkspaceConnectionString

				if (sdc.DatasetType != esriDatasetType.esriDTFeatureClass)
					return false;

				var unqualified = DatasetNameUtils.UnqualifyDatasetName(sdc.Dataset);
				if (! string.Equals(unqualified, unqualifiedTableName, StringComparison.OrdinalIgnoreCase))
					return false;

				// TODO compare datastore... how?

				return true;
			}

			if (layer.FeatureTable?.DataConnection is CIMFeatureDatasetDataConnection fdc)
			{
				// Same as standard data connection, plus:
				// FeatureDataset (string)

				if (fdc.DatasetType != esriDatasetType.esriDTFeatureClass)
					return false;

				var unqualified = DatasetNameUtils.UnqualifyDatasetName(fdc.Dataset);
				if (! string.Equals(unqualified, unqualifiedTableName, StringComparison.OrdinalIgnoreCase))
					return false;

				// TODO compare datastore... how?

				return true;
			}

			// TODO other DataConnection types? CIMSqlQueryDataConnection? (query layers)

			return false;
		}
	}
}
