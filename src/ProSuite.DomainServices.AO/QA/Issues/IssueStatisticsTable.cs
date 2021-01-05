using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public class IssueStatisticsTable : IIssueStatisticsTable
	{
		[CLSCompliant(false)]
		public IssueStatisticsTable([NotNull] ITable table,
		                            [NotNull] IIssueStatisticsTableFieldNames fieldNames)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(fieldNames, nameof(fieldNames));

			Table = table;
			FieldNames = fieldNames;
		}

		[CLSCompliant(false)]
		public ITable Table { get; }

		public IIssueStatisticsTableFieldNames FieldNames { get; }
	}
}
