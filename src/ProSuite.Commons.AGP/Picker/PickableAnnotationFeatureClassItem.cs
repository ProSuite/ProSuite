using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ProSuite.Commons.Essentials.CodeAnnotations;
using Geometry = ArcGIS.Core.Geometry.Geometry;

namespace ProSuite.Commons.AGP.Picker
{
	public class PickableAnnotationFeatureClassItem : PickableFeatureClassItemBase
	{
		private BitmapImage _image;

		public PickableAnnotationFeatureClassItem(string datasetName, IReadOnlyList<long> oids,
		                                          Geometry geometry) :
			base(datasetName, oids, geometry) { }

		[NotNull]
		public override ImageSource ImageSource =>
			_image ??=
				new BitmapImage(
					new Uri(
						@"pack://application:,,,/ProSuite.AGP.Editing;component/PickerUI/Images/Annotation.png"));
	}
}
