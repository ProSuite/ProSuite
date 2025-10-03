using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.Commons.AO.Raster
{
	public class RasterBlockDefinition
	{
		public int TopLeftCol { get; set; }
		public int TopLeftRow { get; set; }

		public int Width { get; set; }
		public int Height { get; set; }

		public IPnt GetBlockSize()
		{
			IPnt blockSize = new PntClass();
			blockSize.SetCoords(Width, Height);

			return blockSize;
		}

		public IPnt GetTopLeftCorner()
		{
			IPnt tlc = new PntClass();
			tlc.SetCoords(TopLeftCol, TopLeftRow);

			return tlc;
		}

		public override string ToString()
		{
			return
				$"TopLeft: {TopLeftCol}|{TopLeftRow}, width: {Width}, height: {Height}";
		}
	}
}
