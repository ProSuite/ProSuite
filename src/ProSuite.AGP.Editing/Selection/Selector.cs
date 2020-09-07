using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;

namespace ProSuite.AGP.Editing.Selection
{
	public class Selector
	{
		public static List<FeatureClassInfo> GetSelectableFeatureclassInfos()
		{
			IEnumerable<FeatureLayer> featureLayers = MapView.Active.Map.Layers
			                                                 .OfType<FeatureLayer>();

			var fClassGroups = featureLayers.Where(fLayer =>
				                                       fLayer.IsSelectable).Select(fLayer => fLayer.GetFeatureClass()).GroupBy(fc => fc.GetName());

			var layerGroupsByFcName = featureLayers.GroupBy(layer => layer.GetFeatureClass().GetName());

			List<FeatureClassInfo> featureClassInfos = new List<FeatureClassInfo>();

			foreach (var group in layerGroupsByFcName)
			{
				List<FeatureLayer> belongingLayers = new List<FeatureLayer>();

				foreach (var layer in group)
				{
					belongingLayers.Add(layer);
				}

				FeatureClass fClass = belongingLayers.First().GetFeatureClass();
				string featureClassName = fClass.GetName();
				esriGeometryType gType = belongingLayers.First().ShapeType;

				FeatureClassInfo featureClassInfo = new FeatureClassInfo()
				                                    {
					                                    BelongingLayers = belongingLayers,
					                                    FeatureClass = fClass,
					                                    FeatureClassName = featureClassName,
					                                    ShapeType = gType
				                                    };
				featureClassInfos.Add(featureClassInfo);
			}

			featureClassInfos.OrderBy(info => info.ShapeType);
			return featureClassInfos;
		}

		public static void SelectLayersByOids(Dictionary<BasicFeatureLayer, List<long>> featuresPerLayer)
		{
			foreach(var kvp in featuresPerLayer)
			{
				QueryFilter qf = new QueryFilter()
				                 {
									 ObjectIDs = kvp.Value
				                 };
				kvp.Key.Select(qf);
			}
		}
	}
}
