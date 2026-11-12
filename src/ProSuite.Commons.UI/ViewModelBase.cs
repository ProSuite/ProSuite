using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Input;
using ProSuite.Commons.UI.WPF;

namespace ProSuite.Commons.UI
{
	public abstract class ViewModelBase : NotifyPropertyChangedBase, IDisposable
	{
		private bool _isDisposed;
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected ViewModelBase()
		{
			EnterCommand = new RelayCommand<Window>(OnPressEnter, IsEnterPressed);
			CancelCommand = new RelayCommand<Window>(OnPressCancel, CanPressCancel);
			EscapeCommand = new RelayCommand<Window>(OnPressEscape, IsEscapePressed);
		}

		public ICommand EnterCommand { get; }
		public ICommand CancelCommand { get; }
		public ICommand EscapeCommand { get; }

		protected virtual bool IsEnterPressed(Window window)
		{
			return KeyboardUtils.IsKeyDown(Key.Enter);
		}

		protected virtual async void OnPressEnter(Window window)
		{
			try
			{
				if (await OnPressEnterCoreAsync())
				{
					window?.Close();
				}
			}
			catch (Exception ex)
			{
				ViewUtils.ShowError(ex, _msg);
			}
		}

		protected virtual Task<bool> OnPressEnterCoreAsync()
		{
			return Task.FromResult(true);
		}

		protected virtual bool CanPressCancel(Window window)
		{
			return true;
		}

		protected virtual async void OnPressCancel(Window window)
		{
			try
			{
				if (await OnPressCancelCoreAsync())
				{
					window?.Close();
				}
			}
			catch (Exception ex)
			{
				ViewUtils.ShowError(ex, _msg);
			}
		}

		protected virtual Task<bool> OnPressCancelCoreAsync()
		{
			return Task.FromResult(true);
		}

		protected virtual bool IsEscapePressed(Window window)
		{
			return KeyboardUtils.IsKeyDown(Key.Escape);
		}

		protected virtual async void OnPressEscape(Window window)
		{
			try
			{
				if (await OnPressEscapeCoreAsync())
				{
					window?.Close();
				}
			}
			catch (Exception ex)
			{
				ViewUtils.ShowError(ex, _msg);
			}
		}

		protected virtual Task<bool> OnPressEscapeCoreAsync()
		{
			return Task.FromResult(true);
		}

		public void Dispose()
		{
			if (_isDisposed)
			{
				return;
			}

			_isDisposed = true;

			DisposeCore();
		}

		protected abstract void DisposeCore();
	}
}
