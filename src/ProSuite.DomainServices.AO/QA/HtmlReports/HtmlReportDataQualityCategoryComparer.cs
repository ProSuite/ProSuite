using System;
using System.Collections.Generic;

namespace ProSuite.DomainServices.AO.QA.HtmlReports
{
	public class HtmlReportDataQualityCategoryComparer :
		IComparer<HtmlReportDataQualityCategory>
	{
		public int Compare(HtmlReportDataQualityCategory c1,
		                   HtmlReportDataQualityCategory c2)
		{
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
