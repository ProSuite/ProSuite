using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.Env
{
	public interface IUIEnvironment
	{
		DialogResult ShowDialog([NotNull] IModalDialog modalDialog,
		                        [CanBeNull] IWin32Window owner,
		                        [CanBeNull] Action<DialogResult> procedure);

		Task<DialogResult> ShowDialogAsync([NotNull] IModalDialog modalDialog,
		                                   [CanBeNull] IWin32Window owner,
		                                   [CanBeNull] Action<DialogResult> procedure);

		[NotNull]
		CursorState ReleaseCursor();

		[ItemCanBeNull]
		Task<CursorState> ReleaseCursorAsync();

		void WithReleasedCursor([NotNull] Action procedure);

		Task WithReleasedCursorAsync(Func<Task> func);

		void RestoreCursor([NotNull] CursorState cursorState);

		Task RestoreCursorAsync([NotNull] CursorState cursorState);
	}
}
