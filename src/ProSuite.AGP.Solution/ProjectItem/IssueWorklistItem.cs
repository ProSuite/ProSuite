using System.Threading.Tasks;
using System.Windows.Media;
using ArcGIS.Desktop.Core;
using ProSuite.AGP.Solution.Commons;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.ProjectItem
{
	// If only one constructor is implemented, all have to be implemented or it wouldn't work.
	[UsedImplicitly]
	public class IssueWorklistItem : CustomProjectItemBase
	{
		#region Overrides

		/// <summary>
		/// Gets whether the project item can contain child items
		/// </summary>
		public override bool IsContainer => false;

		public override ImageSource LargeImage =>
			ImageUtils.GetImageSource(@"NavigateErrorsCmd32.png");

		public override Task<ImageSource> SmallImage =>
			Task.FromResult(ImageUtils.GetImageSource(@"NavigateErrorsCmd16.png"));

		//public override ImageSource LargeImage => System.Windows.Application.Current.Resources["BexDog32"] as ImageSource;
		//public override Task<ImageSource> SmallImage => Task.FromResult(System.Windows.Application.Current.Resources["BexDog16"] as ImageSource);

		public override ProjectItemInfo OnGetInfo()
		{
			return new ProjectItemInfo
			       {
				       Name = Name,
				       Path = Path,
				       Type = WorklistsContainer.ContainerTypeName
			       };
		}

		#endregion
	}
}
