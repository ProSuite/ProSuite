using System;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ProSuite.AGP.Solution.Commons
{
	public static class ImageUtils
	{
		public static ImageSource GetImageSource(string imageName)
		{
			return new BitmapImage(ResourceAccessor.Get($"Images/{imageName}"));
		}
	}

	internal static class ResourceAccessor
	{
		public static Uri Get(string resourcePath)
		{
			string uri = string.Format(
				"pack://application:,,,/{0};component/{1}",
				Assembly.GetExecutingAssembly().GetName().Name,
				resourcePath
			);

			return new Uri(uri, UriKind.Absolute);
		}
	}
}
