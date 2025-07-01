using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI;
using ProSuite.Commons.UI.WPF;

namespace ProSuite.Commons.AGP.MapOverlay
{
	public abstract class MapOverlayViewModelBase : NotifyPropertyChangedBase, IMapOverlayViewModel
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private string _statusMessage;
		private bool _showStatusMessage;
		private MessageType _messageType;

		protected MapOverlayViewModelBase()
		{
			EscapeCommand = new RelayCommand<Window>(OnPressEscape, _ => true);
			OkCommand = new RelayCommand<Window>(OnPressOk, CanPressOk);
			CancelCommand = new RelayCommand<Window>(OnPressCancel, _ => true);
		}

		public ICommand EscapeCommand { get; }
		public ICommand OkCommand { get; }
		public ICommand CancelCommand { get; }

		public string StatusMessage
		{
			get => _statusMessage;
			set => SetProperty(ref _statusMessage, value);
		}

		public bool ShowStatusMessage
		{
			get => _showStatusMessage;
			set => SetProperty(ref _showStatusMessage, value);
		}

		public MessageType MessageType
		{
			get => _messageType;
			set => SetProperty(ref _messageType, value);
		}

		protected virtual bool CanPressOk(Window window)
		{
			return true;
		}

		private async void OnPressCancel(Window window)
		{
			try
			{
				if (await OnPressCancelCoreAsync())
				{
					ResetStatusMessage();
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

		private async void OnPressOk(Window window)
		{
			try
			{
				if (await OnPressOkCoreAsync())
				{
					window?.Close();
				}
			}
			catch (Exception ex)
			{
				ViewUtils.ShowError(ex, _msg);
			}
		}

		protected virtual Task<bool> OnPressOkCoreAsync()
		{
			return Task.FromResult(false);
		}

		protected async void OnPressEscape(Window window)
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

		private void ResetStatusMessage()
		{
			StatusMessage = null;
			ShowStatusMessage = false;
			MessageType = MessageType.None;
		}
	}

	public enum MessageType
	{
		Error,
		Warning,
		Confirmation,
		Information,
		Lock,
		None
	}
}
