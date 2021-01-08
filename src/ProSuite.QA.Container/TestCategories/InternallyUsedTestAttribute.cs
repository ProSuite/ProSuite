using System.ComponentModel;

namespace ProSuite.QA.Container.TestCategories
{
	public class InternallyUsedTestAttribute : CategoryAttribute
	{
		public InternallyUsedTestAttribute() : base(TestCategoryNames.InternallyUsed) { }
	}
}
