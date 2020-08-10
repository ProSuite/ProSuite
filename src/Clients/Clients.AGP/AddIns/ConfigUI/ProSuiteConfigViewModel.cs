using ArcGIS.Desktop.Framework.Contracts;
using System.Collections.ObjectModel;

namespace Clients.AGP.ProSuiteSolution.ConfigUI
{
	public class ProSuiteConfigViewModel
	{
		private ObservableCollection<ViewModelBase> _configTabViewModels;

		public ProSuiteConfigViewModel()
		{
			_configTabViewModels = new ObservableCollection<ViewModelBase>();

			// TODO algr: number of available Configs should depend from settings
			ConfigTabViewModels.Add(new ProSuiteConfigCommonsViewModel());
			ConfigTabViewModels.Add(new ProSuiteConfigQAViewModel());
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
				// save?
				//RaisePropertyChanged(() => TabViewModels);
			}
		}
	}
}
