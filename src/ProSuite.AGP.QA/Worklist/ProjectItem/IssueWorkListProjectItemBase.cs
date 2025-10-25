using System.Threading.Tasks;
using System.Windows.Media;
using ESRI.ArcGIS.ItemIndex;
using ProSuite.AGP.WorkList.ProjectItem;

namespace ProSuite.AGP.QA.Worklist.ProjectItem
{
	public class IssueWorkListProjectItemBase : WorkListProjectItem
	{
		public IssueWorkListProjectItemBase() { }

		public IssueWorkListProjectItemBase(ItemInfoValue itemInfoValue) : base(itemInfoValue) { }

		public IssueWorkListProjectItemBase(string name, string catalogPath, string typeID,
		                                    string containerType) : base(
			name, catalogPath, typeID, containerType) { }

		public override ImageSource LargeImage => null;

		public override Task<ImageSource> SmallImage =>
			Task.FromResult(GetImageSource("Properties/Images", @"NavigateErrorsCmd16.png"));
	}
}
