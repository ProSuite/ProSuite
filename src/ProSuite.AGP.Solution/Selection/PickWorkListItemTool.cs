using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing;
using ProSuite.AGP.Editing.Selection;
using ProSuite.AGP.Solution.WorkLists;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.Essentials.CodeAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ProSuite.AGP.Solution.Selection
{
	class PickWorkListItemTool : SelectionToolBase
	{
		[CanBeNull]
		public FeatureLayer WorkListLayer { get; set; }

		protected override Cursor SelectionCursor { get => ToolUtils.GetCursor(Resource.PickerToolCursor); }

		protected override void OnToolKeyDown(MapViewKeyEventArgs k)
		{
			// ignore modifier keys
			return;
		}

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
				FeatureLayer layer = null; // TODO - should be better comparison
				if ( WorkListsModule.Current.LayersByWorklistName.TryGetValue(WorkListsModule.Current.ActiveWorkListlayer?.Name, out layer)
					&& layer?.URI == featureLayer.URI) {
					WorkListLayer = featureLayer;
					ProSuite.Commons.AGP.Carto.LayerUtils.SetLayerSelectability(WorkListLayer, true);
					return true;
				}
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
					ProSuite.Commons.AGP.Carto.LayerUtils.SetLayerSelectability(featureLayer, true);
				}
			}

			SelectionUtils.ClearSelection(ActiveMapView.Map);
			return true;
		}

		protected override void OnToolDeactivateCore(bool hasMapViewChanged)
		{
			if (WorkListLayer != null)
			{
				ProSuite.Commons.AGP.Carto.LayerUtils.SetLayerSelectability(WorkListLayer, false);
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
