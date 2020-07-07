using System;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.Env
{
	public interface IUIEnvironment
	{
		DialogResult ShowDialog([NotNull] IModalDialog modalDialog,
		                        [CanBeNull] IWin32Window owner,
		                        [CanBeNull] Action<DialogResult> procedure);

		[NotNull]
		CursorState ReleaseCursor();

		void WithReleasedCursor([NotNull] Action procedure);

		void RestoreCursor([NotNull] CursorState cursorState);
	}
}
