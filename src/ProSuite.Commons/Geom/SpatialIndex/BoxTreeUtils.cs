using System;
using System.Collections.Generic;
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

		[CanBeNull]
		public static BoxTree<T> CreateBoxTree<T>([CanBeNull] IEnumerable<T> items,
		                                          Func<T, Box> getBox,
		                                          int maxElementsPerTile = 64,
		                                          bool dynamic = true)
		{
			if (items == null)
			{
				return null;
			}

			Box allBox = null;
			List<KeyValuePair<IBox, T>> itemList = new List<KeyValuePair<IBox, T>>();
			foreach (var item in items)
			{
				Box box = getBox(item);
				if (box == null)
				{
					continue;
				}

				itemList.Add(new KeyValuePair<IBox, T>(box, item));
				if (allBox == null)
				{
					allBox = box.Clone();
				}
				else
				{
					allBox.Include(box);
				}
			}

			if (allBox == null)
			{
				return null;
			}

			var result = CreateBoxTree<T>(allBox, maxElementsPerTile, dynamic);

			foreach (KeyValuePair<IBox, T> pair in itemList)
			{
				result.Add(pair.Key, pair.Value);
			}

			return result;
		}
	}
}
