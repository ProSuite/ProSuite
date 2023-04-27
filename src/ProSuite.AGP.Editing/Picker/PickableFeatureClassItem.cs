using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;
using Geometry = ArcGIS.Core.Geometry.Geometry;

namespace ProSuite.AGP.Editing.Picker
{
	public class PickableFeatureClassItem : IPickableItem
	{
		private readonly string _itemText;
		private bool _isSelected;
		[CanBeNull] private Geometry _geometry;
		private readonly Uri _itemImageUri;
		private List<BasicFeatureLayer> _belongingFeatureLayers;
		private BitmapImage _img;

		public PickableFeatureClassItem(FeatureClass featureClass, esriGeometryType geometryType,
		                                List<BasicFeatureLayer> belongingFeatureLayers)
		{
			_itemText = featureClass.GetName();
			_geometry = null;
			_itemImageUri = GetImagePath(geometryType);
			BelongingFeatureLayers = belongingFeatureLayers;
		}

		public string ItemText => _itemText;

		public bool IsSelected
		{
			get => _isSelected;
			set => _isSelected = value;
		}

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

		public int Score { get; set; }

		public List<BasicFeatureLayer> BelongingFeatureLayers
		{
			get => _belongingFeatureLayers;
			set => _belongingFeatureLayers = value;
		}

		private static Uri GetImagePath(esriGeometryType geometryType)
		{
			switch (geometryType)
			{
				case esriGeometryType.esriGeometryPoint:
				case esriGeometryType.esriGeometryMultipoint:
					return new Uri(
						@"pack://application:,,,/ProSuite.AGP.Editing;component/PickerUI/Images/PointGeometry.bmp");
				case esriGeometryType.esriGeometryLine:
				case esriGeometryType.esriGeometryPolyline:
					return new Uri(
						@"pack://application:,,,/ProSuite.AGP.Editing;component/PickerUI/Images/LineGeometry.bmp");
				case esriGeometryType.esriGeometryPolygon:
					return new Uri(
						@"pack://application:,,,/ProSuite.AGP.Editing;component/PickerUI/Images/PolygonGeometry.bmp",
						UriKind.Absolute);
				case esriGeometryType.esriGeometryMultiPatch:
					return new Uri(
						@"pack://application:,,,/ProSuite.AGP.Editing;component/PickerUI/Images/MultipatchGeometry.bmp");
				default:
					throw new ArgumentOutOfRangeException(
						$"Unsupported geometry type: {geometryType}");
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
