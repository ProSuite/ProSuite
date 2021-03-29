using System.Threading.Tasks;
using System.Windows.Media;
using ArcGIS.Desktop.Core;
using ProSuite.AGP.Solution.Commons;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.ProjectItem
{
	[UsedImplicitly]
	public class WorklistsContainer : CustomProjectItemContainer<IssueWorklistItem>
	{
		//This should be an arbitrary unique string. It must match your <content type="..." 
		//in the Config.daml for the container
		public static readonly string ContainerTypeName = $"{typeof(WorklistsContainer).FullName}";

		public WorklistsContainer() : base(ContainerTypeName) { }

		public override ImageSource LargeImage =>
			ImageUtils.GetImageSource(@"GenericButtonPurple32.png");

		public override Task<ImageSource> SmallImage =>
			Task.FromResult(ImageUtils.GetImageSource(@"GenericButtonPurple16.png"));

		//public override Item CreateItem(string name, string path, string containerType, string data)
		//{
		//	var item = (IssueWorklistItem) ItemFactory.Instance.Create(path);

		//	// IncludeInPackages true ensures that the quake file is included
		//	// in any project templates and project packages.
		//	item.IncludeInPackages(true);
		//	Add(item);

		//	return item;
		//}
	}
}
