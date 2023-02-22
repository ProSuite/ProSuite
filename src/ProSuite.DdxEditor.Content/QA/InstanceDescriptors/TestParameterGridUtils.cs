using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core;

namespace ProSuite.DdxEditor.Content.QA.InstanceDescriptors
{
	internal static class TestParameterGridUtils
	{
		[NotNull]
		public static DataTable BindParametersDataGridView([NotNull] DataGridView dataGridView)
		{
			Assert.ArgumentNotNull(dataGridView, nameof(dataGridView));

			var dataTable = new DataTable();

			DataColumn paramDataColumn = dataTable.Columns.Add("Parameter");
			DataColumn typeDataColumn = dataTable.Columns.Add("Type");
			DataColumn descDataColumn = dataTable.Columns.Add("Description");

			var dataView = new DataView(dataTable)
			{
				AllowNew = false,
				AllowEdit = false,
				AllowDelete = false
			};

			DataGridViewColumn paramColumn =
				AddDataGridViewColumn(dataGridView, paramDataColumn, "Parameter");
			paramColumn.DataPropertyName = paramDataColumn.ColumnName;
			paramColumn.MinimumWidth = 40;
			paramColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
			paramColumn.SortMode = DataGridViewColumnSortMode.NotSortable;

			DataGridViewColumn typeColumn =
				AddDataGridViewColumn(dataGridView, typeDataColumn, "Type");
			typeColumn.DataPropertyName = typeDataColumn.ColumnName;
			typeColumn.MinimumWidth = 40;
			typeColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
			typeColumn.SortMode = DataGridViewColumnSortMode.NotSortable;

			DataGridViewColumn descColumn =
				AddDataGridViewColumn(dataGridView, descDataColumn, "Description");
			descColumn.DataPropertyName = descDataColumn.ColumnName;
			descColumn.MinimumWidth = 40;
			descColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
			descColumn.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
			descColumn.SortMode = DataGridViewColumnSortMode.NotSortable;

			dataGridView.AutoGenerateColumns = false;
			dataGridView.DataSource = dataView;
			dataGridView.ReadOnly = true;

			return dataTable;
		}

		public static void PopulateDataTable([NotNull] DataTable dataTable,
											 [CanBeNull] IEnumerable<TestParameter> testParameters)
		{
			Assert.ArgumentNotNull(dataTable, nameof(dataTable));

			dataTable.Clear();

			if (testParameters == null)
			{
				return;
			}

			foreach (TestParameter parameter in testParameters)
			{
				dataTable.Rows.Add(InstanceUtils.GetParameterNameString(parameter),
								   InstanceUtils.GetParameterTypeString(parameter),
								   parameter.Description);
			}
		}

		[NotNull]
		private static DataGridViewColumn AddDataGridViewColumn(
			[NotNull] DataGridView dataGridView,
			[NotNull] DataColumn dataColumn, [NotNull] string heading)
		{
			int columnIndex = dataGridView.Columns.Add(dataColumn.ColumnName, heading);

			return dataGridView.Columns[columnIndex];
		}
	}
}
