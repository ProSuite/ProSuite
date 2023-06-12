using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.UI.Dialogs
{
	public interface IDialogService
	{
		/// <summary>
		/// Shows the error dialog.
		/// </summary>
		/// <param name="owner">The owner.</param>
		/// <param name="message">The message.</param>
		/// <param name="title">The title.</param>
		/// <param name="e">The exception.</param>
		/// <param name="msg">The message sink. Allows to log the error using the logger 
		/// of the calling context.</param>
		void ShowError([CanBeNull] IWin32Window owner,
		               [NotNull] string title,
		               [NotNull] string message,
		               [CanBeNull] Exception e,
		               [CanBeNull] IMsg msg);

		/// <summary>
		/// Shows the error list box dialog.
		/// </summary>
		/// <param name="owner">The owner.</param>
		/// <param name="title">The title.</param>
		/// <param name="headerMessage">The header message.</param>
		/// <param name="listLines">The list lines.</param>
		/// <param name="footerMessage">The footer message.</param>
		void ShowErrorList([CanBeNull] IWin32Window owner,
		                   [NotNull] string title,
		                   [NotNull] string headerMessage,
		                   [NotNull] IList<string> listLines,
		                   [CanBeNull] string footerMessage);

		/// <summary>
		/// Shows the warning dialog.
		/// </summary>
		/// <param name="owner">The owner.</param>
		/// <param name="message">The message.</param>
		/// <param name="title">The title.</param>
		void ShowWarning([CanBeNull] IWin32Window owner,
		                 [NotNull] string title,
		                 [NotNull] string message);

		/// <summary>
		/// Shows the warning list box dialog.
		/// </summary>
		/// <param name="owner">The owner.</param>
		/// <param name="title">The title.</param>
		/// <param name="headerMessage">The header message.</param>
		/// <param name="listLines">The list lines.</param>
		/// <param name="footerMessage">The footer message.</param>
		void ShowWarningList([CanBeNull] IWin32Window owner,
		                     [NotNull] string title,
		                     [NotNull] string headerMessage,
		                     [NotNull] IList<string> listLines,
		                     [CanBeNull] string footerMessage);

		/// <summary>
		/// Shows the info dialog.
		/// </summary>
		/// <param name="owner">The owner.</param>
		/// <param name="message">The message.</param>
		/// <param name="title">The title.</param>
		void ShowInfo([CanBeNull] IWin32Window owner,
		              [NotNull] string title,
		              [NotNull] string message);

		/// <summary>
		/// Shows the info list box dialog.
		/// </summary>
		/// <param name="owner">The owner.</param>
		/// <param name="title">The title.</param>
		/// <param name="headerMessage">The header message.</param>
		/// <param name="listLines">The list lines.</param>
		/// <param name="footerMessage">The footer message.</param>
		void ShowInfoList([CanBeNull] IWin32Window owner,
		                  [NotNull] string title,
		                  [NotNull] string headerMessage,
		                  [NotNull] IList<string> listLines,
		                  [CanBeNull] string footerMessage);

		/// <summary>
		/// Shows the yes no dialog.
		/// </summary>
		/// <param name="owner">The owner.</param>
		/// <param name="message">The message.</param>
		/// <param name="title">The title.</param>
		/// <returns></returns>
		bool ShowYesNo([CanBeNull] IWin32Window owner,
		               [NotNull] string title,
		               [NotNull] string message);

		bool ShowYesNoList([CanBeNull] IWin32Window owner,
		                   [NotNull] string title,
		                   [NotNull] string headerMessage,
		                   [NotNull] IList<string> listLines,
		                   [CanBeNull] string footerMessage);

		/// <summary>
		/// Shows the Ok/Cancel dialog.
		/// </summary>
		/// <param name="owner">The owner.</param>
		/// <param name="title">The title.</param>
		/// <param name="message">The message.</param>
		/// <param name="defaultIsCancel">if set to <c>true</c> the default button is 'Cancel'. Otherwise, 'Ok' is the default.</param>
		/// <returns></returns>
		bool ShowOkCancel(IWin32Window owner,
		                  [NotNull] string title,
		                  [NotNull] string message,
		                  bool defaultIsCancel = false);

		/// <summary>
		/// Shows the Yes/No/Cancel dialog.
		/// </summary>
		/// <param name="owner">The owner.</param>
		/// <param name="message">The message.</param>
		/// <param name="title">The title.</param>
		/// <param name="defaultResult">The default result.</param>
		/// <returns></returns>
		YesNoCancelDialogResult ShowYesNoCancel([CanBeNull] IWin32Window owner,
		                                        [NotNull] string title,
		                                        [NotNull] string message,
		                                        YesNoCancelDialogResult defaultResult);

		YesNoCancelDialogResult ShowYesNoCancelList(IWin32Window owner,
		                                            [NotNull] string title,
		                                            [NotNull] string headerMessage,
		                                            [NotNull] IList<string> listLines,
		                                            [CanBeNull] string footerMessage);

		/// <summary>
		/// Shows the Ok/Cancel list dialog.
		/// </summary>
		/// <param name="owner">The owner.</param>
		/// <param name="title">The title.</param>
		/// <param name="headerMessage">The header message.</param>
		/// <param name="listLines">The list lines.</param>
		/// <param name="footerMessage">The footer message.</param>
		/// <returns></returns>
		bool ShowOkCancelList([CanBeNull] IWin32Window owner,
		                      [NotNull] string title,
		                      [NotNull] string headerMessage,
		                      [NotNull] IList<string> listLines,
		                      [CanBeNull] string footerMessage);

		/// <summary>
		/// Shows a fully configurable message box.
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="message"></param>
		/// <param name="title"></param>
		/// <param name="buttons"></param>
		/// <param name="icon"></param>
		/// <param name="defaultButton"></param>
		/// <returns></returns>
		DialogResult Show([CanBeNull] IWin32Window owner,
		                  [NotNull] string message,
		                  [NotNull] string title,
		                  MessageBoxButtons buttons,
		                  MessageBoxIcon icon,
		                  MessageBoxDefaultButton defaultButton);
	}
}
