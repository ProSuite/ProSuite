using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.WinForms
{
	/// <summary>
	/// High-quality scaling for WinForms controls that draw bitmaps at another size than
	/// their source (tool strips, grid image columns). GDI+'s default scaling drops or
	/// doubles 1 px details depending on the sampling phase, which makes a symmetric border
	/// look thicker on one side.
	/// </summary>
	public static class ImageDpiUtils
	{
		/// <summary>
		/// Returns a copy of <paramref name="image"/> at <paramref name="size"/>, with the
		/// aspect ratio preserved and centred, using high-quality interpolation.
		/// </summary>
		[NotNull]
		public static Image Scale([NotNull] Image image, Size size)
		{
			var scaled = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);

			using (Graphics graphics = Graphics.FromImage(scaled))
			{
				graphics.CompositingMode = CompositingMode.SourceCopy;
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
				graphics.SmoothingMode = SmoothingMode.HighQuality;

				float scale = Math.Min(size.Width / (float) image.Width,
				                       size.Height / (float) image.Height);
				int width = Math.Max(1, (int) Math.Round(image.Width * scale));
				int height = Math.Max(1, (int) Math.Round(image.Height * scale));

				var target = new Rectangle((size.Width - width) / 2,
				                           (size.Height - height) / 2,
				                           width, height);

				using (var attributes = new ImageAttributes())
				{
					// Avoid transparent edge pixels bleeding into the border.
					attributes.SetWrapMode(WrapMode.TileFlipXY);
					graphics.DrawImage(image, target, 0, 0, image.Width, image.Height,
					                   GraphicsUnit.Pixel, attributes);
				}
			}

			return scaled;
		}
	}
}
