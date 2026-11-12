using System.Windows;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI
{
	public interface IWindowPositioner
	{
		void SetWindow([NotNull] Window window, Point desiredPosition);

		Point FindSuitablePosition(Point desiredPosition, double windowWidth,
		                           double windowHeight);
	}
}