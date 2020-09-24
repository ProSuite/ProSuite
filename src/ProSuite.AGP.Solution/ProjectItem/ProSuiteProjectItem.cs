using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Linq;
using ArcGIS.Desktop.Core;
using Clients.AGP.ProSuiteSolution.Commons;
using ESRI.ArcGIS.ItemIndex;
using ProSuite.AGP.WorkList.Domain.Persistence.Xml;
using ProSuite.Commons.Xml;
using ProSuite.QA.ServiceManager.Types;

namespace ProSuite.AGP.Solution.ProjectItem
{

	public class ProSuiteProjectItem : CustomProjectItemBase
	{
		public ProSuiteQASpecificationsConfiguration SpecificationConfiguration { get; set; }
		public IEnumerable<ProSuiteQAServerConfiguration> ServerConfigurations { get; set; }

		protected ProSuiteProjectItem() : base()
		{
		}

		protected ProSuiteProjectItem(ItemInfoValue iiv) : base(FlipBrowseDialogOnly(iiv))
		{
		}

		//public ProSuiteProjectItem(IEnumerable<ProSuiteQAServerConfiguration> serverConfigurations, ProSuiteQASpecificationsConfiguration specifationConfiguration)
		//{
		//	SpecificationConfiguration = specifationConfiguration;
		//	ServerConfigurations = serverConfigurations;
		//}

		private static ItemInfoValue FlipBrowseDialogOnly(ItemInfoValue iiv)
		{
			iiv.browseDialogOnly = "FALSE";
			return iiv;
		}

		//TODO: Overload for use in your container create item
		public ProSuiteProjectItem(string name, string catalogPath, string typeID, string containerTypeID) :
		  base(name, catalogPath, typeID, containerTypeID)
		{
		}

		public ProSuiteProjectItem Clone() => new ProSuiteProjectItem(this.Name, this.Path, this.TypeID, this.ContainerType);

		/// <summary>
		/// DTor
		/// </summary>
		~ProSuiteProjectItem()
		{
		}

		public override ImageSource LargeImage => ImageUtils.GetImageSource(@"GeodatabaseFeatureDataset32.png");

		public override Task<ImageSource> SmallImage => Task.FromResult((ImageSource)ImageUtils.GetImageSource(@"GeodatabaseFeatureDataset16.png"));

		public override ProjectItemInfo OnGetInfo()
		{
			var projectItemInfo = new ProjectItemInfo
			{
				Name = this.Name,
				Path = this.Path,
				Type = ProSuiteProjectItemContainer.ContainerName
			};

			return projectItemInfo;
		}

		public override bool IsContainer => true;

		//TODO: Fetch is required if <b>IsContainer</b> = <b>true</b>
		public override void Fetch()
		{
			this.ClearChildren();
			string filePath = this.Path; 

			var helper = new XmlSerializationHelper<XmlWorkListDefinition>();
			XmlWorkListDefinition definition = helper.ReadFromFile(filePath);

			ProSuiteProjectItemWorkList workList = new ProSuiteProjectItemWorkList(
				definition.GeodatabasePath, filePath, "ProSuiteItem_WorkListItem", null);

			List<ProSuiteProjectItemWorkList> workLists = new List<ProSuiteProjectItemWorkList>();
			workLists.Add(workList);
			this.AddRangeToChildren(workLists);
		}

	}

	// TODO ProSuiteDataSubItem for different types of data?
	internal class ProSuiteProjectItemWorkList : CustomItemBase
	{
		public ProSuiteProjectItemWorkList(string name, string path, string type, string lastModifiedTime) : base(name, path, type, lastModifiedTime)
		{
			DisplayType = "WorkList";
		}

		public override ImageSource LargeImage => ImageUtils.GetImageSource(@"GeodatabaseFeatureDataset32.png");
		public override Task<ImageSource> SmallImage => Task.FromResult((ImageSource)ImageUtils.GetImageSource(@"GeodatabaseFeatureDataset16.png"));
		public override bool IsContainer => false;
	}

	// probably this subitem should not be visible in catalog
	internal class ProSuiteProjectItemConfig : CustomItemBase
	{
		public ProSuiteProjectItemConfig(string name, string path, string type, string lastModifiedTime) : base(name, path, type, lastModifiedTime)
		{
			DisplayType = "Configuration";
		}

		public override ImageSource LargeImage => ImageUtils.GetImageSource(@"GeodatabaseFeatureDataset32.png");
		public override Task<ImageSource> SmallImage => Task.FromResult((ImageSource)ImageUtils.GetImageSource(@"GeodatabaseFeatureDataset16.png"));
		public override bool IsContainer => false;
	}

}
