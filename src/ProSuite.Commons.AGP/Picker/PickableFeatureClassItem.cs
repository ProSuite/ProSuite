using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using Geometry = ArcGIS.Core.Geometry.Geometry;

namespace ProSuite.Commons.AGP.Picker;

public class PickableFeatureClassItem : PickableFeatureClassItemBase
{
	private BitmapImage _image;

	public PickableFeatureClassItem([NotNull] string datasetName,
	                                [NotNull] IReadOnlyList<long> oids,
	                                [NotNull] Geometry geometry) :
		base(datasetName, oids, geometry) { }

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

			// todo: daro refactor, unkown image
			BasicFeatureLayer layer = Assert.NotNull(Layers.FirstOrDefault());

			_image = new BitmapImage(PickerUtils.GetImagePath(layer.ShapeType));

			return _image;
		}
	}
}
