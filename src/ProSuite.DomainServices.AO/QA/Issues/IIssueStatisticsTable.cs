using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public interface IIssueStatisticsTable : IDisposable
	{
		[NotNull]
		ITable Table { get; }

		[NotNull]
		IIssueStatisticsTableFieldNames FieldNames { get; }
	}
}
