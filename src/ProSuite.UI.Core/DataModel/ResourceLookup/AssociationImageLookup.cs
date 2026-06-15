using System.Collections.Generic;
using System.Drawing;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.UI.Core.Properties;

namespace ProSuite.UI.Core.DataModel.ResourceLookup
{
	public static class AssociationImageLookup
	{
		private const string _imageKeyDeleted = "asc.deleted";
		private const string _imageKeyUnknown = "asc.unknown";
		[CanBeNull] private static IDictionary<AssociationCardinality, AssociationImage> _imageMap;

		[NotNull]
		public static Image GetImage([NotNull] Association association)
		{
			return GetImage(association, out string _);
		}

		[NotNull]
		public static Image GetImage([NotNull] Association association,
		                             [NotNull] out string imageKey)
		{
			Assert.ArgumentNotNull(association, nameof(association));

			if (! association.Deleted)
			{
				// cache by attribute id if slow (only if not deleted, 
				// "deleted" can change during session)
				return GetImage(association.Cardinality, out imageKey);
			}

			imageKey = _imageKeyDeleted;
			return AssociationImages.AssociationDeleted;
		}

		public static Image GetImage(AssociationCardinality cardinality,
		                             [NotNull] out string imageKey)
		{
			if (_imageMap == null)
			{
				_imageMap = CreateImageMap();
			}

			if (_imageMap.TryGetValue(cardinality, out AssociationImage associationImage))
			{
				imageKey = associationImage.Key;
				return associationImage.Image;
			}

			imageKey = _imageKeyUnknown;
			return AssociationImages.AssociationUnknown;
		}

		[NotNull]
		private static IDictionary<AssociationCardinality, AssociationImage> CreateImageMap()
		{
			return new Dictionary<AssociationCardinality, AssociationImage>
			       {
				       {
					       AssociationCardinality.Unknown,
					       new AssociationImage(0, AssociationImages.AssociationUnknown, "asc.unk")
				       },
				       {
					       AssociationCardinality.OneToOne,
					       new AssociationImage(1, AssociationImages.AssociationOneToOne, "asc.1:1")
				       },
				       {
					       AssociationCardinality.OneToMany,
					       new AssociationImage(2, AssociationImages.AssociationManyToOne,
					                            "asc.1:n")
				       },
				       {
					       AssociationCardinality.ManyToMany,
					       new AssociationImage(3, AssociationImages.AssociationManyToMany,
					                            "asc.n:m")
				       }
			       };
		}

		#region Nested type: AssociationImage

		private class AssociationImage
		{
			public AssociationImage(int defaultSort, [NotNull] Image image, [NotNull] string key)
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
