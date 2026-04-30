using ProSuite.Commons.Logging;
using System;
using System.Windows;
using System.Windows.Input;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI
{
	public class WindowConfigurator<T> where T : ViewModelBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private Window _window;

		public WindowConfigurator([NotNull] Window window)
		{
			_window = window;
			_window.SizeToContent = SizeToContent.WidthAndHeight;
		}

		public void Apply([NotNull] T viewModel)
		{
			try
			{
				_window.Unloaded += OnUnloaded;
				_window.KeyDown += OnKeyDown;
				_window.DataContext = viewModel;

				var escapeBinding =
					new KeyBinding(viewModel.EscapeCommand, Key.Escape, ModifierKeys.None)
					{
						CommandParameter = _window
					};

				var enterBinding =
					new KeyBinding(viewModel.EnterCommand, Key.Enter, ModifierKeys.None)
					{
						CommandParameter = _window
					};

				_window.InputBindings.Add(escapeBinding);
				_window.InputBindings.Add(enterBinding);

				if (_window is IWindowBase<T> view)
				{
					view.ApplyCore(viewModel);
				}
			}
			catch (Exception ex)
			{
				_msg.Debug(ex.Message, ex);
			}
		}

		private void OnKeyDown(object sender, KeyEventArgs e)
		{
			try
			{
				_msg.VerboseDebug(() => $"key {e.Key} down");

				Window windowOwner = _window.Owner;

				Assert.NotNull(windowOwner);

				// Standard routed event “bubbling” only works within a single visual/logical tree.
				// Our window isn’t part of the same tree as the owner.
				PresentationSource source = PresentationSource.FromVisual(windowOwner);
				if (source == null)
				{
					return;
				}

				var args = new KeyEventArgs(e.KeyboardDevice, source,
				                            e.Timestamp, e.Key)
				           {
					           RoutedEvent = e.RoutedEvent
				           };

				windowOwner.RaiseEvent(args);
			}
			catch (Exception ex)
			{
				ViewUtils.ShowError(ex, _msg);
			}
		}

		private void OnUnloaded(object sender, RoutedEventArgs e)
		{
			try
			{
				if (_window is IWindowBase<T> view)
				{
					view.Unload();
				}

				var viewModel = (T) _window.DataContext;
				viewModel?.Dispose();

				_window.Unloaded -= OnUnloaded;
				_window.KeyDown -= OnKeyDown;
				_window.DataContext = null;
				_window = null;
			}
			catch (Exception ex)
			{
				_msg.Debug(ex.Message, ex);
			}
		}
	}
}
