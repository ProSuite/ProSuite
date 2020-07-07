using System.Windows.Forms;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	/// <summary>
	/// Table layout panel control that draws it's content double-buffered
	/// </summary>
	public class DoubleBufferedTableLayoutPanel : TableLayoutPanel
	{
		public DoubleBufferedTableLayoutPanel()
		{
			// base.DoubleBuffered = true;
			SetStyle(ControlStyles.AllPaintingInWmPaint |
			         ControlStyles.OptimizedDoubleBuffer |
			         ControlStyles.UserPaint, true);
		}
	}
}
