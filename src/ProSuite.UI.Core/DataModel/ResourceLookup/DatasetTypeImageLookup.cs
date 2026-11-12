using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom.EsriShape;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.UI.Core.Properties;

namespace ProSuite.UI.Core.DataModel.ResourceLookup
{
	/// <summary>
	/// Provides the icons for the various dataset types.
	/// </summary>
	public static class DatasetTypeImageLookup
	{
		private const string _keyDeleted = "deleted";
		private const string _keyGeometryNetwork = "network";
		private const string _keyMultipatch = "multipatch";
		private const string _keyPoint = "point";
		private const string _keyPolygon = "polygon";
		private const string _keyPolyline = "polyline";
		private const string _keyTable = "table";
		private const string _keyTerrain = "terrain";
		private const string _keyTopology = "topology";
		private const string _keyMosaicDataset = "mosaicdataset";
		private const string _keyRasterDataset = "rasterdataset";
		private const string _keyTransformer = "transformer";
		private const string _keyUnknown = "unknown";

		private static readonly SortedList<string, int> _defaultSort =
			new SortedList<string, int>();

		private static readonly Dictionary<Image, string> _mapImageToKey =
			new Dictionary<Image, string>();

		private static readonly SortedList<string, Image> _mapKeyToImage =
			new SortedList<string, Image>();

		#region Constructor

		/// <summary>
		/// Initializes the <see cref="DatasetTypeImageLookup"/> class.
		/// </summary>
		static DatasetTypeImageLookup()
		{
			_mapKeyToImage.Add(_keyDeleted, DatasetTypeImages.DatasetTypeDeleted);
			_mapKeyToImage.Add(_keyUnknown, DatasetTypeImages.DatasetTypeUnknown);
			_mapKeyToImage.Add(_keyGeometryNetwork,
			                   DatasetTypeImages.DatasetTypeGeometricNetwork);
			_mapKeyToImage.Add(_keyPolyline, DatasetTypeImages.DatasetTypeLine);
			_mapKeyToImage.Add(_keyMultipatch, DatasetTypeImages.DatasetTypeMultipatch);
			_mapKeyToImage.Add(_keyPoint, DatasetTypeImages.DatasetTypePoint);
			_mapKeyToImage.Add(_keyTable, DatasetTypeImages.DatasetTypeTable);
			_mapKeyToImage.Add(_keyTerrain, DatasetTypeImages.DatasetTypeTerrain);
			_mapKeyToImage.Add(_keyTopology, DatasetTypeImages.DatasetTypeTopology);
			_mapKeyToImage.Add(_keyPolygon, DatasetTypeImages.DatasetTypePolygon);
			_mapKeyToImage.Add(_keyMosaicDataset, DatasetTypeImages.DatasetTypeMosaicDataset);
			_mapKeyToImage.Add(_keyRasterDataset, DatasetTypeImages.DatasetTypeRasterDataset);
			_mapKeyToImage.Add(_keyTransformer, TestTypeImages.Transformer);

			foreach (string key in _mapKeyToImage.Keys.ToList())
			{
				_mapKeyToImage[key] = ScaleToDpi(_mapKeyToImage[key]);
			}

			foreach (KeyValuePair<string, Image> pair in _mapKeyToImage)
			{
				_mapImageToKey.Add(pair.Value, pair.Key);
			}

			int i = 0;
			_defaultSort.Add(_keyDeleted, ++i);
			_defaultSort.Add(_keyUnknown, ++i);
			_defaultSort.Add(_keyTable, ++i);
			_defaultSort.Add(_keyPoint, ++i);
			_defaultSort.Add(_keyPolyline, ++i);
			_defaultSort.Add(_keyPolygon, ++i);
			_defaultSort.Add(_keyMultipatch, ++i);
			_defaultSort.Add(_keyGeometryNetwork, ++i);
			_defaultSort.Add(_keyTopology, ++i);
			_defaultSort.Add(_keyTerrain, ++i);
			_defaultSort.Add(_keyMosaicDataset, ++i);
			_defaultSort.Add(_keyRasterDataset, ++i);
			_defaultSort.Add(_keyTransformer, ++i);
		}

		#endregion

		/// <summary>
		/// The icon size in pixels for the screen DPI: 16 at 100 %, 24 at 150 %.
		/// </summary>
		public static int ImageSize { get; } = GetImageSize();

		/// <summary>
		/// Returns a copy of the icon at <see cref="ImageSize"/>. The dataset type icons are
		/// stored large, and grid image columns draw images at their own size.
		/// </summary>
		[NotNull]
		public static Image ScaleToDpi([NotNull] Image image)
		{
			var scaled = new Bitmap(ImageSize, ImageSize);

			using (Graphics graphics = Graphics.FromImage(scaled))
			{
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
				graphics.DrawImage(image, 0, 0, ImageSize, ImageSize);
			}

			return scaled;
		}

		private static int GetImageSize()
		{
			// ponytail: DPI at startup; a window moved to a monitor with another scale keeps it
			using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
			{
				return (int) Math.Round(16 * graphics.DpiX / 96);
			}
		}

		[NotNull]
		public static ImageList CreateImageList(bool addSortTag = false)
		{
			var imageList = new ImageList
			                {
				                ImageSize = new Size(ImageSize, ImageSize),
				                ColorDepth = ColorDepth.Depth32Bit
			                };

			foreach (KeyValuePair<string, Image> pair in _mapKeyToImage)
			{
				string key = pair.Key;

				Image image = pair.Value;
				if (addSortTag)
				{
					image.Tag = GetDefaultSortIndex(key);
				}

				imageList.Images.Add(key, image);
			}

			return imageList;
		}

		[NotNull]
		public static string GetImageKey([NotNull] IDdxDataset dataset)
		{
			return GetImageKey(GetImage(dataset));
		}

		[NotNull]
		public static Image GetImage([NotNull] string key)
		{
			return _mapKeyToImage.TryGetValue(key, out Image image)
				       ? image
				       : _mapKeyToImage[_keyUnknown];
		}

		[NotNull]
		public static Image GetImage([NotNull] IDdxDataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			return dataset.Deleted
				       ? GetImage(_keyDeleted)
				       : GetImage(dataset.GeometryType);
		}

		public static int GetDefaultSortIndex([NotNull] IDdxDataset dataset)
		{
			return GetDefaultSortIndex(GetImageKey(dataset));
		}

		public static int GetDefaultSortIndex([NotNull] string key)
		{
			return _defaultSort[key];
		}

		public static int GetDefaultSortIndex([NotNull] Image image)
		{
			return GetDefaultSortIndex(GetImageKey(image));
		}

		[NotNull]
		public static Image GetImage([CanBeNull] GeometryType geometryType)
		{
			if (geometryType is GeometryTypeTerrain)
			{
				return GetImage(_keyTerrain);
			}

			if (geometryType is GeometryTypeGeometricNetwork)
			{
				return GetImage(_keyGeometryNetwork);
			}

			if (geometryType is GeometryTypeTopology)
			{
				return GetImage(_keyTopology);
			}

			if (geometryType is GeometryTypeNoGeometry)
			{
				return GetImage(_keyTable);
			}

			if (geometryType is GeometryTypeShape geometryTypeShape)
			{
				return GetImage(geometryTypeShape.ShapeType);
			}

			if (geometryType is GeometryTypeRasterMosaic)
			{
				return GetImage(_keyMosaicDataset);
			}

			if (geometryType is GeometryTypeRasterDataset)
			{
				return GetImage(_keyRasterDataset);
			}

			return GetImage(_keyUnknown);
		}

		[NotNull]
		public static Image GetImage(ProSuiteGeometryType geometryType)
		{
			switch (geometryType)
			{
				case ProSuiteGeometryType.Point:
				case ProSuiteGeometryType.Multipoint:
					return GetImage(_keyPoint);

				case ProSuiteGeometryType.Polygon:
					return GetImage(_keyPolygon);

				case ProSuiteGeometryType.Polyline:
					return GetImage(_keyPolyline);

				case ProSuiteGeometryType.MultiPatch:
					return GetImage(_keyMultipatch);

				case ProSuiteGeometryType.Null:
					return GetImage(_keyTable);

				default:
					return GetImage(_keyUnknown);
			}
		}

		[NotNull]
		public static string GetImageKey([NotNull] Image image)
		{
			return _mapImageToKey.TryGetValue(image, out string key)
				       ? key
				       : _keyUnknown;
		}
	}
}
