using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ProSuite.Commons.Callbacks;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.UI.Dialogs
{
	public static class Dialog
	{
		private static IDialogService _service;

		#region Warnings

		public static void Warning([NotNull] string title,
		                           [NotNull] string message)
		{
			Warning(null, title, message);
		}

		[StringFormatMethod("format")]
		public static void WarningFormat([NotNull] string title,
		                                 [NotNull] string format,
		                                 params object[] args)
		{
			WarningFormat(null, title, format, args);
		}

		[StringFormatMethod("format")]
		public static void WarningFormat([CanBeNull] IWin32Window owner,
		                                 [NotNull] string title,
		                                 [NotNull] string format,
		                                 params object[] args)
		{
			Warning(owner, title, string.Format(format, args));
		}

		public static void Warning([CanBeNull] IWin32Window owner,
		                           [NotNull] string title,
		                           [NotNull] string message)
		{
			Service.ShowWarning(owner, title, message);
		}

		public static void WarningList([CanBeNull] IWin32Window owner,
		                               [NotNull] string title,
		                               [NotNull] string headerMessage,
		                               [NotNull] IList<string> listLines,
		                               [CanBeNull] string footerMessage)
		{
			Service.ShowWarningList(owner, title, headerMessage, listLines, footerMessage);
		}

		#endregion

		#region Errors

		[StringFormatMethod("format")]
		public static void ErrorFormat([NotNull] string title,
		                               [NotNull] string format,
		                               params object[] args)
		{
			ErrorFormat(null, title, format, args);
		}

		[StringFormatMethod("format")]
		public static void ErrorFormat([CanBeNull] IWin32Window owner,
		                               [NotNull] string title,
		                               [NotNull] string format,
		                               params object[] args)
		{
			Error(owner, title, string.Format(format, args));
		}

		public static void Error([NotNull] string title,
		                         [NotNull] string message,
		                         [CanBeNull] Exception e = null,
		                         [CanBeNull] IMsg msg = null)
		{
			Error(null, title, message, e, msg);
		}

		public static void Error([CanBeNull] IWin32Window owner,
		                         [NotNull] string title,
		                         [NotNull] string message,
		                         [CanBeNull] Exception e = null,
		                         [CanBeNull] IMsg msg = null)
		{
			Service.ShowError(owner, title, message, e, msg);
		}

		public static void ErrorList([CanBeNull] IWin32Window owner,
		                             [NotNull] string title,
		                             [NotNull] string headerMessage,
		                             [NotNull] IList<string> listLines,
		                             [CanBeNull] string footerMessage)
		{
			Service.ShowErrorList(owner, title, headerMessage, listLines, footerMessage);
		}

		#endregion

		#region Yes/No questions

		public static bool YesNo([NotNull] string title,
		                         [NotNull] string message)
		{
			return YesNo(null, title, message);
		}

		[StringFormatMethod("format")]
		public static bool YesNoFormat([NotNull] string title,
		                               [NotNull] string format,
		                               params object[] args)
		{
			return YesNoFormat(null, title, format, args);
		}

		[StringFormatMethod("format")]
		public static bool YesNoFormat([CanBeNull] IWin32Window owner,
		                               [NotNull] string title,
		                               [NotNull] string format,
		                               params object[] args)
		{
			return YesNo(owner, title, string.Format(format, args));
		}

		public static bool YesNo([CanBeNull] IWin32Window owner,
		                         [NotNull] string title,
		                         [NotNull] string message)
		{
			return Service.ShowYesNo(owner, title, message);
		}

		public static bool YesNoList([CanBeNull] IWin32Window owner,
		                             [NotNull] string title,
		                             [NotNull] string headerMessage,
		                             [NotNull] IList<string> listLines,
		                             [CanBeNull] string footerMessage)
		{
			return Service.ShowYesNoList(owner, title, headerMessage, listLines,
			                             footerMessage);
		}

		#endregion

		#region Ok/Cancel questions

		public static bool OkCancel([NotNull] string title,
		                            [NotNull] string message)
		{
			return OkCancel(null, title, message);
		}

		[StringFormatMethod("format")]
		public static bool OkCancelFormat([NotNull] string title,
		                                  [NotNull] string format,
		                                  params object[] args)
		{
			return OkCancelFormat(null, title, format, args);
		}

		[StringFormatMethod("format")]
		public static bool OkCancelFormat([CanBeNull] IWin32Window owner,
		                                  [NotNull] string title,
		                                  [NotNull] string format,
		                                  params object[] args)
		{
			return OkCancel(owner, title, string.Format(format, args));
		}

		public static bool OkCancel([CanBeNull] IWin32Window owner,
		                            [NotNull] string title,
		                            [NotNull] string message)
		{
			return Service.ShowOkCancel(owner, title, message);
		}

		public static bool OkCancel([CanBeNull] IWin32Window owner,
		                            [NotNull] string title,
		                            [NotNull] string message,
		                            bool defaultIsCancel)
		{
			return Service.ShowOkCancel(owner, title, message, defaultIsCancel);
		}

		public static bool OkCancelList([CanBeNull] IWin32Window owner,
		                                [NotNull] string title,
		                                [NotNull] string headerMessage,
		                                [NotNull] IList<string> listLines,
		                                [CanBeNull] string footerMessage)
		{
			return Service.ShowOkCancelList(owner, title, headerMessage, listLines,
			                                footerMessage);
		}

		#endregion

		#region Yes/No/Cancel questions

		public static YesNoCancelDialogResult YesNoCancel([NotNull] string title,
		                                                  [NotNull] string message)
		{
			return YesNoCancel(null, title, message);
		}

		[StringFormatMethod("format")]
		public static YesNoCancelDialogResult YesNoCancelFormat([NotNull] string title,
		                                                        [NotNull] string format,
		                                                        params object[] args)
		{
			return YesNoCancelFormat(null, title, format, args);
		}

		[StringFormatMethod("format")]
		public static YesNoCancelDialogResult YesNoCancelFormat(
			[NotNull] string title,
			YesNoCancelDialogResult defaultResult,
			[NotNull] string format,
			params object[] args)
		{
			return YesNoCancelFormat(null, title, defaultResult, format, args);
		}

		[StringFormatMethod("format")]
		public static YesNoCancelDialogResult YesNoCancelFormat(
			[CanBeNull] IWin32Window owner,
			[NotNull] string title,
			[NotNull] string format,
			params object[] args)
		{
			return YesNoCancel(owner, title, string.Format(format, args));
		}

		public static YesNoCancelDialogResult YesNoCancelFormat(
			[CanBeNull] IWin32Window owner,
			[NotNull] string title,
			YesNoCancelDialogResult defaultResult,
			[NotNull] string format,
			params object[] args)
		{
			return YesNoCancel(owner, title,
			                   string.Format(format, args), defaultResult);
		}

		public static YesNoCancelDialogResult YesNoCancel(
			[CanBeNull] IWin32Window owner,
			[NotNull] string title,
			[NotNull] string message,
			YesNoCancelDialogResult defaultResult = YesNoCancelDialogResult.Yes)
		{
			return Service.ShowYesNoCancel(owner, title, message, defaultResult);
		}

		public static YesNoCancelDialogResult YesNoCancelList(
			[CanBeNull] IWin32Window owner,
			[NotNull] string title,
			[NotNull] string headerMessage,
			[NotNull] IList<string> listLines,
			[CanBeNull] string footerMessage)
		{
			return Service.ShowYesNoCancelList(owner, title, headerMessage, listLines,
			                                   footerMessage);
		}

		#endregion

		#region Infos

		public static void Info([NotNull] string title,
		                        [NotNull] string message)
		{
			Info(null, title, message);
		}

		[StringFormatMethod("format")]
		public static void InfoFormat([NotNull] string title,
		                              [NotNull] string format,
		                              params object[] args)
		{
			InfoFormat(null, title, format, args);
		}

		[StringFormatMethod("format")]
		public static void InfoFormat([CanBeNull] IWin32Window owner,
		                              [NotNull] string title,
		                              [NotNull] string format,
		                              params object[] args)
		{
			Info(owner, title, string.Format(format, args));
		}

		public static void Info([CanBeNull] IWin32Window owner,
		                        [NotNull] string title,
		                        [NotNull] string message)
		{
			Service.ShowInfo(owner, title, message);
		}

		public static void InfoList([CanBeNull] IWin32Window owner,
		                            [NotNull] string title,
		                            [NotNull] string headerMessage,
		                            [NotNull] IList<string> listLines,
		                            [CanBeNull] string footerMessage)
		{
			Service.ShowInfoList(owner, title, headerMessage, listLines, footerMessage);
		}

		#endregion

		#region Service control

		/// <summary>
		/// Allows setting a mock implementation of the dialog service, for testing purposes.
		/// </summary>
		/// <remarks>Important: after running the test, the mock must be cleared, either 
		/// by explicitly using <see cref="UseDefaultService"/> or by disposing the return 
		/// value of this method (e.g. in a using block). Otherwise, test isolation 
		/// is not guaranteed.</remarks>
		/// <param name="service">The mock implementing <see cref="IDialogService"/></param>
		/// <returns></returns>
		public static DisposableCallback SetService([NotNull] IDialogService service)
		{
			Assert.ArgumentNotNull(service, nameof(service));

			_service = service;

			return new DisposableCallback(UseDefaultService);
		}

		/// <summary>
		/// Clears the mock implementation, reverting to the standard facade implementation.
		/// </summary>
		public static void UseDefaultService()
		{
			_service = null;
		}

		/// <summary>
		/// Gets the dialog service implementation.
		/// </summary>
		/// <value>The dialog service.</value>
		public static IDialogService Service =>
			_service ?? (_service = new DialogService());

		#endregion
	}
}
