using System;
using System.Collections.Generic;

namespace ProSuite.DomainServices.AO.QA.HtmlReports
{
	public class HtmlReportQualityConditionComparer :
		IComparer<HtmlReportQualityCondition>
	{
		public int Compare(HtmlReportQualityCondition q1, HtmlReportQualityCondition q2)
		{
			return string.Compare(q1.QualityConditionName, q2.QualityConditionName,
			                      StringComparison.CurrentCulture);
		}
	}
}
