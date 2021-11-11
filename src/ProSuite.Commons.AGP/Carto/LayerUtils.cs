using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Carto
{
	public static class LayerUtils
	{
		public static IEnumerable<T> GetRows<T>([NotNull] FeatureLayer featureLayer,
		                                        [CanBeNull] QueryFilter filter = null)
			where T : Row
		{
			Assert.ArgumentNotNull(featureLayer, nameof(featureLayer));

			using (RowCursor cursor = featureLayer.Search(filter))
			{
				while (cursor.MoveNext())
				{
					yield return (T) cursor.Current;
				}
			}
		}

		[NotNull]
		public static FeatureLayerCreationParams CreateLayerParams(
			[NotNull] FeatureClass featureClass)
		{
			var layerParams = new FeatureLayerCreationParams(featureClass);
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

		//private static CIMUniqueValueRenderer GetUniqueValueRenderer(LayerDocument template)
		//{
		//	CIMLayerDocument templateLayerDocument = template.GetCIMLayerDocument();
		//	CIMDefinition templateLayerDefinition = templateLayerDocument.LayerDefinitions[0];
		//	return (CIMUniqueValueRenderer) ((CIMFeatureLayer) templateLayerDefinition).Renderer;
		//}

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

		[NotNull]
		public static IEnumerable<string> GetUri(Map map, [NotNull] string mapMemberName)
		{
			Assert.ArgumentNotNull(mapMemberName, nameof(mapMemberName));

			MapView mapView = MapView.Active;

			// todo daro What if mapMember is map itself? Can it be found with this method?
			return mapView == null
				       ? Enumerable.Empty<string>()
				       : map.FindLayers(mapMemberName).Select(GetUri);
		}

		[NotNull]
		public static string GetUri([NotNull] MapMember mapMember)
		{
			return mapMember.URI;
		}

		public static IEnumerable<Layer> FindLayers([NotNull] string name,
		                                            bool recursive = true)
		{
			Assert.ArgumentNotNull(name, nameof(name));

			MapView mapView = MapView.Active;

			return mapView == null
				       ? Enumerable.Empty<Layer>()
				       : mapView.Map.FindLayers(name, recursive);
		}

		// todo daro: move to MapUtils?
		[CanBeNull]
		public static Layer GetLayer([NotNull] string uri, bool recursive = true)
		{
			Assert.ArgumentNotNull(uri, nameof(uri));

			MapView mapView = MapView.Active;

			return mapView.Map.FindLayer(uri, recursive);
		}

		// todo daro: move to MapUtils?
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="map"></param>
		/// <remarks>Doesn't throw an exception if there is no map</remarks>
		/// <returns></returns>
		public static IEnumerable<T> GetLayers<T>([CanBeNull] this Map map) where T : Layer
		{
			return map == null ? Enumerable.Empty<T>() : map.GetLayersAsFlattenedList().OfType<T>();
		}

		
		// todo daro: move to MapUtils?
		/// <summary>
		/// Get tables only from feature layers with established
		/// data source. If the data source of the feature layer
		/// is broken FeatureLayer.GetTable() returns null.
		/// </summary>
		/// <param name="map"></param>
		public static IEnumerable<Table> GetTables([NotNull] this Map map)
		{
			Assert.ArgumentNotNull(map, nameof(map));

			return map.GetLayers<FeatureLayer>().GetTables();
		}

		
		/// <summary>
		/// Get tables only from feature layers with established
		/// data source. If the data source of the feature layer
		/// is broken FeatureLayer.GetTable() returns null.
		/// </summary>
		/// <param name="featureLayers"></param>
		public static IEnumerable<Table> GetTables(
			[NotNull] this IEnumerable<BasicFeatureLayer> featureLayers)
		{
			Assert.ArgumentNotNull(featureLayers, nameof(featureLayers));

			return featureLayers.Select(fl => fl.GetTable()).Where(table => table != null);
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

		public static void SetLayerSelectability([NotNull] FeatureLayer layer,
		                                         bool selectable = true)
		{
			var cimDefinition = (CIMFeatureLayer) layer.GetDefinition();
			cimDefinition.Selectable = selectable;
			layer.SetDefinition(cimDefinition);
		}
	}
}
