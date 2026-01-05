using System;
using System.Collections.ObjectModel;
using System.Windows.Data;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Framework;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Persistence.WPF;

namespace ProSuite.Commons.AGP.LoggerUI;

[UsedImplicitly]
public abstract class LogDockPaneViewModelBase :
	DockPaneViewModelBase,
	IDisposable,
	IFormStateAware<LogDockPaneViewModelBase.LogPaneFormState>
{
	protected abstract string LogDockPaneDamlID { get; }

	protected abstract string ShowLogButtonDamlID { get; }

	private static readonly IMsg _msg = Msg.ForCurrentClass();

	//private LoggingEventsAppender _appenderDelegate = new LoggingEventsAppender();
	private readonly object _lockLogMessages = new();
	private readonly UserStateManager<LogPaneFormState> _formStateManager;
	private LoggingEventItem _selectedRow;

	protected LogDockPaneViewModelBase() : base(new LogDockPane())
	{
		LogMessageList = new ObservableCollection<LoggingEventItem>();
		BindingOperations.CollectionRegistering += BindingOperations_CollectionRegistering;

		FilterLogs(null);

		LoggingEventsAppender.OnNewLogMessage += Logger_OnNewLogMessage;

		_formStateManager = new UserStateManager<LogPaneFormState>(this, LogDockPaneDamlID);
		_formStateManager.RestoreState();
		ProjectClosedEvent.Subscribe(OnProjectClosed);
	}

	public void Dispose()
	{
		LoggingEventsAppender.OnNewLogMessage -= Logger_OnNewLogMessage;
		ProjectClosedEvent.Unsubscribe(OnProjectClosed);

		var pane = FrameworkApplication.DockPaneManager.Find(LogDockPaneDamlID);

		if (pane != null && pane.IsVisible)
		{
			pane.Hide();
		}
	}

	#region Form state persistance

	void IFormStateAware<LogPaneFormState>.SaveState(LogPaneFormState formState)
	{
		if (formState is null)
			throw new ArgumentNullException(nameof(formState));

		formState.IsShowDebugEvents = DebugLogsAreVisible;
		formState.IsVerboseDebugEnabled = VerboseLogsAreVisible;
	}

	void IFormStateAware<LogPaneFormState>.RestoreState(LogPaneFormState formState)
	{
		if (formState is null)
			throw new ArgumentNullException(nameof(formState));

		DebugLogsAreVisible = formState.IsShowDebugEvents;
		VerboseLogsAreVisible = formState.IsVerboseDebugEnabled;
	}

	private void OnProjectClosed(ProjectEventArgs obj)
	{
		// We don't get any Dock Pane hidden/closed event, so save on project closed:
		try
		{
			_formStateManager.SaveState();
		}
		catch (Exception ex)
		{
			_msg.Warn("Error saving form state", ex);
		}
	}

	[UsedImplicitly]
	public class LogPaneFormState : FormState
	{
		public bool IsShowDebugEvents { get; set; }
		public bool IsVerboseDebugEnabled { get; set; }
	}

	#endregion

	public static Exception LoggingConfigurationException { get; set; }

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
		//var message = (LoggingEventItem) msg;

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
		switch (level)
		{
			case LogType.Verbose:
				return VerboseLogsAreVisible;
			case LogType.Debug:
				return DebugLogsAreVisible;
			case LogType.Info:
			case LogType.Warn:
			case LogType.Error:
			case LogType.Other:
				return true;
		}

		return true;
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
		// Called from Context Menu commands.
		// Nothing to do after the last fixing/refactoring,
		// but keep it ready should we want further changes,
		// such as actually filtering the already logged events.
	}

	private bool _debugLogsAreVisible;

	public bool DebugLogsAreVisible
	{
		get => _debugLogsAreVisible;
		set => SetProperty(ref _debugLogsAreVisible, value);
	}

	public bool VerboseLogsAreVisible
	{
		get => _msg.IsVerboseDebugEnabled;
		set => _msg.IsVerboseDebugEnabled = value;
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
