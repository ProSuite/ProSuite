using System.Windows;
using NUnit.Framework;
using ProSuite.Commons.AGP.Picker;

namespace ProSuite.Commons.AGP.Test.Picker
{
	[TestFixture]
	public class PickerServiceTest
	{
		// The working area of the external monitor in device independent pixels
		private static readonly Rect _monitor = new Rect(1707, 0, 2560, 1392);

		// The center of the stereo map, where the picker appears with a fixed stereo cursor
		private static readonly Point _mapCenter = new Point(2351, 670);

		[Test]
		public void WindowThatFitsBelowDesiredLocationIsNotMoved()
		{
			Point result = PickerService.GetTopLeftOnMonitor(
				_mapCenter, new Size(593, 290), _monitor);

			Assert.AreEqual(_mapCenter, result);
		}

		[Test]
		public void WindowThatExtendsBelowMonitorIsMovedUp()
		{
			var windowSize = new Size(593, 1000);

			Point result = PickerService.GetTopLeftOnMonitor(_mapCenter, windowSize, _monitor);

			Assert.AreEqual(_mapCenter.X, result.X);
			Assert.Less(result.Y, _mapCenter.Y);
			Assert.GreaterOrEqual(result.Y, _monitor.Top);
			Assert.Less(result.Y + windowSize.Height, _monitor.Bottom);
		}

		[Test]
		public void WindowWithMaximumHeightStaysOnMonitor()
		{
			// The maximum height of the picker window is the monitor height minus 40
			var windowSize = new Size(610, _monitor.Height - 40);

			Point result = PickerService.GetTopLeftOnMonitor(_mapCenter, windowSize, _monitor);

			Assert.True(_monitor.Contains(new Rect(result, windowSize)));
		}

		[Test]
		public void WindowThatExtendsBeyondRightEdgeIsMovedLeft()
		{
			var windowSize = new Size(593, 290);
			var closeToRightEdge = new Point(_monitor.Right - 100, 670);

			Point result =
				PickerService.GetTopLeftOnMonitor(closeToRightEdge, windowSize, _monitor);

			Assert.AreEqual(_monitor.Right - windowSize.Width, result.X);
			Assert.AreEqual(closeToRightEdge.Y, result.Y);
		}
	}
}
