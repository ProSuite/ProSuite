using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.WinForms;

namespace ProSuite.Commons.UI.Env
{
	internal class UIEnvironmentImpl : IUIEnvironment
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly IMainWindowProvider _mainWindowProvider;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="UIEnvironmentImpl"/> class.
		/// </summary>
		/// <param name="mainWindowProvider">The main window provider.</param>
		public UIEnvironmentImpl([NotNull] IMainWindowProvider mainWindowProvider)
		{
			Assert.ArgumentNotNull(mainWindowProvider, nameof(mainWindowProvider));

			_mainWindowProvider = mainWindowProvider;
		}

		#endregion

		#region IUIEnvironment Members

		public DialogResult ShowDialog(IModalDialog dialog, IWin32Window owner,
		                               Action<DialogResult> procedure)
		{
			Assert.ArgumentNotNull(dialog, nameof(dialog));

			_msg.DebugFormat("Showing modal dialog '{0}', owner: '{1}'", dialog,
			                 owner?.ToString() ?? "<null>");

			owner = ChooseOwner(owner);
			DialogResult result = owner == null
				                      ? dialog.ShowDialog()
				                      : dialog.ShowDialog(owner);

			_msg.DebugFormat("Dialog '{0}' closed, result: {1}", dialog, result);

			if (procedure != null)
			{
				_msg.Debug("Executing procedure");
				procedure(result);
			}

			return result;
		}

		public Task<DialogResult> ShowDialogAsync(IModalDialog modalDialog, IWin32Window owner,
		                                          Action<DialogResult> procedure)
		{
			DialogResult result = ShowDialog(modalDialog, owner, procedure);

			return Task.FromResult(result);
		}

		public CursorState ReleaseCursor()
		{
			Point position = Cursor.Position;
			return new CursorState(position.X, position.Y);
		}

		public Task<CursorState> ReleaseCursorAsync()
		{
			CursorState result = ReleaseCursor();

			return Task.FromResult(result);
		}

		public void WithReleasedCursor(Action procedure)
		{
			CursorState cursorState = ReleaseCursor();

			try
			{
				procedure();
			}
			finally
			{
				RestoreCursor(cursorState);
			}
		}

		public async Task WithReleasedCursorAsync(Func<Task> func)
		{
			CursorState cursorState = await ReleaseCursorAsync();

			try
			{
				await func();
			}
			finally
			{
				if (cursorState != null)
				{
					await RestoreCursorAsync(cursorState);
				}
			}
		}

		public void RestoreCursor(CursorState cursorState)
		{
			// do nothing;
		}

		public Task RestoreCursorAsync(CursorState cursorState)
		{
			RestoreCursor(cursorState);

			return Task.CompletedTask;
		}

		#endregion

		[CanBeNull]
		private IWin32Window ChooseOwner([CanBeNull] IWin32Window owner)
		{
			if (owner != null)
			{
				return owner;
			}

			// The user interface thread is not always managed thread 1 (in ArcGIS Pro it is not).
			IWin32Window mainWindow = _mainWindowProvider.GetMainWindow();

			return mainWindow != null && WinFormUtils.IsCreatedOnCurrentThread(mainWindow)
				       ? mainWindow
				       : null;
		}
	}
}
