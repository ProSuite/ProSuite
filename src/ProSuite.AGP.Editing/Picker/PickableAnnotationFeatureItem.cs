using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;
using Geometry = ArcGIS.Core.Geometry.Geometry;

namespace ProSuite.AGP.Editing.Picker;

public class PickableAnnotationFeatureItem : PickableFeatureItemBase
{
	private BitmapImage _image;

	/// <summary>
	/// Has to be called on MCT
	/// </summary>
	public PickableAnnotationFeatureItem(BasicFeatureLayer layer, Feature feature,
	                                     Geometry geometry, long oid, string displayValue) :
		base(layer, feature, geometry, oid, displayValue) { }

	[NotNull]
	public override ImageSource ImageSource =>
		_image ??=
			new BitmapImage(
				new Uri(
					@"pack://application:,,,/ProSuite.AGP.Editing;component/PickerUI/Images/Annotation.png"));
}
