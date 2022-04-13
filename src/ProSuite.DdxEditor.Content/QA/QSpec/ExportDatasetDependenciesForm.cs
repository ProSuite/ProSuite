using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Dialogs;
using ProSuite.Commons.UI.Keyboard;
using ProSuite.Commons.UI.Persistence.WinForms;
using ProSuite.Commons.UI.ScreenBinding.Lists;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	internal partial class ExportDatasetDependenciesForm :
		Form,
		IExportDatasetDependenciesView,
		IFormStateAware<ExportDatasetDependencyGraphFormState>
	{
		[CanBeNull] private IExportDatasetDependenciesObserver _observer;

		[NotNull] private readonly BoundDataGridHandler<QualitySpecificationListItem>
			_gridHandler;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ExportDatasetDependenciesForm"/> class.
		/// </summary>
		/// <param name="fileFilter">The file filter.</param>
		/// <param name="defaultExtension">The default extension.</param>
		public ExportDatasetDependenciesForm([NotNull] string fileFilter,
		                                     [NotNull] string defaultExtension)
		{
			Assert.ArgumentNotNullOrEmpty(fileFilter, nameof(fileFilter));
			Assert.ArgumentNotNullOrEmpty(defaultExtension, nameof(defaultExtension));

			InitializeComponent();

			// initialize checkboxes (will be overriden by saved state)
			_checkBoxExportModelsAsParentNodes.Checked = true;
			_checkBoxExportBidirectionalDependenciesAsUndirectedEdges.Checked = false;

			_fileSystemPathControlSingleFile.FileFilter = fileFilter;
			_fileSystemPathControlSingleFile.FileDefaultExtension = defaultExtension;
			_fileSystemPathControlSingleFile.FileCheckFileExists = false;
			_fileSystemPathControlSingleFile.FileCheckPathExists = true;
			_fileSystemPathControlSingleFile.ControlPathType = FileSystemPathType.SaveFileName;

			_dataGridView.AutoGenerateColumns = false;
			_gridHandler = new BoundDataGridHandler<QualitySpecificationListItem>(_dataGridView);

			new DataGridViewFindController(_dataGridView, _dataGridViewFindToolStrip)
			{
				CanFilterRows = true
			};

			var stateManager = new FormStateManager<ExportDatasetDependencyGraphFormState>(this);
			stateManager.RestoreState();
			FormClosed += delegate { stateManager.SaveState(); };
		}

		#endregion

		public ICollection<string> DeletableFiles { get; private set; }

		public IDictionary<string, ICollection<QualitySpecification>>
			QualitySpecificationsByFileName { get; private set; }

		public bool ExportModelsAsParentNodes => _checkBoxExportModelsAsParentNodes.Checked;

		public bool IncludeSelfDependencies => _checkBoxIncludeSelfDependencies.Checked;

		public bool ExportBidirectionalDependenciesAsUndirectedEdges
			=> _checkBoxExportBidirectionalDependenciesAsUndirectedEdges.Checked;

		public ExportTarget CurrentExportTarget
		{
			get
			{
				return _radioButtonDirectory.Checked
					       ? ExportTarget.MultipleFiles
					       : ExportTarget.SingleFile;
			}
			set
			{
				switch (value)
				{
					case ExportTarget.SingleFile:
						_radioButtonSingleFile.Checked = true;
						break;

					case ExportTarget.MultipleFiles:
						_radioButtonDirectory.Checked = true;
						break;

					default:
						throw new ArgumentOutOfRangeException(
							nameof(value), value,
							$@"Unsupported export target: {value}");
				}
			}
		}

		bool IExportDatasetDependenciesView.FilePathEnabled
		{
			get { return _fileSystemPathControlSingleFile.Enabled; }
			set { _fileSystemPathControlSingleFile.Enabled = value; }
		}

		bool IExportDatasetDependenciesView.DirectoryPathEnabled
		{
			get { return _fileSystemPathControlDirectory.Enabled; }
			set { _fileSystemPathControlDirectory.Enabled = value; }
		}

		IExportDatasetDependenciesObserver IExportDatasetDependenciesView.Observer
		{
			set { _observer = value; }
		}

		bool IExportDatasetDependenciesView.SelectAllEnabled
		{
			get { return _toolStripButtonSelectAll.Enabled; }
			set { _toolStripButtonSelectAll.Enabled = value; }
		}

		bool IExportDatasetDependenciesView.SelectNoneEnabled
		{
			get { return _toolStripButtonSelectNone.Enabled; }
			set { _toolStripButtonSelectNone.Enabled = value; }
		}

		bool IExportDatasetDependenciesView.OKEnabled
		{
			get { return _buttonOK.Enabled; }
			set { _buttonOK.Enabled = value; }
		}

		string IExportDatasetDependenciesView.CurrentFilePath
		{
			get { return _fileSystemPathControlSingleFile.TextBox.Text; }
			set { _fileSystemPathControlSingleFile.TextBox.Text = value; }
		}

		string IExportDatasetDependenciesView.CurrentDirectoryPath
		{
			get { return _fileSystemPathControlDirectory.TextBox.Text; }
			set { _fileSystemPathControlDirectory.TextBox.Text = value; }
		}

		void IExportDatasetDependenciesView.BindTo(
			IEnumerable<QualitySpecificationListItem> items)
		{
			var list = new SortableBindingList<QualitySpecificationListItem>(items.ToList());
			_gridHandler.BindTo(list);
		}

		void IExportDatasetDependenciesView.Select(
			IEnumerable<QualitySpecificationListItem> items)
		{
			var itemsToSelect = new HashSet<QualitySpecificationListItem>(items);

			foreach (QualitySpecificationListItem item in
			         _gridHandler.GetAllRows(excludeInvisible: true))
			{
				item.Selected = itemsToSelect.Contains(item);
			}

			RefreshGrid();
		}

		int IExportDatasetDependenciesView.ItemCount => _dataGridView.RowCount;

		void IExportDatasetDependenciesView.SelectAll()
		{
			SetAll(check: true);
		}

		void IExportDatasetDependenciesView.SelectNone()
		{
			SetAll(check: false);
		}

		string IExportDatasetDependenciesView.StatusText
		{
			get { return _toolStripStatusLabel.Text; }
			set { _toolStripStatusLabel.Text = value; }
		}

		IList<QualitySpecificationListItem> IExportDatasetDependenciesView.SelectedItems
		{
			get
			{
				return _gridHandler.GetAllRows(excludeInvisible: true)
				                   .Where(item => item.Selected)
				                   .ToList();
			}
		}

		void IExportDatasetDependenciesView.SetCancelResult()
		{
			DialogResult = DialogResult.Cancel;

			DeletableFiles = null;
			QualitySpecificationsByFileName = null;
		}

		bool IExportDatasetDependenciesView.Confirm(string message)
		{
			return Dialog.YesNo(this, Text, message);
		}

		void IExportDatasetDependenciesView.SetOKResult(
			IDictionary<string, ICollection<QualitySpecification>>
				qualitySpecificationsByFileName,
			ICollection<string> deletableFiles)
		{
			DialogResult = DialogResult.OK;

			QualitySpecificationsByFileName = qualitySpecificationsByFileName;
			DeletableFiles = deletableFiles;
		}

		#region Implementation of IExportQualitySpecificationsView

		void IExportDatasetDependenciesView.SetCurrentFilePathError(string message)
		{
			_errorProvider.SetError(_fileSystemPathControlSingleFile, message);
		}

		void IExportDatasetDependenciesView.SetCurrentDirectoryPathError(string message)
		{
			_errorProvider.SetError(_fileSystemPathControlDirectory, message);
		}

		#endregion

		private void SetAll(bool check)
		{
			foreach (QualitySpecificationListItem item in
			         _gridHandler.GetAllRows(excludeInvisible: true))
			{
				item.Selected = check;
			}

			RefreshGrid();
		}

		private void RefreshGrid()
		{
			_dataGridView.RefreshEdit();
			_dataGridView.Refresh();
		}

		#region Event handlers

		private void ExportQualitySpecificationsForm_Load(object sender, EventArgs e)
		{
			_gridHandler.ClearSelection();
		}

		private void _fileSystemPathControlFile_LeaveTextBox(object sender, EventArgs e)
		{
			_observer?.FilePathFocusLost();
		}

		private void _fileSystemPathControlFile_ValueChanged(object sender, EventArgs e)
		{
			_observer?.FilePathChanged();
		}

		private void _fileSystemPathControlDirectory_LeaveTextBox(object sender, EventArgs e)
		{
			_observer?.DirectoryPathFocusLost();
		}

		private void _fileSystemPathControlDirectory_ValueChanged(object sender, EventArgs e)
		{
			_observer?.DirectoryPathChanged();
		}

		private void _radioButtonSingleFile_CheckedChanged(object sender, EventArgs e)
		{
			_observer?.ExportTargetChanged();
		}

		private void _buttonOK_Click(object sender, EventArgs e)
		{
			_observer?.OKClicked();
		}

		private void _buttonCancel_Click(object sender, EventArgs e)
		{
			_observer?.CancelClicked();
		}

		private void _toolStripButtonSelectAll_Click(object sender, EventArgs e)
		{
			_observer?.SelectAllClicked();
		}

		private void _toolStripButtonSelectNone_Click(object sender, EventArgs e)
		{
			_observer?.SelectNoneClicked();
		}

		private void _dataGridView_FilteredRowsChanged(object sender, EventArgs e)
		{
			_observer?.SelectedItemsChanged();
		}

		private void _dataGridView_CellValueChanged(object sender,
		                                            DataGridViewCellEventArgs e)
		{
			if (e.ColumnIndex == _columnSelected.Index)
			{
				const bool exclusive = true;
				if (KeyboardUtils.IsModifierPressed(Keys.Control, exclusive))
				{
					QualitySpecificationListItem clickedItem =
						Assert.NotNull(_gridHandler.GetRow(e.RowIndex));

					bool newValue = clickedItem.Selected;

					foreach (QualitySpecificationListItem selectedItem in
					         _gridHandler.GetSelectedRows())
					{
						selectedItem.Selected = newValue;
					}
				}
			}

			RefreshGrid();

			_observer?.SelectedItemsChanged();
		}

		private void _dataGridView_CurrentCellDirtyStateChanged(object sender, EventArgs e)
		{
			if (_dataGridView.IsCurrentCellDirty)
			{
				_dataGridView.CommitEdit(DataGridViewDataErrorContexts.Commit);
			}
		}

		#endregion

		#region Implementation of IFormStateAware<ExportQualitySpecificationsFormState>

		void IFormStateAware<ExportDatasetDependencyGraphFormState>.RestoreState(
			ExportDatasetDependencyGraphFormState formState)
		{
			_dataGridViewFindToolStrip.FilterRows = formState.FilterRows;
			_dataGridViewFindToolStrip.MatchCase = formState.MatchCase;

			CurrentExportTarget = formState.ExportTarget;
			_checkBoxExportModelsAsParentNodes.Checked = formState.ExportModelsAsParentNodes;
			_checkBoxExportBidirectionalDependenciesAsUndirectedEdges.Checked =
				formState.ExportBidirectionalDependenciesAsUndirectedEdges;
			_checkBoxIncludeSelfDependencies.Checked = formState.IncludeSelfDependencies;
		}

		void IFormStateAware<ExportDatasetDependencyGraphFormState>.GetState(
			ExportDatasetDependencyGraphFormState formState)
		{
			formState.FilterRows = _dataGridViewFindToolStrip.FilterRows;
			formState.MatchCase = _dataGridViewFindToolStrip.MatchCase;

			formState.ExportTarget = CurrentExportTarget;
			formState.ExportModelsAsParentNodes = _checkBoxExportModelsAsParentNodes.Checked;
			formState.ExportBidirectionalDependenciesAsUndirectedEdges =
				_checkBoxExportBidirectionalDependenciesAsUndirectedEdges.Checked;
			formState.IncludeSelfDependencies = _checkBoxIncludeSelfDependencies.Checked;
		}

		#endregion
	}
}
