using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Globalization;

namespace ProSuite.QA.Core.TestCategories
{
	public class TestCategoryAttribute : LocalizedCategoryAttribute
	{
		public TestCategoryAttribute([NotNull] string resourceName)
			: base(TestCategoryNames.ResourceManager, resourceName) { }
	}
}
