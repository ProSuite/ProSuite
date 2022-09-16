using System.Drawing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Properties;

namespace ProSuite.DdxEditor.Framework.Items
{
	public static class ItemUtils
	{
		[NotNull]
		public static Image GetGroupItemImage([CanBeNull] Image overlay = null)
		{
			Bitmap result = Resources.GroupItem;

			if (overlay != null)
			{
				AddOverlay(overlay, result);
			}

			return result;
		}

		[NotNull]
		public static Image GetGroupItemSelectedImage([CanBeNull] Image overlay = null)
		{
			Bitmap result = Resources.GroupItemSelected;

			if (overlay != null)
			{
				AddOverlay(overlay, result);
			}

			return result;
		}

		private static void AddOverlay([NotNull] Image overlay, [NotNull] Image image)
		{
			Graphics g = Graphics.FromImage(image);

			try
			{
				var destinationRectangle = new Rectangle(0, 0, image.Width, image.Height);

				g.DrawImage(overlay, destinationRectangle);
			}
			finally
			{
				g.Dispose();
			}
		}
	}
}
