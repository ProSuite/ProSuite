using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Clients.AGP.ProSuiteSolution.Commons
{
	public class ImageUtils
	{
		public static ImageSource GetImageSource(string imageName)
		{
			var image = new BitmapImage(ResourceAccessor.Get($"Images/{imageName}"));
			return image;
		}
	}

	internal static class ResourceAccessor
	{
		public static Uri Get(string resourcePath)
		{
			var uri = string.Format(
				"pack://application:,,,/{0};component/{1}"
				, Assembly.GetExecutingAssembly().GetName().Name
				, resourcePath
			);

			return new Uri(uri, UriKind.Absolute);
		}
	}
}
