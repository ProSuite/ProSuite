using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.Essentials.Assertions;
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

				var featureClassInfo = new FeatureClassInfo
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
			[CanBeNull] List<FeatureClassSelection> featuresPerLayer,
			SelectionCombinationMethod selectionCombinationMethod)
		{
			if (featuresPerLayer == null)
			{
				return;
			}

			if (selectionCombinationMethod == SelectionCombinationMethod.New)
			{
				//since SelectionCombinationMethod.New is only applied to
				//the current layer but selections of other layers remain,
				//we manually need to clear all selections first. 
				SelectionUtils.ClearSelection();
			}

			foreach (var layerFeatures in featuresPerLayer)
			{
				SelectionUtils.SelectFeatures(Assert.NotNull(layerFeatures.FeatureLayer),
				                              selectionCombinationMethod, layerFeatures.ObjectIds);
			}
		}

		public static void SelectLayersFeaturesByOids(
			FeatureClassSelection featuresOfLayer,
			SelectionCombinationMethod selectionCombinationMethod)
		{
			if (! featuresOfLayer.ObjectIds.Any())
			{
				return;
			}

			if (selectionCombinationMethod == SelectionCombinationMethod.New)
			{
				//since SelectionCombinationMethod.New is only applied to
				//the current layer but selections of other layers remain,
				//we manually need to clear all selections first. 
				SelectionUtils.ClearSelection();
			}

			SelectionUtils.SelectFeatures(Assert.NotNull(featuresOfLayer.FeatureLayer),
			                              selectionCombinationMethod,
			                              featuresOfLayer.ObjectIds);
		}

		public static void SelectFeature(FeatureLayer featureLayer,
		                                 SelectionCombinationMethod selectionCombinationMethod,
		                                 long oid)
		{
			if (selectionCombinationMethod == SelectionCombinationMethod.New)
			{
				//since SelectionCombinationMethod.New is only applied to
				//the current layer but selections of other layers remain,
				//we manually need to clear all selections first. 
				SelectionUtils.ClearSelection();
			}

			SelectionUtils.SelectFeature(featureLayer, selectionCombinationMethod, oid);
		}
	}
}
