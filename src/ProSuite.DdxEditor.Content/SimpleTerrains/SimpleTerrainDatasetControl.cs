using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Misc;
using ProSuite.Commons.UI.Dialogs;
using ProSuite.Commons.UI.ScreenBinding;
using ProSuite.Commons.UI.ScreenBinding.Elements;
using ProSuite.Commons.UI.ScreenBinding.Lists;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.SimpleTerrains
{
	public partial class SimpleTerrainDatasetControl : UserControl, ISimpleTerrainDatasetView
	{
		public SimpleTerrainDatasetControl()
		{
			InitializeComponent();

			_gridHandler =
				new BoundDataGridHandler<TerrainSourceDatasetTableRow>(
					_dataGridViewSourceDatasets);

			_binder =
				new ScreenBinder<SimpleTerrainDataset>(
					new ErrorProviderValidationMonitor(_errorProvider));
		}

		private readonly ScreenBinder<SimpleTerrainDataset> _binder;

		private readonly BoundDataGridHandler<TerrainSourceDatasetTableRow> _gridHandler;

		private readonly Latch _latch = new Latch();

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public ISimpleTerrainDatasetObserver Observer { get; set; }

		public Func<object> FindDatasetCategoryDelegate
		{
			get { return _objectReferenceControlDatasetCategory.FindObjectDelegate; }
			set { _objectReferenceControlDatasetCategory.FindObjectDelegate = value; }
		}

		public void BindTo(SimpleTerrainDataset target)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<TerrainSourceDataset> GetSelectedSourceDatasets()
		{
			throw new NotImplementedException();
		}

		public IList<TerrainSourceDatasetTableRow> GetSelectedSourceDatasetTableRows()
		{
			return _gridHandler.GetSelectedRows();
		}

		public void BindToSourceDatasets(
			SortableBindingList<TerrainSourceDatasetTableRow> sourceDatasetTableRows)
		{
			_latch.RunInsideLatch(
				delegate { _dataGridViewSourceDatasets.DataSource = sourceDatasetTableRows; });
		}

		public void OnBindingTo(SimpleTerrainDataset entity)
		{
			if (entity.GeometryType != null)
			{
				_textBoxGeometryType.Text = entity.GeometryType.Name;
			}
		}

		public void SetBinder(ScreenBinder<SimpleTerrainDataset> binder)
		{
			binder.Bind(m => m.Name)
			      .To(_textBoxName)
			      .WithLabel(_labelName);

			binder.Bind(m => m.Description)
			      .To(_textBoxDescription)
			      .WithLabel(_labelDescription);

			binder.Bind(m => m.AliasName)
			      .To(_textBoxAliasName)
			      .WithLabel(_labelAliasName);

			binder.Bind(m => m.Abbreviation)
			      .To(_textBoxAbbreviation)
			      .WithLabel(_labelAbbreviation);

			binder.AddElement(new ObjectReferenceScreenElement(
				                  binder.GetAccessor(m => m.DatasetCategory),
				                  _objectReferenceControlDatasetCategory));

			binder.Bind(m => m.PointDensity).To(_updownPointDensity);

			binder.OnChange = BinderChanged;
		}

		private static void Try(Action proc)
		{
			try
			{
				proc();
			}
			catch (Exception e)
			{
				ErrorHandler.HandleError(e, _msg);
			}
		}

		private void BinderChanged()
		{
			Observer?.NotifyChanged(_binder.IsDirty());
		}

		public void OnBoundTo(SimpleTerrainDataset entity)
		{
			Observer.OnBoundTo(entity);
		}

		private void _buttonAddSourceDatasets_Click(object sender, EventArgs e)
		{
			Try(delegate { Observer?.AddTargetClicked(); });
		}

		private void _buttonRemoveSourceDatasets_Click(object sender, EventArgs e)
		{
			Try(delegate { Observer?.RemoveTargetClicked(); });
		}

		private void _customTolerance_Click(object sender, EventArgs e) { }

		private void _dataGridViewSourceDatasets_CellValueChanged(
			object sender, DataGridViewCellEventArgs e)
		{
			Try(delegate
			{
				if (e.RowIndex < 0 || e.ColumnIndex < 0)
				{
					return;
				}

				Observer?.NotifyChanged(true);
			});
		}

		private void _dataGridViewSourceDatasets_SelectionChanged(object sender, EventArgs e)
		{
			if (_latch.IsLatched)
			{
				return;
			}

			Try(delegate { Observer?.TargetSelectionChanged(); });
		}

		private void _dataGridViewSourceDatasets_CellValidating(
			object sender, DataGridViewCellValidatingEventArgs e) { }
	}
}
