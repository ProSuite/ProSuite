using ProSuite.Commons.AO.Geodatabase;

namespace ProSuite.DomainModel.AO.Test.QA
{
	public class DerivedTest : BaseTest
	{
		public DerivedTest(IReadOnlyTable table) :
			base(table) { }

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			return 0;
		}
	}
}
