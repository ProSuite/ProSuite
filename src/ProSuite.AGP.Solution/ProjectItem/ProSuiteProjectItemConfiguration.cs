using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;
using ProSuite.AGP.Solution.Commons;
using ProSuite.Commons.Xml;
using ProSuite.QA.ServiceManager.Types;

namespace ProSuite.AGP.Solution.ProjectItem
{
	public class ProSuiteProjectItemConfiguration : ProSuiteProjectItem
	{
		public ProSuiteQASpecificationsConfiguration SpecificationConfiguration { get; set; }
		public IEnumerable<ProSuiteQAServerConfiguration> ServerConfigurations { get; set; }

		public ProSuiteProjectItemConfiguration(string name, string catalogPath, string typeID, string containerTypeID) :
			base(name, catalogPath, typeID, containerTypeID)
		{
		}

		public ProSuiteProjectItemConfiguration(
			IEnumerable<ProSuiteQAServerConfiguration> defaultQAServiceConfig,
			ProSuiteQASpecificationsConfiguration defaultQASpecConfig)
		{
			ServerConfigurations = defaultQAServiceConfig;
			SpecificationConfiguration = defaultQASpecConfig;
		}

		public override ImageSource LargeImage => ImageUtils.GetImageSource(@"GeodatabaseFeatureDataset32.png");

		public override Task<ImageSource> SmallImage => Task.FromResult((ImageSource)ImageUtils.GetImageSource(@"GeodatabaseFeatureDataset16.png"));

		//TODO algr: IsContainer = true will allow create subitem(s) from one file
		public override bool IsContainer => false;//true;

		//TODO algr: this is necessary only for IsContainer = true
		public override void Fetch()
		{
			this.ClearChildren();

			// serialize configuration info
			var helper = new XmlSerializationHelper<IEnumerable<ProSuiteQAServerConfiguration>>();
			ServerConfigurations = helper.ReadFromFile(Path);

			// add subtype - ProSuiteProjectItemConfig?
		}
	}

	//internal class ProSuiteProjectItemConfig : CustomItemBase
	//{
	//	public ProSuiteProjectItemConfig(string name, string path, string type, string lastModifiedTime) : base(name, path, type, lastModifiedTime)
	//	{
	//		DisplayType = "Configuration";
	//	}

	//	public override ImageSource LargeImage => ImageUtils.GetImageSource(@"GeodatabaseFeatureDataset32.png");
	//	public override Task<ImageSource> SmallImage => Task.FromResult((ImageSource)ImageUtils.GetImageSource(@"GeodatabaseFeatureDataset16.png"));
	//	public override bool IsContainer => false;
	//}

}
