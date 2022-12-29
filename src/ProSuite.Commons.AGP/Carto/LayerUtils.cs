using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Carto
{
	public static class LayerUtils
	{
		/// <summary>
		/// Returns the Rows or features found by the layer search. Honors definition queries,
		/// layer time, etc. defined on the layer. According to the documentation, valid rows returned
		/// by a cursor should be disposed.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="layer"></param>
		/// <param name="filter"></param>
		/// <param name="predicate"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public static IEnumerable<T> SearchRows<T>([NotNull] BasicFeatureLayer layer,
		                                           [CanBeNull] QueryFilter filter = null,
		                                           [CanBeNull] Predicate<T> predicate = null,
		                                           CancellationToken cancellationToken = default)
			where T : Row
		{
			Assert.ArgumentNotNull(layer, nameof(layer));

			if (predicate == null)
			{
				predicate = f => true;
			}

			using (RowCursor cursor = layer.Search(filter))
			{
				while (cursor.MoveNext())
				{
					if (cancellationToken.IsCancellationRequested)
					{
						yield break;
					}

					T currentRow = (T) cursor.Current;

					if (predicate(currentRow))
					{
						yield return currentRow;
					}
				}
			}
		}

		/// <summary>
		/// Returns the Object IDs of features found by the layer search. Honors definition queries,
		/// layer time, etc. defined on the layer. 
		/// </summary>
		public static IEnumerable<long> SearchObjectIds(
			[NotNull] BasicFeatureLayer layer,
			[CanBeNull] QueryFilter filter = null,
			[CanBeNull] Predicate<Feature> predicate = null,
			CancellationToken cancellationToken = default)
		{
			if (filter == null)
			{
				filter = new QueryFilter
				         {
					         SubFields = layer.GetTable().GetDefinition()?.GetObjectIDField()
				         };
			}
			else if (string.IsNullOrEmpty(filter.SubFields))
			{
				filter.SubFields = layer.GetTable().GetDefinition()?.GetObjectIDField();
			}

			foreach (Feature row in SearchRows(layer, filter, predicate))
			{
				if (cancellationToken.IsCancellationRequested)
				{
					yield break;
				}

				yield return row.GetObjectID();

				// Documentation: If a valid Row is returned by RowCursor.Current, it should be Disposed.
				row.Dispose();
			}
		}

		[CanBeNull]
		public static T GetRenderer<T>([NotNull] LayerDocument template) where T : CIMRenderer
		{
			CIMLayerDocument layerDocument = template.GetCIMLayerDocument();

			// todo daro: implement more robust
			CIMDefinition definition = layerDocument.LayerDefinitions[0];
			return ((CIMFeatureLayer) definition)?.Renderer as T;
		}

		[NotNull]
		public static LayerDocument CreateLayerDocument([NotNull] string path)
		{
			if (! File.Exists(path))
			{
				throw new ArgumentException($"{path} does not exist");
			}

			// todo daro no valid .lyrx file

			return new LayerDocument(path);
		}

		[CanBeNull]
		public static LayerDocument CreateLayerDocument([NotNull] string path,
		                                                string layerName)
		{
			var layerDocument = CreateLayerDocument(path);

			CIMLayerDocument cimLayerDocument = layerDocument.GetCIMLayerDocument();
			cimLayerDocument.LayerDefinitions[0].Name = layerName;

			return new LayerDocument(cimLayerDocument);
		}

		public static void ApplyRenderer(FeatureLayer layer, LayerDocument template)
		{
			// todo daro: inline
			var renderer = GetRenderer<CIMUniqueValueRenderer>(template);
			layer.SetRenderer(renderer);
		}

		/// <summary>
		/// Get tables only from feature layers with established
		/// data source. If the data source of the feature layer
		/// is broken FeatureLayer.GetTable() returns null.
		/// </summary>
		public static IEnumerable<Table> GetTables(
			[NotNull] this IEnumerable<BasicFeatureLayer> layers)
		{
			Assert.ArgumentNotNull(layers, nameof(layers));

			return layers.Select(fl => fl.GetTable()).Where(table => table != null);
		}

		/// <summary>
		/// Gets the ObjectIDs of selected features from feature
		/// layers with valid data source.
		/// Even tough a layer data source is broken the BasicFeatureLayer.SelectionCount
		/// can return a valid result.
		/// </summary>
		/// <param name="layer"></param>
		/// <remarks>
		/// Altough a layer data source is broken BasicFeatureLayer.SelectionCount
		/// can return a valid result.
		/// </remarks>
		public static IEnumerable<long> GetSelectionOids([NotNull] this BasicFeatureLayer layer)
		{
			Assert.ArgumentNotNull(layer, nameof(layer));

			Selection selection = layer.GetSelection();

			return selection == null ? Enumerable.Empty<long>() : selection.GetObjectIDs();
		}

		public static bool HasSelection([CanBeNull] BasicFeatureLayer layer)
		{
			return layer?.SelectionCount > 0;
		}

		public static void SetLayerSelectability([NotNull] BasicFeatureLayer layer,
		                                         bool selectable = true)
		{
			var cimDefinition = (CIMFeatureLayer) layer.GetDefinition();
			cimDefinition.Selectable = selectable;
			layer.SetDefinition(cimDefinition);
		}

		public static bool IsLayerValid([CanBeNull] BasicFeatureLayer featureLayer)
		{
			// ReSharper disable once UseNullPropagation
			if (featureLayer == null)
			{
				return false;
			}

			if (featureLayer.GetTable() == null)
			{
				return false;
			}

			return true;
		}
	}
}
