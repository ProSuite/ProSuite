using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.Commons.QA.ServiceManager.Interfaces;
using ProSuite.Commons.QA.ServiceManager.Types;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Clients.AGP.ProSuiteSolution.ConfigUI
{
	public class ProSuiteConfigQAViewModel : ViewModelBase
	{
		public ObservableCollection<ProSuiteQAServerConfiguration> ServiceProviderConfigs { get; set; }

		public ProSuiteConfigQAViewModel(IEnumerable<ProSuiteQAServerConfiguration> configurations)
		{
			ServiceProviderConfigs = new ObservableCollection<ProSuiteQAServerConfiguration>(configurations);
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
