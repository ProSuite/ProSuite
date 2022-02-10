using System.Windows;
using System.Windows.Media;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.WPF
{
	public static class DisplayUtils
	{
		/// <summary>
		/// Converts the specified device point to a device-independent location (96 DPI)
		/// for the application's main window.
		/// </summary>
		/// <param name="devicePoint">Input point in device pixels.</param>
		/// <returns></returns>
		public static Point ToDeviceIndependentPixels(Point devicePoint)
		{
			Window ownerWindow = Assert.NotNull(Application.Current.MainWindow);

			return ToDeviceIndependentPixels(devicePoint, ownerWindow);
		}

		/// <summary>
		/// Converts the specified device point to a device-independent location (96 DPI)
		/// for the application's main window.
		/// </summary>
		/// <param name="devicePoint">Input point in device pixels.</param>
		/// <param name="visual">The rendering visual</param>
		/// <returns></returns>
		public static Point ToDeviceIndependentPixels(Point devicePoint,
		                                              [NotNull] Visual visual)
		{
			PresentationSource presentationSource =
				Assert.NotNull(PresentationSource.FromVisual(visual));

			double devicePixelToDipRatioY =
				Assert.NotNull(presentationSource.CompositionTarget).TransformToDevice.M11;

			double devicePixelToDipRatioX =
				presentationSource.CompositionTarget.TransformToDevice.M22;

			double left = devicePoint.X / devicePixelToDipRatioX;
			double top = devicePoint.Y / devicePixelToDipRatioY;

			return new Point(left, top);
		}
	}
}
