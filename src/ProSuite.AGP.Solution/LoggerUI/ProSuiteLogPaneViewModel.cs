using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.Commons.Logging;
using Button = ArcGIS.Desktop.Framework.Contracts.Button;

namespace ProSuite.AGP.Solution.LoggerUI
{
    public class ProSuiteLogPaneViewModel : DockPane, IDisposable
    {
        private const string _dockPaneID = "ProSuiteTools_Logger_ProSuiteLogPane";
		private static readonly IMsg _msg = new Msg(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public ObservableCollection<LoggingEventItem> LogMessageList { get; set; }
        public readonly object _lockLogMessages = new object();

		//private LoggingEventsAppender _appenderDelegate = new LoggingEventsAppender();
		private readonly List<LogType> _disabledLogTypes = new List<LogType>();

		#region Clear messages

		private RelayCommand _clearLogEntries;
		public RelayCommand ClearLogEntries =>
			_clearLogEntries ?? (_clearLogEntries = new RelayCommand(
				                     ClearAllLogEntries, CanClearAllLogEntries));

        public bool CanClearAllLogEntries => LogMessageList.Count > 0;

        public Action ClearAllLogEntries => LogMessageList.Clear;

        #endregion

        #region Filter messages

        // filter log list command
        private RelayCommand _filterLogEntries;
		public RelayCommand FilterLogEntries =>
	        _filterLogEntries ?? (_filterLogEntries = new RelayCommand(
		                              FilterLogs, CanFilterLogs));

        public void FilterLogs(object parameter)
        {
			//var type = (string)parameter;

			// TODO filter log list or log4net has built in option?
			//filtereMessagedList = LogMessageList.Where(t => t.Type == LogType.Debug)

			if (DebugLogsAreVisible )
				_disabledLogTypes.Remove(LogType.Debug);
			else
				_disabledLogTypes.Add(LogType.Debug);

			if (VerboseLogsAreVisible) 
				_disabledLogTypes.Remove(LogType.Verbose);
			else
				_disabledLogTypes.Add(LogType.Verbose);

			_msg.Info(DebugLogsAreVisible ? "Debug logs visible" : "Debug logs hidden");
			_msg.Info(VerboseLogsAreVisible ? "Verbose logs visible" : "Verbose logs hidden");
        }

        public bool CanFilterLogs(object parameter) {
            //var type = (string)parameter;
            return true;
        }

        public bool DebugLogsAreVisible { set; get; } = true;

        public bool VerboseLogsAreVisible
        {
	        set => _msg.IsVerboseDebugEnabled = value;
	        get => _msg.IsVerboseDebugEnabled;
        }
        #endregion

        #region Open message

        private RelayCommand _openMessage;

        public ICommand OpenMessage
        {
            get
            {
                return _openMessage ??
                       (_openMessage = new RelayCommand(OpenLogMessage, () => true));
            }
        }

        private void OpenLogMessage(object msg)
        {
            var message = (LoggingEventItem) msg;
			_msg.Info($"Open message: {message?.Time} {message?.Message}");

			LogMessageActionEvent.Publish(new LogMessageActionEventArgs(message, LogMessageAction.Details));
		}

		#endregion

		private static RelayCommand _openLinkMessage;
        public static RelayCommand OpenLinkMessage
        {
            get
            {
                return _openLinkMessage ??
                       (_openLinkMessage = new RelayCommand(OpenLogLinkMessage, () => true));
            }
        }

        public static bool Visible
        {
	        get
	        {
		        var pane = (ProSuiteLogPaneViewModel)FrameworkApplication.DockPaneManager.Find(_dockPaneID);
		        return pane != null && pane.IsVisible;
	        }
        }

        private static void OpenLogLinkMessage(object msg)
        {
            var message = (LoggingEventItem)msg;

			// TODO inform UI than "Hyperlink" is clicked
			//_msg.Info($"Hyperlink clicked {message.LinkMessage}");

		}

        protected ProSuiteLogPaneViewModel() {

            LogMessageList = new ObservableCollection<LoggingEventItem>();
            BindingOperations.CollectionRegistering += BindingOperations_CollectionRegistering;

			LoggingEventsAppender.OnNewLogMessage += this.Logger_OnNewLogMessage;
		}

		private void BindingOperations_CollectionRegistering(object sender, CollectionRegisteringEventArgs e)
        {
            // to make safe cross thread updates - collection must be registered
            if (e.Collection == LogMessageList)
            {
                BindingOperations.EnableCollectionSynchronization(LogMessageList, _lockLogMessages);
            }
        }

        private void Logger_OnNewLogMessage(object sender, LoggingEventArgs e)
        {
			if (e == null) return;

            lock (_lockLogMessages)
            {
				// TODO save messages to buffer(?)

				if(!IsLogLevelDisabled(e.LogItem))
					LogMessageList.Add(e.LogItem);
            }
        }

		private bool IsLogLevelDisabled(LoggingEventItem logItem)
		{
			return _disabledLogTypes.Contains(logItem.Type);
		}

		internal static void ToggleDockWindowVisibility(bool show)
		{
			var pane = (ProSuiteLogPaneViewModel)FrameworkApplication.DockPaneManager.Find(_dockPaneID);
			if (pane == null)
				return;

			if (show)
				pane.Activate();
			else
				if (pane.IsVisible)
					pane.Hide();
		}

		public void Dispose()
		{
			LoggingEventsAppender.OnNewLogMessage -= this.Logger_OnNewLogMessage;

			var pane = (ProSuiteLogPaneViewModel)FrameworkApplication.DockPaneManager.Find(_dockPaneID);
			if (pane == null)
				return;

			if (pane.IsVisible)
				//this.Visible = Visibility.Collapsed;
				pane.Hide();
		}

		internal static void GenerateMockMessages(int number)
		{
			var i = 0;
			while( i++ < number)
			{
				_msg.Error($"Click error <e>link</e> to get something nr={i}");
				_msg.Info("Click info");
				_msg.Debug("<e>Debug</e> to Click debug");
			}			
		}
	}

    /// <summary>
    /// Button implementation to show the DockPane.
    /// </summary>
    internal class ProSuiteLogPane_ShowButton : Button
    {
	    private readonly string _hideLog = "Hide Log";
	    private readonly string _showLog = "Show Log";

		public ProSuiteLogPane_ShowButton()
	    {
		    Caption = ProSuiteLogPaneViewModel.Visible ? _hideLog : _showLog;
		    IsChecked = ProSuiteLogPaneViewModel.Visible;
	    }
        protected override void OnClick()
        {
			IsChecked = !IsChecked;
			Caption = IsChecked ? _hideLog : _showLog;
			ProSuiteLogPaneViewModel.ToggleDockWindowVisibility(IsChecked);
		}


    }
}
