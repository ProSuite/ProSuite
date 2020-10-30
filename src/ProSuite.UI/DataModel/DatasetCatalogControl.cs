using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.UI.DataModel
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

		public void SetDatasets([NotNull] IEnumerable<Dataset> datasets)
		{
			Assert.ArgumentNotNull(datasets, "datasets");

			_groupedListView.BeginUpdate();

			try
			{
				_groupedListView.ClearItems();

				foreach (Dataset dataset in datasets)
				{
					string categoryName = (dataset.DatasetCategory != null)
						                      ? dataset.DatasetCategory.Name
						                      : null;
					string imageKey = DatasetTypeImageLookup.GetImageKey(dataset);

					_groupedListView.AddItem(dataset.DisplayName, categoryName, imageKey);
				}
			}
			finally
			{
				_groupedListView.EndUpdate();
			}
		}

		public int DatasetCount
		{
			get { return _groupedListView.CountItems(); }
		}

		public int CategoryCount
		{
			get { return _groupedListView.Groups.Count; }
		}

		public event EventHandler DatasetsChanged;

		protected virtual void OnDatasetListChanged(EventArgs e)
		{
			if (DatasetsChanged != null)
			{
				DatasetsChanged(this, e);
			}
		}

		public override Size GetPreferredSize(Size proposedSize)
		{
			return _groupedListView.GetPreferredSize(proposedSize);
		}
	}
}
