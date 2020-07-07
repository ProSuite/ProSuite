namespace ProSuite.Commons.UI.WinForms.Controls
{
	public class DataGridViewFindResultCell
	{
		public DataGridViewFindResultCell(int rowIndex, int columnIndex, int findCellIndex)
		{
			RowIndex = rowIndex;
			ColumnIndex = columnIndex;
			FindCellIndex = findCellIndex;
		}

		public int FindCellIndex { get; private set; }

		public int RowIndex { get; private set; }

		public int ColumnIndex { get; private set; }
	}
}
