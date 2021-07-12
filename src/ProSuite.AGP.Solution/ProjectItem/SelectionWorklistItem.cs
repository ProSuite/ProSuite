using System.Threading.Tasks;
using System.Windows.Media;
using ESRI.ArcGIS.ItemIndex;
using ProSuite.AGP.Solution.Commons;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.ProjectItem
{
	[UsedImplicitly]
	public class SelectionWorklistItem : WorklistItem
	{
		public SelectionWorklistItem() { }

		public SelectionWorklistItem(ItemInfoValue itemInfoValue) : base(itemInfoValue) { }

		public SelectionWorklistItem(string name, string catalogPath, string typeID,
		                             string containerType) : base(
			name, catalogPath, typeID, containerType) { }

		public override ImageSource LargeImage =>
			ImageUtils.GetImageSource(@"NavigateSelectionCmd32.png");

		public override Task<ImageSource> SmallImage =>
			Task.FromResult(ImageUtils.GetImageSource(@"NavigateSelectionCmd16.png"));
	}
}
