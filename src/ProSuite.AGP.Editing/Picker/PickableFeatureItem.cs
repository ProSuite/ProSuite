using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using Geometry = ArcGIS.Core.Geometry.Geometry;

namespace ProSuite.AGP.Editing.Picker
{
	public class PickableFeatureItem : IPickableFeatureItem
	{
		public PickableFeatureItem(MapMember mapMember, Feature feature, string text)
		{
			_feature = feature;
			_basicFeatureLayer = mapMember as BasicFeatureLayer;
			Oid = feature.GetObjectID();
			_displayValue = text;
			Geometry = feature.GetShape();
			_itemImageUri = GetImagePath(Geometry);
		}

		private readonly BasicFeatureLayer _basicFeatureLayer;
		private readonly Feature _feature;
		private bool _isSelected;
		private Geometry _geometry;
		private Uri _itemImageUri;
		private BitmapImage _img;
		private readonly string _displayValue;

		private static Uri GetImagePath(Geometry geometry)
		{
			switch (geometry.GeometryType)
			{
				case GeometryType.Point:
				case GeometryType.Multipoint:
					return new Uri(
						@"pack://application:,,,/ProSuite.AGP.Editing;component/PickerUI/Images/PointGeometry.bmp");
				case GeometryType.Polyline:
					return new Uri(
						@"pack://application:,,,/ProSuite.AGP.Editing;component/PickerUI/Images/LineGeometry.bmp");
				case GeometryType.Polygon:
					return new Uri(
						@"pack://application:,,,/ProSuite.AGP.Editing;component/PickerUI/Images/PolygonGeometry.bmp");
				case GeometryType.Multipatch:
					return new Uri(
						@"pack://application:,,,/ProSuite.AGP.Editing;component/PickerUI/Images/MultipatchGeometry.bmp");
				default:
					throw new ArgumentOutOfRangeException(
						$"Unsupported geometry type: {geometry.GeometryType}");
			}
		}

		public bool IsSelected
		{
			get => _isSelected;
			set
			{
				_isSelected = value;
				OnPropertyChanged("IsSelected");
			}
		}

		public long Oid { get; }

		// todo daro to DisplayValue
		public string ItemText => ToString();

		public string LayerName => _basicFeatureLayer.Name;

		public Geometry Geometry
		{
			get => _geometry;
			set => _geometry = value;
		}

		public ImageSource ItemImageSource
		{
			get
			{
				if (_img == null)
				{
					_img = new BitmapImage(_itemImageUri);
				}

				return _img;
			}
		}

		public double Score { get; set; }

		public BasicFeatureLayer Layer => _basicFeatureLayer;

		public Feature Feature => _feature;

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(
			[CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public override string ToString()
		{
			return $"{_displayValue} - {Score}";
		}
	}
}
