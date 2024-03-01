using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.AO.Test.QA.TestReport
{
	[UsedImplicitly]
	public class TestReportTest4 : ContainerTest
	{
		public TestReportTest4(IReadOnlyTable table) : base(table) { }

		[TestParameter]
		public bool In3D { get; set; }

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			return 0;
		}
	}
}
