using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Controls;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Framework;
using log4net.Core;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Logging;
using ProSuite.Commons.UI.Persistence.WPF;

namespace ProSuite.Commons.AGP.LoggerUI;

/// <summary>
/// Dock pane view model that hosts the (WinForms) <see cref="LogWindowControl"/> inside an
/// ArcGIS Pro dock pane. This is a drop-in alternative to the native WPF
/// <see cref="LogDockPaneViewModelBase"/>; both remain available and a client picks one via
/// the <c>className</c> of its DAML <c>dockPane</c> declaration.
/// </summary>
/// <remarks>
/// Compared to the WPF pane, the hosted control provides throttled refresh (no flicker under
/// heavy logging), a bounded backing buffer, a message history window, per-message details,
/// tooltips and clipboard copy. It is fed from the same <see cref="LoggingEventsAppender"/>
/// that the WPF pane uses, so no additional log4net appender is required.
/// </remarks>
[UsedImplicitly]
public abstract class WinFormsLogDockPaneViewModelBase :
	DockPaneViewModelBase,
	IDisposable,
	IFormStateAware<WinFormsLogDockPaneViewModelBase.LogPaneFormState>
{
	protected abstract string LogDockPaneDamlID { get; }

	protected abstract string ShowLogButtonDamlID { get; }

	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private readonly UserStateManager<LogPaneFormState> _formStateManager;
	private WinFormsLogDockPane _view;

	// Visibility gating. While the pane is hidden (or was never shown) the live handler does
	// nothing: the appender keeps every event in its replay buffer, so no work is marshalled to
	// the UI thread for a grid nobody sees. When the pane becomes visible the grid is rebuilt
	// from that buffer in a single pass (RefreshFromBuffer), which also shows events logged
	// before the pane was ever opened (the ArcMap/Topgis log window behavior).
	//
	// _lastShownSequence is the highest event sequence shown by the last rebuild; live events at
	// or below it were part of that rebuild and are skipped so history and live do not overlap.
	private volatile bool _showRequested;
	private volatile bool _visible;
	private long _lastShownSequence;

	protected WinFormsLogDockPaneViewModelBase()
	{
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

	protected override Control CreateView()
	{
		_view = new WinFormsLogDockPane();

		if (FrameworkApplication.ApplicationTheme == ApplicationTheme.Dark)
		{
			_view.LogWindow.ApplyDarkTheme();
		}

		// The hosted control has no window handle until the pane is first realized. If a show is
		// already pending by then, rebuild the grid from the buffer as soon as the handle exists.
		_view.LogWindow.HandleCreated += (_, _) =>
		{
			if (_showRequested)
			{
				RefreshFromBuffer();
			}
		};

		return _view;
	}

	protected override void OnShowCore(bool isVisible)
	{
		UpdateLogBtn(isVisible);

		_showRequested = isVisible;

		if (isVisible)
		{
			RefreshFromBuffer();
		}
		else
		{
			// Stop feeding the (now hidden) grid; the appender keeps buffering, and the grid is
			// rebuilt from the buffer the next time the pane is shown.
			_visible = false;
		}
	}

	private void Logger_OnNewLogMessage(object sender, LoggingEventArgs args)
	{
		// While hidden (or never shown) do nothing: the event stays in the appender's replay
		// buffer and is shown when the pane is next made visible. This keeps a hidden pane from
		// marshalling work to the UI thread under heavy logging.
		if (! _visible) return;

		LoggingEvent loggingEvent = args?.LoggingEvent;
		if (loggingEvent is null) return;

		// Already shown by the most recent rebuild-from-buffer (the events straddling the moment
		// the pane became visible).
		if (args.Sequence <= Interlocked.Read(ref _lastShownSequence)) return;

		LogWindowControl logWindow = _view?.LogWindow;
		if (logWindow is null || ! logWindow.IsHandleCreated) return;

		logWindow.AddLogEvent(loggingEvent);
	}

	/// <summary>
	/// Rebuilds the grid from the events buffered by <see cref="LoggingEventsAppender"/> and
	/// starts the live feed. Runs on the UI thread (from <see cref="OnShowCore"/> or the control's
	/// HandleCreated event). No-op if already live, so re-entrancy is harmless.
	/// </summary>
	private void RefreshFromBuffer()
	{
		if (_visible) return;

		LogWindowControl logWindow = _view?.LogWindow;
		if (logWindow is null || ! logWindow.IsHandleCreated) return;

		IReadOnlyList<LoggingEvent> history;

		// Snapshot the buffer and flip to "live" under the appender's lock so no event is produced
		// concurrently. Events in the snapshot (sequence <= high-water) are shown by the rebuild
		// and skipped by the live handler; events produced afterwards (sequence > high-water) are
		// fed live. The pane was not visible until this point, so nothing was fed in the meantime.
		lock (LoggingEventsAppender.SyncRoot)
		{
			history = LoggingEventsAppender.SnapshotRecentEvents(out long highWaterSequence);
			_lastShownSequence = highWaterSequence;
			_visible = true;
		}

		logWindow.ReplaceLogEvents(history);
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

	#region Form state persistence

	void IFormStateAware<LogPaneFormState>.SaveState(LogPaneFormState formState)
	{
		if (formState is null)
			throw new ArgumentNullException(nameof(formState));

		LogWindowControl logWindow = _view?.LogWindow;
		if (logWindow != null)
		{
			formState.IsShowDebugEvents = logWindow.ShowDebugMessages;
		}

		formState.IsVerboseDebugEnabled = _msg.IsVerboseDebugEnabled;
	}

	void IFormStateAware<LogPaneFormState>.RestoreState(LogPaneFormState formState)
	{
		if (formState is null)
			throw new ArgumentNullException(nameof(formState));

		LogWindowControl logWindow = _view?.LogWindow;
		if (logWindow != null)
		{
			logWindow.ShowDebugMessages = formState.IsShowDebugEvents;
		}

		_msg.IsVerboseDebugEnabled = formState.IsVerboseDebugEnabled;
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
}
