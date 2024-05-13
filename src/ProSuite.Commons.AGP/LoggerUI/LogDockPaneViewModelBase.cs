using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Data;
using ArcGIS.Desktop.Framework;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.LoggerUI
{
	[UsedImplicitly]
	public abstract class LogDockPaneViewModelBase : DockPaneViewModelBase, IDisposable
	{
		protected abstract string LogDockPaneDamlID { get; }

		protected abstract string ShowLogButtonDamlID { get; }

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		//private LoggingEventsAppender _appenderDelegate = new LoggingEventsAppender();
		private readonly List<LogType> _disabledLogTypes = new();
		private readonly object _lockLogMessages = new();

		private LoggingEventItem _selectedRow;

		protected LogDockPaneViewModelBase() : base(new LogDockPane())
		{
			LogMessageList = new ObservableCollection<LoggingEventItem>();
			BindingOperations.CollectionRegistering += BindingOperations_CollectionRegistering;

			FilterLogs(null);

			LoggingEventsAppender.OnNewLogMessage += Logger_OnNewLogMessage;
		}

		public void Dispose()
		{
			LoggingEventsAppender.OnNewLogMessage -= Logger_OnNewLogMessage;

			var pane =
				(LogDockPaneViewModelBase) FrameworkApplication.DockPaneManager.Find(LogDockPaneDamlID);
			if (pane == null)
			{
				return;
			}

			if (pane.IsVisible)
			{
				pane.Hide();
			}
		}

		public static Exception LoggingConfigurationException { get; set; }

		// TODO Use a ring buffer! Now we accumulate forever! (user could clear manually)
		public ObservableCollection<LoggingEventItem> LogMessageList { get; }

		public LoggingEventItem SelectedRow
		{
			get => _selectedRow;
			set
			{
				_selectedRow = value;
				NotifyPropertyChanged();
			}
		}

		private static RelayCommand _openLinkMessage;
		public static RelayCommand OpenLinkMessage =>
			_openLinkMessage ??= new RelayCommand(OpenLogLinkMessage, () => true);

		private static void OpenLogLinkMessage(object msg)
		{
			var message = (LoggingEventItem) msg;

			// TODO inform UI that "Hyperlink" is clicked
			//_msg.Info($"Hyperlink clicked {message.LinkMessage}");
		}

		private void BindingOperations_CollectionRegistering(
			object sender, CollectionRegisteringEventArgs e)
		{
			// to make safe cross thread updates - collection must be registered
			if (e.Collection == LogMessageList)
			{
				BindingOperations.EnableCollectionSynchronization(LogMessageList, _lockLogMessages);
			}
		}

		private void Logger_OnNewLogMessage(object sender, LoggingEventArgs args)
		{
			var logItem = args?.LogItem;
			if (logItem is null) return;

			if (IsLogLevelEnabled(args.LogItem.Type))
			{
				lock (_lockLogMessages)
				{
					// TODO If list has > N entries, remove first K entries (0 < K <= N, say K about 20% of N and N about 1000)
					LogMessageList.Add(args.LogItem);
				}
			}
		}

		private bool IsLogLevelEnabled(LogType level)
		{
			return ! _disabledLogTypes.Contains(level);
		}

		protected override void OnShow(bool isVisible)
		{
			UpdateLogBtn(isVisible);
		}

		private void UpdateLogBtn(bool visible)
		{
			IPlugInWrapper buttonWrapper = FrameworkApplication.GetPlugInWrapper(ShowLogButtonDamlID);
			if (buttonWrapper is null)
			{
				return;
			}

			buttonWrapper.Caption = visible ? "Hide Log" : "Show Log";
			buttonWrapper.Checked = visible;
		}

		internal static void GenerateMockMessages(int number)
		{
			var i = 0;
			while (i++ < number)
			{
				_msg.Error($"Click error <e>link</e> to get something nr={i}");
				_msg.Info("Click info");
				_msg.Debug("<e>Debug</e> to Click debug");
			}
		}

		#region Clear messages

		private RelayCommand _clearLogEntries;
		public RelayCommand ClearLogEntries =>
			_clearLogEntries ??= new RelayCommand(ClearAllLogEntries, CanClearAllLogEntries);

		private bool CanClearAllLogEntries => LogMessageList.Count > 0;

		private Action ClearAllLogEntries => LogMessageList.Clear;

		#endregion

		#region Filter messages

		private RelayCommand _filterLogEntries;
		public RelayCommand FilterLogEntries =>
			_filterLogEntries ??= new RelayCommand(FilterLogs, _ => true);

		private void FilterLogs(object parameter)
		{
			//var type = (string)parameter;

			// TODO filter log list or log4net has built in option?
			//filteredMessagedList = LogMessageList.Where(t => t.Type == LogType.Debug)

			if (DebugLogsAreVisible)
			{
				_disabledLogTypes.Remove(LogType.Debug);
			}
			else
			{
				_disabledLogTypes.Add(LogType.Debug);
			}

			if (VerboseLogsAreVisible)
			{
				_disabledLogTypes.Remove(LogType.Verbose);
			}
			else
			{
				_disabledLogTypes.Add(LogType.Verbose);
			}
		}

		public bool DebugLogsAreVisible { set; get; }

		public bool VerboseLogsAreVisible
		{
			set => _msg.IsVerboseDebugEnabled = value;
			get => _msg.IsVerboseDebugEnabled;
		}

		#endregion

		#region Open message

		private RelayCommand _openMessage;
		public RelayCommand OpenMessage =>
			_openMessage ??= new RelayCommand(OpenLogMessage, () => true);

		private static void OpenLogMessage(object msg)
		{
			if (msg is not LoggingEventItem message) return;

			//_msg.Info($"Open message: {message?.Time} {message?.Message}");
			LogMessageActionEvent.Publish(
				new LogMessageActionEventArgs(message, LogMessageAction.Details));
		}

		#endregion
	}
}
