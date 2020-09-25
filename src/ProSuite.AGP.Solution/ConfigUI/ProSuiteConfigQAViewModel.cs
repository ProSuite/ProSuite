using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.QA.ServiceManager.Types;

namespace ProSuite.AGP.Solution.ConfigUI
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

		private ICommand _cmdBrowseConnection;
		public ICommand CmdBrowseConnection
		{
			get
			{
				if (_cmdBrowseConnection == null)
				{
					_cmdBrowseConnection = new RelayCommand(sender =>
					{
						var fileFilter = BrowseProjectFilter.GetFilter("esri_browseDialogFilters_browseFiles");
						fileFilter.BrowsingFilesMode = true;
						if (selectedConfiguration.ServiceType == ProSuiteQAServiceType.GPService)
						{
							fileFilter.FileExtension = "*.ags";
							fileFilter.Name = "ArcGIS Server Connection (*.ags)";
						}
						else
						{
							fileFilter.FileExtension = "*.pyt";
							fileFilter.Name = "Python Toolbox (*.pyt)";
						}

						var selectedFile = SelectFileItem(fileFilter);
						if (!string.IsNullOrEmpty(selectedFile)) { 

							// update current configuration (cancel?)
							SelectedConfiguration.ServiceConnection = selectedFile;
							NotifyPropertyChanged("SelectedConfiguration");
						}

					}, () => true);
				}
				return _cmdBrowseConnection;
			}
		}

		public string TabName
		{
			get
			{
				return "QA";
			}
		}

		// TODO algr: this should be moved to file utils
		private string SelectFileItem(BrowseProjectFilter projectFilter)
		{
			var dlg = new OpenItemDialog()
			{
				BrowseFilter = projectFilter,
				Title = "Browse file ..."
			};
			if (!dlg.ShowDialog().Value)
				return "";

			return dlg.Items.First()?.Path;
		}
	}
}
