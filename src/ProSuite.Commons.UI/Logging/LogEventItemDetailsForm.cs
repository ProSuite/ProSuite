using System;
using System.Text;
using System.Windows.Forms;

namespace ProSuite.Commons.UI.Logging
{
	internal partial class LogEventItemDetailsForm : Form
	{
		private LogEventItem _logEventItem;

		public LogEventItemDetailsForm()
		{
			InitializeComponent();
		}

		public void ShowDialog(LogEventItem logEventItem)
		{
			_logEventItem = logEventItem;

			// TODO
			ShowDialog();
			// UIEnvironment.ShowDialog(this);
		}

		private void LogEventItemDetails_Load(object sender, EventArgs e)
		{
			textBoxDetails.Text = FormatLogEventItem(_logEventItem);
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

				s.Append(logEventItem.Exception.ToString());
			}

			return s.ToString();
		}

		private static void WriteInnerException(StringBuilder s, Exception inner)
		{
			if (inner != null)
			{
				// recurse, write innermost first
				WriteInnerException(s, inner.InnerException);

				s.Append(inner.ToString());
				s.AppendLine("--- End of inner exception stack trace ---");
				s.AppendLine();
			}
		}

		private void buttonCopy_Click(object sender, EventArgs e)
		{
			Clipboard.SetText(textBoxDetails.Text);

			toolStripStatusLabel.Text = "Log message details copied";
		}
	}
}