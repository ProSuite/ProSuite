using System.Windows.Media;
using System.Windows.Media.Imaging;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using Geometry = ArcGIS.Core.Geometry.Geometry;

namespace ProSuite.AGP.Editing.Picker
{
	public class PickableFeatureItem : PropertyChangedBase, IPickableFeatureItem
	{
		private readonly string _displayValue;
		private BitmapImage _image;
		private bool _selected;

		/// <summary>
		/// Has to be called on MCT
		/// </summary>
		public PickableFeatureItem(BasicFeatureLayer layer, Feature feature)
		{
			Feature = feature;
			Layer = layer;
			Oid = feature.GetObjectID();
			Geometry = feature.GetShape();
			_displayValue = $"{GdbObjectUtils.GetDisplayValue(Feature, Layer.Name)}";
		}

		[NotNull]
		public Feature Feature { get; }

		public long Oid { get; }

		[NotNull]
		public Geometry Geometry { get; }

		[NotNull]
		public BasicFeatureLayer Layer { get; }

		public bool Selected
		{
			get => _selected;
			set => SetProperty(ref _selected, value);
		}

		[NotNull]
		public string DisplayValue => ToString();

		[NotNull]
		public ImageSource ImageSource =>
			_image ?? (_image = new BitmapImage(PickerUtils.GetImagePath(Layer.ShapeType)));

		public double Score { get; set; }

		public override string ToString()
		{
			// TODO: Alternatively allow using layer.QueryDisplayExpressions. But typically this is just the OID which is not very useful -> Requires configuration
			// string[] displayExpressions = layer.QueryDisplayExpressions(new[] { feature.GetObjectID() });

			return $"{_displayValue}";
		}
	}
}
