using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;
using ArcGIS.Desktop.Core;
using ProSuite.AGP.Solution.Commons;
using ProSuite.AGP.WorkList;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.ProjectItem
{
	[UsedImplicitly]
	public class WorklistsContainer : CustomProjectItemContainer<WorklistItem>
	{
		//This should be an arbitrary unique string. It must match your <content type="..." 
		//in the Config.daml for the container
		public static readonly string ContainerTypeName = $"{typeof(WorklistsContainer).FullName}";

		public WorklistsContainer() : this(ContainerTypeName) { }

		public WorklistsContainer(string containerTypeName) : base(containerTypeName) { }

		public override ImageSource LargeImage =>
			ImageUtils.GetImageSource(@"NavigateSelectionCmd32.png");

		public override Task<ImageSource> SmallImage =>
			Task.FromResult(ImageUtils.GetImageSource(@"WorklistsFolder16.png"));

		[CanBeNull]
		public override Item CreateItem(string name, string path, string containerType, string data)
		{
			var item = ItemFactory.Instance.Create(path) as WorklistItem;

			if (item == null)
			{
				// todo daro remove items from project if they can't be restored, e.g. deleted on file system?
				return null;
			}

			item.IncludeInPackages(true);
			item.WorklistName = WorkListUtils.GetName(path);
			Add(item);

			return item;
		}

		/// <summary>
		/// From the Pro SDK documentation: Internally, the base implementation of CreateItem calls CreateItemPrototype.
		/// Customizations to the creation of your custom project items should ideally be added to CreateItemPrototype.
		/// </summary>
		//public override Item CreateItemPrototype(string name, string path, string containerType, string data)
		//{
		//	Item item = base.CreateItemPrototype(name, path, containerType, data);

		//	// todo daro: revise purpose of this method, remove it
		//	var worklistItem = item as SelectionWorklistItem;
		//	Assert.Null(worklistItem);

		//	return item;
		//}

		public override Task DeleteAsync(IEnumerable<Item> items)
		{
			return base.DeleteAsync(items);
		}

		public override void OnCurrentRoot(bool isActivating)
		{
			base.OnCurrentRoot(isActivating);
		}

		protected override void Update()
		{
			base.Update();
		}
	}
}
