using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ProSuite.Commons.Essentials.CodeAnnotations;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Point = System.Drawing.Point;

namespace ProSuite.Commons.UI.Drawing
{
	public static class BitmapUtils
	{
		/// <summary>
		/// Copies the content of a BitmapImage to a Bitmap.
		/// </summary>
		/// <param name="bitmapImage"></param>
		/// <returns></returns>
		public static Bitmap CreateBitmap([NotNull] BitmapImage bitmapImage)
		{
			using (MemoryStream memoryStream = new MemoryStream())
			{
				// Create a BitmapEncoder to encode the BitmapImage to a stream
				BitmapEncoder encoder = new PngBitmapEncoder();
				encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
				encoder.Save(memoryStream);
				memoryStream.Position = 0;

				// Create a System.Drawing.Image from the stream
				return (Bitmap) Image.FromStream(memoryStream);
			}
		}

		/// <summary>
		/// Rasterizes the given drawing image into a bitmap.
		/// </summary>
		/// <returns>The bitmap, or null if the drawing has no usable extent.</returns>
		/// <remarks>Rasterization goes through the WPF rendering subsystem (MIL), which can
		/// fail with a COMException (such as MILERR_WIN32ERROR) if no render target can be
		/// created, for example in remote desktop / virtualized graphics environments or
		/// when graphics resources are exhausted.
		/// </remarks>
		[CanBeNull]
		public static Bitmap CreateBitmap([NotNull] DrawingImage drawingImage)
		{
			BitmapSource bitmapSource = Rasterize(drawingImage.Drawing);

			return bitmapSource == null ? null : CreateBitmap(bitmapSource);
		}

		/// <summary>
		/// Rasterizes the given image source into a square bitmap of the given size in
		/// device pixels. The image is scaled to fit (preserving its aspect ratio) and
		/// centered.
		/// </summary>
		/// <returns>The bitmap, or null if the image source has no usable extent.</returns>
		/// <remarks>Rasterization goes through the WPF rendering subsystem (MIL), which can
		/// fail with a COMException (such as MILERR_WIN32ERROR) if no render target can be
		/// created, for example in remote desktop / virtualized graphics environments or
		/// when graphics resources are exhausted.
		/// </remarks>
		[CanBeNull]
		public static Bitmap CreateBitmap([NotNull] ImageSource imageSource, int pixelSize)
		{
			if (pixelSize <= 0)
			{
				return null;
			}

			double sourceWidth = imageSource.Width;
			double sourceHeight = imageSource.Height;

			if (! IsPositiveFinite(sourceWidth) || ! IsPositiveFinite(sourceHeight))
			{
				return null;
			}

			double scale = Math.Min(pixelSize / sourceWidth, pixelSize / sourceHeight);

			double width = sourceWidth * scale;
			double height = sourceHeight * scale;

			var targetRect = new Rect((pixelSize - width) / 2, (pixelSize - height) / 2,
			                          width, height);

			var visual = new DrawingVisual();

			// The small (16x16) images of the ArcGIS Pro commands have to be scaled up
			// for a high-dpi display: use the best available interpolation for that.
			RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.HighQuality);

			using (DrawingContext dc = visual.RenderOpen())
			{
				dc.DrawImage(imageSource, targetRect);
			}

			var target = new RenderTargetBitmap(pixelSize, pixelSize, 96.0, 96.0,
			                                    PixelFormats.Pbgra32);

			target.Render(visual);

			// Freeze: the pixels are read (and the bitmap released) right away, there is
			// no point in keeping it attached to the rendering thread's media context.
			target.Freeze();

			return CreateBitmap(target);
		}

		[NotNull]
		private static Bitmap CreateBitmap([NotNull] BitmapSource bitmapSource)
		{
			Bitmap bitmap = new Bitmap(bitmapSource.PixelWidth, bitmapSource.PixelHeight,
			                           PixelFormat.Format32bppPArgb);

			var rectangle = new Rectangle(Point.Empty, bitmap.Size);

			BitmapData data = bitmap.LockBits(rectangle,
			                                  ImageLockMode.WriteOnly,
			                                  PixelFormat.Format32bppPArgb);

			bitmapSource.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride,
			                        data.Stride);

			bitmap.UnlockBits(data);

			return bitmap;
		}

		[CanBeNull]
		private static RenderTargetBitmap Rasterize(
			[CanBeNull] System.Windows.Media.Drawing drawing)
		{
			if (drawing == null)
			{
				return null;
			}

			// The drawing is rendered in its own coordinates, i.e. the render target
			// extends from the origin to the lower right corner of the drawing.
			Rect bounds = drawing.Bounds;

			if (bounds.IsEmpty || ! IsFinite(bounds.Right) || ! IsFinite(bounds.Bottom))
			{
				return null;
			}

			var width = (int) Math.Ceiling(bounds.Right);
			var height = (int) Math.Ceiling(bounds.Bottom);

			if (width <= 0 || height <= 0)
			{
				return null;
			}

			DrawingVisual visual = new DrawingVisual();
			using (DrawingContext dc = visual.RenderOpen())
			{
				dc.DrawDrawing(drawing);
			}

			RenderTargetBitmap target = new RenderTargetBitmap(
				width, height, 96.0, 96.0, PixelFormats.Pbgra32);

			target.Render(visual);

			// Freeze: the pixels are read (and the bitmap released) right away, there is
			// no point in keeping it attached to the rendering thread's media context.
			target.Freeze();

			return target;
		}

		private static bool IsFinite(double value)
		{
			return ! double.IsNaN(value) && ! double.IsInfinity(value);
		}

		private static bool IsPositiveFinite(double value)
		{
			return IsFinite(value) && value > 0;
		}
	}
}
