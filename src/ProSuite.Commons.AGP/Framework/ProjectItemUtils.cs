using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Framework;

public static class ProjectItemUtils
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	[CanBeNull]
	public static string GetSelectedProjectItemPath()
	{
		var window = FrameworkApplication.ActiveWindow as IProjectWindow;

		string path = window?.SelectedItems.FirstOrDefault()?.Path;

		if (! File.Exists(path))
		{
			_msg.DebugFormat("{0} does not exist", path);
		}

		return path;
	}

	public static IEnumerable<T> Get<T>() where T : Item
	{
		Project project = Project.Current;

		return project == null ? Enumerable.Empty<T>() : project.GetItems<T>();
	}

	[CanBeNull]
	public static T Get<T>([NotNull] string name) where T : Item
	{
		Assert.ArgumentNotNullOrEmpty(name, nameof(name));

		Project project = Project.Current;

		if (project == null)
		{
			return null;
		}

		foreach (T item in project.GetItems<T>())
		{
			// after adding the project item it's name has an file extension, e.g. "worklist.swl"
			if (string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
			{
				return item;
			}

			// after renaming the item it is missing the extension (although we explicitly
			// assign a extension to the item), e.g. "worklist"
			if (string.Equals(Path.GetFileNameWithoutExtension(item.Name),
			                  Path.GetFileNameWithoutExtension(name),
			                  StringComparison.OrdinalIgnoreCase))
			{
				return item;
			}
		}

		return null;
	}

	public static IEnumerable<T> GetItemsStartingWith<T>([NotNull] string substring)
		where T : Item
	{
		Assert.ArgumentNotNullOrEmpty(substring, nameof(substring));

		Project project = Project.Current;

		if (project == null)
		{
			return Enumerable.Empty<T>();
		}

		return project.GetItems<T>()
		              .Where(item => item.Name.StartsWith(
			                     substring, StringComparison.OrdinalIgnoreCase));
	}

	public static bool TryAdd<T>([NotNull] string path, [CanBeNull] out T item) where T : Item
	{
		Assert.ArgumentNotNullOrEmpty(path, nameof(path));

		item = null;

		var projectItem = ItemFactory.Instance.Create(path) as IProjectItem;

		Project project = Project.Current;

		if (project == null)
		{
			return false;
		}

		if (projectItem == null || ! project.AddItem(projectItem))
		{
			return false;
		}

		string itemName = Path.GetFileNameWithoutExtension(path);

		var result = Get<T>(itemName);

		if (result == null)
		{
			return false;
		}

		item = result;
		return true;
	}

	public static bool TryAdd<T>([NotNull] string path, ItemFactory.ItemType type,
	                             [CanBeNull] out T item)
		where T : Item
	{
		Assert.ArgumentNotNullOrEmpty(path, nameof(path));

		item = null;

		var projectItem = ItemFactory.Instance.Create(path, type) as IProjectItem;

		Project project = Project.Current;

		if (project == null)
		{
			return false;
		}

		if (projectItem == null || ! project.AddItem(projectItem))
		{
			return false;
		}

		string itemName = Path.GetFileName(path);

		var result = Get<T>(itemName);

		if (result == null)
		{
			return false;
		}

		item = result;
		return true;
	}

	public static bool Remove([NotNull] string path)
	{
		Assert.ArgumentNotNullOrEmpty(path, nameof(path));

		var item = ItemFactory.Instance.Create(path) as IProjectItem;
		Assert.NotNull(item);

		return Remove(item);
	}

	public static bool Remove([NotNull] IProjectItem item)
	{
		Assert.ArgumentNotNull(item, nameof(item));

		Project project = Project.Current;

		return project != null && project.RemoveItem(item);
	}
}
