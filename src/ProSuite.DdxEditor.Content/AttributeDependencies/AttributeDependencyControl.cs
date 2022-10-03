using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AttributeDependencies;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Misc;
using ProSuite.Commons.UI.ScreenBinding;
using ProSuite.Commons.UI.ScreenBinding.Elements;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.AttributeDependencies;

namespace ProSuite.DdxEditor.Content.AttributeDependencies
{
	public partial class AttributeDependencyControl : UserControl,
	                                                  IAttributeDependencyView
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private IAttributeDependencyObserver _observer;

		private readonly BoundDataGridHandler<AttributeTableRow> _gridHandlerAvailable;
		private readonly BoundDataGridHandler<AttributeTableRow> _gridHandlerSource;
		private readonly BoundDataGridHandler<AttributeTableRow> _gridHandlerTarget;

		private readonly Latch _latch = new Latch();
		private static readonly Color _sourceBackColor = Color.FromArgb(255, 225, 225);
		private static readonly Color _targetBackColor = Color.FromArgb(225, 255, 225);

		private static string _lastSelectedTabPage;

		public AttributeDependencyControl()
		{
			InitializeComponent();

			_dataGridViewAvailable.AutoGenerateColumns = false;
			_dataGridViewSource.AutoGenerateColumns = false;
			_dataGridViewTarget.AutoGenerateColumns = false;

			_gridHandlerAvailable =
				new BoundDataGridHandler<AttributeTableRow>(_dataGridViewAvailable);
			_gridHandlerSource =
				new BoundDataGridHandler<AttributeTableRow>(_dataGridViewSource);
			_gridHandlerTarget =
				new BoundDataGridHandler<AttributeTableRow>(_dataGridViewTarget);

			SetCellBackColor(_dataGridViewSource, _sourceBackColor);
			SetCellBackColor(_dataGridViewTarget, _targetBackColor);

			TabControlUtils.SelectTabPage(_tabControl, _lastSelectedTabPage);
		}

		private static void SetCellBackColor(DataGridView grid, Color color)
		{
			foreach (DataGridViewColumn column in grid.Columns)
			{
				column.DefaultCellStyle.BackColor = color;
			}
		}

		#region IWrappedEntityControl

		void IWrappedEntityControl<AttributeDependency>.OnBindingTo(
			AttributeDependency entity) { }

		void IWrappedEntityControl<AttributeDependency>.SetBinder(
			ScreenBinder<AttributeDependency> binder)
		{
			binder.AddElement(new ObjectReferenceScreenElement(
				                  binder.GetAccessor(m => m.Dataset),
				                  _objectReferenceControlDataset));
			binder.OnChange = OnBinderChanged;
		}

		void IWrappedEntityControl<AttributeDependency>.OnBoundTo(
			AttributeDependency entity)
		{
			_observer?.EntityBound();
		}

		#endregion

		#region IAttributeDependencyView

		IAttributeDependencyObserver IAttributeDependencyView.Observer
		{
			get { return _observer; }
			set { _observer = value; }
		}

		Func<object> IAttributeDependencyView.FindDatasetDelegate
		{
			get { return _objectReferenceControlDataset.FindObjectDelegate; }
			set { _objectReferenceControlDataset.FindObjectDelegate = value; }
		}

		public void BindToAvailableAttributeRows(IList<AttributeTableRow> rows)
		{
			_latch.RunInsideLatch(
				delegate { _dataGridViewAvailable.DataSource = rows; });
		}

		public void BindToSourceAttributeRows(IList<AttributeTableRow> rows)
		{
			_latch.RunInsideLatch(
				delegate { _dataGridViewSource.DataSource = rows; });
		}

		public void BindToTargetAttributeRows(IList<AttributeTableRow> rows)
		{
			_latch.RunInsideLatch(
				delegate { _dataGridViewTarget.DataSource = rows; });
		}

		public void SetupMappingGrid(IList<AttributeInfo> sourceAttrs,
		                             IList<AttributeInfo> targetAttrs,
		                             string descriptionFieldName)
		{
			_latch.RunInsideLatch(
				delegate
				{
					_dataGridViewMappings.Columns.Clear();

					_dataGridViewMappings.AutoGenerateColumns = false;

					foreach (AttributeInfo attr in sourceAttrs)
					{
						var column = new DataGridViewTextBoxColumn();
						column.DataPropertyName = attr.Name;
						column.HeaderText = attr.Name;
						column.Name = attr.Name;
						column.DefaultCellStyle.BackColor = _sourceBackColor;
						column.HeaderCell.ContextMenuStrip = CreateContextMenu(column, attr, true);

						_dataGridViewMappings.Columns.Add(column);
					}

					foreach (AttributeInfo attr in targetAttrs)
					{
						var column = new DataGridViewTextBoxColumn();
						column.DataPropertyName = attr.Name;
						column.HeaderText = attr.Name;
						column.Name = attr.Name;
						column.DefaultCellStyle.BackColor = _targetBackColor;
						column.HeaderCell.ContextMenuStrip = CreateContextMenu(column, attr, false);

						_dataGridViewMappings.Columns.Add(column);
					}

					if (! string.IsNullOrEmpty(descriptionFieldName))
					{
						var column = new DataGridViewTextBoxColumn();
						column.DataPropertyName = descriptionFieldName;
						column.HeaderText = descriptionFieldName;
						column.DefaultCellStyle.BackColor = Color.LightGray;
						column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
						column.MinimumWidth = 80;

						_dataGridViewMappings.Columns.Add(column);
					}

					new DataGridViewFindController(_dataGridViewMappings,
					                               _dataGridViewFindToolStrip);
				});
		}

		public void BindToAttributeValueMappings(DataView mappingTableView)
		{
			_latch.RunInsideLatch(
				delegate
				{
					Assert.AreEqual(
						_dataGridViewMappings.Columns.Count,
						mappingTableView.Table.Columns.Count,
						"Grid and View column counts don't agree");

					_dataGridViewMappings.DataSource = mappingTableView; // bind
				});
		}

		public IList<AttributeTableRow> GetSelectedAvailableAttributes()
		{
			return _gridHandlerAvailable.GetSelectedRows();
		}

		public IList<AttributeTableRow> GetSelectedSourceAttributes()
		{
			return _gridHandlerSource.GetSelectedRows();
		}

		public IList<AttributeTableRow> GetSelectedTargetAttributes()
		{
			return _gridHandlerTarget.GetSelectedRows();
		}

		public int SelectedAvailableAttributeCount => _gridHandlerAvailable.SelectedRowCount;

		public int SelectedSourceAttributeCount => _gridHandlerSource.SelectedRowCount;

		public int SelectedTargetAttributeCount => _gridHandlerTarget.SelectedRowCount;

		public bool AddSourceAttributesEnabled
		{
			get { return _buttonAddToSource.Enabled; }
			set { _buttonAddToSource.Enabled = value; }
		}

		public bool RemoveSourceAttributesEnabled
		{
			get { return _buttonRemoveFromSource.Enabled; }
			set { _buttonRemoveFromSource.Enabled = value; }
		}

		public bool AddTargetAttributesEnabled
		{
			get { return _buttonAddToTarget.Enabled; }
			set { _buttonAddToTarget.Enabled = value; }
		}

		public bool RemoveTargetAttributesEnabled
		{
			get { return _buttonRemoveFromTarget.Enabled; }
			set { _buttonRemoveFromTarget.Enabled = value; }
		}

		public bool ImportMappingsEnabled
		{
			get { return _buttonImportMappings.Enabled; }
			set { _buttonImportMappings.Enabled = value; }
		}

		public bool ExportMappingsEnabled
		{
			get { return _buttonExportMappings.Enabled; }
			set { _buttonExportMappings.Enabled = value; }
		}

		#endregion

		#region Event handler

		private void _buttonAddToSource_Click(object sender, EventArgs e)
		{
			if (_observer != null)
			{
				Try("_buttonAddToSource_Click", _observer.AddSourceAttributesClicked);
			}
		}

		private void _buttonRemoveFromSource_Click(object sender, EventArgs e)
		{
			if (_observer != null)
			{
				Try("_buttonRemoveFromSource_Click", _observer.RemoveSourceAttributesClicked);
			}
		}

		private void _buttonAddToTarget_Click(object sender, EventArgs e)
		{
			if (_observer != null)
			{
				Try("_buttonAddToTarget_Click", _observer.AddTargetAttributesClicked);
			}
		}

		private void _buttonRemoveFromTarget_Click(object sender, EventArgs e)
		{
			if (_observer != null)
			{
				Try("_buttonRemoveFromTarget_Click", _observer.RemoveTargetAttributesClicked);
			}
		}

		private void _dataGridViewAvailable_SelectionChanged(object sender,
		                                                     EventArgs e)
		{
			if (_observer != null)
			{
				Try("_dataGridViewAvailable_SelectionChanged", _observer.AttributeSelectionChanged);
			}
		}

		private void _dataGridViewSource_DataBindingComplete(object sender,
		                                                     DataGridViewBindingCompleteEventArgs
			                                                     e)
		{
			Try("_dataGridViewSource_DataBindingComplete", _dataGridViewSource.ClearSelection);
		}

		private void _dataGridViewSource_SelectionChanged(object sender, EventArgs e)
		{
			if (_observer != null)
			{
				Try("_dataGridViewSource_SelectionChanged", _observer.AttributeSelectionChanged);
			}
		}

		private void _dataGridViewTarget_DataBindingComplete(object sender,
		                                                     DataGridViewBindingCompleteEventArgs
			                                                     e)
		{
			Try("_dataGridViewTarget_DataBindingComplete", _dataGridViewTarget.ClearSelection);
		}

		private void _dataGridViewTarget_SelectionChanged(object sender, EventArgs e)
		{
			if (_observer != null)
			{
				Try("_dataGridViewTarget_SelectionChanged", _observer.AttributeSelectionChanged);
			}
		}

		private void _buttonImportMappings_Click(object sender, EventArgs e)
		{
			if (_observer != null)
			{
				Try("_buttonImportMappings_Click", _observer.ImportMappingsClicked);
			}
		}

		private void _buttonExportMappings_Click(object sender, EventArgs e)
		{
			if (_observer != null)
			{
				Try("_buttonExportMappings_Click", _observer.ExportMappingsClicked);
			}
		}

		private void _dataGridViewMappings_CellFormatting(object sender,
		                                                  DataGridViewCellFormattingEventArgs
			                                                  e)
		{
			if (_observer != null)
			{
				try
				{
					e.Value = _observer.FormatMappingValue(e.Value, e.ColumnIndex,
					                                       e.DesiredType);
					e.FormattingApplied = true;
				}
				catch (Exception ex)
				{
					_msg.WarnFormat(
						"Cannot format cell value (row {0}, column {1}): {2}",
						e.RowIndex, e.ColumnIndex, ex.Message);
					e.FormattingApplied = false;
				}
			}
		}

		private void _dataGridViewMappings_CellParsing(object sender,
		                                               DataGridViewCellParsingEventArgs
			                                               e)
		{
			if (_observer != null)
			{
				try
				{
					e.Value = _observer.ParseMappingValue(e.Value, e.ColumnIndex,
					                                      e.DesiredType);
					e.ParsingApplied = true;
				}
				catch (Exception ex)
				{
					_msg.WarnFormat("Cannot parse cell value \"{0}\": {1}", e.Value,
					                ex.Message);
					throw;
					// Rethrow! We don't want DataGridView to work around this...
				}
			}
		}

		private void _dataGridViewMappings_CellValueChanged(object sender,
		                                                    DataGridViewCellEventArgs
			                                                    e)
		{
			if (_latch.IsLatched)
			{
				return;
			}

			if (_observer != null)
			{
				Try("_dataGridViewMappings_CellValueChanged", _observer.MappingValueChanged);
			}
		}

		private void _dataGridViewMappings_UserAddedRow(object sender,
		                                                DataGridViewRowEventArgs e)
		{
			if (_latch.IsLatched)
			{
				return;
			}

			if (_observer != null)
			{
				Try("_dataGridViewMappings_UserAddedRow", _observer.MappingRowAdded);
			}
		}

		private void _dataGridViewMappings_UserDeletedRow(object sender,
		                                                  DataGridViewRowEventArgs e)
		{
			if (_latch.IsLatched)
			{
				return;
			}

			if (_observer != null)
			{
				Try("_dataGridViewMappings_UserDeletedRow", _observer.MappingRowDeleted);
			}
		}

		private void _dataGridViewMappings_Sorted(object sender, EventArgs e)
		{
			// DataGridView manages selection by row index,
			// so it's now invalid and we better clear it:
			Try("_dataGridViewMappings_Sorted", _dataGridViewMappings.ClearSelection);
		}

		private void _tabControl_SelectedIndexChanged(object sender, EventArgs e)
		{
			_lastSelectedTabPage = TabControlUtils.GetSelectedTabPageName(_tabControl);
		}

		#endregion

		#region Private

		private static void Try(string methodName, Action procedure)
		{
			try
			{
				procedure();
			}
			catch (Exception ex)
			{
				_msg.Error(string.Format("{0} failed: {1}", methodName, ex.Message), ex);
			}
		}

		private ContextMenuStrip CreateContextMenu(
			DataGridViewColumn column, AttributeInfo attr, bool includeWildcard)
		{
			var menu = new ContextMenuStrip();
			menu.Tag = column; // required by event handlers

			var menuItems = new List<ToolStripItem>();

			string text = attr.IsSubtypeField
				              ? string.Format("{0} (Subtype Field)", attr.Type)
				              : attr.Type;
			ToolStripMenuItem item = CreateToolStripMenuItem(text);
			item.Image = attr.Image;
			item.Enabled = false;
			menuItems.Add(item);

			menuItems.Add(new ToolStripSeparator());

			if (includeWildcard)
			{
				item = CreateToolStripMenuItem("* (any)", SetWildcardClicked);
				menuItems.Add(item);
			}

			if (attr.IsNullable)
			{
				item = CreateToolStripMenuItem("NULL", SetNullClicked);
				menuItems.Add(item);
			}

			if (attr.CodedValues != null)
			{
				menuItems.Add(new ToolStripSeparator());

				item = CreateToolStripMenuItem(attr.DomainName);
				item.Enabled = false;
				menuItems.Add(item);

				foreach (CodedValue pair in attr.CodedValues)
				{
					text = string.Format("{0} = {1}", pair.Value, pair.Name);
					item = CreateToolStripMenuItem(text, SetValueClicked);
					item.Tag = pair.Value;
					menuItems.Add(item);
				}
			}

			if (attr.DefaultValue != null && attr.DefaultValue != DBNull.Value)
			{
				menuItems.Add(new ToolStripSeparator());

				text = string.Format("{0} (Default Value)", attr.DefaultValue);
				item = CreateToolStripMenuItem(text, SetValueClicked);
				item.Tag = attr.DefaultValue;
				menuItems.Add(item);
			}

			menu.Items.AddRange(menuItems.ToArray());

			return menu;
		}

		private ToolStripMenuItem CreateToolStripMenuItem(
			string text, EventHandler onClickHandler = null)
		{
			var toolStripMenuItem = new ToolStripMenuItem(text);
			if (onClickHandler != null)
			{
				toolStripMenuItem.Click += onClickHandler;
			}

			return toolStripMenuItem;
		}

		private void SetWildcardClicked(object sender, EventArgs e)
		{
			var item = (ToolStripItem) sender;
			var column = (DataGridViewColumn) item.Owner.Tag;
			SetColumnValue(column, Wildcard.Value);
		}

		private void SetNullClicked(object sender, EventArgs e)
		{
			var item = (ToolStripItem) sender;
			var column = (DataGridViewColumn) item.Owner.Tag;
			SetColumnValue(column, DBNull.Value);
		}

		private void SetValueClicked(object sender, EventArgs e)
		{
			var item = (ToolStripItem) sender;
			var column = (DataGridViewColumn) item.Owner.Tag;
			SetColumnValue(column, ((ToolStripItem) sender).Tag);
		}

		private void SetColumnValue(DataGridViewColumn column, object value)
		{
			// Set given value in all selected rows.
			// Remove the "new record row" from the selected rows
			// unless it is the only selected row; reason:
			//   1. must be treated differently (see below)
			//   2. separate UPDATE case from INSERT case
			// If no row is selected, do nothing.

			DataGridViewRow newRecordRow = null;
			foreach (DataGridViewRow row in _dataGridViewMappings.SelectedRows)
			{
				if (row.IsNewRow)
				{
					newRecordRow = row;
					break;
				}
			}

			// Remove "new record row" if also other rows are selected:
			if (newRecordRow != null && _dataGridViewMappings.SelectedRows.Count > 1)
			{
				newRecordRow.Selected = false;
				newRecordRow = null;
			}

			// For UPDATE, work with the data bound stuff behind the grid veiw.
			// For INSERT, set the cell's value and do some magic fiddling found here:
			// http://stackoverflow.com/questions/16783511/how-to-finalize-a-new-row
			// (the magic makes the value change persist).

			var modified = false;

			if (newRecordRow != null)
			{
				// INSERT new record and set cell value:
				DataGridViewCell cell = newRecordRow.Cells[column.Index];
				_dataGridViewMappings.CurrentCell = cell;
				_dataGridViewMappings.NotifyCurrentCellDirty(true);
				cell.Value = value;
				_dataGridViewMappings.EndEdit();
				_dataGridViewMappings.NotifyCurrentCellDirty(false);
				modified = true;
			}
			else
			{
				// UPDATE existing record for each selected row:
				foreach (DataGridViewRow row in _dataGridViewMappings.SelectedRows)
				{
					// Work on the data bound stuff, not the data grid view:
					// (The new record row's DataBoundItem would be null!)
					DataRowView item = Assert.NotNull((DataRowView) row.DataBoundItem);

					item.BeginEdit();
					item[column.Index] = value;
					item.EndEdit();
					modified = true;
				}
			}

			if (modified)
			{
				_observer?.MappingValueChanged(); // TODO rename/add MappingsChanged
			}
		}

		//private void SelectGridRows(
		//    BoundDataGridHandler<ObjectAttributeTableRow> gridHandler,
		//    IEnumerable<ObjectAttribute> select)
		//{
		//    Assert.ArgumentNotNull(gridHandler, "gridHandler");
		//    Assert.ArgumentNotNull(select, "select");

		//    var selectable = new SimpleSet<ObjectAttribute>(select);

		//    _latch.RunInsideLatch(
		//        () => gridHandler.SelectRows(
		//            row => selectable.Contains((ObjectAttribute) row.Entity)));

		//    OnAttributeSelectionChanged();
		//}

		private void OnBinderChanged()
		{
			_observer?.DatasetChanged();
		}

		#endregion
	}
}
