using ArcGIS.Core.CIM;

namespace ProSuite.Commons.AGP.Core.Carto
{
	/// <summary>
	/// Utilities for creating CIM colors
	/// </summary>
	public static class ColorUtils
	{
		public static CIMColor BlackRGB => CreateRGB(0, 0, 0);
		public static CIMColor RedRGB => CreateRGB(255, 0, 0);
		public static CIMColor GreenRGB => CreateRGB(0, 255, 0);
		public static CIMColor BlueRGB => CreateRGB(0, 0, 255);
		public static CIMColor GrayRGB => CreateRGB(127, 127, 127);
		public static CIMColor WhiteRGB => CreateRGB(255, 255, 255);

		public static CIMRGBColor CreateRGB(int red, int green, int blue, int alpha = 100)
		{
			return new CIMRGBColor {R = red, G = green, B = blue, Alpha = alpha};
		}
	}
}
