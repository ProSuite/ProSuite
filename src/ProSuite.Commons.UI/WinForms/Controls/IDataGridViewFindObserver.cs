using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	public interface IDataGridViewFindObserver
	{
		int CurrentFindResultIndex { get; set; }

		int FindResultCount { get; }

		bool MatchCase { get; set; }

		bool FilterRows { get; set; }

		void Find([NotNull] string text);

		void ClearFilter();

		void MoveToNext();

		void MoveToPrevious();

		void HandleFindKeyEvent(KeyEventArgs e);
	}
}
