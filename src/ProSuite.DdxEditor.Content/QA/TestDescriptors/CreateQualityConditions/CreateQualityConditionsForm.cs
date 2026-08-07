using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Dialogs;
using ProSuite.Commons.UI.Persistence.WinForms;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Content.QA.QSpec;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Menus;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors.CreateQualityConditions
{
	public partial class CreateQualityConditionsForm :
		Form,
		ICreateQualityConditionsView,
		IFormStateAware<CreateQualityConditionsFormState>
	{
		private readonly FormStateManager<CreateQualityConditionsFormState>
			_formStateManager;

		private ICreateQualityConditionsObserver _observer;
		private IList<QualityConditionParameters> _qualityConditionParameters;

		private readonly BoundDataGridHandler<QualitySpecificationTableRow>
			_qualitySpecificationGridHandler;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Initializes a new instance of the <see cref="CreateQualityConditionsForm"/> class.
		/// </summary>
		public CreateQualityConditionsForm()
		{
			InitializeComponent();

			// Workaround. See CustomizeQASpecForm's Load handler.
			_splitContainer.Panel1MinSize = 200;
			_splitContainer.Panel2MinSize = 100;

			_formStateManager = new FormStateManager<CreateQualityConditionsFormState>(this);

			FormClosed += delegate { _formStateManager.SaveState(); };

			_dataGridView.AutoGenerateColumns = false;

			// Button cells ("…") are only used by presenters that add such columns; the
			// classic one does not, and its observer ignores the notification.
			_dataGridView.CellContentClick += _dataGridView_CellContentClick;

			_qualitySpecificationGridHandler =
				new BoundDataGridHandler<QualitySpecificationTableRow>(
					_dataGridViewQualitySpecifications);
		}

		#region ICreateQualityConditionsView Members

		public IList<QualityConditionParameters> QualityConditionParameters
		{
			get { return _qualityConditionParameters; }
			set { _qualityConditionParameters = value; }
		}

		ICreateQualityConditionsObserver ICreateQualityConditionsView.Observer
		{
			get { return _observer; }
			set
			{
				_observer = value;

				if (value != null && value.CanFindCategory)
				{
					_objectReferenceControlCategory.FindObjectDelegate = value.FindCategory;
					_objectReferenceControlCategory.FormatTextDelegate = value.FormatCategoryText;
				}
				else
				{
					_objectReferenceControlCategory.FindObjectDelegate = null;
					_objectReferenceControlCategory.FormatTextDelegate = null;
				}
			}
		}

		string ICreateQualityConditionsView.TestDescriptorName
		{
			get { return _textBoxTestDescriptorName.Text; }
			set { _textBoxTestDescriptorName.Text = value; }
		}

		string ICreateQualityConditionsView.SubjectCaption
		{
			get { return label2.Text; }
			set { SetTextKeepingRightEdge(label2, value); }
		}

		string ICreateQualityConditionsView.ExcludeDatasetsCaption
		{
			get { return _checkBoxExcludeDatasetsUsingThisTest.Text; }
			set
			{
				SetTextKeepingRightEdge(_checkBoxExcludeDatasetsUsingThisTest, value);
			}
		}

		/// <summary>
		/// Both captions sit on auto-sizing controls that are aligned by their right
		/// edge (the labels line up with the input fields, the checkbox with the grid),
		/// so a new text must move the control left or right by the width difference.
		/// </summary>
		private static void SetTextKeepingRightEdge([NotNull] Control control,
		                                            [NotNull] string text)
		{
			int right = control.Right;

			control.Text = text;

			control.Left = right - control.Width;
		}

		string ICreateQualityConditionsView.QualityConditionNames
		{
			get { return _textBoxQualityConditionNames.Text; }
			set { _textBoxQualityConditionNames.Text = value; }
		}

		void ICreateQualityConditionsView.BindParameters(DataTable parametersDataTable)
		{
			_dataGridView.DataSource = parametersDataTable;
		}

		void ICreateQualityConditionsView.AddParametersColumn(DataGridViewColumn gridColumn)
		{
			_dataGridView.Columns.Add(gridColumn);
		}

		void ICreateQualityConditionsView.ClearParametersColumns()
		{
			// unbind first: the bound DataTable's columns are what the grid columns
			// refer to through DataPropertyName
			_dataGridView.DataSource = null;
			_dataGridView.Columns.Clear();
		}

		void ICreateQualityConditionsView.AddOptionsControl(Control control)
		{
			Assert.ArgumentNotNull(control, nameof(control));

			// The header area is laid out with absolute positions, so make room for the
			// additional control by moving the splitter (and everything below it) down.
			const int gap = 6;

			control.Dock = DockStyle.None;
			control.Anchor = AnchorStyles.Top | AnchorStyles.Left;

			_panelMain.Controls.Add(control);

			control.Location = new Point(_textBoxSupportedVariables.Left,
			                             _splitContainer.Top);

			int addedHeight = control.Height + gap;

			// grow the window by the same amount, so the grid below keeps its size
			Height += addedHeight;

			MinimumSize = new Size(MinimumSize.Width, MinimumSize.Height + addedHeight);

			_splitContainer.SetBounds(_splitContainer.Left,
			                          _splitContainer.Top + addedHeight,
			                          _splitContainer.Width,
			                          _splitContainer.Height - addedHeight);
		}

		string ICreateQualityConditionsView.SupportedVariablesText
		{
			get { return _textBoxSupportedVariables.Text; }
			set { _textBoxSupportedVariables.Text = value; }
		}

		bool ICreateQualityConditionsView.SelectAllParametersRowsEnabled
		{
			get { return _toolStripButtonSelectAll.Enabled; }
			set { _toolStripButtonSelectAll.Enabled = value; }
		}

		bool ICreateQualityConditionsView.ClearParametersRowSelectionEnabled
		{
			get { return _toolStripButtonSelectNone.Enabled; }
			set { _toolStripButtonSelectNone.Enabled = value; }
		}

		bool ICreateQualityConditionsView.ApplyToParametersRowSelectionEnabled
		{
			get { return _toolStripButtonApplyNamingConventionToSelection.Enabled; }
			set { _toolStripButtonApplyNamingConventionToSelection.Enabled = value; }
		}

		bool ICreateQualityConditionsView.RemoveSelectedParametersRowsEnabled
		{
			get { return _toolStripButtonRemove.Enabled; }
			set { _toolStripButtonRemove.Enabled = value; }
		}

		int ICreateQualityConditionsView.TotalParametersRowCount => _dataGridView.Rows.Count;

		IList<DataRow> ICreateQualityConditionsView.SelectedParametersRows
		{
			get
			{
				return _dataGridView.SelectedRows.Cast<DataGridViewRow>()
				                    .Select(row => GetDataRow(row))
				                    .ToList();
			}
		}

		CellSelection ICreateQualityConditionsView.GetParametersCellSelection()
		{
			return new CellSelection(_dataGridView);
		}

		bool ICreateQualityConditionsView.OKEnabled
		{
			get { return _buttonOK.Enabled; }
			set { _buttonOK.Enabled = value; }
		}

		int ICreateQualityConditionsView.SelectedParametersRowCount =>
			_dataGridView.SelectedRows.Count;

		void ICreateQualityConditionsView.SelectAllParametersRows()
		{
			_dataGridView.SelectAll();
		}

		void ICreateQualityConditionsView.ClearParametersRowSelection()
		{
			_dataGridView.ClearSelection();
		}

		bool ICreateQualityConditionsView.Confirm(string message,
		                                          bool defaultIsCancel)
		{
			return Dialog.OkCancel(this, Text, message, defaultIsCancel);
		}

		void ICreateQualityConditionsView.Warn(string message)
		{
			Dialog.Warning(this, Text, message);
		}

		void ICreateQualityConditionsView.BindToQualitySpecifications(
			IList<QualitySpecificationTableRow> selectedQualitySpecifications)
		{
			_qualitySpecificationGridHandler.BindTo(selectedQualitySpecifications);
		}

		IList<QualitySpecificationTableRow> ICreateQualityConditionsView.
			GetSelectedQualitySpecifications()
		{
			return _qualitySpecificationGridHandler.GetSelectedRows();
		}

		bool ICreateQualityConditionsView.RemoveFromQualitySpecificationsEnabled
		{
			get { return _toolStripButtonRemoveFromQualitySpecifications.Enabled; }
			set { _toolStripButtonRemoveFromQualitySpecifications.Enabled = value; }
		}

		bool ICreateQualityConditionsView.HasSelectedQualitySpecifications =>
			_qualitySpecificationGridHandler.HasSelectedRows;

		public bool ExcludeDatasetsUsingThisTest
		{
			get { return _checkBoxExcludeDatasetsUsingThisTest.Checked; }
			set { _checkBoxExcludeDatasetsUsingThisTest.Checked = value; }
		}

		public DataQualityCategory TargetCategory
		{
			get { return (DataQualityCategory) _objectReferenceControlCategory.DataSource; }
			set { _objectReferenceControlCategory.DataSource = value; }
		}

		#endregion

		#region Implementation of IFormStateAware<CreateQualityConditionsFormState>

		void IFormStateAware<CreateQualityConditionsFormState>.RestoreState(
			CreateQualityConditionsFormState formState)
		{
			if (formState.ParametersPanelHeight > 0)
			{
				_splitContainer.SplitterDistance = formState.ParametersPanelHeight;
			}
		}

		void IFormStateAware<CreateQualityConditionsFormState>.GetState(
			CreateQualityConditionsFormState formState)
		{
			formState.ParametersPanelHeight = _splitContainer.SplitterDistance;
		}

		#endregion

		[NotNull]
		private static DataRow GetDataRow([NotNull] DataGridViewRow gridRow)
		{
			return Assert.NotNull(((DataRowView) gridRow.DataBoundItem).Row, "no bound item");
		}

		#region Event handlers

		private void CreateQualityConditionsForm_Load(object sender, EventArgs e)
		{
			// restore state here to avoid splitter distance problem when maximized
			_formStateManager.RestoreState();

			//_splitContainer.Panel1MinSize = 200;
			//_splitContainer.Panel2MinSize = 110;

			if (_observer != null)
			{
				_observer.ViewLoaded();
			}
		}

		private void _buttonOK_Click(object sender, EventArgs e)
		{
			if (_observer != null)
			{
				_observer.OKClicked();
			}
		}

		private void _buttonCancel_Click(object sender, EventArgs e)
		{
			if (_observer != null)
			{
				_observer.CancelClicked();
			}
		}

		private void _toolStripButtonAdd_Click(object sender, EventArgs e)
		{
			if (_observer != null)
			{
				_observer.AddClicked();
			}
		}

		private void _toolStripButtonRemove_Click(object sender, EventArgs e)
		{
			if (_observer != null)
			{
				_observer.RemoveClicked();
			}
		}

		private void _textBoxQualityConditionNames_TextChanged(object sender, EventArgs e)
		{
			if (_observer != null)
			{
				_observer.QualitySpecificationNamingChanged();
			}
		}

		private void _dataGridView_SelectionChanged(object sender, EventArgs e)
		{
			if (_observer != null)
			{
				_observer.QualityConditionParametersSelectionChanged();
			}
		}

		private void _contextMenuStripDataGrid_Opening(object sender, CancelEventArgs e)
		{
			_msg.VerboseDebug(() => "_contextMenuStripDataGrid_Opening");

			var commands = new List<ICommand>();

			_observer.CollectContextCommands(commands);

			if (commands.Count == 0)
			{
				e.Cancel = true;
				return;
			}

			_contextMenuStripDataGrid.SuspendLayout();

			try
			{
				_contextMenuStripDataGrid.Items.Clear();

				foreach (ICommand command in commands)
				{
					_contextMenuStripDataGrid.Items.Add(new CommandMenuItem(command));
				}
			}
			finally
			{
				const bool performLayout = true;
				_contextMenuStripDataGrid.ResumeLayout(performLayout);
			}

			e.Cancel = false;
		}

		private void _dataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
		{
			Dialog.Warning(this, "Error applying changes", e.Exception.Message);

			e.ThrowException = false;
		}

		private void _toolStripButtonSelectAll_Click(object sender, EventArgs e)
		{
			if (_observer != null)
			{
				_observer.SelectAllClicked();
			}
		}

		private void _toolStripButtonSelectNone_Click(object sender, EventArgs e)
		{
			if (_observer != null)
			{
				_observer.SelectNoneClicked();
			}
		}

		private void _dataGridView_CellEnter(object sender, DataGridViewCellEventArgs e)
		{
			if (_dataGridView.SelectedCells.Count > 1)
			{
				return;
			}

			if (_dataGridView.IsCurrentCellInEditMode)
			{
				return;
			}

			const bool selectAll = true;
			_dataGridView.BeginEdit(selectAll);
		}

		private void _dataGridView_CellValidated(object sender, DataGridViewCellEventArgs e)
		{
			DataRow dataRow = GetDataRow(_dataGridView.Rows[e.RowIndex]);
			string columnName = _dataGridView.Columns[e.ColumnIndex].DataPropertyName;

			if (_observer != null)
			{
				_observer.CellValidated(dataRow, columnName);
			}
		}

		private void _dataGridView_CellContentClick(object sender,
		                                            DataGridViewCellEventArgs e)
		{
			if (_observer == null || e.RowIndex < 0 || e.ColumnIndex < 0)
			{
				return;
			}

			DataGridViewColumn column = _dataGridView.Columns[e.ColumnIndex];

			if (! (column is DataGridViewButtonColumn))
			{
				return;
			}

			_observer.CellButtonClicked(GetDataRow(_dataGridView.Rows[e.RowIndex]),
			                            column.Name);
		}

		private void _toolStripButtonApplyNamingConventionToSelection_Click(object sender,
			EventArgs e)
		{
			if (_observer != null)
			{
				_observer.ApplyToSelectionClicked();
			}
		}

		private void _toolStripButtonAssignToQualitySpecifications_Click(object sender,
			EventArgs e)
		{
			if (_observer != null)
			{
				_observer.AssignToQualitySpecificationsClicked();
			}
		}

		private void _toolStripButtonRemoveFromQualitySpecifications_Click(object sender,
			EventArgs e)
		{
			if (_observer != null)
			{
				_observer.RemoveFromQualitySpecificationsClicked();
			}
		}

		private void _dataGridViewQualitySpecifications_SelectionChanged(object sender,
			EventArgs e)
		{
			if (_observer != null)
			{
				_observer.QualitySpecificationSelectionChanged();
			}
		}

		#endregion
	}
}
