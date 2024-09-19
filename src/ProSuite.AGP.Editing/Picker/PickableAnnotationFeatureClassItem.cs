using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing.Picker;

public class PickableAnnotationFeatureClassItem : PickableFeatureClassItemBase
{
	private BitmapImage _image;

	public PickableAnnotationFeatureClassItem(Dataset dataset, IReadOnlyList<Feature> features) :
		base(dataset, features) { }

	[NotNull]
	public override ImageSource ImageSource =>
		_image ??=
			new BitmapImage(
				new Uri(
					@"pack://application:,,,/ProSuite.AGP.Editing;component/PickerUI/Images/Annotation.png"));
}
