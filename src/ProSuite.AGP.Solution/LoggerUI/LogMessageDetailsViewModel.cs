using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.AGP.Solution.ConfigUI;
using System.Windows.Input;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Solution.LoggerUI
{
	public class LogMessageDetailsViewModel : ViewModelBase
	{
		public LogMessageDetailsViewModel(LoggingEventItem logItem)
		{
			LogItem = logItem;
		}	

		public LoggingEventItem LogItem { get; set; }

		public string MessageType => LogItem.Type.ToString().ToUpper();

		public string MessageTime => LogItem.Time.ToShortTimeString();

		public string MessageDate => LogItem.Time.ToShortDateString();

		private RelayCommand _cmdCopyDetails;
		public ICommand CmdCopyDetails => _cmdCopyDetails ??
		                                  (_cmdCopyDetails = new RelayCommand(parameter => CloseSettingsWindow(parameter, true), () => true));

		private RelayCommand _cmdCancelDetails;
		public ICommand CmdCancelDetails => _cmdCancelDetails ??
											(_cmdCancelDetails = new RelayCommand(parameter => CloseSettingsWindow(parameter, false), () => true));

		private void CloseSettingsWindow(object parameter, bool saveSettings)
		{
			ICloseable window = (ICloseable)parameter;
			window?.CloseWindow(saveSettings);
		}
	}
}
