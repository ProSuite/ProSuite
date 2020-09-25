using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using Geometry = ArcGIS.Core.Geometry.Geometry;

namespace ProSuite.AGP.Editing.Picker
{
	public class PickableFeatureItem : IPickableItem
	{
		public PickableFeatureItem(MapMember mapMember, Feature feature, string text)
		{
			_feature = feature;
			_featureLayer = mapMember as FeatureLayer;
			Oid = feature.GetObjectID();
			ItemText = text;
			QueuedTask.Run(() =>
			{
				Geometry = feature.GetShape();
				ItemImageUri = GetImagePath(Geometry);
			});
		}

		private readonly FeatureLayer _featureLayer;
		private readonly Feature _feature;
		private bool _isSelected;
		private Geometry _geometry;
		private Uri _itemImage;

		private static Uri GetImagePath(Geometry geometry)
		{
			if (geometry.GeometryType == GeometryType.Point)
			{
				return new Uri(
					"pack://application:,,,/ProSuite.AGP.Editing;component/Images/PointGeometry.bmp");
			}

			if (geometry.GeometryType == GeometryType.Polyline)
			{
				return new Uri(
					"pack://application:,,,/ProSuite.AGP.Editing;component/Images/LineGeometry.bmp");
			}

			if (geometry.GeometryType == GeometryType.Polygon)
			{
				return new Uri(
					"pack://application:,,,/ProSuite.AGP.Editing;component/Images/PolygonGeometry.bmp");
			}

			return new Uri("");
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

		public string ItemText { get; }

		public string LayerName => _featureLayer.Name;

		public Geometry Geometry
		{
			get => _geometry;
			set => _geometry = value;
		}

		public Uri ItemImageUri
		{
			get => _itemImage;
			set => _itemImage = value;
		}

		public FeatureLayer Layer => _featureLayer;

		public Feature Feature => _feature;

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(
			[CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
