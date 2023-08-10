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
		/// Given a layer, find the map to which it belongs.
		/// </summary>
		public static Map FindMap(Layer layer)
		{
			var parent = layer.Parent;

			while (parent != null)
			{
				if (parent is Map map) return map;
				if (parent is not Layer other) break;

				parent = other.Parent;
			}

			return null;
		}

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
			if (layer is null)
				throw new ArgumentNullException(nameof(layer));

			if (predicate == null)
			{
				predicate = _ => true;
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
			this IEnumerable<BasicFeatureLayer> layers)
		{
			return layers is null
				       ? Enumerable.Empty<Table>()
				       : layers.Select(fl => fl.GetTable()).Where(table => table != null);
		}

		/// <summary>
		/// Gets the ObjectIDs of selected features from the given <paramref name="layer"/>.
		/// </summary>
		/// <remarks>
		/// Although a layer data source is broken BasicFeatureLayer.SelectionCount
		/// can return a valid result.
		/// </remarks>
		public static IEnumerable<long> GetSelectionOids(this BasicFeatureLayer layer)
		{
			using (ArcGIS.Core.Data.Selection selection = layer?.GetSelection())
			{
				return selection == null ? Enumerable.Empty<long>() : selection.GetObjectIDs();
			}
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

		/// <summary>
		/// Gets the layer's visibility state. Works as well for layers nested in group layers.
		/// </summary>
		public static bool IsVisible(this Layer layer)
		{
			if (! layer.IsVisible)
			{
				return false;
			}

			if (layer.Parent is Layer parentLayer)
			{
				return IsVisible(parentLayer);
			}

			return true;
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

		// todo daro to MapUtils?
		[NotNull]
		public static FeatureClass GetFeatureClass([NotNull] this BasicFeatureLayer basicFeatureLayer)
		{
			Assert.ArgumentNotNull(basicFeatureLayer, nameof(basicFeatureLayer));
			Assert.ArgumentCondition(
				basicFeatureLayer is FeatureLayer || basicFeatureLayer is AnnotationLayer,
				"AnnotationLayer has it's own GetFeatureClass() method. There is no base method on BasicFeatureLayer.");

			// todo daro try (FeatureClass)BasicFeatureLayer.GetTable()
			if (basicFeatureLayer is FeatureLayer featureLayer)
			{
				return Assert.NotNull(featureLayer.GetFeatureClass());
			}

			if (basicFeatureLayer is AnnotationLayer annotationLayer)
			{
				return Assert.NotNull(annotationLayer.GetFeatureClass());
			}

			throw new ArgumentException(
				$"{nameof(basicFeatureLayer)} is not of type FeatureLayer nor AnnotationLayer");
		}
	}
}
