using System.Threading.Tasks;
using System.Windows.Media;
using ESRI.ArcGIS.ItemIndex;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.ProjectItem
{
	[UsedImplicitly]
	public class SelectionWorkListProjectItemBase : WorkListProjectItem
	{
		public SelectionWorkListProjectItemBase() { }

		public SelectionWorkListProjectItemBase(ItemInfoValue itemInfoValue) :
			base(itemInfoValue) { }

		public SelectionWorkListProjectItemBase(string name, string catalogPath, string typeID,
		                                        string containerType) : base(
			name, catalogPath, typeID, containerType) { }

		public override ImageSource LargeImage => null;

		public override Task<ImageSource> SmallImage =>
			Task.FromResult(GetImageSource("Properties/Images", @"NavigateSelectionCmd16.png"));
	}
}
