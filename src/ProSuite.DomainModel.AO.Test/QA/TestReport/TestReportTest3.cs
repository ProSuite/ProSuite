using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core.TestCategories;

namespace ProSuite.DomainModel.AO.Test.QA.TestReport
{
	[UsedImplicitly]
	[TopologyTest]
	[GeometryTest]
	public class TestReportTest3 : ContainerTest
	{
		public TestReportTest3(IReadOnlyTable table) : base(table) { }

		public TestReportTest3(IReadOnlyTable table, double limit) : base(table) { }

		public TestReportTest3(IReadOnlyTable table, EnumType enumValue) : base(table) { }

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			return 0;
		}
	}

	public enum EnumType
	{
		Value1,
		Value2,
		Value3
	}
}
