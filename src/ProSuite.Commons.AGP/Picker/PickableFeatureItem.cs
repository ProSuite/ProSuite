using System.Windows.Media;
using System.Windows.Media.Imaging;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;
using Geometry = ArcGIS.Core.Geometry.Geometry;

namespace ProSuite.Commons.AGP.Picker;

public class PickableFeatureItem : PickableFeatureItemBase
{
	private BitmapImage _image;

	public PickableFeatureItem([NotNull] BasicFeatureLayer layer,
	                           [NotNull] Feature feature,
	                           [NotNull] Geometry geometry,
	                           long oid,
	                           string displayValue)
		: base(layer, feature, geometry, oid, displayValue) { }

	public override ImageSource ImageSource =>
		_image ??= new BitmapImage(PickerUtils.GetImagePath(Layer.ShapeType));
}
