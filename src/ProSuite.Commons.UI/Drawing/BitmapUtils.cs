using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
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
			// Copy pixeld from BitmapImage to byte array
			int stride = bitmapImage.PixelWidth * 4;
			byte[] buffer = new byte[stride * bitmapImage.PixelHeight];
			bitmapImage.CopyPixels(buffer, stride, 0);

			// Create Bitmap in the correct size and format
			Bitmap bitmap =
				new Bitmap(bitmapImage.PixelWidth, bitmapImage.PixelHeight,
				           PixelFormat.Format32bppArgb);

			// Lock the bitmap in memory
			BitmapData bitmapData =
				bitmap.LockBits(
					new Rectangle(0, 0, bitmap.Width, bitmap.Height),
					ImageLockMode.WriteOnly,
					bitmap.PixelFormat);

			// Copy the byte array to the bitmap
			Marshal.Copy(buffer, 0, bitmapData.Scan0, buffer.Length);

			// Unlock the bitmap
			bitmap.UnlockBits(bitmapData);

			return bitmap;
		}

		public static Bitmap CreateBitmap(DrawingImage drawingImage)
		{
			BitmapSource bitmapSource = Rasterize(drawingImage.Drawing);

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

		private static RenderTargetBitmap Rasterize([NotNull] System.Windows.Media.Drawing drawing)
		{
			DrawingVisual visual = new DrawingVisual();
			using (DrawingContext dc = visual.RenderOpen())
			{
				dc.DrawDrawing(drawing);
				dc.Close();
			}

			RenderTargetBitmap target = new RenderTargetBitmap(
				(int) drawing.Bounds.Right, (int) drawing.Bounds.Bottom, 96.0, 96.0,
				PixelFormats.Pbgra32);
			target.Render(visual);

			return target;
		}
	}
}
