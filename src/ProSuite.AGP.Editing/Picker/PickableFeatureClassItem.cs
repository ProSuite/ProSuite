using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Picker
{
	public class PickableFeatureClassItem: IPickableItem
	{
		private string _itemText;
		private bool _isSelected;
		private Geometry _geometry;
		private Uri _itemImageUri;

		public PickableFeatureClassItem(FeatureClass featureClass, esriGeometryType geometryType)
		{
			_itemText = featureClass.GetName();
			_geometry = null;
			ItemImageUri = GetImagePath(geometryType);
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

		public Uri ItemImageUri
		{
			get => _itemImageUri;
			set => _itemImageUri = value;
		}

		
		private static Uri GetImagePath(esriGeometryType geometryType)
		{
			if (geometryType == esriGeometryType.esriGeometryPoint)
			{
				return new Uri("pack://application:,,,/ProSuite.AGP.Editing;component/Images/PointGeometry.bmp");
			}

			if (geometryType == esriGeometryType.esriGeometryPolyline)
			{
				return new Uri("pack://application:,,,/ProSuite.AGP.Editing;component/Images/LineGeometry.bmp");
			}

			if (geometryType == esriGeometryType.esriGeometryPolygon)
			{
				return new Uri("pack://application:,,,/ProSuite.AGP.Editing;component/Images/PolygonGeometry.bmp");
			}

			return new Uri("");
		}

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
