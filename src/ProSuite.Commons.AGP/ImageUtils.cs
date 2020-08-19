using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ProSuite.Commons.AGP
{
	public class ImageUtils
	{
		public static ImageSource GetImageSource(string imageName)
		{
			return new BitmapImage(new Uri($"pack://application:,,,/AssemblyName;component/{imageName}"));
		}
	}
}
