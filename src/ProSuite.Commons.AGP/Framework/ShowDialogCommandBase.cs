using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Env;
using ProSuite.Commons.UI.WinForms;

namespace ProSuite.Commons.AGP.Framework;

public abstract class ShowDialogCommandBase : ButtonCommandBase
{
	private bool _clicked;

	protected override async Task<bool> OnClickAsyncCore()
	{
		if (_clicked)
		{
			return false;
		}

		_clicked = true;
		try
		{
			await ProcessClickAsync();
		}
		finally
		{
			_clicked = false;
		}

		return true;
	}

	protected abstract Task ProcessClickAsync();

	protected static Point GetScreenLocation(CursorState cursorState)
	{
		return cursorState != null
			       ? new Point(cursorState.X, cursorState.Y)
			       : Cursor.Position;
	}

	protected static Point GetCorrectedMenuPosition([NotNull] Control control,
	                                                CursorState cursorState)
	{
		Assert.ArgumentNotNull(control, nameof(control));

		Point screenLocation = GetScreenLocation(cursorState);

		return GetCorrectedLocation(control, screenLocation);
	}

	protected static Point GetCorrectedLocation([NotNull] Control control,
	                                            Point clickLocation)
	{
		Assert.ArgumentNotNull(control, nameof(control));

		return WinFormUtils.GetCorrectedPopupLocation(control, clickLocation);
	}

	protected static async Task ShowDialogAsync([NotNull] Action<Point> showDialog)
	{
		Assert.ArgumentNotNull(showDialog, nameof(showDialog));

		CursorState cursorState = await UIEnvironment.ReleaseCursorAsync();
		try
		{
			showDialog(new Point(cursorState.X, cursorState.Y));
		}
		finally
		{
			await UIEnvironment.RestoreCursorAsync(cursorState);
		}
	}
}
