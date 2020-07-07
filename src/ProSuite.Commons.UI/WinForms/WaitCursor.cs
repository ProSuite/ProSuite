using System;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.WinForms
{
	/// <summary>
	/// Set wait cursor (hourglass), either for a single Control
	/// or for the entire application. Usage pattern:
	/// <code>using (new WaitCursor()) { lengthy activity }</code>
	/// Or:
	/// <code>using (new WaitCursor(control)) { hard work }</code>
	/// </summary>
	public class WaitCursor : IDisposable
	{
		private readonly Control _control;
		private readonly Cursor _oldCursor;

		public WaitCursor()
		{
			_control = null;
			_oldCursor = Cursor.Current;

			// Cursor.Current is the app level cursor
			Cursor.Current = Cursors.WaitCursor;
		}

		public WaitCursor([NotNull] Control control)
		{
			Assert.ArgumentNotNull(control, nameof(control));

			_control = control;
			_oldCursor = control.Cursor;

			_control.Cursor = Cursors.WaitCursor;
		}

		public void Dispose()
		{
			if (_control != null)
			{
				_control.Cursor = _oldCursor;
			}
			else
			{
				Cursor.Current = _oldCursor;
			}
		}
	}
}
