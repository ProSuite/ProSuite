using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Globalization;

namespace ProSuite.QA.Container.TestCategories
{
	[CLSCompliant(false)]
	public class TestCategoryAttribute : LocalizedCategoryAttribute
	{
		public TestCategoryAttribute([NotNull] string resourceName)
			: base(TestCategoryNames.ResourceManager, resourceName) { }
	}
}
