using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.Commons.UI.Env;

namespace ProSuite.Commons.UI.Dialogs
{
	public abstract class DialogServiceBase : IDialogService
	{
		//private readonly IMainWindowProvider _mainWindowProvider;

		//protected DialogServiceBase([NotNull] IMainWindowProvider mainWindowProvider)
		//{
		//    Assert.ArgumentNotNull(mainWindowProvider, "mainWindowProvider");

		//    _mainWindowProvider = mainWindowProvider;
		//}

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region IDialogService Members

		/// <summary>
		/// Shows the info dialog.
		/// </summary>
		/// <param name="owner">The owner.</param>
		/// <param name="message">The message.</param>
		/// <param name="title">The title.</param>
		public void ShowInfo(IWin32Window owner, string title, string message)
		{
			Assert.ArgumentNotNullOrEmpty(title, nameof(title));
			Assert.ArgumentNotNullOrEmpty(message, nameof(message));

			_msg.Info(message);

			Show(owner,
			     MsgBase.ReplaceBreakTags(message), title,
			     MessageBoxButtons.OK,
			     MessageBoxIcon.Information,
			     MessageBoxDefaultButton.Button1);
		}

		/// <summary>
		/// Shows the info list box dialog.
		/// </summary>
		/// <param name="owner">The owner.</param>
		/// <param name="title">The title.</param>
		/// <param name="headerMessage">The header message.</param>
		/// <param name="listLines">The list lines.</param>
		/// <param name="footerMessage">The footer message.</param>
		public void ShowInfoList(IWin32Window owner,
		                         string title,
		                         string headerMessage,
		                         IList<string> listLines,
		                         string footerMessage)
		{
			Assert.ArgumentNotNullOrEmpty(title, nameof(title));
			Assert.ArgumentNotNullOrEmpty(headerMessage, nameof(headerMessage));
			Assert.ArgumentNotNull(listLines, nameof(listLines));

			_msg.Info(headerMessage);

			ShowListBox(ChooseOwner(owner), title,
			            MsgBase.ReplaceBreakTags(headerMessage),
			            listLines,
			            footerMessage,
			            MessageBoxButtons.OK,
			            MessageBoxIcon.Information);
		}

		/// <summary>
		/// Shows the warning dialog.
		/// </summary>
		/// <param name="owner">The owner.</param>
		/// <param name="message">The message.</param>
		/// <param name="title">The title.</param>
		public void ShowWarning(IWin32Window owner, string title, string message)
		{
			Assert.ArgumentNotNullOrEmpty(title, nameof(title));
			Assert.ArgumentNotNullOrEmpty(message, nameof(message));

			_msg.Warn(message);

			Show(owner,
			     MsgBase.ReplaceBreakTags(message), title,
			     MessageBoxButtons.OK,
			     MessageBoxIcon.Warning,
			     MessageBoxDefaultButton.Button1);
		}

		/// <summary>
		/// Shows the warning list box dialog.
		/// </summary>
		/// <param name="owner">The owner.</param>
		/// <param name="title">The title.</param>
		/// <param name="headerMessage">The header message.</param>
		/// <param name="listLines">The list lines.</param>
		/// <param name="footerMessage">The footer message.</param>
		public void ShowWarningList(IWin32Window owner,
		                            string title,
		                            string headerMessage,
		                            IList<string> listLines,
		                            string footerMessage)
		{
			Assert.ArgumentNotNullOrEmpty(title, nameof(title));
			Assert.ArgumentNotNullOrEmpty(headerMessage, nameof(headerMessage));
			Assert.ArgumentNotNull(listLines, nameof(listLines));

			_msg.Info(headerMessage);

			ShowListBox(ChooseOwner(owner), title,
			            MsgBase.ReplaceBreakTags(headerMessage),
			            listLines,
			            footerMessage,
			            MessageBoxButtons.OK,
			            MessageBoxIcon.Warning);
		}

		/// <summary>
		/// Shows the error dialog.
		/// </summary>
		/// <param name="owner">The owner.</param>
		/// <param name="message">The message.</param>
		/// <param name="title">The title.</param>
		/// <param name="e">The exception.</param>
		/// <param name="msg">The message sink. Allows to log the error using the logger 
		/// of the calling context.</param>
		public void ShowError(IWin32Window owner, string title, string message,
		                      Exception e, IMsg msg)
		{
			Assert.ArgumentNotNullOrEmpty(title, nameof(title));
			Assert.ArgumentNotNullOrEmpty(message, nameof(message));

			if (msg == null)
			{
				msg = _msg;
			}

			// write to log
			if (e != null)
			{
				msg.Error(message, e);
			}
			else
			{
				msg.Error(message);
			}

			Show(owner,
			     MsgBase.ReplaceBreakTags(message), title,
			     MessageBoxButtons.OK,
			     MessageBoxIcon.Error,
			     MessageBoxDefaultButton.Button1);
		}

		/// <summary>
		/// Shows the error list box dialog.
		/// </summary>
		/// <param name="owner">The owner.</param>
		/// <param name="title">The title.</param>
		/// <param name="headerMessage">The header message.</param>
		/// <param name="listLines">The list lines.</param>
		/// <param name="footerMessage">The footer message.</param>
		public void ShowErrorList(IWin32Window owner,
		                          string title,
		                          string headerMessage,
		                          IList<string> listLines,
		                          string footerMessage)
		{
			Assert.ArgumentNotNullOrEmpty(title, nameof(title));
			Assert.ArgumentNotNullOrEmpty(headerMessage, nameof(headerMessage));
			Assert.ArgumentNotNull(listLines, nameof(listLines));

			_msg.Info(headerMessage);

			ShowListBox(ChooseOwner(owner), title,
			            MsgBase.ReplaceBreakTags(headerMessage),
			            listLines,
			            footerMessage,
			            MessageBoxButtons.OK,
			            MessageBoxIcon.Error);
		}

		/// <summary>
		/// Shows the Yes/No dialog.
		/// </summary>
		/// <param name="owner">The owner.</param>
		/// <param name="message">The message.</param>
		/// <param name="title">The title.</param>
		/// <returns></returns>
		public bool ShowYesNo(IWin32Window owner, string title, string message)
		{
			Assert.ArgumentNotNullOrEmpty(title, nameof(title));
			Assert.ArgumentNotNullOrEmpty(message, nameof(message));

			_msg.Info(message);

			string msg = MsgBase.ReplaceBreakTags(message);

			DialogResult result = Show(owner, msg, title,
			                           MessageBoxButtons.YesNo,
			                           MessageBoxIcon.Question,
			                           MessageBoxDefaultButton.Button1);

			_msg.DebugFormat("YES NO Dialog: {0}, '{1}'. Answer: '{2}'",
			                 msg, title, result);

			return result == DialogResult.Yes;
		}

		public bool ShowYesNoList(IWin32Window owner,
		                          string title,
		                          string headerMessage,
		                          IList<string> listLines,
		                          string footerMessage)
		{
			var result = DialogResult.None;

			UIEnvironment.WithReleasedCursor(
				delegate
				{
					result = ShowListBox(ChooseOwner(owner), title,
					                     headerMessage,
					                     listLines,
					                     footerMessage,
					                     MessageBoxButtons.YesNo,
					                     MessageBoxIcon.Question);
				});

			_msg.DebugFormat("YES NO List Dialog: {0} - {1} - {2}. Answer: '{3}'",
			                 headerMessage,
			                 StringUtils.Concatenate(listLines, ", "),
			                 footerMessage, result);

			return result == DialogResult.Yes;
		}

		/// <summary>
		/// Shows the Ok/Cancel dialog.
		/// </summary>
		/// <param name="owner">The owner.</param>
		/// <param name="title">The title.</param>
		/// <param name="message">The message.</param>
		/// <param name="defaultIsCancel">if set to <c>true</c> the default button is 'Cancel'. Otherwise, 'Ok' is the default.</param>
		/// <returns></returns>
		public bool ShowOkCancel(IWin32Window owner,
		                         string title,
		                         string message,
		                         bool defaultIsCancel = false)
		{
			Assert.ArgumentNotNullOrEmpty(title, nameof(title));
			Assert.ArgumentNotNullOrEmpty(message, nameof(message));

			_msg.Info(message);
			string msg = MsgBase.ReplaceBreakTags(message);

			DialogResult result = Show(owner, msg, title,
			                           MessageBoxButtons.OKCancel,
			                           MessageBoxIcon.Question,
			                           defaultIsCancel
				                           ? MessageBoxDefaultButton.Button2
				                           : MessageBoxDefaultButton.Button1);

			_msg.DebugFormat("OK CANCEL Dialog: {0}, '{1}'. Answer: '{2}'",
			                 msg, title, result);

			return result == DialogResult.OK;
		}

		/// <summary>
		/// Shows the Ok/Cancel list dialog.
		/// </summary>
		/// <param name="owner">The owner.</param>
		/// <param name="title">The title.</param>
		/// <param name="headerMessage">The header message.</param>
		/// <param name="listLines">The list lines.</param>
		/// <param name="footerMessage">The footer message.</param>
		/// <returns></returns>
		public bool ShowOkCancelList(IWin32Window owner,
		                             string title,
		                             string headerMessage,
		                             IList<string> listLines,
		                             string footerMessage)
		{
			Assert.ArgumentNotNullOrEmpty(title, nameof(title));
			Assert.ArgumentNotNullOrEmpty(headerMessage, nameof(headerMessage));
			Assert.ArgumentNotNull(listLines, nameof(listLines));

			_msg.Info(headerMessage);

			string msg = MsgBase.ReplaceBreakTags(headerMessage);

			DialogResult result = ShowListBox(ChooseOwner(owner), title,
			                                  msg,
			                                  listLines,
			                                  footerMessage,
			                                  MessageBoxButtons.OKCancel,
			                                  MessageBoxIcon.Question);

			_msg.DebugFormat("OK CANCEL List Dialog: {0}, '{1}'. Answer: '{2}'",
			                 msg, title, result);

			return result == DialogResult.OK;
		}

		/// <summary>
		/// Shows the Yes/No/Cancel dialog.
		/// </summary>
		/// <param name="owner">The owner.</param>
		/// <param name="message">The message.</param>
		/// <param name="title">The title.</param>
		/// <param name="defaultResult">The default result.</param>
		/// <returns></returns>
		public YesNoCancelDialogResult ShowYesNoCancel(IWin32Window owner, string title,
		                                               string message,
		                                               YesNoCancelDialogResult
			                                               defaultResult)
		{
			Assert.ArgumentNotNullOrEmpty(title, nameof(title));
			Assert.ArgumentNotNullOrEmpty(message, nameof(message));

			_msg.Info(message);

			string msg = MsgBase.ReplaceBreakTags(message);

			MessageBoxDefaultButton defaultButton;
			switch (defaultResult)
			{
				case YesNoCancelDialogResult.Yes:
					defaultButton = MessageBoxDefaultButton.Button1;
					break;
				case YesNoCancelDialogResult.No:
					defaultButton = MessageBoxDefaultButton.Button2;
					break;
				case YesNoCancelDialogResult.Cancel:
					defaultButton = MessageBoxDefaultButton.Button3;
					break;
				default:
					throw new ArgumentException(
						string.Format(
							"Invalid default result ({0})", defaultResult),
						nameof(defaultResult));
			}

			DialogResult result = Show(owner, msg, title,
			                           MessageBoxButtons.YesNoCancel,
			                           MessageBoxIcon.Question,
			                           defaultButton);

			_msg.DebugFormat("YES NO CANCEL Dialog: {0}, '{1}'. Answer: '{2}'",
			                 msg, title, result);

			return GetYesNoCancelAnswer(result);
		}

		public YesNoCancelDialogResult ShowYesNoCancelList(IWin32Window owner,
		                                                   string title,
		                                                   string headerMessage,
		                                                   IList<string> listLines,
		                                                   string footerMessage)
		{
			Assert.ArgumentNotNullOrEmpty(title, nameof(title));
			Assert.ArgumentNotNullOrEmpty(headerMessage, nameof(headerMessage));

			var result = DialogResult.None;

			UIEnvironment.WithReleasedCursor(
				delegate
				{
					result = ShowListBox(ChooseOwner(owner), title,
					                     headerMessage, listLines,
					                     footerMessage,
					                     MessageBoxButtons.YesNoCancel,
					                     MessageBoxIcon.Question);
				});

			_msg.DebugFormat("YES NO CANCEL List Dialog: {0} - {1} - {2}. Answer: '{3}'",
			                 headerMessage,
			                 StringUtils.Concatenate(listLines, ", "),
			                 footerMessage, result);

			return GetYesNoCancelAnswer(result);
		}

		#endregion

		protected abstract DialogResult ShowListBox([CanBeNull] IWin32Window owner,
		                                            [NotNull] string title,
		                                            [NotNull] string headerMessage,
		                                            [NotNull] IList<string> listLines,
		                                            [CanBeNull] string footerMessage,
		                                            MessageBoxButtons buttons,
		                                            MessageBoxIcon icon);

		protected abstract DialogResult ShowMessageBox(
			[CanBeNull] IWin32Window owner,
			[NotNull] string title,
			[NotNull] string message,
			MessageBoxButtons buttons,
			MessageBoxIcon icon,
			MessageBoxDefaultButton defaultButton);

		[CanBeNull]
		private static IWin32Window ChooseOwner([CanBeNull] IWin32Window owner)
		{
			// If not on the UI thread, do not impose the main window from the provider but
			// what the caller suggested. Otherwise, the following exception occurs:
			// The calling thread cannot access this object because a different thread owns it
			// NOTE: Windows can be shown from other STA threads than the Main thread (with limitations)
			if (Environment.CurrentManagedThreadId != 1 || Thread.CurrentThread.IsBackground)
			{
				return owner;
			}

			return owner ?? UIEnvironment.MainWindow;
		}

		private static YesNoCancelDialogResult GetYesNoCancelAnswer(DialogResult result)
		{
			switch (result)
			{
				case DialogResult.Yes:
					return YesNoCancelDialogResult.Yes;
				case DialogResult.No:
					return YesNoCancelDialogResult.No;
				default:
					return YesNoCancelDialogResult.Cancel;
			}
		}

		public DialogResult Show(IWin32Window owner,
		                         string message,
		                         string title,
		                         MessageBoxButtons buttons,
		                         MessageBoxIcon icon,
		                         MessageBoxDefaultButton defaultButton)
		{
			var result = DialogResult.None;
			UIEnvironment.WithReleasedCursor(
				delegate
				{
					result = ShowMessageBox(ChooseOwner(owner), title,
					                        message, buttons,
					                        icon, defaultButton);
				});

			return result;
		}
	}
}
