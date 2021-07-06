using System.Collections.Generic;
using System.Drawing;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.UI.Properties;

namespace ProSuite.UI.DataModel.ResourceLookup
{
	public static class AssociationImageLookup
	{
		private const string _imageKeyDeleted = "asc.deleted";
		private const string _imageKeyUnknown = "asc.unknown";
		private static Dictionary<AssociationCardinality, AssociationImage> _imageMap;

		public static Image GetImage(Association association)
		{
			string imageKey;
			return GetImage(association, out imageKey);
		}

		public static Image GetImage(Association association, out string imageKey)
		{
			Assert.ArgumentNotNull(association, nameof(association));

			if (! association.Deleted)
			{
				// cache by attribute id if slow (only if not deleted, 
				// "deleted" can change during session)
				return GetImage(association.Cardinality, out imageKey);
			}

			// TODO special "deleted association" image
			imageKey = _imageKeyDeleted;
			return AssociationImages.AssociationUnknown;
		}

		public static Image GetImage(AssociationCardinality cardinality,
		                             out string imageKey)
		{
			if (_imageMap == null)
			{
				_imageMap = CreateImageMap();
			}

			AssociationImage associationImage;
			if (_imageMap.TryGetValue(cardinality, out associationImage))
			{
				imageKey = associationImage.Key;
				return associationImage.Image;
			}

			imageKey = _imageKeyUnknown;
			return AssociationImages.AssociationUnknown;
		}

		private static Dictionary<AssociationCardinality, AssociationImage> CreateImageMap
			()
		{
			var map =
				new Dictionary<AssociationCardinality, AssociationImage>();

			map.Add(AssociationCardinality.Unknown,
			        new AssociationImage(0, AssociationImages.AssociationUnknown,
			                             "asc.unk"));

			map.Add(AssociationCardinality.OneToOne,
			        new AssociationImage(1,
			                             AssociationImages.AssociationOneToOne,
			                             "asc.1:1"));

			map.Add(AssociationCardinality.OneToMany,
			        new AssociationImage(2,
			                             AssociationImages.AssociationManyToOne,
			                             "asc.1:n"));

			map.Add(AssociationCardinality.ManyToMany,
			        new AssociationImage(3,
			                             AssociationImages.AssociationManyToMany,
			                             "asc.n:m"));

			return map;
		}

		private class AssociationImage
		{
			public AssociationImage(int defaultSort, Image image, string key)
			{
				Assert.ArgumentNotNullOrEmpty(key, nameof(key));
				Assert.ArgumentNotNull(image, nameof(image));

				Key = key;
				Image = image;

				Image.Tag = defaultSort;
			}

			public string Key { get; }

			public Image Image { get; }
		}
	}
}
