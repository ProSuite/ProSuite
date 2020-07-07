using System.Windows.Forms;

namespace ProSuite.Commons.UI.Persistence.WinForms
{
	public interface IFormState
	{
		bool HasSize { get; }

		bool HasLocation { get; }

		int Width { get; set; }

		int Height { get; set; }

		int Left { get; set; }

		int Top { get; set; }

		bool TopMost { get; set; }

		FormWindowState WindowState { get; set; }
	}
}
