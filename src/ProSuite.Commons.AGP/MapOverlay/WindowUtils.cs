using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI;
using ProSuite.Commons.UI.Input;
using ProSuite.Commons.UI.WPF;

namespace ProSuite.Commons.AGP.MapOverlay
{
	public static class WindowUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		// Callback can be used to adjust window parameters before it is shown
		// (e.g. to hide / show the title bar and close buttons)
		public static void ShowAtMousePosition<T>(IMapOverlayViewModel vm,
		                                          string title,
		                                          WindowPositioner positioner = null,
		                                          Action<IProSuiteWindow> callback = null)
			where T : UserControl, new()
		{
			Show<T>(vm, MouseUtils.GetMouseScreenPosition(), title, positioner, callback);
		}

		// Callback can be used to adjust window parameters before it is shown
		// (e.g. to hide / show the title bar and close buttons)
		public static void Show<T>(IMapOverlayViewModel vm,
		                           Point screenLocation,
		                           string title,
		                           WindowPositioner positioner = null,
		                           Action<IProSuiteWindow> callback = null)
			where T : UserControl, new()
		{
			try
			{
				Dispatcher dispatcher = Application.Current.Dispatcher;

				dispatcher.Invoke(() =>
				{
					ShowCore<T>(vm, screenLocation, positioner, title, callback);
				});
			}
			catch (Exception ex)
			{
				ViewUtils.ShowError(ex, _msg);
			}
		}

		// Callback can be used to adjust window parameters before it is shown
		// (e.g. to hide / show the title bar and close buttons)
		public static void ShowSingle<T>(IMapOverlayViewModel vm,
		                                 Point screenLocation,
		                                 string title,
		                                 WindowPositioner positioner = null,
		                                 Action<IProSuiteWindow> callback = null)
			where T : UserControl, new()
		{
			try
			{
				Dispatcher dispatcher = Application.Current.Dispatcher;

				dispatcher.Invoke(() =>
				{
					CloseCore<T>();

					ShowCore<T>(vm, screenLocation, positioner, title, callback);
				});
			}
			catch (Exception ex)
			{
				ViewUtils.ShowError(ex, _msg);
			}
		}

		public static bool IsWindowOpen<T>() where T : UserControl
		{
			Dispatcher dispatcher = Application.Current.Dispatcher;

			bool result = false;
			dispatcher.Invoke(() =>
			{
				foreach (var window in Application.Current.Windows)
				{
					if (window.GetType().IsAssignableFrom(typeof(ProSuiteWindow)))
					{
						var proWindow = (IProSuiteWindow) window;
						var control = proWindow.GetControlOfType<T>();
						if (control != null)
						{
							result = true;
						}
					}
				}
			});

			return result;
		}

		public static void ExecuteForOpenWindowsOfType<T>(Action<IProSuiteWindow> callback)
			where T : UserControl
		{
			Dispatcher dispatcher = Application.Current.Dispatcher;
			dispatcher.Invoke(() =>
			{
				foreach (var window in Application.Current.Windows)
				{
					if (window.GetType().IsAssignableFrom(typeof(ProSuiteWindow)))
					{
						var proWindow = (IProSuiteWindow) window;
						var control = proWindow.GetControlOfType<T>();
						if (control != null)
						{
							callback(proWindow);
						}
					}
				}
			});
		}

		public static void Close<T>() where T : UserControl
		{
			try
			{
				Dispatcher dispatcher = Application.Current.Dispatcher;

				dispatcher.Invoke(CloseCore<T>);
			}
			catch (Exception ex)
			{
				ViewUtils.ShowError(ex, _msg);
			}
		}

		private static void CloseCore<T>() where T : UserControl
		{
			foreach (var window in Application.Current.Windows)
			{
				if (window.GetType().IsAssignableFrom(typeof(ProSuiteWindow)))
				{
					var proWindow = (IProSuiteWindow) window;
					var control = proWindow.GetControlOfType<T>();
					if (control != null)
					{
						proWindow.Close();
					}
				}
			}
		}

		private static void ShowCore<T>(IMapOverlayViewModel vm,
		                                Point screenLocation,
		                                WindowPositioner positioner, string title,
		                                Action<IProSuiteWindow> callback = null)
			where T : UserControl, new()
		{
			var window = Activator.CreateInstance<ProSuiteWindow>();

			window.InitializeWindow(new T(), vm, title);

			callback?.Invoke(window);

			SetWindowLocation(window, screenLocation);

			window.SetPositioner(positioner, screenLocation);

			window.Show();
		}

		/// <remarks>Must call on main (UI) thread</remarks>
		private static void SetWindowLocation(IProSuiteWindow window, Point location)
		{
			Window ownerWindow = Assert.NotNull(Application.Current.MainWindow);

			window.Owner = ownerWindow;

			if (LocationUnknown(location))
			{
				window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
				return;
			}

			// NOTE: The window's Top/Left coordinates must be set in logical units or DIP (1/96 inch)
			Point dipLocation = DisplayUtils.ToDeviceIndependentPixels(location, ownerWindow);

			window.Left = dipLocation.X;
			window.Top = dipLocation.Y;
		}

		private static bool LocationUnknown(Point location)
		{
			return double.IsNaN(location.X) || double.IsNaN(location.Y);
		}
	}
}
