using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Persistence.WinForms;
using ProSuite.Commons.UI.ScreenBinding.Lists;
using ProSuite.Commons.UI.WinForms.Controls;

namespace ProSuite.Commons.UI.Finder
{
	internal partial class TypeFinderForm : Form, ITypeFinderView,
	                                        IFormStateAware<TypeFinderFormState>
	{
		private ITypeFinderObserver _observer;
		private bool _loadingRows;
		private IList<Type> _selectedTypes;

		private readonly FormStateManager<TypeFinderFormState> _formStateManager;
		private string _lastUsedAssemblyPath;

		#region Constructors

		public TypeFinderForm() : this(false, null) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="TypeFinderForm"/> class.
		/// </summary>
		/// <param name="allowMultiSelection">if set to <c>true</c> the form allows to select
		/// multiple types. Otherwise only one can be selected.</param>
		/// <param name="targetType">Type of the target.</param>
		public TypeFinderForm(bool allowMultiSelection, [CanBeNull] Type targetType)
		{
			InitializeComponent();

			string contextID = targetType == null
				                   ? string.Empty
				                   : targetType.FullName;
			_formStateManager = new FormStateManager<TypeFinderFormState>(this, contextID);
			_formStateManager.RestoreState();

			_dataGridView.AutoGenerateColumns = false;
			_dataGridView.MultiSelect = allowMultiSelection;

			_buttonSelectAll.Visible = allowMultiSelection;
			_buttonSelectNone.Visible = allowMultiSelection;

			new DataGridViewFindController(_dataGridView, _dataGridViewFindToolStrip);
		}

		#endregion

		#region ITypeFinderView Members

		string ITypeFinderView.LastUsedAssemblyPath => _lastUsedAssemblyPath;

		public IList<Type> SelectedTypes
		{
			get { return _selectedTypes; }
			set { _selectedTypes = value; }
		}

		ITypeFinderObserver ITypeFinderView.Observer
		{
			get { return _observer; }
			set { _observer = value; }
		}

		int ITypeFinderView.SelectedTypeCount => _dataGridView.SelectedRows.Count;

		bool ITypeFinderView.OKEnabled
		{
			get { return _buttonOK.Enabled; }
			set { _buttonOK.Enabled = value; }
		}

		bool ITypeFinderView.SelectAllTypesEnabled
		{
			get { return _buttonSelectAll.Enabled; }
			set { _buttonSelectAll.Enabled = value; }
		}

		bool ITypeFinderView.SelectNoTypesEnabled
		{
			get { return _buttonSelectNone.Enabled; }
			set { _buttonSelectNone.Enabled = value; }
		}

		int ITypeFinderView.TypeCount => _dataGridView.Rows.Count;

		string ITypeFinderView.StatusText
		{
			get { return _toolStripStatusLabel.Text; }
			set { _toolStripStatusLabel.Text = value; }
		}

		void ITypeFinderView.SetAssemblyError(string format, params string[] args)
		{
			Assert.ArgumentNotNullOrEmpty(format, nameof(format));

			_errorProvider.SetError(_fileSystemPathAssembly,
			                        string.Format(format, args.Cast<object>()));
		}

		void ITypeFinderView.ClearAssemblyError()
		{
			_errorProvider.SetError(_fileSystemPathAssembly, string.Empty);
		}

		void ITypeFinderView.ClearTypeRows()
		{
			_dataGridView.DataSource = null;
		}

		void ITypeFinderView.SetTypeRows(SortableBindingList<TypeTableRow> rows)
		{
			Assert.ArgumentNotNull(rows, nameof(rows));

			_dataGridView.SuspendLayout();

			bool origLoadingRows = _loadingRows;
			try
			{
				_loadingRows = true;

				_dataGridView.DataSource = typeof(TypeTableRow);
				_dataGridView.DataSource = rows;

				if (_dataGridView.SortedColumn == null)
				{
					_dataGridView.Sort(_columnName, ListSortDirection.Ascending);
				}
			}
			finally
			{
				_dataGridView.ResumeLayout();
				_loadingRows = origLoadingRows;
			}
		}

		bool ITypeFinderView.TrySelectRow(TypeTableRow selectTableRow)
		{
			Assert.ArgumentNotNull(selectTableRow, nameof(selectTableRow));

			_dataGridView.ClearSelection();

			foreach (DataGridViewRow row in _dataGridView.Rows)
			{
				var tableRow = row.DataBoundItem as TypeTableRow;

				if (tableRow == null || ! tableRow.Equals(selectTableRow))
				{
					continue;
				}

				// this is the row to select. Make sure it is visible
				if (! row.Displayed)
				{
					_dataGridView.FirstDisplayedScrollingRowIndex = row.Index;
				}

				_dataGridView.CurrentCell = row.Cells[0];
				row.Selected = true;

				return true;
			}

			return false;
		}

		IList<TypeTableRow> ITypeFinderView.GetSelectedTypeRows()
		{
			var result = new List<TypeTableRow>();

			foreach (DataGridViewRow row in _dataGridView.SelectedRows)
			{
				result.Add((TypeTableRow) row.DataBoundItem);
			}

			return result;
		}

		string ITypeFinderView.AssemblyPath
		{
			get { return _fileSystemPathAssembly.TextBox.Text; }
			set { _fileSystemPathAssembly.TextBox.Text = value; }
		}

		#endregion

		#region Event handlers

		private void _dataGridView_SelectionChanged(object sender, EventArgs e)
		{
			if (_loadingRows)
			{
				return;
			}

			if (_observer != null)
			{
				_observer.TypeSelectionChanged();
			}
		}

		private void _dataGridView_CellDoubleClick(object sender,
		                                           DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
			{
				return;
			}

			if (_observer != null)
			{
				_observer.RowDoubleClicked();
			}
		}

		private void _buttonOK_Click(object sender, EventArgs e)
		{
			if (_observer != null)
			{
				_observer.OKClicked();
			}
		}

		private void _fileSystemPathAssembly_ValueChanged(object sender, EventArgs e)
		{
			if (_observer != null)
			{
				_observer.AssemblyPathChanged();
			}
		}

		private void _buttonSelectAll_Click(object sender, EventArgs e)
		{
			_dataGridView.SelectAll();
		}

		private void _buttonSelectNone_Click(object sender, EventArgs e)
		{
			_dataGridView.ClearSelection();
		}

		private void TypeFinderForm_Load(object sender, EventArgs e)
		{
			if (_observer != null)
			{
				_observer.ViewLoaded();
			}
		}

		private void TypeFinderForm_FormClosed(object sender, FormClosedEventArgs e)
		{
			_formStateManager.SaveState();
		}

		#endregion

		#region Implementation of IFormStateAware<TypeFinderFormState>

		void IFormStateAware<TypeFinderFormState>.RestoreState(TypeFinderFormState formState)
		{
			_lastUsedAssemblyPath = formState.AssemblyPath;
		}

		void IFormStateAware<TypeFinderFormState>.GetState(TypeFinderFormState formState)
		{
			formState.AssemblyPath = _fileSystemPathAssembly.TextBox.Text;
		}

		#endregion
	}
}
