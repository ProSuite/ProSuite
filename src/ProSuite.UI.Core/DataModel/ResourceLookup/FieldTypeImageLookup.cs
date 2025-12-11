using System.Collections.Generic;
using System.Drawing;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.UI.Core.Properties;

namespace ProSuite.UI.Core.DataModel.ResourceLookup
{
	public static class FieldTypeImageLookup
	{
		[CanBeNull] private static IDictionary<FieldType, FieldTypeImage> _imageMap;

		[NotNull] private static readonly FieldTypeImage _unknownFieldTypeImage =
			new FieldTypeImage(98, FieldTypeImages.FieldTypeUnknown, "fld.unknown");

		[NotNull] private static readonly FieldTypeImage _deletedFieldTypeImage =
			new FieldTypeImage(99, FieldTypeImages.FieldTypeDeleted, "fld.deleted");

		[NotNull]
		public static Image GetImage([NotNull] Attribute attribute)
		{
			return GetImage(attribute, out string _);
		}

		[NotNull]
		public static Image GetImage([NotNull] Attribute attribute,
		                             [NotNull] out string imageKey)
		{
			Assert.ArgumentNotNull(attribute, nameof(attribute));

			if (attribute.Deleted)
			{
				return GetDeletedImage(out imageKey);
			}

			// cache by attribute id if slow (only if not deleted, 
			// "deleted" can change during session)
			try
			{
				return GetImage(attribute.FieldType, out imageKey);
			}
			catch (ModelElementAccessException)
			{
				return GetUnknownImage(out imageKey);
			}
		}

		[NotNull]
		public static Image GetUnknownImage()
		{
			return GetUnknownImage(out string _);
		}

		[NotNull]
		public static Image GetUnknownImage([NotNull] out string imageKey)
		{
			imageKey = _unknownFieldTypeImage.Key;
			return _unknownFieldTypeImage.Image;
		}

		[NotNull]
		public static Image GetDeletedImage()
		{
			return GetDeletedImage(out string _);
		}

		[NotNull]
		public static Image GetDeletedImage([NotNull] out string imageKey)
		{
			imageKey = _deletedFieldTypeImage.Key;
			return _deletedFieldTypeImage.Image;
		}

		[NotNull]
		public static Image GetImage(FieldType fieldType)
		{
			return GetImage(fieldType, out string _);
		}

		[NotNull]
		public static Image GetImage(FieldType fieldType,
		                             [NotNull] out string imageKey)
		{
			if (_imageMap == null)
			{
				_imageMap = CreateImageMap();
			}

			if (! _imageMap.TryGetValue(fieldType, out FieldTypeImage fieldTypeImage))
			{
				return GetUnknownImage(out imageKey);
			}

			imageKey = fieldTypeImage.Key;
			return fieldTypeImage.Image;
		}

		[NotNull]
		private static IDictionary<FieldType, FieldTypeImage> CreateImageMap()
		{
			return new Dictionary<FieldType, FieldTypeImage>
			       {
				       {
					       FieldType.ObjectID,
					       new FieldTypeImage(0, FieldTypeImages.FieldTypeOID, "fld.oid")
				       },
				       {
					       FieldType.GlobalID,
					       new FieldTypeImage(1, FieldTypeImages.FieldTypeUUID, "fld.globid")
				       },
				       {
					       FieldType.Guid,
					       new FieldTypeImage(2, FieldTypeImages.FieldTypeUUID, "fld.guid")
				       },
				       {
					       FieldType.Integer,
					       new FieldTypeImage(3, FieldTypeImages.FieldTypeInteger, "fld.int")
				       },
					   // TODO: BigInteger
				       {
					       FieldType.ShortInteger,
					       new FieldTypeImage(4, FieldTypeImages.FieldTypeInteger, "fld.sint")
				       },
				       {
					       FieldType.Text,
					       new FieldTypeImage(5, FieldTypeImages.FieldTypeText, "fld.text")
				       },
				       {
					       FieldType.Date,
					       new FieldTypeImage(6, FieldTypeImages.FieldTypeDateTime, "fld.date")
				       },
				       {
					       FieldType.Double,
					       new FieldTypeImage(7, FieldTypeImages.FieldTypeFloat, "fld.dbl")
				       },
				       {
					       FieldType.Float,
					       new FieldTypeImage(8, FieldTypeImages.FieldTypeFloat, "fld.sngl")
				       },
				       {
					       FieldType.Xml,
					       new FieldTypeImage(9, FieldTypeImages.FieldTypeXml, "fld.xml")
				       },
				       {
					       FieldType.Blob,
					       new FieldTypeImage(10, FieldTypeImages.FieldTypeBinary, "fld.blob")
				       },
				       {
					       FieldType.Geometry,
					       new FieldTypeImage(11, FieldTypeImages.FieldTypeShape, "fld.shp")
				       },
				       {
					       FieldType.Raster,
					       new FieldTypeImage(12, FieldTypeImages.FieldTypeRaster, "fld.raster")
				       }
			       };
		}

		#region Nested type: FieldTypeImage

		private class FieldTypeImage
		{
			public FieldTypeImage(int defaultSort, [NotNull] Image image, [NotNull] string key)
			{
				Assert.ArgumentNotNullOrEmpty(key, nameof(key));
				Assert.ArgumentNotNull(image, nameof(image));

				Key = key;
				Image = image;

				Image.Tag = defaultSort;
			}

			[NotNull]
			public string Key { get; }

			[NotNull]
			public Image Image { get; }
		}

		#endregion
	}
}
