using ArcGIS.Desktop.Core;
using Clients.AGP.ProSuiteSolution.Commons;
using ESRI.ArcGIS.ItemIndex;
using ProSuite.AGP.WorkList.Domain.Persistence.Xml;
using ProSuite.Commons.Xml;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ProSuite.AGP.Solution.ProjectItem
{

	public class ProSuiteProjectItemFile : CustomProjectItemBase
	{
		protected ProSuiteProjectItemFile() : base()
		{
		}

		protected ProSuiteProjectItemFile(ItemInfoValue iiv) : base(FlipBrowseDialogOnly(iiv))
		{
		}

		private static ItemInfoValue FlipBrowseDialogOnly(ItemInfoValue iiv)
		{
			iiv.browseDialogOnly = "FALSE";
			return iiv;
		}

		public ProSuiteProjectItemFile(string name, string catalogPath, string typeID, string containerTypeID) :
		  base(name, catalogPath, typeID, containerTypeID)
		{
		}

		public ProSuiteProjectItemFile Clone() => new ProSuiteProjectItemFile(this.Name, this.Path, this.TypeID, this.ContainerType);

		~ProSuiteProjectItemFile()
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

		//TODO algr: IsContainer = true will allow create subitem(s) from one file
		public override bool IsContainer => false;//true;

		//TODO algr: this is necessary only for IsContainer = true
		//public override void Fetch()
		//{
		//	this.ClearChildren();
		//	string filePath = this.Path; 

		//	var helper = new XmlSerializationHelper<XmlWorkListDefinition>();
		//	XmlWorkListDefinition definition = helper.ReadFromFile(filePath);

		//	ProSuiteProjectItemFile workList = new ProSuiteProjectItemFile(
		//		definition.GeodatabasePath, filePath, "ProSuiteItem_WorkListItem", null);

		//	List<ProSuiteProjectItemFile> workLists = new List<ProSuiteProjectItemFile>();
		//	workLists.Add(workList);
		//	this.AddRangeToChildren(workLists);
		//}

	}

	// TODO algr: are ProSuiteDataSubItem for different types of data necessary?
	//internal class ProSuiteProjectItemWorkList : CustomItemBase
	//{
	//	public ProSuiteProjectItemWorkList(string name, string path, string type, string lastModifiedTime) : base(name, path, type, lastModifiedTime)
	//	{
	//		DisplayType = "WorkList";
	//	}

	//	public override ImageSource LargeImage => ImageUtils.GetImageSource(@"GeodatabaseFeatureDataset32.png");
	//	public override Task<ImageSource> SmallImage => Task.FromResult((ImageSource)ImageUtils.GetImageSource(@"GeodatabaseFeatureDataset16.png"));
	//	public override bool IsContainer => false;
	//}

}
