using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Reflection;

namespace ProSuite.UI.Core.Processing
{
	public static class ProcessImageSetUtils
	{
		//Todo: Think if/how we want to expose this?
		//resource reference is "<name>,<resourcename>,<assembly>"
		//e.g. "AlignRepresentations,EsriDE.ProSuite.Processing.Processes.Properties.Resources,EsriDE.ProSuite.Processing.Processes"

		//	expected structure of resources per CP is:
		//<name>.png
		//<name>_Alt.png or _alt.png
		//<name>Cursor.cur
		//<name>CursorShift.cur

		private const char _separator = ',';

		[NotNull]
		public static IDictionary<ProcessImageSet, string> GetDefaultProcessImageSets(
			[NotNull] string resourceName, [NotNull] string assemblyName)
		{
			Assert.ArgumentNotNullOrEmpty(resourceName, nameof(resourceName));
			Assert.ArgumentNotNullOrEmpty(assemblyName, nameof(assemblyName));

			var result = new Dictionary<ProcessImageSet, string>();

			Assembly iconAssembly = PrivateAssemblyUtils.LoadAssembly(assemblyName);
			var resourceManager = new ResourceManager(resourceName, iconAssembly);

			ResourceSet resourceSet =
				resourceManager.GetResourceSet(CultureInfo.InvariantCulture, true, false);
			foreach (DictionaryEntry entry in resourceSet)
			{
				string imageKey = entry.Key.ToString();
				string imageResourceName = string.Concat(imageKey, _separator, resourceName,
				                                         _separator, assemblyName);
				if (entry.Value is Bitmap bitmap)
				{
					result.Add(new ProcessImageSet(bitmap), imageResourceName);
				}
			}

			return result;
		}

		[NotNull]
		public static ProcessImageSet GetProcessImageSet(
			[NotNull] string associatedCommandIcon, bool getCursors)
		{
			Assert.ArgumentNotNullOrEmpty(associatedCommandIcon, nameof(associatedCommandIcon));

			var iconFileInfo = new FileInfo(associatedCommandIcon);

			return iconFileInfo.Exists
				       ? GetProcessImageSetFromFileSystem(iconFileInfo, getCursors)
				       : GetProcessImageSetFromResource(associatedCommandIcon, getCursors);
		}

		[NotNull]
		private static ProcessImageSet GetProcessImageSetFromResource(
			[NotNull] string associatedCommandIcon, bool getCursors)
		{
			string[] iconReference = associatedCommandIcon.Split(_separator);
			if (iconReference.Length != 3)
			{
				throw new FormatException(
					$"Reference '{associatedCommandIcon}' does not have expected format: " +
					$"'<iconname>{_separator}<resourcename>{_separator}<assembly>'");
			}

			string iconName = iconReference[0];
			string resourceName = iconReference[1];
			string assemblyName = iconReference[2];

			try
			{
				Assembly iconAssembly = PrivateAssemblyUtils.LoadAssembly(assemblyName);
				var resourceManager = new ResourceManager(resourceName, iconAssembly);

				var bitmap = resourceManager.GetObject(iconName) as Bitmap;
				bitmap?.MakeTransparent(bitmap.GetPixel(0, 0));

				if (getCursors)
				{
					GetImageSetNames(iconName, out string cursorName, out string shiftName);

					Cursor cursor = ! (resourceManager.GetObject(cursorName) is byte[] b)
						                ? null
						                : new Cursor(new MemoryStream(b));

					Cursor shiftCursor = ! (resourceManager.GetObject(shiftName) is byte[] b2)
						                     ? cursor
						                     : new Cursor(new MemoryStream(b2));

					return new ProcessImageSet(bitmap, cursor, shiftCursor);
				}

				return new ProcessImageSet(bitmap);
			}
			catch (Exception ex)
			{
				throw new Exception(
					$"Reference '{associatedCommandIcon}' could not be loaded: {ex.Message}", ex);
			}
		}

		[NotNull]
		private static ProcessImageSet GetProcessImageSetFromFileSystem(
			[NotNull] FileInfo iconFileInfo, bool getCursors)
		{
			Bitmap bitmap = new Bitmap(iconFileInfo.FullName);

			if (getCursors)
			{
				GetImageSetNames(Path.GetFileNameWithoutExtension(iconFileInfo.Name),
				                 out string cursorName, out string shiftName);

				string dir = iconFileInfo.DirectoryName;
				Assert.True(dir != null && Directory.Exists(dir),
				            "Directory does not exist: {0}", iconFileInfo);

				string cursorFile = Path.Combine(dir, string.Concat(cursorName, ".cur"));
				Cursor cursor = File.Exists(cursorFile) ? new Cursor(cursorFile) : null;

				string shiftFile = Path.Combine(dir, string.Concat(shiftName, ".cur"));
				Cursor shiftCursor = File.Exists(shiftFile) ? new Cursor(shiftFile) : null;

				return new ProcessImageSet(bitmap, cursor, shiftCursor);
			}

			return new ProcessImageSet(bitmap);
		}

		private static void GetImageSetNames([NotNull] string iconName,
		                                     [NotNull] out string cursorName,
		                                     [NotNull] out string shiftCursorName)
		{
			cursorName = string.Concat(
				iconName.EndsWith("_alt", StringComparison.OrdinalIgnoreCase)
					? iconName.Substring(0, iconName.Length - 4)
					: iconName, "Cursor");

			shiftCursorName = string.Concat(cursorName, "Shift");
		}
	}
}
