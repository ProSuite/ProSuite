using System.Threading.Tasks;
using System.Windows.Media;
using ArcGIS.Desktop.Core;
using ESRI.ArcGIS.ItemIndex;
using ProSuite.AGP.Solution.Commons;

namespace ProSuite.AGP.Solution.ProjectItem
{
	public class ProSuiteProjectItem : CustomProjectItemBase
	{
		protected ProSuiteProjectItem() { }

		protected ProSuiteProjectItem(ItemInfoValue iiv) : base(FlipBrowseDialogOnly(iiv)) { }

		//TODO: Overload for use in your container create item
		public ProSuiteProjectItem(string name, string catalogPath, string typeID,
		                           string containerTypeID) :
			base(name, catalogPath, typeID, containerTypeID) { }

		public override ImageSource LargeImage =>
			ImageUtils.GetImageSource(@"GeodatabaseFeatureDataset32.png");

		public override Task<ImageSource> SmallImage =>
			Task.FromResult(ImageUtils.GetImageSource(@"GeodatabaseFeatureDataset16.png"));

		public override bool IsContainer => false; //true;

		public override ProjectItemInfo OnGetInfo()
		{
			var projectItemInfo = new ProjectItemInfo
			                      {
				                      Name = Name,
				                      Path = Path,
				                      Type = ProSuiteProjectItemContainer.ContainerName
			                      };

			return projectItemInfo;
		}

		private static ItemInfoValue FlipBrowseDialogOnly(ItemInfoValue iiv)
		{
			iiv.browseDialogOnly = "FALSE";
			return iiv;
		}

		//TODO: Fetch is required if <b>IsContainer</b> = <b>true</b>
		//public override void Fetch()
		//{
		//	this.ClearChildren();
		//	string filePath = this.Path;

		//	var helper = new XmlSerializationHelper<XmlWorkListDefinition>();
		//	XmlWorkListDefinition definition = helper.ReadFromFile(filePath);

		//	ProSuiteProjectItemWorkList workList = new ProSuiteProjectItemWorkList(
		//		definition.GeodatabasePath, filePath, "ProSuiteItem_WorkListItem", null);

		//	List<ProSuiteProjectItemWorkList> workLists = new List<ProSuiteProjectItemWorkList>();
		//	workLists.Add(workList);
		//	this.AddRangeToChildren(workLists);
		//}
	}
}
