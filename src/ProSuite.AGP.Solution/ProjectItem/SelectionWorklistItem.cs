using System.Threading.Tasks;
using System.Windows.Media;
using ArcGIS.Desktop.Core;
using ProSuite.AGP.Solution.Commons;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.ProjectItem
{
	[UsedImplicitly]
	public class SelectionWorklistItem : CustomProjectItemBase
	{
		public override bool IsContainer => false;

		public override ImageSource LargeImage =>
			ImageUtils.GetImageSource(@"NavigateSelectionCmd32.png");

		public override Task<ImageSource> SmallImage =>
			Task.FromResult(ImageUtils.GetImageSource(@"NavigateSelectionCmd16.png"));

		public override ProjectItemInfo OnGetInfo()
		{
			return new ProjectItemInfo
			       {
				       Name = Name,
				       Path = Path,
				       Type = WorklistsContainer.ContainerTypeName
			       };
		}
	}
}
