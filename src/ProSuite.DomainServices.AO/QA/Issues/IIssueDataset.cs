using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	[CLSCompliant(false)]
	public interface IIssueDataset
	{
		[NotNull]
		IObjectClass ObjectClass { get; }

		int IssueCount { get; }

		[NotNull]
		IIssueTableFields Fields { get; }
	}
}
