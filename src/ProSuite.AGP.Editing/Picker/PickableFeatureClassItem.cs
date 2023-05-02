using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using Geometry = ArcGIS.Core.Geometry.Geometry;

namespace ProSuite.AGP.Editing.Picker
{
	public class PickableFeatureClassItem : PropertyChangedBase, IPickableFeatureClassItem
	{
		private readonly string _featureClassName;
		private BitmapImage _image;
		private bool _selected;

		/// <summary>
		/// Has to be called on MCT
		/// </summary>
		public PickableFeatureClassItem(FeatureClass featureClass,
		                                IReadOnlyList<long> oids)
		{
			_featureClassName = featureClass.GetName();
			Oids = oids;
		}

		public IReadOnlyList<long> Oids { get; }

		// todo daro get feature shapes and union
		public Geometry Geometry { get; }

		public List<BasicFeatureLayer> Layers { get; } = new List<BasicFeatureLayer>();

		public bool Selected
		{
			get => _selected;
			set => SetProperty(ref _selected, value);
		}

		public string DisplayValue => ToString();

		[NotNull]
		public ImageSource ImageSource
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

		public double Score { get; set; }

		public override string ToString()
		{
			return $"{_featureClassName}: #{Oids.Count}";
		}
	}
}
