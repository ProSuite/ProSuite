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
		public static FeatureLayerCreationParams CreateLayerParams([NotNull] FeatureClass featureClass)
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
		public static IEnumerable<string> GetUri([NotNull] string mapMemberName)
		{
			// todo daro What if mapMember is map itself? Can it be found with this method?
			return GetUri(MapView.Active.Map, mapMemberName);
		}

		[NotNull]
		public static IEnumerable<string> GetUri(Map map, [NotNull] string mapMemberName)
		{
			// todo daro What if mapMember is map itself? Can it be found with this method?
			IReadOnlyList<Layer> layers = map.FindLayers(mapMemberName);

			return layers.Select(GetUri);
		}

		[NotNull]
		public static string GetUri([NotNull] MapMember mapMember)
		{
			return mapMember.URI;
		}

		[CanBeNull]
		public static Layer GetLayer([NotNull] string uri)
		{
			return GetLayer(MapView.Active.Map, uri);
		}

		[CanBeNull]
		public static Layer GetLayer([NotNull] Map map, [NotNull] string uri, bool recursive = true)
		{
			return map.FindLayer(uri, recursive);
		}

		public static void SetLayerSelectability([NotNull] FeatureLayer layer, bool selectable = true)
		{
			var cimDefinition = (CIMFeatureLayer) layer.GetDefinition();
			cimDefinition.Selectable = selectable;
			layer.SetDefinition(cimDefinition);
		}
	}
}
