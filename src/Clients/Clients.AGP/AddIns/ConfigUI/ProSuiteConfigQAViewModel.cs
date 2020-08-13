using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.Commons.QA.ServiceManager.Interfaces;
using ProSuite.Commons.QA.ServiceManager.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace Clients.AGP.ProSuiteSolution.ConfigUI
{
	public class ProSuiteConfigQAViewModel : ViewModelBase
	{
		public ObservableCollection<ProSuiteQAServerConfiguration> ServiceProviderConfigs { get; set; }

		public ProSuiteConfigQAViewModel(IEnumerable<ProSuiteQAServerConfiguration> configurations)
		{
			ServiceProviderConfigs = new ObservableCollection<ProSuiteQAServerConfiguration>(configurations);
			if (ServiceProviderConfigs.Count > 0)
				SelectedConfiguration = ServiceProviderConfigs.FirstOrDefault();
		}

		private ProSuiteQAServerConfiguration selectedConfiguration = new ProSuiteQAServerConfiguration();
		public ProSuiteQAServerConfiguration SelectedConfiguration
		{
			get {
				return selectedConfiguration;
			}
			set {
				selectedConfiguration = value;
				NotifyPropertyChanged("SelectedConfiguration");
			}
		}

		private ICommand _openConnectionFileCmd = null;
		public ICommand OpenConnectionFileCmd
		{
			get
			{
				if (_openConnectionFileCmd == null)
				{
					_openConnectionFileCmd = new RelayCommand(new Action<Object>((sender) =>
					{
						var fileFilter = BrowseProjectFilter.GetFilter("esri_browseDialogFilters_browseFiles"); // 
						fileFilter.FileExtension = "*.*";
						fileFilter.BrowsingFilesMode = true;

						var dlg = new OpenItemDialog()
						{
							BrowseFilter = fileFilter,
							Title = "Browse Content"
						};
						if (!dlg.ShowDialog().Value)
							return;

						var item = dlg.Items.First();

						// update 
						var newServiceConnection = new ProSuiteQAServerConfiguration()
						{
							ServiceType = SelectedConfiguration.ServiceType,
							ServiceName = SelectedConfiguration.ServiceName,
							ServiceConnection = item.Path
						};
						ServiceProviderConfigs[0] = newServiceConnection;
						//SelectedConfiguration = newServiceConnection;

						//SelectedConfiguration.ServiceConnection = item.Path;

					}), () => { return true; });
				}
				return _openConnectionFileCmd;
			}
		}

		public string TabName
		{
			get
			{
				return "QA";
			}
		}

	}
}
