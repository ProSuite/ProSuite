using System.ComponentModel;

namespace ProSuite.QA.Core.TestCategories
{
	public class InternallyUsedTestAttribute : CategoryAttribute
	{
		public InternallyUsedTestAttribute() : base(TestCategoryNames.InternallyUsed) { }
	}
}
