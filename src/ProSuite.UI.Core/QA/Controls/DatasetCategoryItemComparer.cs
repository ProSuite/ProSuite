using System.Collections.Generic;

namespace ProSuite.UI.Core.QA.Controls
{
	public class DatasetCategoryItemComparer : IComparer<DatasetCategoryItem>
	{
		#region IComparer<DatasetCategoryItem> Members

		public int Compare(DatasetCategoryItem x, DatasetCategoryItem y)
		{
			if (x == null && y == null)
			{
				return 0;
			}

			if (x == null)
			{
				return 1;
			}

			if (y == null)
			{
				return -1;
			}

			if (x.IsNull && y.IsNull)
			{
				return 0;
			}

			if (x.IsNull)
			{
				return 1;
			}

			if (y.IsNull)
			{
				return -1;
			}

			return string.CompareOrdinal(x.Name, y.Name);
		}

		#endregion
	}
}
