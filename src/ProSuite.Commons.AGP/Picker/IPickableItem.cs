using System.Windows.Media;
using ProSuite.Commons.Essentials.CodeAnnotations;
using Geometry = ArcGIS.Core.Geometry.Geometry;

namespace ProSuite.Commons.AGP.Picker
{
	public interface IPickableItem
	{
		[NotNull]
		string DisplayValue { get; }

		bool Selected { get; set; }

		[NotNull]
		Geometry Geometry { get; }

		[NotNull]
		ImageSource ImageSource { get; }

		double Score { get; set; }
	}
}
