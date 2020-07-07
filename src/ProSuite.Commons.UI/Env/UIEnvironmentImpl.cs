using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.UI.Env
{
	internal class UIEnvironmentImpl : IUIEnvironment
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

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

		public CursorState ReleaseCursor()
		{
			Point position = Cursor.Position;
			return new CursorState(position.X, position.Y);
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

		public void RestoreCursor(CursorState cursorState)
		{
			// do nothing;
		}

		#endregion

		[CanBeNull]
		private IWin32Window ChooseOwner([CanBeNull] IWin32Window owner)
		{
			return owner ?? _mainWindowProvider.GetMainWindow();
		}
	}
}
