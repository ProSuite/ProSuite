using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using log4net.Core;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.UI.Logging
{
	public partial class LogWindowControl : UserControl, ILogWindow
	{
		private const int _logLevelDebugImageIndex = 0;
		private const int _logLevelErrorImageIndex = 3;
		private const int _logLevelInfoImageIndex = 1;
		private const int _logLevelWarnImageIndex = 2;
		private const int _maxDisplayedLogMessages = 80;
		private const int _maxLogMessages = 750;
		private const int _fullListRemoveCount = 20;
		private const int _refreshIntervalMs = 800;

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly List<LogEventItem> _logMessages = new List<LogEventItem>();

		private bool _initialized;

		private int _lastClickedRowIndex;
		private long _lastRefreshTickCount;
		private long _logCount;
		private readonly int[] _textColumnIndices;

		private bool _hideDebugMessages;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="LogWindowControl"/> class.
		/// </summary>
		public LogWindowControl()
		{
			InitializeComponent();

			_dataGridView.AutoGenerateColumns = false;
			_hideDebugMessages = _toolStripMenuItemHideDebugMessages.Checked;

			_textColumnIndices = new[]
			                     {
				                     _columnLogMessage.Index,
				                     _columnLogDateTime.Index,
				                     _columnLogNumber.Index
			                     };
		}

		#endregion

		#region ILogWindow Members

		/// <summary>
		/// Adds a new logginEvent to the log datagrid
		/// The method is thread safe...
		/// </summary>
		/// <param name="loggingEvent">New log event to add</param>
		public void AddLogEvent([CanBeNull] LoggingEvent loggingEvent)
		{
			// loggingEvent may be null

			// check logging event for relevance, still on calling thread
			if (loggingEvent == null || IgnoreLoggingEvent(loggingEvent))
			{
				return;
			}

			if (_dataGridView.InvokeRequired)
			{
				// Control.Invoke may hang, use BeginInvoke 
				// (executes as soon as foreground thread is idle again)
				BeginInvoke((AddLogginEventCallback) AddLoggingEventCore,
				            new object[] {loggingEvent});
			}
			else
			{
				AddLoggingEventCore(loggingEvent);
			}
		}

		public void ScrollToEnd()
		{
			if (_dataGridView.InvokeRequired)
			{
				BeginInvoke((ThreadStart) ScrollToEndCore);
			}
			else
			{
				ScrollToEndCore();
			}
		}

		#endregion

		#region Non-public members

		private void ScrollToEndCore()
		{
			int rowCount = _dataGridView.RowCount;

			if (rowCount > 0 && _dataGridView.ColumnCount > 0)
			{
				_dataGridView.CurrentCell = _dataGridView[0, rowCount - 1];
			}
		}

		private void AddLoggingEventCore([NotNull] LoggingEvent loggingEvent)
		{
			Assert.ArgumentNotNull(loggingEvent, nameof(loggingEvent));

			// guards
			if (IsDisposed)
			{
				return;
			}

			if (_logLevelImages.Images.Count == 0)
			{
				// seems to happen during shutdown
				return;
			}

			Image levelImage = GetLevelImage(loggingEvent.Level);
			_logCount++;
			var logItem = new LogEventItem(_logCount, levelImage, loggingEvent);

			AddRow(logItem);
			AddBackupEventItem(logItem);

			// refresh the grid after some time. Handles the fact that tick count 
			// wraps around after a few weeks.
			int tickCount = Environment.TickCount;
			bool forceRefresh = tickCount < _lastRefreshTickCount ||
			                    _lastRefreshTickCount + _refreshIntervalMs < tickCount;

			if (forceRefresh)
			{
				// not sufficient to refresh:
				//_dataGridView.InvalidateRow(_dataGridView.Rows.Count - 1);

				_dataGridView.Refresh();
			}
		}

		private void UpdateLastRefreshTickCount()
		{
			_lastRefreshTickCount = Environment.TickCount;
		}

		private void AddRow([NotNull] LogEventItem logEventItem)
		{
			Assert.ArgumentNotNull(logEventItem, nameof(logEventItem));

			if (_dataGridView.RowCount > _maxDisplayedLogMessages)
			{
				DataGridViewRow firstRow = _dataGridView.Rows[0];
				firstRow.Tag = null;
				_dataGridView.Rows.Remove(firstRow);

				firstRow.Dispose();
			}

			int rowIndex = _dataGridView.Rows.Add(
				logEventItem.LogLevelImage,
				logEventItem.LogNummer,
				logEventItem.LogDateTime,
				logEventItem.LogMessage);

			DataGridViewRow row = _dataGridView.Rows[rowIndex];
			row.Tag = logEventItem;

			DataGridViewCell newMessageCell = row.Cells[_columnLogMessage.Index];

			// set as current cell --> scrolls
			_dataGridView.CurrentCell = newMessageCell;
			_dataGridView.CurrentCell.Selected = false;
		}

		private bool IgnoreLoggingEvent([NotNull] LoggingEvent loggingEvent)
		{
			return _hideDebugMessages && loggingEvent.Level == Level.Debug;
		}

		private void AddBackupEventItem([NotNull] LogEventItem logEventItem)
		{
			Assert.ArgumentNotNull(logEventItem, nameof(logEventItem));

			if (_logMessages.Count >= _maxLogMessages)
			{
				_logMessages.RemoveRange(0, _fullListRemoveCount);
			}

			_logMessages.Add(logEventItem);
		}

		/// <summary>
		/// Gets an image representing the given level
		/// </summary>
		/// <param name="level">Level that should be represented by an image</param>
		/// <returns>Level representing image</returns>
		[NotNull]
		private Image GetLevelImage(Level level)
		{
			if (level == Level.Info)
			{
				return _logLevelImages.Images[_logLevelInfoImageIndex];
			}

			if (level == Level.Warn)
			{
				return _logLevelImages.Images[_logLevelWarnImageIndex];
			}

			if (level == Level.Debug)
			{
				return _logLevelImages.Images[_logLevelDebugImageIndex];
			}

			return _logLevelImages.Images[_logLevelErrorImageIndex];
		}

		[NotNull]
		private static string FormatToolTip([NotNull] LogEventItem logEventItem)
		{
			if (logEventItem.Exception == null)
			{
				return logEventItem.LogMessage;
			}

			return string.Format(@"{0}<br><br>{1}",
			                     logEventItem.LogMessage,
			                     logEventItem.Exception);
		}

		private void ShowLastClickedRowDetails()
		{
			LogEventItem item = GetLastClickedRowLogEventItem();
			if (item == null)
			{
				return;
			}

			ShowItemDetailsDialog(item);
		}

		private static void ShowItemDetailsDialog([NotNull] LogEventItem item)
		{
			Assert.ArgumentNotNull(item, nameof(item));

			using (var form = new LogEventItemDetailsForm())
			{
				form.ShowDialog(item);
			}
		}

		[CanBeNull]
		private LogEventItem GetCurrentRowLogEventItem()
		{
			return _dataGridView.CurrentRow != null
				       ? GetLogEventItem(_dataGridView.CurrentRow)
				       : null;
		}

		[CanBeNull]
		private LogEventItem GetLastClickedRowLogEventItem()
		{
			if (_lastClickedRowIndex < 0 ||
			    _lastClickedRowIndex >= _dataGridView.Rows.Count)
			{
				return null;
			}

			DataGridViewRow row = _dataGridView.Rows[_lastClickedRowIndex];

			return GetLogEventItem(row);
		}

		[CanBeNull]
		private static LogEventItem GetLogEventItem(DataGridViewBand row)
		{
			Assert.ArgumentNotNull(row, nameof(row));

			return row.Tag as LogEventItem;
		}

		#region Event handlers

		private void LoggingWindow_Load(object sender, EventArgs e)
		{
			//_defaultMessagePadding = _columnLogMessage.DefaultCellStyle.Padding;

			_initialized = true;
		}

		private void _contextMenuStripLogGridBox_Opening(object sender, CancelEventArgs e)
		{
			_toolStripMenuItemShowLogEventItemDetails.Visible =
				GetCurrentRowLogEventItem() != null;

			_toolStripMenuItemVerboseDebugLogging.Enabled = _msg.IsDebugEnabled;
			_toolStripMenuItemVerboseDebugLogging.Checked = _msg.IsVerboseDebugEnabled;
		}

		private void _toolStripMenuItemShowLogEventItemDetails_Click(object sender,
		                                                             EventArgs e)
		{
			ShowLastClickedRowDetails();
		}

		private void _toolStripMenuItemShowAll_Click(object sender, EventArgs e)
		{
			using (var form = new LogHistoryForm())
			{
				form.ShowDialog(_logMessages,
				                _columnLogNumber.Visible,
				                _columnLogDateTime.Visible);
			}
		}

		/// <summary>
		/// Show/Hide message number
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void _toolStripMenuItemMsgNumber_CheckedChanged(object sender,
		                                                        EventArgs e)
		{
			_columnLogNumber.Visible = _toolStripMenuItemMsgNumber.Checked;
		}

		/// <summary>
		/// Show/Hide message date
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void _toolStripMenuItemMsgDate_CheckedChanged(object sender,
		                                                      EventArgs e)
		{
			_columnLogDateTime.Visible = _toolStripMenuItemMsgDate.Checked;
		}

		private void _toolStripMenuItemVerboseDebugLogging_CheckedChanged(object sender,
		                                                                  EventArgs e)
		{
			if (_initialized)
			{
				_msg.IsVerboseDebugEnabled = _toolStripMenuItemVerboseDebugLogging.Checked;
			}
		}

		/// <summary>
		/// Clearing the messages
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void _toolStripMenuItemClearAllMessages_Click(object sender,
		                                                      EventArgs e)
		{
			foreach (DataGridViewRow row in _dataGridView.Rows)
			{
				row.Tag = null;
				row.Dispose();
			}

			_dataGridView.Rows.Clear();

			GC.Collect();
		}

		private void _toolStripMenuItemHideDebugMessages_CheckedChanged(object sender,
		                                                                EventArgs e)
		{
			_hideDebugMessages = _toolStripMenuItemHideDebugMessages.Checked;
		}

		private void _dataGridView_CellDoubleClick(object sender,
		                                           DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0)
			{
				// ignore click on header
				return;
			}

			LogEventItem item = GetCurrentRowLogEventItem();

			if (item == null)
			{
				return;
			}

			ShowItemDetailsDialog(item);
		}

		private void _dataGridView_CellMouseDown(object sender,
		                                         DataGridViewCellMouseEventArgs e)
		{
			if (e.RowIndex < 0)
			{
				// ignore click on header
				return;
			}

			_lastClickedRowIndex = e.RowIndex;
		}

		private void _dataGridView_CellToolTipTextNeeded(object sender,
		                                                 DataGridViewCellToolTipTextNeededEventArgs
			                                                 e)
		{
			if (e.RowIndex < 0)
			{
				return;
			}

			DataGridViewRow row = _dataGridView.Rows[e.RowIndex];

			LogEventItem logEventItem = GetLogEventItem(row);

			if (logEventItem != null)
			{
				e.ToolTipText = FormatToolTip(logEventItem);
			}
		}

		private void _dataGridView_CellFormatting(object sender,
		                                          DataGridViewCellFormattingEventArgs e)
		{
			DataGridViewRow row = _dataGridView.Rows[e.RowIndex];

			LogEventItem logEventItem = GetLogEventItem(row);
			if (logEventItem == null)
			{
				return;
			}

			LogWindowUtils.HandleCellFormattingEvent(e, row, logEventItem,
			                                         _columnLogMessage.Index,
			                                         _textColumnIndices);
		}

		private void _dataGridView_DataError(object sender,
		                                     DataGridViewDataErrorEventArgs e)
		{
			_msg.Error(
				string.Format(
					"Error displaying grid content (context: {0}; row: {1} column: {2})",
					e.Context, e.RowIndex, e.ColumnIndex),
				e.Exception);
			e.ThrowException = false;
		}

		private void _dataGridView_Paint(object sender, PaintEventArgs e)
		{
			UpdateLastRefreshTickCount();
		}

		private void _forceRefreshTimer_Tick(object sender, EventArgs e)
		{
			UpdateLastRefreshTickCount();
		}

		#endregion

		#endregion

		#region Nested type: AddLogginEventCallback

		private delegate void AddLogginEventCallback(LoggingEvent logEvent);

		#endregion
	}
}
