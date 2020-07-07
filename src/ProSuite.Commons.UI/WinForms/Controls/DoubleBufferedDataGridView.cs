using System.Windows.Forms;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	/// <summary>
	/// DataGridView with double-buffered drawing, suitable for display on Planar 
	/// stereo displays.
	/// </summary>
	/// <remarks>This class is a workaround for https://issuetracker02.eggits.net/browse/TOP-3246. 
	/// Use only for datagrids that need to be displayed in stereo (potentially this is all datagrids in Commons 
	/// and ProSuite, plus datagrids in specific projects targeted for stereo environments.
	/// </remarks>
	public class DoubleBufferedDataGridView : FilterableDataGridView
	{
		public DoubleBufferedDataGridView()
		{
			SetStyle(ControlStyles.ContainerControl |
			         ControlStyles.Selectable |
			         ControlStyles.AllPaintingInWmPaint |
			         ControlStyles.OptimizedDoubleBuffer,
			         true);
		}
	}
}
