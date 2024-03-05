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

		/// <remarks>
		/// A layer document (.lyrx file) can contain one or more layer definitions!
		/// </remarks>
		[CanBeNull]
		public static CIMRenderer GetRenderer(LayerDocument layerDocument,
		                                      Func<CIMDefinition, bool> predicate = null)
		{
			if (layerDocument is null) return null;

			CIMLayerDocument cim = layerDocument.GetCIMLayerDocument();
			var definitions = cim?.LayerDefinitions;
			if (definitions is null || definitions.Length <= 0) return null;

			var definition = predicate is null
				                 ? definitions.First()
				                 : definitions.First(predicate);

			return definition is CIMFeatureLayer featureLayer
				       ? featureLayer.Renderer
				       : null;
		}

		/// <summary>
		/// Get first renderer from <paramref name="layerDocument"/>
		/// compatible with the given <paramref name="targetLayer"/>.
		/// </summary>
		[CanBeNull]
		public static CIMRenderer GetRenderer(
			LayerDocument layerDocument, FeatureLayer targetLayer)
		{
			return GetRenderer(layerDocument, IsCompatible);

			bool IsCompatible(CIMDefinition cimDefinition)
			{
				if (targetLayer is null) return true;
				return cimDefinition is CIMFeatureLayer cimFeatureLayer &&
				       targetLayer.CanSetRenderer(cimFeatureLayer.Renderer);
			}
		}

		[NotNull]
		public static LayerDocument OpenLayerDocument([NotNull] string filePath)
		{
			if (! File.Exists(filePath))
			{
				throw new ArgumentException($"{filePath} does not exist");
			}

			// todo daro no valid .lyrx file

			return new LayerDocument(filePath);
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
		public static bool IsVisible([NotNull] Layer layer)
		{
			if (! layer.IsVisible)
			{
				return false;
			}

			if (layer.Parent is Layer parentLayer)
			{
				// ReSharper disable once TailRecursiveCall
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

			// TODO should dispose table!
			if (featureLayer.GetTable() == null)
			{
				return false;
			}

			return true;
		}

		// todo daro to MapUtils?
		[NotNull]
		public static FeatureClass GetFeatureClass(
			[NotNull] this BasicFeatureLayer basicFeatureLayer)
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
