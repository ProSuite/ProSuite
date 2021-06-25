using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom.SpatialIndex
{
	public static class BoxTreeUtils
	{
		[NotNull]
		public static BoxTree<T> CreateBoxTree<T>(double xmin, double ymin,
		                                          double xmax, double ymax,
		                                          int maxElementsPerTile = 64,
		                                          bool dynamic = true)
		{
			Box box = GeomUtils.CreateBox(xmin, ymin, xmax, ymax);

			return CreateBoxTree<T>(box, maxElementsPerTile, dynamic);
		}

		[NotNull]
		public static BoxTree<T> CreateBoxTree<T>([NotNull] IBox initialBox,
		                                          int maxElementsPerTile = 64,
		                                          bool dynamic = true)
		{
			Assert.ArgumentNotNull(initialBox, nameof(initialBox));

			var result = new BoxTree<T>(initialBox.Dimension, maxElementsPerTile, dynamic);

			result.InitSize(new IGmtry[] {initialBox});

			return result;
		}
	}
}