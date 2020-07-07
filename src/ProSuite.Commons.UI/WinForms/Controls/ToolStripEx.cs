using System;
using System.Windows.Forms;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	/// <summary>
	/// This class adds on to the functionality provided in System.Windows.Forms.ToolStrip.
	/// </summary>
	/// <remarks>Based on http://blogs.msdn.com/rickbrew/archive/2006/01/09/511003.aspx
	/// </remarks>
	public class ToolStripEx : ToolStrip
	{
		private bool _clickThrough = true;

		/// <summary>
		/// Gets or sets whether the ToolStripEx honors item clicks when its containing form does
		/// not have input focus.
		/// </summary>
		/// <remarks>Default is <c>true</c></remarks>
		public bool ClickThrough
		{
			get { return _clickThrough; }
			set { _clickThrough = value; }
		}

		protected override void WndProc(ref Message m)
		{
			base.WndProc(ref m);

			if (_clickThrough &&
			    m.Msg == MessageConstants.WM_MOUSEACTIVATE &&
			    m.Result == (IntPtr) MessageConstants.MA_ACTIVATEANDEAT)
			{
				m.Result = (IntPtr) MessageConstants.MA_ACTIVATE;
			}
		}
	}
}
