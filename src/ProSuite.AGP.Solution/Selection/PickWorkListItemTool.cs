using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.AGP.Editing.Selection;
using ProSuite.AGP.Solution.Commons;
using ProSuite.AGP.Solution.WorkLists;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.Essentials.CodeAnnotations;
using LayerUtils = ProSuite.AGP.Solution.Commons.LayerUtils;

namespace ProSuite.AGP.Solution.Selection
{
	internal class PickWorkListItemTool : SelectionToolBase
	{
		[CanBeNull]
		public FeatureLayer WorkListLayer { get; set; }

		protected override void AfterSelection(IList<Feature> selectedFeatures,
		                                       CancelableProgressor progressor)
		{
			WorkListsModule.Current.OnWorkItemPicked(new WorkItemPickArgs
			                                         {features = selectedFeatures.ToList()});
			SelectionUtils.ClearSelection(ActiveMapView.Map);
		}

		protected override bool CanSelectFromLayerCore(FeatureLayer featureLayer)
		{
			//can select from layer if the layer is a worklist layer
			if (WorkListsModule.Current.LayersByWorklistName.ContainsValue(featureLayer))
			{
				WorkListLayer = featureLayer;
				Commons.LayerUtils.SetLayerSelectability(WorkListLayer, true);
				return true;
			}
			return false;
		}

		protected override bool OnToolActivatedCore(bool hasMapViewChanged)
		{
			var featureLayers = ActiveMapView.Map.Layers.OfType<FeatureLayer>();
			foreach (FeatureLayer featureLayer in featureLayers)
			{
				if (WorkListsModule.Current.LayersByWorklistName.ContainsValue(featureLayer))
				{
					LayerUtils.SetLayerSelectability(featureLayer, true);
				}
			}

			SelectionUtils.ClearSelection(ActiveMapView.Map);

			return true;
		}

		protected override void OnToolDeactivateCore(bool hasMapViewChanged)
		{
			if (WorkListLayer != null)
			{
				Commons.LayerUtils.SetLayerSelectability(WorkListLayer, false);
			}
			
		}

		protected override bool CanUseSelection(IEnumerable<Feature> selectedFeatures)
		{
			var found = false;

			if (WorkListLayer == null)
			{
				return false;
			}

			//check if a selected feature is part of the worklist layer
			foreach (Feature selectedFeature in selectedFeatures)
			{
				var filter = new SpatialQueryFilter();
				filter.FilterGeometry = selectedFeature.GetShape();
				filter.SpatialRelationship = SpatialRelationship.Intersects;
				using (RowCursor cursor = WorkListLayer.Search(filter))
				{
					while (cursor.MoveNext())
					{
						var feature = cursor.Current as Feature;

						//if selected feature is a feature of the worklist layer
						if (feature.GetShape().ToJson() == selectedFeature.GetShape().ToJson())
						{
							found = true;
						}
					}
				}
			}

			return found;
		}
	}
}
