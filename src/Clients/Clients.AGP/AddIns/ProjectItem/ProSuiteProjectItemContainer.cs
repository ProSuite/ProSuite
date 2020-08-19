using ArcGIS.Desktop.Core;
using ProSuite.Commons.AGP;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Clients.AGP.ProSuiteSolution.ProjectItem
{
	internal class ProSuiteProjectItemContainer : CustomProjectItemContainer<ProSuiteQAProjectItem>
	{
		//This should be an arbitrary unique string. It must match your <content type="..."  in the Config.daml for the container
		public static readonly string ContainerName = "ProSuiteContainer";
		public ProSuiteProjectItemContainer() : base(ContainerName)
		{
		}

		//componentType will be your "ComponentTypeValue" value
		//see "this.ComponentType" property also
		public ProSuiteProjectItemContainer(string componentType) : base(componentType)
		{
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
			var item = ItemFactory.Instance.Create(path) as ProSuiteQAProjectItem;
			if (item != null)
			{
				// IncludeInPackages true ensures that the stored file is included
				// in any project templates and project packages.
				item.IncludeInPackages(true);
				//this.Add(item);
			}
			return item;
		}

		/// <summary>
		/// Adds an item to the container. This will trigger the visibility of the
		/// container if it was previously empty.
		/// </summary>
		/// <param name="projectItem"></param>
		public void AddItem(ProSuiteQAProjectItem projectItem)
		{
			this.Add(projectItem);
		}

		public override ImageSource LargeImage
		{
			get
			{
				return ImageUtils.GetImageSource(@"Images/GeodatabaseFeatureDataset32.png");
			}
		}

		public override Task<System.Windows.Media.ImageSource> SmallImage
		{
			get
			{
				return Task.FromResult((ImageSource)ImageUtils.GetImageSource(@"Images/GeodatabaseFeatureDataset16.png"));
			}
		}

	}
}
