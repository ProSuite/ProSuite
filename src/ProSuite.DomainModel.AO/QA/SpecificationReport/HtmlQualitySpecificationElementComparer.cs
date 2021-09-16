using System;
using System.Collections.Generic;

namespace ProSuite.DomainModel.AO.QA.SpecificationReport
{
	public class HtmlQualitySpecificationElementComparer :
		IComparer<HtmlQualitySpecificationElement>
	{
		public int Compare(HtmlQualitySpecificationElement q1,
		                   HtmlQualitySpecificationElement q2)
		{
			return string.Compare(q1?.QualityCondition.Name,
			                      q2?.QualityCondition.Name,
			                      StringComparison.CurrentCulture);
		}
	}
}
