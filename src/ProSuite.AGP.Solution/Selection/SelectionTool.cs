using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.AGP.Editing.Selection;

namespace ProSuite.AGP.Solution.Selection
{
	class SelectionTool : SelectionToolBase
	{
		/*
		Select features by clicking or dragging a box.
		- Press and hold SHIFT to add or remove features from the existing selection.
			x/y			-> SelectionMode.Original	-> SelectFeature(x,y) -> multi results -> Picker
		    envelope	-> SelectionMode.Normal		-> SelectAllFeatures(envelope)
		- Press CTRL and click on overlapping features to select a single feature. -> SelectionMode.UserSelect  -> SelectFeature(x,y)
		- Press CTRL and drag a box to show a list of selectable feature classes. -> SelectionMode.UserSelect -> SelectFeatures(envelope)
		- Press ALT and click to select all features at the click point. -> SelectionMode.Original 
		*/
		
		protected override bool CanUseSelection(IEnumerable<Feature> selectedFeatures)
		{
			return false;
		}
	}
}
