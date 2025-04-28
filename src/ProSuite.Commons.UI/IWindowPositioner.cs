using System.Windows;

namespace ProSuite.Commons.UI
{
	public interface IWindowPositioner
	{
		void SetWindow(Window window, Point desiredPosition);
	}
}
