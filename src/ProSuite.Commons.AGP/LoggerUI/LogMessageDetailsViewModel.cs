using System.Text;
using System.Windows;
using System.Windows.Input;
using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.WPF;
using RelayCommand = ProSuite.Commons.UI.WPF.RelayCommand;

namespace ProSuite.Commons.AGP.LoggerUI
{
	public class LogMessageDetailsViewModel : ViewModelBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public LogMessageDetailsViewModel(LoggingEventItem logItem)
		{
			LogItem = logItem;
		}

		private LoggingEventItem LogItem { get; }

		[UsedImplicitly]
		public string MessageType => LogItem.Type.ToString().ToUpper();

		[UsedImplicitly]
		public string MessageTime => LogItem.Time.ToLongTimeString();

		[UsedImplicitly]
		public string MessageDate => LogItem.Time.ToShortDateString();

		[UsedImplicitly]
		public string CurrentUser => EnvironmentUtils.UserDisplayName;

		[UsedImplicitly]
		public string MessageSource => LogItem.Source;

		[UsedImplicitly]
		public string MessageText => BuildMessageString();

		[UsedImplicitly]
		public ICommand CmdCopyDetails => _cmdCopyDetails ??= new RelayCommand(
			                                  CopyDetails, () => true);
		private RelayCommand _cmdCopyDetails;

		[UsedImplicitly]
		public ICommand CmdCloseDialog => _cmdCloseDialog ??= new RelayCommand<object>(
			                                  CloseWindow, _ => true);
		private ICommand _cmdCloseDialog;

		private void CopyDetails()
		{
			var text = BuildStringForClipboard();
			Clipboard.SetText(text);
			_msg.Debug("Log message copied to clipboard");
		}

		private static void CloseWindow(object parameter)
		{
			if (parameter is ICloseableWindow window)
			{
				window.CloseWindow();
			}
		}

		private string BuildStringForClipboard()
		{
			var sb = new StringBuilder();

			sb.AppendLine($"Level:\t{MessageType}");
			sb.AppendLine($"Date:\t{MessageDate}");
			sb.AppendLine($"Time:\t{MessageTime}");
			sb.AppendLine($"User:\t{CurrentUser}");
			sb.AppendLine($"Source:\t{MessageSource}");

			sb.AppendLine("Message:");
			sb.AppendLine(MessageText);

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
	}
}
