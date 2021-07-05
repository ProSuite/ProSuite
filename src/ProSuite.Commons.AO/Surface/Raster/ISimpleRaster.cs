using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;

namespace ProSuite.Commons.AO.Surface.Raster
{
	public interface ISimpleRaster : IDisposable, IEquatable<ISimpleRaster>
	{
		/// <summary>
		/// The X coordinate of the origin (i.e. top left of the raster extent) in georeferenced space.
		/// </summary>
		double OriginX { get; }

		/// <summary>
		/// The Y coordinate of the origin (i.e. top left of the raster extent) in georeferenced space.
		/// </summary>
		double OriginY { get; }

		int Width { get; set; }

		int Height { get; set; }

		/// <summary>
		/// The east-west pixel resolution / cell size
		/// </summary>
		double PixelSizeX { get; }

		/// <summary>
		/// The north-south pixel resolution / cell size
		/// </summary>
		double PixelSizeY { get; }

		object NoDataValue { get; set; }

		[NotNull]
		ISimplePixelBlock<T> CreatePixelBlock<T>(int bufferSizeX, int bufferSizeY);

		void ReadPixelBlock<T>(int pixelOffsetX, int pixelOffsetY,
		                       [NotNull] ISimplePixelBlock<T> simplePixelBlock,
		                       int nPixelSpace = 0, int nLineSpace = 0);

		EnvelopeXY GetEnvelope();
	}
}
