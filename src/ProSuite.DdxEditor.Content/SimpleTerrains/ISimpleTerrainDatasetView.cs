using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ProSuite.Commons.UI.ScreenBinding.Lists;
using ProSuite.DdxEditor.Framework.ItemViews;

namespace ProSuite.DdxEditor.Content.SimpleTerrains
{
	public interface ISimpleTerrainDatasetView : IWrappedEntityControl<SimpleTerrainDataset>,
	                                             IWin32Window
	{
		ISimpleTerrainDatasetObserver Observer { get; set; }

		Func<object> FindDatasetCategoryDelegate { get; set; }

		IEnumerable<TerrainSourceDataset> GetSelectedSourceDatasets();

		IList<TerrainSourceDatasetTableRow> GetSelectedSourceDatasetTableRows();

		void BindToSourceDatasets(
			SortableBindingList<TerrainSourceDatasetTableRow> sourceDatasetTableRows);
	}
}
