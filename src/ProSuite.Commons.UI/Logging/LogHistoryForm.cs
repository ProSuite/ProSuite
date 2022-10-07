using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Persistence.WinForms;
using ProSuite.Commons.UI.WinForms.Controls;

namespace ProSuite.Commons.UI.Logging
{
	internal partial class LogHistoryForm : Form
	{
		private readonly BasicFormStateManager _formStateManager;

		// private Padding _defaultMessagePadding;

		private IList<LogEventItem> _logEventItems;
		private readonly int[] _textColumnIndices;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="LogHistoryForm"/> class.
		/// </summary>
		public LogHistoryForm()
		{
			InitializeComponent();

			_formStateManager = new BasicFormStateManager(this);
			_formStateManager.RestoreState();

			_textColumnIndices =
				new[]
				{
					logMessageDataGridViewTextBoxColumn.Index,
					logDateTimeDataGridViewTextBoxColumn.Index,
					LogNummer.Index
				};
		}

		#endregion

		public DialogResult ShowDialog([NotNull] IList<LogEventItem> logEventItems,
		                               bool showLogNr, bool showLogDate)
		{
			Assert.ArgumentNotNull(logEventItems, nameof(logEventItems));

			_toolStripStatusLabel.Text = string.Empty;

			LogNummer.Visible = showLogNr;
			logDateTimeDataGridViewTextBoxColumn.Visible = showLogDate;

			// take a copy of the list, otherwise exceptions result if 
			// the grid itself also writes to the log (list changed under enumerator)
			_logEventItems = new List<LogEventItem>(logEventItems);

			return ShowDialog();
			//return UIEnvironment.ShowDialog(this);
		}

		#region Non-public methods

		private void RefreshDetails()
		{
			textBoxDetails.Text = string.Empty;

			LogEventItem eventItem = GetCurrentRowLogEventItem();
			if (eventItem != null)
			{
				textBoxDetails.Text = FormatLogEventItem(eventItem);
			}

			EnableCopyButton();
		}

		private void EnableCopyButton()
		{
			_buttonCopy.Enabled = textBoxDetails.Text.Length > 0;
		}

		private static string FormatLogEventItem(LogEventItem logEventItem)
		{
			var s = new StringBuilder();

			s.AppendFormat("Level:  {0}", logEventItem.LogLevel);
			s.AppendLine();
			s.AppendFormat("Date:   {0}",
			               logEventItem.LogDateTime.ToShortDateString());
			s.AppendLine();
			s.AppendFormat("Time:   {0}", logEventItem.LogDateTime.ToLongTimeString());
			s.AppendLine();
			s.AppendFormat("User:   {0}", EnvironmentUtils.UserDisplayName);
			s.AppendLine();
			s.AppendFormat("Source: {0}", logEventItem.LoggerName);
			s.AppendLine();
			s.AppendLine();
			s.AppendLine("Message:");
			s.AppendLine(logEventItem.LogMessage);

			if (logEventItem.Exception != null)
			{
				s.AppendLine();
				s.AppendLine("Exception details:");

				WriteInnerException(s, logEventItem.Exception.InnerException);

				s.Append(logEventItem.Exception);
			}

			return s.ToString();
		}

		private static void WriteInnerException(StringBuilder s, Exception inner)
		{
			if (inner == null)
			{
				return;
			}

			// recurse, write innermost first
			WriteInnerException(s, inner.InnerException);

			s.Append(inner);
			s.AppendLine("--- End of inner exception stack trace ---");
			s.AppendLine();
		}

		private LogEventItem GetCurrentRowLogEventItem()
		{
			if (_dataGridViewLogEvents.CurrentRow != null)
			{
				var item =
					_dataGridViewLogEvents.CurrentRow.DataBoundItem as LogEventItem;

				return item;
			}

			return null;
		}

		#endregion // Non-public methods

		#region Event handlers

		private void BackupLogEventsView_Shown(object sender, EventArgs e)
		{
			if (_logEventItems == null || _logEventItems.Count <= 0)
			{
				return;
			}

			_bindingSourceLogEvent.Clear();
			foreach (LogEventItem item in _logEventItems)
			{
				_bindingSourceLogEvent.Add(item);
			}

			_dataGridViewLogEvents.CurrentCell =
				_dataGridViewLogEvents
				[logMessageDataGridViewTextBoxColumn.Index,
				 _dataGridViewLogEvents.RowCount - 1];

			new DataGridViewFindController(_dataGridViewLogEvents, _dataGridViewFindToolStrip);
		}

		private void BackupLogEventsView_FormClosed(object sender, FormClosedEventArgs e)
		{
			_logEventItems = null;
			_bindingSourceLogEvent.Clear();

			_formStateManager.SaveState();
		}

		private void buttonCopy_Click(object sender, EventArgs e)
		{
			Clipboard.SetText(textBoxDetails.Text);

			_toolStripStatusLabel.Text = @"Log message details copied";
		}

		private void _dataGridView_CurrentCellChanged(object sender, EventArgs e)
		{
			RefreshDetails();
		}

		private void _dataGridView_SelectionChanged(object sender, EventArgs e)
		{
			RefreshDetails();
		}

		private void _buttonClose_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void _dataGridView_CellFormatting(object sender,
		                                          DataGridViewCellFormattingEventArgs e)
		{
			DataGridViewRow row = _dataGridViewLogEvents.Rows[e.RowIndex];

			var logEventItem = row.DataBoundItem as LogEventItem;

			LogWindowUtils.HandleCellFormattingEvent(
				e, row, logEventItem, logMessageDataGridViewTextBoxColumn.Index,
				_textColumnIndices);
		}

		private static void _dataGridView_DataError(object sender,
		                                            DataGridViewDataErrorEventArgs e)
		{
			_msg.Error(
				string.Format(
					"Error displaying grid content (context: {0}; row: {1} column: {2})",
					e.Context, e.RowIndex, e.ColumnIndex),
				e.Exception);
			e.ThrowException = false;
		}

		#endregion
	}
}
