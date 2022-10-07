using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Reflection;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Essentials.System;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Misc;
using ProSuite.Commons.UI.Persistence.WinForms;

namespace ProSuite.Commons.UI.PropertyEditors
{
	internal partial class ListEditorForm : Form, IFormStateAware<ListEditorFormState>
	{
		public event EventHandler DataChanged;

		private const string _itemColumnName = "Item";
		private const string _positionColumnName = "Position";

		// private IList _initValue;
		private readonly string _attributeName;

		private readonly DataView _sourceView;
		private bool _suspend;

		private readonly bool _readOnly;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ListEditorForm"/> class.
		/// </summary>
		/// <param name="initValue">The init value.</param>
		/// <param name="propertyType">Type of the property.</param>
		/// <param name="context">The context.</param>
		/// <param name="readOnly">Cannot add/remove data</param>
		public ListEditorForm(
			[CanBeNull] IList initValue,
			[NotNull] Type propertyType,
			[NotNull] ITypeDescriptorContext context,
			bool readOnly)
		{
			InitializeComponent();

			var formStateManager = new FormStateManager<ListEditorFormState>(this);
			formStateManager.RestoreState();
			FormClosed += delegate { formStateManager.SaveState(); };

			// _initValue = initValue;
			PropertyType = propertyType;
			Context = context;
			_readOnly = readOnly;

			_propertyGrid.ToolbarVisible = false;

			var sourceTable = new DataTable();

			sourceTable.Columns.Add(_positionColumnName, typeof(int));
			sourceTable.Columns.Add(_itemColumnName, PropertyType);

			columnPosition.DataPropertyName = _positionColumnName;
			columnItem.DataPropertyName = _itemColumnName;

			if (initValue != null)
			{
				for (var i = 0; i < initValue.Count; i++)
				{
					sourceTable.Rows.Add(i + 1, initValue[i]);
				}
			}

			_sourceView = new DataView(sourceTable)
			              {
				              AllowDelete = false,
				              AllowNew = false,
				              Sort = _positionColumnName
			              };

			_dataGridView.DataSource = _sourceView;
			_suspend = false;

			string displayName = Assert.NotNull(context.PropertyDescriptor).DisplayName;
			_attributeName = displayName.Trim('[').Trim(']');

			base.Text = $@"{_attributeName} Editor";

			_textBoxItem.Text = _attributeName;
			_textBoxDescription.Text = context.PropertyDescriptor.Description;

			EnableButtons();
		}

		#endregion

		[NotNull]
		public ITypeDescriptorContext Context { get; }

		[NotNull]
		public Type PropertyType { get; }

		[NotNull]
		public IList GetNewValue()
		{
			IList value = CreateList();

			foreach (DataRowView row in _sourceView)
			{
				value.Add(row[_itemColumnName]);
			}

			return value;
		}

		protected void OnChanged()
		{
			DataChanged?.Invoke(this, null);
		}

		[NotNull]
		private IList CreateList()
		{
			Type listType = typeof(List<>);
			var types = new[] {PropertyType};
			Type genericType = listType.MakeGenericType(types);

			object value = Activator.CreateInstance(genericType);

			return (IList) value;
		}

		private void EnableButtons()
		{
			try
			{
				SuspendLayout();

				_buttonAdd.Enabled = ! _readOnly;
				_buttonRemove.Enabled = ! _readOnly;
				_buttonUp.Enabled = ! _readOnly;
				_buttonDown.Enabled = ! _readOnly;

				if (_dataGridView.SelectedRows.Count == 0)
				{
					_buttonRemove.Enabled = false;
					_buttonUp.Enabled = false;
					_buttonDown.Enabled = false;
				}
				else
				{
					if (_dataGridView.Rows[0].Selected)
					{
						_buttonUp.Enabled = false;
					}

					int rowCount = _dataGridView.Rows.Count;
					if (_dataGridView.Rows[rowCount - 1].Selected)
					{
						_buttonDown.Enabled = false;
					}
				}
			}
			finally
			{
				ResumeLayout();
			}
		}

		#region Event handlers

		private void _buttonCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
			// Hide();
		}

		private void _buttonOK_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
			// Hide();
		}

		private void _buttonAdd_Click(object sender, EventArgs e)
		{
			int position = _sourceView.Count + 1;

			var o = (IAttributeInfoProvider) Activator.CreateInstance(PropertyType);
			o.SetAttributeName(_attributeName);

			_sourceView.Table.Rows.Add(position, o);

			OnChanged();

			// select the last row
			// select the first added row (there should always only be one)

			int newRowIndex = _dataGridView.Rows.Count - 1;
			_dataGridView.ClearSelection();
			_dataGridView.Rows[newRowIndex].Selected = true;
		}

		private void _buttonRemove_Click(object sender, EventArgs e)
		{
			try
			{
				_suspend = true;
				foreach (DataGridViewRow vRow in _dataGridView.SelectedRows)
				{
					var row = (DataRowView) vRow.DataBoundItem;
					row.Row.Delete();
				}

				var i = 1;
				foreach (DataRowView vRow in _sourceView)
				{
					vRow.Row[_positionColumnName] = i;
					i++;
				}

				_sourceView.Table.AcceptChanges();
				_dataGridView.ClearSelection();
				_propertyGrid.SelectedObject = null;
			}
			finally
			{
				_suspend = false;

				OnChanged();
			}
		}

		private void _dataGridView_SelectionChanged(object sender, EventArgs e)
		{
			if (_suspend)
			{
				return;
			}

			if (_dataGridView.SelectedRows.Count != 1)
			{
				_propertyGrid.SelectedObject = null;
			}
			else
			{
				DataGridViewRow firstSelectedRow = _dataGridView.SelectedRows[0];

				var row = (DataRowView) firstSelectedRow.DataBoundItem;

				object selected = row[_itemColumnName];

				var context = selected as IContextAware;
				if (context != null)
				{
					context.SetContext(Context.Instance);
				}

				AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
				try
				{
					_propertyGrid.SelectedObject = selected;
				}
				finally
				{
					AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
				}

				_propertyGrid.ExpandAllGridItems();

				_propertyGrid.Focus(); // TODO test
			}

			EnableButtons();
		}

		private static Assembly CurrentDomain_AssemblyResolve(object sender,
		                                                      ResolveEventArgs args)
		{
			return AssemblyResolveUtils.TryLoadAssembly(
				args.Name, Assembly.GetExecutingAssembly().CodeBase, _msg.Debug);
		}

		private void _dataGridView_RowsAdded(object sender,
		                                     DataGridViewRowsAddedEventArgs e)
		{
			EnableButtons();
		}

		private void _dataGridView_RowsRemoved(object sender,
		                                       DataGridViewRowsRemovedEventArgs e)
		{
			EnableButtons();
		}

		private void _buttonUp_Click(object sender, EventArgs e)
		{
			try
			{
				_dataGridView.SuspendLayout();
				int n = _sourceView.Count;
				IList<DataRow> rows = new List<DataRow>(n);
				IList<bool> selected = new List<bool>(n);

				for (var i = 0; i < n; i++)
				{
					rows.Add(_sourceView[i].Row);
					selected.Add(_dataGridView.Rows[i].Selected);
				}

				DataRow unselected = rows[0];

				for (var i = 1; i < n; i++)
				{
					if (selected[i])
					{
						rows[i][_positionColumnName] = i;
						unselected[_positionColumnName] = i + 1;
					}
					else
					{
						unselected = rows[i];
					}
				}

				_sourceView.Table.AcceptChanges();
			}
			finally
			{
				_dataGridView.ResumeLayout();
				OnChanged();
			}

			EnableButtons();
		}

		private void _buttonDown_Click(object sender, EventArgs e)
		{
			try
			{
				_dataGridView.SuspendLayout();

				int n = _sourceView.Count;
				IList<DataRow> rows = new List<DataRow>(n);
				IList<bool> selected = new List<bool>(n);

				for (var i = 0; i < n; i++)
				{
					rows.Add(_sourceView[i].Row);
					selected.Add(_dataGridView.Rows[i].Selected);
				}

				DataRow unselected = rows[n - 1];

				for (int i = n - 2; i >= 0; i--)
				{
					if (selected[i])
					{
						rows[i][_positionColumnName] = i + 2;
						unselected[_positionColumnName] = i + 1;
					}
					else
					{
						unselected = rows[i];
					}
				}

				_sourceView.Table.AcceptChanges();
			}
			finally
			{
				_dataGridView.ResumeLayout();
				OnChanged();
			}

			EnableButtons();
		}

		private void _propertyGrid_SelectedGridItemChanged(
			object sender, SelectedGridItemChangedEventArgs e)
		{
			_dataGridView.Invalidate();
		}

		#endregion

		#region IFormStateAware<ListEditorFormState> Members

		void IFormStateAware<ListEditorFormState>.RestoreState(
			ListEditorFormState formState)
		{
			if (formState.SplitterDistance > 0)
			{
				_splitContainer.SplitterDistance = formState.SplitterDistance;
			}
		}

		void IFormStateAware<ListEditorFormState>.GetState(ListEditorFormState formState)
		{
			formState.SplitterDistance = _splitContainer.SplitterDistance;
		}

		#endregion

		private void _propertyGrid_PropertyValueChanged(object s,
		                                                PropertyValueChangedEventArgs e)
		{
			OnChanged();
		}
	}
}
