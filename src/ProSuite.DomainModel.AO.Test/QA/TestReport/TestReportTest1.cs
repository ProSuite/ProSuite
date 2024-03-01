using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core.TestCategories;

namespace ProSuite.DomainModel.AO.Test.QA.TestReport
{
	[UsedImplicitly]
	[AttributeTest]
	public class TestReportTest1 : ContainerTest
	{
		public TestReportTest1(IReadOnlyTable table) : base(table) { }

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			return 0;
		}
	}
}
