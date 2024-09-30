using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing.Picker;

public class PickableFeatureClassItem : PickableFeatureClassItemBase
{
	private BitmapImage _image;

	public PickableFeatureClassItem(Dataset dataset,
	                                IReadOnlyList<Feature> features) :
		base(dataset, features) { }

	[NotNull]
	public override ImageSource ImageSource
	{
		get
		{
			BitmapImage image = _image;

			if (image != null)
			{
				return _image;
			}

			// todo daro refactor, unkown image
			BasicFeatureLayer layer = Assert.NotNull(Layers.FirstOrDefault());

			_image = new BitmapImage(PickerUtils.GetImagePath(layer.ShapeType));

			return _image;
		}
	}
}
