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
	internal partial class ExportQualitySpecificationsForm :
		Form,
		IExportQualitySpecificationsView,
		IFormStateAware<ExportQualitySpecificationsFormState>
	{
		[CanBeNull] private IExportQualitySpecificationsObserver _observer;

		[NotNull] private readonly BoundDataGridHandler<QualitySpecificationListItem>
			_gridHandler;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ExportQualitySpecificationsForm"/> class.
		/// </summary>
		/// <param name="fileFilter">The file filter.</param>
		/// <param name="defaultExtension">The default extension.</param>
		public ExportQualitySpecificationsForm([NotNull] string fileFilter,
		                                       [NotNull] string defaultExtension)
		{
			Assert.ArgumentNotNullOrEmpty(fileFilter, nameof(fileFilter));
			Assert.ArgumentNotNullOrEmpty(defaultExtension, nameof(defaultExtension));

			InitializeComponent();

			// initialize checkboxes (will be overriden by saved state)
			_checkBoxExportMetadata.Checked = true;
			_checkBoxExportWorkspaceConnections.Checked = false;

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

			var stateManager = new FormStateManager<ExportQualitySpecificationsFormState>(this);
			stateManager.RestoreState();
			FormClosed += delegate { stateManager.SaveState(); };
		}

		#endregion

		public ICollection<string> DeletableFiles { get; private set; }

		public IDictionary<string, ICollection<QualitySpecification>>
			QualitySpecificationsByFileName { get; private set; }

		public bool ExportMetadata => _checkBoxExportMetadata.Checked;

		public bool ExportWorkspaceConnections
			=> _checkBoxExportWorkspaceConnections.Checked;

		public bool ExportConnectionFilePaths => _checkBoxExportConnectionFilePaths.Checked;

		public bool ExportAllTestDescriptors => _radioButtonExportAllTestDescriptors.Checked;

		public bool ExportAllCategories => _radioButtonExportAllCategories.Checked;

		public bool ExportNotes => _checkBoxExportNotes.Checked;

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

		bool IExportQualitySpecificationsView.FilePathEnabled
		{
			get { return _fileSystemPathControlSingleFile.Enabled; }
			set { _fileSystemPathControlSingleFile.Enabled = value; }
		}

		bool IExportQualitySpecificationsView.DirectoryPathEnabled
		{
			get { return _fileSystemPathControlDirectory.Enabled; }
			set { _fileSystemPathControlDirectory.Enabled = value; }
		}

		IExportQualitySpecificationsObserver IExportQualitySpecificationsView.Observer
		{
			set { _observer = value; }
		}

		bool IExportQualitySpecificationsView.SelectAllEnabled
		{
			get { return _toolStripButtonSelectAll.Enabled; }
			set { _toolStripButtonSelectAll.Enabled = value; }
		}

		bool IExportQualitySpecificationsView.SelectNoneEnabled
		{
			get { return _toolStripButtonSelectNone.Enabled; }
			set { _toolStripButtonSelectNone.Enabled = value; }
		}

		bool IExportQualitySpecificationsView.OKEnabled
		{
			get { return _buttonOK.Enabled; }
			set { _buttonOK.Enabled = value; }
		}

		bool IExportQualitySpecificationsView.ExportSdeConnectionFilePathsEnabled
		{
			get { return _checkBoxExportConnectionFilePaths.Enabled; }
			set { _checkBoxExportConnectionFilePaths.Enabled = value; }
		}

		string IExportQualitySpecificationsView.CurrentFilePath
		{
			get { return _fileSystemPathControlSingleFile.TextBox.Text; }
			set { _fileSystemPathControlSingleFile.TextBox.Text = value; }
		}

		string IExportQualitySpecificationsView.CurrentDirectoryPath
		{
			get { return _fileSystemPathControlDirectory.TextBox.Text; }
			set { _fileSystemPathControlDirectory.TextBox.Text = value; }
		}

		void IExportQualitySpecificationsView.BindTo(
			IEnumerable<QualitySpecificationListItem> items)
		{
			var list = new SortableBindingList<QualitySpecificationListItem>(items.ToList());
			_gridHandler.BindTo(list);
		}

		void IExportQualitySpecificationsView.Select(
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

		int IExportQualitySpecificationsView.ItemCount => _dataGridView.RowCount;

		void IExportQualitySpecificationsView.SelectAll()
		{
			SetAll(check: true);
		}

		void IExportQualitySpecificationsView.SelectNone()
		{
			SetAll(check: false);
		}

		string IExportQualitySpecificationsView.StatusText
		{
			get { return _toolStripStatusLabel.Text; }
			set { _toolStripStatusLabel.Text = value; }
		}

		IList<QualitySpecificationListItem> IExportQualitySpecificationsView.SelectedItems
		{
			get
			{
				return _gridHandler.GetAllRows(excludeInvisible: true)
				                   .Where(item => item.Selected)
				                   .ToList();
			}
		}

		void IExportQualitySpecificationsView.SetCancelResult()
		{
			DialogResult = DialogResult.Cancel;

			DeletableFiles = null;
			QualitySpecificationsByFileName = null;
		}

		bool IExportQualitySpecificationsView.Confirm(string message)
		{
			return Dialog.YesNo(this, Text, message);
		}

		void IExportQualitySpecificationsView.SetOKResult(
			IDictionary<string, ICollection<QualitySpecification>>
				qualitySpecificationsByFileName,
			ICollection<string> deletableFiles)
		{
			DialogResult = DialogResult.OK;

			QualitySpecificationsByFileName = qualitySpecificationsByFileName;
			DeletableFiles = deletableFiles;
		}

		#region Implementation of IExportQualitySpecificationsView

		void IExportQualitySpecificationsView.SetCurrentFilePathError(string message)
		{
			_errorProvider.SetError(_fileSystemPathControlSingleFile, message);
		}

		void IExportQualitySpecificationsView.SetCurrentDirectoryPathError(string message)
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

		private void _checkBoxExportWorkspaceConnections_CheckedChanged(object sender,
		                                                                EventArgs e)
		{
			_observer?.ExportWorkspaceConnectionsChanged();
		}

		#endregion

		#region Implementation of IFormStateAware<ExportQualitySpecificationsFormState>

		void IFormStateAware<ExportQualitySpecificationsFormState>.RestoreState(
			ExportQualitySpecificationsFormState formState)
		{
			_dataGridViewFindToolStrip.FilterRows = formState.FilterRows;
			_dataGridViewFindToolStrip.MatchCase = formState.MatchCase;

			_checkBoxExportMetadata.Checked = formState.ExportMetadata;
			_checkBoxExportWorkspaceConnections.Checked =
				formState.ExportWorkspaceConnectionStrings;
			_checkBoxExportConnectionFilePaths.Checked =
				formState.ExportSdeConnectionFilePaths;
			_radioButtonExportAllTestDescriptors.Checked = formState.ExportAllTestDescriptors;
			_radioButtonExportAllCategories.Checked = formState.ExportAllCategories;
			CurrentExportTarget = formState.ExportTarget;
			_checkBoxExportNotes.Checked = formState.ExportNotes;
		}

		void IFormStateAware<ExportQualitySpecificationsFormState>.GetState(
			ExportQualitySpecificationsFormState formState)
		{
			formState.FilterRows = _dataGridViewFindToolStrip.FilterRows;
			formState.MatchCase = _dataGridViewFindToolStrip.MatchCase;

			formState.ExportMetadata = _checkBoxExportMetadata.Checked;
			formState.ExportWorkspaceConnectionStrings =
				_checkBoxExportWorkspaceConnections.Checked;
			formState.ExportSdeConnectionFilePaths =
				_checkBoxExportConnectionFilePaths.Checked;
			formState.ExportAllTestDescriptors = _radioButtonExportAllTestDescriptors.Checked;
			formState.ExportAllCategories = _radioButtonExportAllCategories.Checked;
			formState.ExportTarget = CurrentExportTarget;
			formState.ExportNotes = _checkBoxExportNotes.Checked;
		}

		#endregion
	}
}