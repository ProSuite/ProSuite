using System.Drawing;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.Core.Properties;

namespace ProSuite.UI.Core.QA.ResourceLookup
{
	public static class QualitySpecificationImageLookup
	{
		[NotNull] private static readonly Image _image;
		[NotNull] private static readonly Image _imageHidden;
		private const int _sortIndexNormal = 0;
		private const int _sortIndexHidden = 1;

		static QualitySpecificationImageLookup()
		{
			_image = QualitySpecificationImages.QualitySpecification;
			_imageHidden = QualitySpecificationImages.QualitySpecificationHidden;

			// for sorting
			_image.Tag = _sortIndexNormal;
			_imageHidden.Tag = _sortIndexHidden;
		}

		[NotNull]
		public static Image GetImage([NotNull] QualitySpecification qualitySpecification)
		{
			Assert.ArgumentNotNull(qualitySpecification, nameof(qualitySpecification));

			return qualitySpecification.Hidden
				       ? _imageHidden
				       : _image;
		}

		public static int GetDefaultSortIndex([NotNull] QualitySpecification qualitySpecification)
		{
			Assert.ArgumentNotNull(qualitySpecification, nameof(qualitySpecification));

			return qualitySpecification.Hidden
				       ? _sortIndexHidden
				       : _sortIndexNormal;
		}

		public static string GetImageKey([NotNull] QualitySpecification qualitySpecification)
		{
			Assert.ArgumentNotNull(qualitySpecification, nameof(qualitySpecification));

			return qualitySpecification.Hidden
				       ? "hidden"
				       : "normal";
		}
	}
}
