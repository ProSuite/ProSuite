using System;
using System.Collections.Generic;

namespace ProSuite.DomainModel.AO.QA.SpecificationReport
{
	public class HtmlDataQualityCategoryComparer :
		IComparer<HtmlDataQualityCategory>
	{
		public int Compare(HtmlDataQualityCategory c1,
		                   HtmlDataQualityCategory c2)
		{
			if (c1 == null && c2 == null) return 0;
			if (c1 == null) return -1;
			if (c2 == null) return +1;

			if (c1.IsUndefinedCategory != c2.IsUndefinedCategory)
			{
				return c1.IsUndefinedCategory
					       ? 1 // no category --> last 
					       : -1; // defined category --> first
			}

			int listOrderComparison = c1.ListOrder.CompareTo(c2.ListOrder);

			if (listOrderComparison != 0)
			{
				return listOrderComparison;
			}

			// culture-specific comparison
			return string.Compare(c1.Name, c2.Name, StringComparison.CurrentCulture);
		}
	}
}
