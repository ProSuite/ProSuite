using System.Windows.Media;
using ProSuite.Commons.Essentials.CodeAnnotations;
using Geometry = ArcGIS.Core.Geometry.Geometry;

namespace ProSuite.Commons.AGP.Picker
{
	public interface IPickableItem
	{
		string DisplayValue { get; }
		
		bool Selected { get; set; }

		[CanBeNull]
		Geometry Geometry { get; }

		ImageSource ImageSource { get; }

		double Score { get; set; }
	}
}
