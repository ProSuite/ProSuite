using System.Collections.Generic;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.UI.Core.QA.Controls
{
	public class DatasetComparer : IComparer<Dataset>
	{
		#region IComparer<Dataset> Members

		public int Compare(Dataset x, Dataset y)
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

			if (Equals(x, y))
			{
				return 0;
			}

			int i = string.CompareOrdinal(x.AliasName, y.AliasName);

			if (i == 0)
			{
				// different dataset but same alias name. Return as different 
				// (no matter who's first)
				return -1;
			}

			return i;
		}

		#endregion
	}
}
