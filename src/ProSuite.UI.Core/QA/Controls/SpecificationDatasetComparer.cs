using System;
using System.Collections.Generic;

namespace ProSuite.UI.Core.QA.Controls
{
	public class SpecificationDatasetComparer : IComparer<SpecificationDataset>
	{
		#region IComparer<SpecificationDataset> Members

		public int Compare(SpecificationDataset x, SpecificationDataset y)
		{
			if (x == null && y == null)
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

			int i = string.Compare(x.TestName, y.TestName, StringComparison.CurrentCulture);
			if (i != 0)
			{
				return i;
			}

			// if test name is equal: compare dataset name
			if (x.DatasetName == null && y.DatasetName == null)
			{
				return 0;
			}

			if (x.DatasetName == null)
			{
				return -1;
			}

			if (y.DatasetName == null)
			{
				return 1;
			}

			return string.Compare(x.DatasetName, y.DatasetName, StringComparison.CurrentCulture);
		}

		#endregion
	}
}
