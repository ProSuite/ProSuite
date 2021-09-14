using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing.Selection
{
	public class Selector
	{
		public static List<FeatureClassInfo> GetSelectableFeatureclassInfos()
		{
			IEnumerable<FeatureLayer> featureLayers = MapView.Active.Map.GetLayersAsFlattenedList()
			                                                 .OfType<FeatureLayer>()
			                                                 .Where(layer => layer.IsVisible);

			IEnumerable<IGrouping<string, FeatureLayer>> layerGroupsByFcName =
				featureLayers.GroupBy(layer => layer.GetFeatureClass().GetName());

			var featureClassInfos = new List<FeatureClassInfo>();

			foreach (IGrouping<string, FeatureLayer> group in layerGroupsByFcName)
			{
				var belongingLayers = new List<FeatureLayer>();

				foreach (FeatureLayer layer in group)
				{
					belongingLayers.Add(layer);
				}

				FeatureClass fClass = belongingLayers.First().GetFeatureClass();
				string featureClassName = fClass.GetName();
				esriGeometryType gType = belongingLayers.First().ShapeType;

				var featureClassInfo = new FeatureClassInfo()
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

		public static void SelectLayersFeaturesByOids(
			[CanBeNull] Dictionary<BasicFeatureLayer, List<long>> featuresPerLayer,
			SelectionCombinationMethod method)
		{
			if (featuresPerLayer == null)
			{
				return;
			}

			if (method == SelectionCombinationMethod.New)
			{
				//since SelectionCombinationMethod.New is only applied to
				//the current layer but selections of other layers remain,
				//we manually need to clear all selections first. 
				SelectionUtils.ClearSelection();
			}

			foreach (KeyValuePair<BasicFeatureLayer, List<long>> kvp in featuresPerLayer)
			{
				SelectionUtils.SelectFeatures(kvp.Key, method, kvp.Value);
			}
		}

		public static void SelectLayersFeaturesByOids(
			KeyValuePair<BasicFeatureLayer, List<long>> featuresOfLayer,
			SelectionCombinationMethod method)
		{
			if (! featuresOfLayer.Value.Any())
			{
				return;
			}

			if (method == SelectionCombinationMethod.New)
			{
				//since SelectionCombinationMethod.New is only applied to
				//the current layer but selections of other layers remain,
				//we manually need to clear all selections first. 
				SelectionUtils.ClearSelection();
			}

			SelectionUtils.SelectFeatures(featuresOfLayer.Key, method, featuresOfLayer.Value);
		}
	}
}
