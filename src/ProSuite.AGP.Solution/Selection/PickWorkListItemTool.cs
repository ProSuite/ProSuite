using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing;
using ProSuite.AGP.Editing.Selection;
using ProSuite.AGP.Solution.WorkLists;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.Selection
{
	class PickWorkListItemTool : SelectionToolBase
	{
		[CanBeNull]
		public FeatureLayer WorkListLayer { get; set; }

		protected override Cursor SelectionCursor
		{
			get => ToolUtils.GetCursor(Resource.PickerToolCursor);
		}

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
			SelectionUtils.ClearSelection();
		}

		protected override bool CanSelectFromLayerCore(FeatureLayer featureLayer)
		{
			WorkListsModule workListsModule = WorkListsModule.Current;
			
			if (! workListsModule.IsWorklistLayer(featureLayer))
			{
				return false;
			}

			WorkListLayer = featureLayer;

			LayerUtils.SetLayerSelectability(WorkListLayer);

			return true;
		}

		protected override bool OnToolActivatedCore(bool hasMapViewChanged)
		{
			WorkListsModule module = WorkListsModule.Current;

			foreach (FeatureLayer layer in ActiveMapView.Map.GetLayersAsFlattenedList()
			                                            .OfType<FeatureLayer>()
			                                            .Where(lyr => module.IsWorklistLayer(lyr)))
			{
				LayerUtils.SetLayerSelectability(layer);
			}

			SelectionUtils.ClearSelection();
			return true;
		}

		protected override void OnToolDeactivateCore(bool hasMapViewChanged)
		{
			if (WorkListLayer != null)
			{
				LayerUtils.SetLayerSelectability(WorkListLayer, false);
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
