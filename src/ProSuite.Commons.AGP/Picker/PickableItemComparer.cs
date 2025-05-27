using System.Collections.Generic;
using ArcGIS.Core.Geometry;

namespace ProSuite.Commons.AGP.Picker
{
	public class PickableItemComparer : IComparer<IPickableItem>
	{
		public int Compare(IPickableItem x, IPickableItem y)
		{
			if (x == y)
			{
				return 0;
			}

			if (x == null)
			{
				return -1;
			}

			if (y == null)
			{
				return 1;
			}

			if (x.Geometry is { GeometryType: GeometryType.Polygon } &&
			    y.Geometry is { GeometryType: GeometryType.Polygon })
			{
				if (x is PickableAnnotationFeatureItem &&
				    y is PickableAnnotationFeatureItem)
				{
					return 0;
				}

				if (x is PickableAnnotationFeatureItem)
				{
					return -1;
				}

				if (y is PickableAnnotationFeatureItem)
				{
					return 1;
				}
			}

			if (x.Score < y.Score)
			{
				return -1;
			}

			if (x.Score > y.Score)
			{
				return 1;
			}

			return 0;
		}
	}
}
