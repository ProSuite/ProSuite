using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.Commons.QA.ServiceManager.Interfaces;
using ProSuite.Commons.QA.ServiceManager.Types;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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

		public string TabName
		{
			get
			{
				return "QA";
			}
		}

	}
}
