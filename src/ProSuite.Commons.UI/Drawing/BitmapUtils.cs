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
