using ArcGIS.Desktop.Framework.Contracts;

namespace Clients.AGP.ProSuiteSolution.ConfigUI
{
	public class ProSuiteConfigQAViewModel : ViewModelBase
	{
		public string TabName
		{
			get
			{
				return "QA";
			}
		}

		public string TabContent
		{
			get
			{
				return "QA config content";
			}
		}

	}
}
