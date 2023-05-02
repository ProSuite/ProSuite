using System.Collections.Generic;
using System.Linq;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing.Selection
{
	// todo daro move to SelectionUtils
	public class Selector
	{
		public static void SelectLayersFeaturesByOids(
			[CanBeNull] IList<FeatureClassSelection> featuresPerLayer,
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

			foreach (FeatureClassSelection layerFeatures in featuresPerLayer)
			{
				SelectionUtils.SelectFeatures(Assert.NotNull(layerFeatures.BasicFeatureLayer),
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

			SelectionUtils.SelectFeatures(Assert.NotNull(featuresOfLayer.BasicFeatureLayer),
			                              selectionCombinationMethod,
			                              featuresOfLayer.ObjectIds);
		}

		public static void SelectFeature(BasicFeatureLayer basicFeatureLayer,
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

			SelectionUtils.SelectFeature(basicFeatureLayer, selectionCombinationMethod, oid);
		}
	}
}
