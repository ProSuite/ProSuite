using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public class IssueStatisticsTable : IIssueStatisticsTable
	{
		public IssueStatisticsTable([NotNull] ITable table,
		                            [NotNull] IIssueStatisticsTableFieldNames fieldNames)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(fieldNames, nameof(fieldNames));

			Table = table;
			FieldNames = fieldNames;
		}

		public ITable Table { get; }

		public IIssueStatisticsTableFieldNames FieldNames { get; }

		public void Dispose()
		{
			Marshal.ReleaseComObject(Table);
		}
	}
}
