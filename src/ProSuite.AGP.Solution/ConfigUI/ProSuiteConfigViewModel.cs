using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.QA.ServiceManager.Types;

namespace ProSuite.AGP.Solution.ConfigUI
{
	public class ProSuiteConfigViewModel : ViewModelBase
	{
		private ObservableCollection<ViewModelBase> _configTabViewModels;

		public ProSuiteConfigViewModel(IEnumerable<ProSuiteQAServerConfiguration> serviceConfigurations)
		{
			_configTabViewModels = new ObservableCollection<ViewModelBase>();

			// TODO algr: number of available Configs should depend from settings
			ConfigTabViewModels.Add(new ProSuiteConfigCommonsViewModel());
			ConfigTabViewModels.Add(new ProSuiteConfigQAViewModel(serviceConfigurations));
		}

		public ObservableCollection<ViewModelBase> ConfigTabViewModels
		{
			get
			{
				return _configTabViewModels;
			}
			set
			{
				_configTabViewModels = value;
				//NotifyPropertyChanged("ConfigTabViewModels");
			}
		}

		private RelayCommand _cmdSaveSettings;
		private RelayCommand _cmdCancelSettings;

		public ICommand CmdSaveSettings
		{
			get
			{
				return _cmdSaveSettings ??
					   (_cmdSaveSettings = new RelayCommand(parameter => CloseSettingsWindow(parameter, true), () => true));
			}
		}

		public ICommand CmdCancelSettings
		{
			get
			{
				return _cmdCancelSettings ??
					   (_cmdCancelSettings = new RelayCommand(parameter => CloseSettingsWindow(parameter, false), () => true));
			}
		}

		private void CloseSettingsWindow(object parameter, bool saveSettings)
		{
			ICloseable window = (ICloseable)parameter;
			if (window != null)
				window.CloseWindow(saveSettings);
		}

	}
}
