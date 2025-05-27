using System.Windows.Media;
using Geometry = ArcGIS.Core.Geometry.Geometry;

namespace ProSuite.Commons.AGP.Picker
{
	public interface IPickableItem
	{
		string DisplayValue { get; }
		
		bool Selected { get; set; }

		Geometry Geometry { get; }

		ImageSource ImageSource { get; }

		double Score { get; set; }
	}
}
