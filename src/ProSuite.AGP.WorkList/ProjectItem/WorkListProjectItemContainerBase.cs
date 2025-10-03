using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ArcGIS.Desktop.Core;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList.ProjectItem;

public abstract class WorkListProjectItemContainerBase
	: CustomProjectItemContainer<WorkListProjectItem>
{
	protected WorkListProjectItemContainerBase(string containerTypeName) :
		base(containerTypeName) { }

	private static readonly IMsg _msg = Msg.ForCurrentClass();

	public override ImageSource LargeImage =>
		GetImageSource("Properties/Images", @"WorkListsFolder32.png");

	public override Task<ImageSource> SmallImage =>
		Task.FromResult(GetImageSource("Properties/Images", @"WorkListsFolder16.png"));

	/// <summary>
	/// This method is called by the framework when de-serializing the XML fragments for the
	/// project items stored in the aprx.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="path"></param>
	/// <param name="containerType"></param>
	/// <param name="data"></param>
	/// <returns></returns>
	[CanBeNull]
	public override Item CreateItem(string name, string path, string containerType, string data)
	{
		// NOTE: Previously the item was created even if the file already existed
		WorkListProjectItem item = ItemFactory.Instance.Create(path) as WorkListProjectItem;

		// But in some cases the item is null (starting with 3.4)
		if (item == null)
		{
			item = CreateProjectItem(name, path, containerType);
		}

		if (item == null)
		{
			_msg.DebugFormat("Could not create item for {0}", path);
			return null;
		}

		item.IncludeInPackages(true);
		item.WorkListName = WorkListUtils.GetWorklistName(path);
		Add(item);

		return item;
	}

	protected abstract WorkListProjectItem CreateProjectItem(
		string name, string path, string containerType);

	/// <summary>
	/// Gets the image source for a WPF pack URI. Ensure that the calling assembly
	/// has UseWPF=true in the project file. Don't move this method to a utility class,
	/// because the calling assembly has the image as a builtin resource.
	/// </summary>
	private static ImageSource GetImageSource(string relativePath, string imageName)
	{
		try
		{
			string resourcePath = $"{relativePath}/{imageName}";

			string uri = string.Format(
				"pack://application:,,,/{0};component/{1}",
				Assembly.GetCallingAssembly().GetName().Name,
				resourcePath
			);

			Uri uriSource = new Uri(uri);

			return new BitmapImage(uriSource);
		}
		catch (Exception ex)
		{
			_msg.Debug(ex.Message, ex);
		}

		return null;
	}
}
