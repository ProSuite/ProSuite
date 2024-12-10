using System.Collections.Generic;
using ProSuite.Commons.AGP.Picker;

namespace ProSuite.AGP.Editing.Picker
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
