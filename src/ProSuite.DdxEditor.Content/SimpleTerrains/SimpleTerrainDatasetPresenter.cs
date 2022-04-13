using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.ScreenBinding.Lists;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Content.Datasets;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.SimpleTerrains
{
	public class SimpleTerrainDatasetPresenter :
		SimpleEntityItemPresenter<SimpleTerrainDatasetItem>,
		ISimpleTerrainDatasetObserver
	{
		#region Delegates

		public delegate IList<DatasetTableRow> GetSurfaceDatasetsToAdd(
			IWin32Window owner,
			params ColumnDescriptor[] columns);

		#endregion

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly GetSurfaceDatasetsToAdd _getSurfaceDatasetsToAdd;

		private readonly SortableBindingList<TerrainSourceDatasetTableRow> _sourceDatasetTableRows
			= new SortableBindingList<TerrainSourceDatasetTableRow>();

		private readonly ISimpleTerrainDatasetView _view;

		public SimpleTerrainDatasetPresenter(SimpleTerrainDatasetItem item,
		                                     ISimpleTerrainDatasetView view,
		                                     DatasetControlPresenter.FindDatasetCategory
			                                     findDatasetCategory,
		                                     GetSurfaceDatasetsToAdd findDatasetsToAdd)
			: base(item)
		{
			Assert.ArgumentNotNull(findDatasetsToAdd, nameof(findDatasetsToAdd));

			_view = view;
			_getSurfaceDatasetsToAdd = findDatasetsToAdd;
			_view.Observer = this;

			view.FindDatasetCategoryDelegate =
				() => findDatasetCategory(view, new ColumnDescriptor("Name"));
		}

		private void RenderSourceDatasets(SimpleTerrainDataset surface)
		{
			Assert.ArgumentNotNull(surface, nameof(surface));

			_sourceDatasetTableRows.Clear();

			foreach (var sourceDataset in surface.Sources)
			{
				_sourceDatasetTableRows.Add(
					new TerrainSourceDatasetTableRow(sourceDataset));
			}

			_view.BindToSourceDatasets(_sourceDatasetTableRows);
		}

		public void OnBoundTo(SimpleTerrainDataset terrainDataset)
		{
			RenderSourceDatasets(terrainDataset);
		}

		public void TargetSelectionChanged() { }

		void ISimpleTerrainDatasetObserver.AddTargetClicked()
		{
			SimpleTerrainDataset simpleTerrainDataset = Assert.NotNull(Item.GetEntity());

			IList<DatasetTableRow> selectedItems =
				_getSurfaceDatasetsToAdd(_view,
				                         new ColumnDescriptor("Image", string.Empty),
				                         new ColumnDescriptor("AliasName", "Dataset"),
				                         new ColumnDescriptor("ModelName", "Model"),
				                         new ColumnDescriptor("DatasetCategory", "Category"));

			if (selectedItems == null)
			{
				return;
			}

			var anyAdded = false;
			foreach (DatasetTableRow selectedItem in selectedItems)
			{
				var selectedDataset = (VectorDataset) selectedItem.Entity;

				if (simpleTerrainDataset.Sources.Any(d => d.Dataset.Id == selectedDataset.Id))
				{
					_msg.WarnFormat("The simple terrain already contains the dataset {0}",
					                selectedDataset.Name);
				}
				else
				{
					var sourceDataset =
						new TerrainSourceDataset(selectedDataset, TinSurfaceType.MassPoint);
					simpleTerrainDataset.AddSourceDataset(sourceDataset);

					_sourceDatasetTableRows.Add(new TerrainSourceDatasetTableRow(sourceDataset));

					anyAdded = true;
				}
			}

			if (anyAdded)
			{
				Item.NotifyChanged();
			}
		}

		void ISimpleTerrainDatasetObserver.RemoveTargetClicked()
		{
			IList<TerrainSourceDatasetTableRow> selected =
				_view.GetSelectedSourceDatasetTableRows();

			SimpleTerrainDataset simpleTerrain = Assert.NotNull(Item.GetEntity());

			foreach (TerrainSourceDatasetTableRow item in selected)
			{
				simpleTerrain.RemoveSourceDataset(item.TargetDataset);
				_sourceDatasetTableRows.Remove(item);
			}

			Item.NotifyChanged();
		}
	}
}
