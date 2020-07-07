using System;
using System.Windows.Forms;

namespace ProSuite.Commons.UI.ScreenBinding.Drivers
{
	public static class ControlDriverFactory
	{
		public static IControlDriver GetDriver(object control)
		{
			if (control is Control)
			{
				return new WinFormsControlDriver((Control) control);
			}

			// only Winforms controls supported at this point
			throw new NotImplementedException();
		}
	}
}
