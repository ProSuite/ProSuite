using System.Collections.Generic;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core.TestCategories;

namespace ProSuite.DomainModel.AO.Test.QA.TestReport
{
	[UsedImplicitly]
	[GeometryTest]
	public class TestReportTest2 : ContainerTest
	{
		public TestReportTest2(IReadOnlyTable table) : base(table) { }

		public TestReportTest2(IList<IReadOnlyTable> tables) : base(tables[0]) { }

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			return 0;
		}
	}
}
