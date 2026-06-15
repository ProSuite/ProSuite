using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.UI.Core.DataModel.ResourceLookup;

namespace ProSuite.UI.Core.DataModel
{
	public partial class DatasetCatalogControl : UserControl
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DatasetCatalogControl"/> class.
		/// </summary>
		public DatasetCatalogControl()
		{
			InitializeComponent();

			_groupedListView.ShowCheckBoxes = false;
			_groupedListView.SmallImageList = DatasetTypeImageLookup.CreateImageList();
		}

		public void ClearDatasets()
		{
			_groupedListView.ClearItems();
		}

		public void SetDatasets([NotNull] IEnumerable<IDdxDataset> datasets)
		{
			Assert.ArgumentNotNull(datasets, nameof(datasets));

			_groupedListView.BeginUpdate();

			try
			{
				_groupedListView.ClearItems();

				foreach (IDdxDataset dataset in datasets)
				{
					string categoryName = dataset.DatasetCategory?.Name;
					string imageKey = DatasetTypeImageLookup.GetImageKey(dataset);

					_groupedListView.AddItem(dataset.DisplayName, categoryName, imageKey);
				}
			}
			finally
			{
				_groupedListView.EndUpdate();
			}
		}

		public int DatasetCount => _groupedListView.CountItems();

		public int CategoryCount => _groupedListView.Groups.Count;

		public event EventHandler DatasetsChanged;

		protected virtual void OnDatasetListChanged(EventArgs e)
		{
			DatasetsChanged?.Invoke(this, e);
		}

		public override Size GetPreferredSize(Size proposedSize)
		{
			return _groupedListView.GetPreferredSize(proposedSize);
		}
	}
}
