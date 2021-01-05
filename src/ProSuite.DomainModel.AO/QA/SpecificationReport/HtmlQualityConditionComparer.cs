using System;
using System.Collections.Generic;

namespace ProSuite.DomainModel.AO.QA.SpecificationReport
{
	public class HtmlQualityConditionComparer :
		IComparer<HtmlQualityCondition>
	{
		public int Compare(HtmlQualityCondition q1, HtmlQualityCondition q2)
		{
			return string.Compare(q1.Name, q2.Name,
			                      StringComparison.CurrentCulture);
		}
	}
}
