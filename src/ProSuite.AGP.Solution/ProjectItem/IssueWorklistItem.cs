using System.Threading.Tasks;
using System.Windows.Media;
using ESRI.ArcGIS.ItemIndex;
using ProSuite.AGP.Solution.Commons;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.ProjectItem
{
	[UsedImplicitly]
	public class IssueWorklistItem : WorklistItem
	{
		public IssueWorklistItem() { }

		public IssueWorklistItem(ItemInfoValue itemInfoValue) : base(itemInfoValue) { }

		public IssueWorklistItem(string name, string catalogPath, string typeID,
		                         string containerType) : base(
			name, catalogPath, typeID, containerType) { }

		public override ImageSource LargeImage =>
			ImageUtils.GetImageSource(@"NavigateErrorsCmd32.png");

		public override Task<ImageSource> SmallImage =>
			Task.FromResult(ImageUtils.GetImageSource(@"NavigateErrorsCmd16.png"));
	}
}
