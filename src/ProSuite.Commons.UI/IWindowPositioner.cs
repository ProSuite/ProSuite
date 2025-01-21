using System.Windows;

namespace ProSuite.Commons.UI;

public interface IWindowPositioner
{
	public void SetWindow(Window window, Point desiredPosition);
}
