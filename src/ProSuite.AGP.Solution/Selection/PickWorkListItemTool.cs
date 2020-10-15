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
using ProSuite.AGP.Solution.WorkLists;

namespace ProSuite.AGP.Solution.Selection
{
	class PickWorkListItemTool: SelectionToolBase
	{
		private IList<Feature> _selection;

		public delegate void OnSelectionComplete(IList<Feature> features, EventArgs args);

		public event OnSelectionComplete ItemSelected;

		public PickWorkListItemTool()
		{
			
		}

		protected override void AfterSelection(IList<Feature> selectedFeatures, CancelableProgressor progressor)
		{
			WorkListsModule.Current.OnWorkItemPicked(new WorkItemPickArgs { features = selectedFeatures.ToList() });
		}

		

		//protected override bool CanUseSelection(IEnumerable<Feature> selectedFeatures)
		//{
		//	return false;
		//}
		
	}
}
