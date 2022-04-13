using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.QA;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors
{
	internal static class TestParameterGridUtils
	{
		[NotNull]
		public static DataTable BindParametersDataGridView(
			[NotNull] DataGridView dataGridView)
		{
			Assert.ArgumentNotNull(dataGridView, nameof(dataGridView));

			var dataTable = new DataTable();

			DataColumn paramDataColumn = dataTable.Columns.Add("Parameter");
			DataColumn typeDataColumn = dataTable.Columns.Add("Type");
			DataColumn arrayDataColumn = dataTable.Columns.Add("Array");
			DataColumn descDataColumn = dataTable.Columns.Add("Description");

			var dataView = new DataView(dataTable)
			               {
				               AllowNew = false,
				               AllowEdit = false,
				               AllowDelete = false
			               };

			DataGridViewColumn paramColumn = AddDataGridViewColumn(dataGridView,
				paramDataColumn,
				"Parameter");
			paramColumn.DataPropertyName = paramDataColumn.ColumnName;
			paramColumn.MinimumWidth = 40;
			paramColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
			paramColumn.SortMode = DataGridViewColumnSortMode.NotSortable;

			DataGridViewColumn typeColumn = AddDataGridViewColumn(dataGridView,
				typeDataColumn,
				"Type");
			typeColumn.DataPropertyName = typeDataColumn.ColumnName;
			typeColumn.MinimumWidth = 40;
			typeColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
			typeColumn.SortMode = DataGridViewColumnSortMode.NotSortable;

			DataGridViewColumn arrayColumn = AddDataGridViewColumn(dataGridView,
				arrayDataColumn,
				"List");
			arrayColumn.DataPropertyName = arrayDataColumn.ColumnName;
			arrayColumn.Width = 80;
			arrayColumn.MinimumWidth = 40;
			arrayColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
			arrayColumn.SortMode = DataGridViewColumnSortMode.NotSortable;

			DataGridViewColumn descColumn = AddDataGridViewColumn(dataGridView,
				descDataColumn,
				"Description");
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
		                                     [CanBeNull] IEnumerable<TestParameter>
			                                     testParameters)
		{
			Assert.ArgumentNotNull(dataTable, nameof(dataTable));

			dataTable.Clear();

			if (testParameters == null)
			{
				return;
			}

			foreach (TestParameter parameter in testParameters)
			{
				dataTable.Rows.Add(GetParameterName(parameter),
				                   GetTypeName(parameter),
				                   parameter.ArrayDimension > 0
					                   ? "Yes"
					                   : "No",
				                   parameter.Description);
			}
		}

		[NotNull]
		private static string GetParameterName([NotNull] TestParameter parameter)
		{
			return ! parameter.IsConstructorParameter
				       ? string.Format("[{0}]", parameter.Name)
				       : parameter.Name;
		}

		[NotNull]
		private static string GetTypeName([NotNull] TestParameter parameter)
		{
			TestParameterType type = TestParameterTypeUtils.GetParameterType(parameter.Type);

			return type == TestParameterType.CustomScalar
				       ? parameter.Type.Name
				       : string.Format("{0}", type);
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
