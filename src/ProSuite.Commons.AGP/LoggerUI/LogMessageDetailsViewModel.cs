using System.Text;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.LoggerUI
{
	public class LogMessageDetailsViewModel : ViewModelBase
	{
		public LogMessageDetailsViewModel(LoggingEventItem logItem)
		{
			LogItem = logItem;
		}

		public LoggingEventItem LogItem { get; }

		public string MessageType => LogItem.Type.ToString().ToUpper();

		public string MessageTime => LogItem.Time.ToLongTimeString();

		public string MessageDate => LogItem.Time.ToShortDateString();

		public string CurrentUser => EnvironmentUtils.UserDisplayName;

		public string LogMessage => BuildMessageString();

		public string ClipboardMessage => BuildStringForClipboard();

		private RelayCommand _cmdCopyDetails;
		public ICommand CmdCopyDetails => _cmdCopyDetails ??= new RelayCommand(
			                                  parameter => CloseSettingsWindow(parameter, true),
			                                  () => true);

		private RelayCommand _cmdCancelDetails;
		public ICommand CmdCancelDetails => _cmdCancelDetails ??= new RelayCommand(
			                                    parameter => CloseSettingsWindow(parameter, false),
			                                    () => true);

		private string BuildStringForClipboard()
		{
			var sb = new StringBuilder();

			sb.AppendLine($"Level:\t{MessageType}");
			sb.AppendLine($"Date:\t{MessageDate}");
			sb.AppendLine($"Time:\t{MessageTime}");
			sb.AppendLine($"User:\t{CurrentUser}");
			sb.AppendLine($"Source:\t{LogItem.Source}");
			sb.AppendLine($"Message:\t{LogItem.Message}");

			if (! string.IsNullOrEmpty(LogItem.ExceptionMessage))
			{
				sb.AppendLine("Exception:");
				sb.AppendLine(LogItem.ExceptionMessage);
			}

			return sb.ToString();
		}

		private string BuildMessageString()
		{
			var sb = new StringBuilder();

			sb.AppendLine($"{LogItem.Message}");

			if (! string.IsNullOrEmpty(LogItem.ExceptionMessage))
			{
				sb.AppendLine("Exception:");
				sb.AppendLine(LogItem.ExceptionMessage);
			}

			return sb.ToString();
		}

		private static void CloseSettingsWindow(object parameter, bool saveSettings)
		{
			if (parameter is ICloseable window)
			{
				window.CloseWindow(saveSettings);
			}
		}
	}
}
