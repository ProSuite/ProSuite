using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;

namespace ProSuite.DdxEditor.Content.ObjectCategoryAttributeConstraints
{
	public partial class ObjectCategoryAttributeConstraintsControl : UserControl
	{
		private IApplicableAttributes _applicableAttributes;
		private IObjectClass _objectClass;
		private const string _notApplicableValue = "n/a";
		private const string _applicableValue = " ";

		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectCategoryAttributeConstraintsControl"/> class.
		/// </summary>
		public ObjectCategoryAttributeConstraintsControl()
		{
			InitializeComponent();

			_dataGridView.RotationAngle = 90;
			_dataGridView.AutoGenerateColumns = true;
		}

		[Browsable(false)]
		public IApplicableAttributes ApplicableAttributes
		{
			get { return _applicableAttributes; }
			set { _applicableAttributes = value; }
		}

		[Browsable(false)]
		public IObjectClass ObjectClass
		{
			get { return _objectClass; }
			set { _objectClass = value; }
		}

		public void Reload()
		{
			Assert.NotNull(_applicableAttributes, "applicable attributes not set");
			Assert.NotNull(_objectClass, "object class not set");

			_applicableAttributes.ClearCache();

			var dataTable = new DataTable();

			_dataGridView.DataSource = null;
			_bindingSource.DataSource = null;

			int subtypeFieldIndex = DatasetUtils.GetSubtypeFieldIndex(_objectClass);
			if (subtypeFieldIndex < 0)
			{
				return;
			}

			string subtypeFieldName = _objectClass.Fields.Field[subtypeFieldIndex].Name;

			//_dataGridView.Columns.Clear();
			dataTable.Columns.Add(new DataColumn(subtypeFieldName));

			var fieldIndexes = new List<int>();
			foreach (IField field in DatasetUtils.GetFields(_objectClass))
			{
				int fieldIndex = _objectClass.FindField(field.Name);

				if (fieldIndex != subtypeFieldIndex)
				{
					dataTable.Columns.Add(new DataColumn(field.Name, typeof(string)));
					fieldIndexes.Add(fieldIndex);
				}
			}

			foreach (Subtype subtype in DatasetUtils.GetSubtypes(_objectClass))
			{
				var values = new object[fieldIndexes.Count + 1];

				// make sure the subtype is first
				var columnIndex = 0;
				values[columnIndex] = subtype.Name;
				columnIndex++;

				// add the fields
				foreach (int fieldIndex in fieldIndexes)
				{
					values[columnIndex] =
						_applicableAttributes.IsApplicable(_objectClass, fieldIndex, subtype.Code)
							? _applicableValue
							: _notApplicableValue;

					columnIndex++;
				}

				dataTable.Rows.Add(values);
			}

			_dataGridView.DataSource = _bindingSource;
			_bindingSource.DataSource = dataTable;

			_dataGridView.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

			for (var i = 1; i < _dataGridView.Columns.Count; i++)
			{
				DataGridViewColumn column = _dataGridView.Columns[i];
				column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
				column.MinimumWidth = 30;
			}
		}

		private void _dataGridView_CellFormatting(object sender,
		                                          DataGridViewCellFormattingEventArgs e)
		{
			int columnIndex = e.ColumnIndex;
			int rowIndex = e.RowIndex;

			if (rowIndex < 0)
			{
				return;
			}

			if (columnIndex == 0)
			{
				e.CellStyle.BackColor = Color.LightGray;
				return;
			}

			var applicableValue = (string) _dataGridView[columnIndex, rowIndex].Value;

			bool isApplicable = ! Equals(applicableValue, _notApplicableValue);

			e.CellStyle.BackColor = isApplicable
				                        ? Color.LightGreen
				                        : Color.OrangeRed;
			e.FormattingApplied = true;
		}
	}
}
