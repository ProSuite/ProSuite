using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using ProSuite.Commons.Essentials.CodeAnnotations;

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
	}
}
