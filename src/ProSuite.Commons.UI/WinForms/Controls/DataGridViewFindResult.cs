namespace ProSuite.Commons.UI.WinForms.Controls
{
	public class DataGridViewFindResult
	{
		private readonly int _columnIndex;
		private readonly int _matchLength;
		private readonly int _rowIndex;
		private readonly int _startCharacterIndex;

		public DataGridViewFindResult(int rowIndex,
		                              int columnIndex,
		                              int startCharacterIndex,
		                              int matchLength)
		{
			_rowIndex = rowIndex;
			_columnIndex = columnIndex;
			_startCharacterIndex = startCharacterIndex;
			_matchLength = matchLength;
		}

		public int RowIndex => _rowIndex;

		public int ColumnIndex => _columnIndex;

		public int StartCharacterIndex => _startCharacterIndex;

		public int MatchLength => _matchLength;
	}
}
