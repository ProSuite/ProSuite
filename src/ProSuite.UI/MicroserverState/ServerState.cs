using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.WPF;
using ProSuite.Microservices.Client;

namespace ProSuite.UI.MicroserverState
{
	public class ServerState : INotifyPropertyChanged
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private const int _initialCheckIntervalSeconds = 5;
		private readonly DispatcherTimer _timer = new DispatcherTimer();

		private SolidColorBrush _serverStateBackColor;

		public event PropertyChangedEventHandler PropertyChanged;

		private readonly MicroserviceClientBase _serviceClient;
		private string _fullAddress;

		private bool _isConnected;
		private bool _isServing;
		private int _pingLatency;
		private string _text;
		private string _serviceNameLabel;
		private bool _canRestart;
		private readonly bool _isLocalHost;
		private RelayCommand<ServerState> _showReportCommand;
		private int _workerServiceCount;

		public ServerState([NotNull] MicroserviceClientBase serviceClient)
		{
			_serviceClient = serviceClient;
			_isLocalHost =
				serviceClient.HostName.Equals("localhost",
				                              StringComparison.CurrentCultureIgnoreCase);

			ServiceNameLabel = $"{serviceClient.ServiceDisplayName}: ";

			string protocol = _serviceClient.UseTls ? "https" : "http";
			FullAddress = $"{protocol}://{_serviceClient.HostName}:{_serviceClient.Port}";

			_timer.Interval = TimeSpan.FromSeconds(_initialCheckIntervalSeconds);
			_timer.Tick += CheckHealth;
		}

		public async Task<bool> Evaluate()
		{
			Stopwatch watch = Stopwatch.StartNew();

			IsServing = await _serviceClient.CanAcceptCallsAsync();

			WorkerServiceCount = IsServing ? await _serviceClient.GetWorkerServiceCountAsync() : 0;

			watch.Stop();

			// The address can always change due to some fail-over:
			string protocol = _serviceClient.UseTls ? "https" : "http";
			FullAddress = $"{protocol}://{_serviceClient.HostName}:{_serviceClient.Port}";

			ServerStateColor = IsServing
				                   ? new SolidColorBrush(Colors.ForestGreen)
				                   : new SolidColorBrush(Colors.Red);

			CanRestart = ! IsServing && (_isLocalHost || _serviceClient.CanFailOver);

			Text = IsServing ? $"Healthy ({watch.ElapsedMilliseconds}ms)" : "Unavailable";

			return IsServing;
		}

		public void StartAutoEvaluation()
		{
			_timer.Start();
		}

		public void StopAutoEvaluation()
		{
			_timer.Stop();
		}

		private async void CheckHealth(object sender, EventArgs e)
		{
			await Evaluate();
		}

		[NotifyPropertyChangedInvocator]
		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public string FullAddress
		{
			get => _fullAddress;
			set
			{
				_fullAddress = value;
				OnPropertyChanged();
			}
		}

		public int WorkerServiceCount
		{
			get => _workerServiceCount;
			set
			{
				_workerServiceCount = value;
				OnPropertyChanged();
			}
		}

		public ICommand RestartCommand
		{
			get
			{
				if (_showReportCommand == null)
				{
					_showReportCommand =
						new RelayCommand<ServerState>(
							vm => Restart(),
							vm => CanRestart);
				}

				return _showReportCommand;
			}
		}

		private void Restart()
		{
			try
			{
				bool started = _serviceClient.TryRestart();

				_showReportCommand?.RaiseCanExecuteChanged(started);
			}
			catch (Exception e)
			{
				_msg.Debug("Error starting microservice process", e);
			}
		}

		public SolidColorBrush ServerStateColor
		{
			get => _serverStateBackColor;
			set
			{
				_serverStateBackColor = value;
				OnPropertyChanged();
			}
		}

		public bool IsConnected
		{
			get => _isConnected;
			set => _isConnected = value;
		}

		public bool IsServing
		{
			get => _isServing;
			set
			{
				_isServing = value;
				OnPropertyChanged();
			}
		}

		public string ServiceNameLabel
		{
			get => _serviceNameLabel;
			set
			{
				_serviceNameLabel = value;
				OnPropertyChanged();
			}
		}

		public int PingLatency
		{
			get => _pingLatency;
			set => _pingLatency = value;
		}

		public string Text
		{
			get => _text;
			set
			{
				_text = value;
				OnPropertyChanged();
			}
		}

		public bool CanRestart
		{
			get => _canRestart;
			set
			{
				try
				{
					_canRestart = value;
					OnPropertyChanged();

					//if (_canRestart != value)
					//{
					//	_canRestart = value;
					//	OnPropertyChanged();
					//	_showReportCommand?.RaiseCanExecuteChanged(true);
					//}
				}
				catch (Exception e)
				{
					_msg.Debug("Error gettign CanRestart state", e);
				}
			}
		}

		public Visibility RestartButtonVisibility =>
			_isLocalHost ? Visibility.Visible : Visibility.Hidden;
	}
}
