using System.Collections.Generic;
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

		[CanBeNull]
		public static LayerDocument CreateLayerDocument([NotNull] string path)
		{
			return new LayerDocument(path);
		}

		public static void ApplyRenderer(FeatureLayer layer, LayerDocument template)
		{
			// todo daro: inline
			var renderer = GetRenderer<CIMUniqueValueRenderer>(template);
			layer.SetRenderer(renderer);
		}
	}
}
