using System.Collections.Generic;
using System.Linq;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.Selection;
using ProSuite.Commons.AGP.Carto;

namespace ProSuite.AGP.Editing.Picker
{
	public class PickableItemAdapter
	{
		public static List<IPickableItem> Get(List<FeatureClassInfo> featureClassInfos)
		{
			List<IPickableItem> pickableItems = new List<IPickableItem>();
			foreach (var info in featureClassInfos)
			{

				pickableItems.Add(new PickableFeatureClassItem(info.FeatureClass, info.ShapeType, info.BelongingLayers));
			}

			return pickableItems;
		}

		public static List<IPickableItem> Get(Dictionary<BasicFeatureLayer, List<long>> layersWithOids)
		{
			List<PickableFeatureItem> pickableFeatureItems =
				new List<PickableFeatureItem>();

			foreach (var layerWithOids in layersWithOids)
			{
				var kvpList = new List<KeyValuePair<BasicFeatureLayer, List<long>>> { layerWithOids };
				var featuresOfLayer = MapUtils.GetFeatures(kvpList);

				foreach (var feature in featuresOfLayer)
				{
					string text =
						$"Layer: {layerWithOids.Key.Name}, OjbectId: {feature.GetObjectID()}";
					PickableFeatureItem pickableFeatureItem =
						new PickableFeatureItem(layerWithOids.Key, feature, text);
					pickableFeatureItems.Add(pickableFeatureItem);
				}
			}

			return pickableFeatureItems.ToList<IPickableItem>();
		}
	}
}
