using System;
using System.Collections.Generic;

namespace ProSuite.DomainServices.AO.QA.HtmlReports
{
	public class HtmlReportIssueGroupComparer : IComparer<HtmlReportIssueGroup>
	{
		private readonly bool _sortOnAffectedComponent;

		public HtmlReportIssueGroupComparer(bool sortOnAffectedComponent = true)
		{
			_sortOnAffectedComponent = sortOnAffectedComponent;
		}

		public int Compare(HtmlReportIssueGroup g1, HtmlReportIssueGroup g2)
		{
			if (_sortOnAffectedComponent)
			{
				int component = string.CompareOrdinal(g1?.AffectedComponent,
				                                      g2?.AffectedComponent);

				if (component != 0)
				{
					return component;
				}
			}

			// TODO extend ordering logic
			return string.Compare(g1?.QualityConditionName,
			                      g2?.QualityConditionName,
			                      StringComparison.Ordinal);
		}
	}
}
