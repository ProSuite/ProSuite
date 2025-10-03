using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.Env
{
	public static class UIEnvironment
	{
		#region Fields

		private static readonly MainWindowProvider _mainWindowProvider =
			new MainWindowProvider();

		private static IUIEnvironment _implementation;

		#endregion

		[CanBeNull]
		public static IWin32Window MainWindow
		{
			set { _mainWindowProvider.SetMainWindow(value); }
			get { return _mainWindowProvider.GetMainWindow(); }
		}

		public static void AddDecorator([NotNull] UIEnvironmentDecorator decorator)
		{
			Assert.ArgumentNotNull(decorator, nameof(decorator));

			decorator.SetTarget(Implementation);

			SetDecorator(decorator);
		}

		/// <summary>
		/// Shows a given form as a modal dialog.
		/// </summary>
		/// <param name="form">The form to display as a modal dialog.</param>
		/// <returns></returns>
		public static DialogResult ShowDialog([NotNull] Form form)
		{
			return Implementation.ShowDialog(new ModalFormAdapter(form), null, null);
		}

		/// <summary>
		/// Shows a given form as a modal dialog, as a child to the given owner window.
		/// </summary>
		/// <param name="form">The form to display as a modal dialog.</param>
		/// <param name="procedure">The procedure to execute after the modal dialog is closed (optional). 
		/// This procedure is executed immediately after the dialog closes, before decorators apply their
		/// post-handling of the call.</param>
		/// <returns></returns>
		public static DialogResult ShowDialog([NotNull] Form form,
		                                      Action<DialogResult> procedure)
		{
			return ShowDialog(form, null, procedure);
		}

		/// <summary>
		/// Shows a given form as a modal dialog, as a child to the given owner window.
		/// </summary>
		/// <param name="form">The form to display as a modal dialog.</param>
		/// <param name="owner">The owner window (optional).</param>
		/// <param name="procedure">The procedure to execute after the modal dialog is closed (optional). 
		/// This procedure is executed immediately after the dialog closes, before decorators apply their
		/// post-handling of the call.</param>
		/// <returns></returns>
		/// <remarks>If called in ArcMap and no owner is specified, the form is 
		/// automatically opened as a child to the ArcMap application window.</remarks>
		public static DialogResult ShowDialog(
			[NotNull] Form form,
			[CanBeNull] IWin32Window owner,
			[CanBeNull] Action<DialogResult> procedure = null)
		{
			return Implementation.ShowDialog(new ModalFormAdapter(form), owner, procedure);
		}

		/// <summary>
		/// Shows a given IModalDialog implementor as a modal dialog, as a child to the given owner window. 
		/// Useful when a controller wants to show a view, which it references by its specific view interface. 
		/// In this case the view interface can implement <see cref="IModalDialog"></see>. 
		/// </summary>
		/// <param name="modalDialog">The modal dialog implementor.</param>
		/// <param name="owner">The owner window (optional).</param>
		/// <returns></returns>
		public static DialogResult ShowDialog([NotNull] IModalDialog modalDialog,
		                                      [CanBeNull] IWin32Window owner)
		{
			return Implementation.ShowDialog(modalDialog, owner, null);
		}

		/// <summary>
		/// Shows a given IModalDialog implementor as a modal dialog, as a child to the given owner window.
		/// Useful when a controller wants to show a view, which it references by its specific view interface.
		/// In this case the view interface can implement <see cref="IModalDialog"></see>.
		/// </summary>
		/// <param name="modalDialog">The modal dialog implementor.</param>
		/// <param name="owner">The owner window (optional).</param>
		/// <param name="procedure">The procedure to execute after the modal dialog is closed (optional). 
		/// This procedure is executed immediately after the dialog closes, before decorators apply their
		/// post-handling of the call.</param>
		/// <returns></returns>
		public static DialogResult ShowDialog([NotNull] IModalDialog modalDialog,
		                                      [CanBeNull] IWin32Window owner,
		                                      [CanBeNull] Action<DialogResult> procedure)
		{
			return Implementation.ShowDialog(modalDialog, owner, procedure);
		}

		[NotNull]
		public static CursorState ReleaseCursor()
		{
			return Implementation.ReleaseCursor();
		}

		[NotNull]
		public static async Task<CursorState> ReleaseCursorAsync()
		{
			return await Implementation.ReleaseCursorAsync();
		}

		public static void WithReleasedCursor([NotNull] Action procedure)
		{
			Implementation.WithReleasedCursor(procedure);
		}

		public static async Task WithReleasedCursorAsync([NotNull] Func<Task> function)
		{
			await Implementation.WithReleasedCursorAsync(function);
		}

		public static void RestoreCursor(CursorState cursorState)
		{
			Implementation.RestoreCursor(cursorState);
		}

		public static async Task RestoreCursorAsync(CursorState cursorState)
		{
			await Implementation.RestoreCursorAsync(cursorState);
		}

		#region Non-public methods

		[NotNull]
		private static IUIEnvironment Implementation
		{
			get
			{
				if (_implementation == null)
				{
					_implementation = new UIEnvironmentImpl(_mainWindowProvider);
				}

				return _implementation;
			}
		}

		private static void SetDecorator([NotNull] IUIEnvironment decorator)
		{
			Assert.ArgumentNotNull(decorator, nameof(decorator));
			Assert.NotNull(_implementation, "no existing implementation to decorate");

			_implementation = decorator;
		}

		#endregion

		#region Nested type: MainWindowProvider

		private class MainWindowProvider : IMainWindowProvider
		{
			private IWin32Window _mainWindow;

			#region IMainWindowProvider Members

			public IWin32Window GetMainWindow()
			{
				return _mainWindow;
			}

			#endregion

			public void SetMainWindow([CanBeNull] IWin32Window value)
			{
				_mainWindow = value;
			}
		}

		#endregion

		#region Nested type: ModalFormAdapter

		private class ModalFormAdapter : IModalDialog
		{
			private readonly Form _form;

			/// <summary>
			/// Initializes a new instance of the <see cref="ModalFormAdapter"/> class.
			/// </summary>
			/// <param name="form">The form.</param>
			public ModalFormAdapter([NotNull] Form form)
			{
				Assert.ArgumentNotNull(form, nameof(form));

				_form = form;
			}

			#region IModalDialog Members

			public DialogResult ShowDialog()
			{
				return _form.ShowDialog();
			}

			public DialogResult ShowDialog(IWin32Window owner)
			{
				return _form.ShowDialog(owner);
			}

			#endregion
		}

		#endregion
	}
}
