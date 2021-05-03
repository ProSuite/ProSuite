using System.Collections.Generic;

namespace ProSuite.Commons.AO.Surface.Raster
{
	public interface ISimplePixelBlock<T>
	{
		/// <summary>
		/// The pixel value at the specified pixel coordinate.
		/// </summary>
		/// <param name="column"></param>
		/// <param name="row"></param>
		/// <returns></returns>
		T GetValue(int column, int row);

		/// <summary>
		/// All the pixel of the pixel block in row-major order, i.e. all pixels of the first row, then
		/// all pixels of the second row, etc.
		/// </summary>
		/// <returns></returns>
		IEnumerable<T> AllPixels();

		/// <summary>
		/// The width of the pixel block in pixels.
		/// </summary>
		int Width { get; }

		/// <summary>
		/// The height of the pixel block in pixels.
		/// </summary>
		int Height { get; }
	}
}
