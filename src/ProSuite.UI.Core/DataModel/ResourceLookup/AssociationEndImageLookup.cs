using System.Collections.Generic;
using System.Drawing;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.UI.Core.Properties;

namespace ProSuite.UI.Core.DataModel.ResourceLookup
{
	public static class AssociationEndImageLookup
	{
		private const string _imageKeyDeleted = "ase.deleted";
		private const string _imageKeyUnknown = "ase.unknown";
		[CanBeNull] private static Dictionary<AssociationEndType, AssociationEndImage> _imageMap;

		[NotNull]
		public static Image GetImage([NotNull] AssociationEnd associationEnd)
		{
			return GetImage(associationEnd, out string _);
		}

		[NotNull]
		public static Image GetImage([NotNull] AssociationEnd associationEnd,
		                             [NotNull] out string imageKey)
		{
			Assert.ArgumentNotNull(associationEnd, nameof(associationEnd));

			if (associationEnd.Deleted)
			{
				imageKey = _imageKeyDeleted;
				return AssociationEndImages.AssociationEndDeleted;
			}

			// cache by attribute id if slow (only if not deleted, 
			// "deleted" can change during session)
			return GetImage(associationEnd.AssociationEndType, out imageKey);
		}

		[NotNull]
		public static Image GetImage(AssociationEndType associationEndType,
		                             [NotNull] out string imageKey)
		{
			if (_imageMap == null)
			{
				_imageMap = CreateImageMap();
			}

			if (_imageMap.TryGetValue(associationEndType,
			                          out AssociationEndImage associationEndImage))
			{
				imageKey = associationEndImage.Key;
				return associationEndImage.Image;
			}

			imageKey = _imageKeyUnknown;
			return AssociationEndImages.AssociationEndUnknown;
		}

		[NotNull]
		private static Dictionary<AssociationEndType, AssociationEndImage> CreateImageMap()
		{
			return new Dictionary<AssociationEndType, AssociationEndImage>
			       {
				       {
					       AssociationEndType.Unknown,
					       new AssociationEndImage(0, AssociationEndImages.AssociationEndUnknown,
					                               "ase.unk")
				       },
				       {
					       AssociationEndType.OneToOneFK,
					       new AssociationEndImage(1, AssociationEndImages.AssociationEndOneToOneFK,
					                               "ase.1->1")
				       },
				       {
					       AssociationEndType.OneToOnePK,
					       new AssociationEndImage(2, AssociationEndImages.AssociationEndOneToOnePK,
					                               "ase.1<-1")
				       },
				       {
					       AssociationEndType.ManyToOne,
					       new AssociationEndImage(3, AssociationEndImages.AssociationEndManyToOne,
					                               "ase.n:1")
				       },
				       {
					       AssociationEndType.OneToMany,
					       new AssociationEndImage(4, AssociationEndImages.AssociationEndOneToMany,
					                               "ase.1:n")
				       },
				       {
					       AssociationEndType.ManyToManyEnd1,
					       new AssociationEndImage(
						       5, AssociationEndImages.AssociationEndManyToMany1, "ase.n:m")
				       },
				       {
					       AssociationEndType.ManyToManyEnd2,
					       new AssociationEndImage(
						       6, AssociationEndImages.AssociationEndManyToMany2, "ase.m:n")
				       }
			       };
		}

		#region Nested type: AssociationEndImage

		private class AssociationEndImage
		{
			public AssociationEndImage(int defaultSort, [NotNull] Image image, [NotNull] string key)
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
