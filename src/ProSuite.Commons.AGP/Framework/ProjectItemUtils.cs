using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Framework
{
	public static class ProjectItemUtils
	{
		[CanBeNull]
		public static string GetSelectedProjectItemPath()
		{
			var window = FrameworkApplication.ActiveWindow as IProjectWindow;

			string path = window?.SelectedItems.FirstOrDefault()?.Path;

			if (File.Exists(path))
			{
				return path;
			}

			string message = $"{path} does not exist.";
			Gateway.ShowMessage(message, "File not found", MessageBoxButton.OK, MessageBoxImage.Error);

			return null;
		}

		public static IEnumerable<T> Get<T>() where T : Item
		{
			return Project.Current.GetItems<T>();
		}

		[CanBeNull]
		public static T Get<T>([NotNull] string name) where T : Item
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			foreach (T item in Project.Current.GetItems<T>())
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

			return Project.Current.GetItems<T>()
			              .Where(item => item.Name.StartsWith(
				                     substring, StringComparison.OrdinalIgnoreCase));
		}

		public static bool TryAdd<T>([NotNull] string path, [CanBeNull] out T item) where T : Item
		{
			Assert.ArgumentNotNullOrEmpty(path, nameof(path));

			item = null;

			var projectItem = ItemFactory.Instance.Create(path) as IProjectItem;

			if (projectItem == null || ! Project.Current.AddItem(projectItem))
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

			if (projectItem == null || ! Project.Current.AddItem(projectItem))
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

			return Project.Current.RemoveItem(item);
		}

		public static bool Remove([NotNull] IProjectItem item)
		{
			Assert.ArgumentNotNull(item, nameof(item));

			return Project.Current.RemoveItem(item);
		}
	}
}
