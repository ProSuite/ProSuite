using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	[CLSCompliant(false)]
	public interface IIssueStatisticsTable
	{
		[NotNull]
		ITable Table { get; }

		[NotNull]
		IIssueStatisticsTableFieldNames FieldNames { get; }
	}
}
