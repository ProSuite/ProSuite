using System.Threading.Tasks;
using System.Windows.Media;
using ArcGIS.Desktop.Core;
using ProSuite.AGP.Solution.Commons;

namespace ProSuite.AGP.Solution.ProjectItem
{
	// this is main container for all specific project items

	internal sealed class
		ProSuiteProjectItemContainer : CustomProjectItemContainer<ProSuiteProjectItem>
	{
		//This should be an arbitrary unique string. It must match your <content type="..." 
		//in the Config.daml for the container
		public static readonly string ContainerName = "ProSuiteContainer";

		public ProSuiteProjectItemContainer() : base(ContainerName)
		{
			this.ContextMenuID = "ProSuiteSolution_AddFoldersMenu";
		}

		/// <summary>
		/// Create item is called whenever a custom item, registered with the container,
		/// is browsed or fetched (eg the user is navigating through different folders viewing
		/// content in the catalog pane).
		/// </summary>
		/// <param name="name"></param>
		/// <param name="path"></param>
		/// <param name="containerType"></param>
		/// <param name="data"></param>
		/// <returns>A custom item created from the input parameters</returns>
		public override Item CreateItem(string name, string path, string containerType, string data)
		{
			var item = ItemFactory.Instance.Create(path);
			if (item is ProSuiteProjectItemContainer)
			{
				var projectItem = item as ProSuiteProjectItem;
				if (projectItem != null)
					this.Add(projectItem);
			}

			return item;
		}

		// TODO algr: better icon (similar to ArcGIS Pro project items)
		public override ImageSource LargeImage =>
			ImageUtils.GetImageSource(@"FolderWithGISData32.png");

		public override Task<ImageSource> SmallImage =>
			Task.FromResult((ImageSource) ImageUtils.GetImageSource(@"FolderWithGISData16.png"));
	}
}
