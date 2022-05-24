using System.ComponentModel;

namespace ProSuite.QA.Core.TestCategories
{
	public class AttributeTestAttribute : CategoryAttribute
	{
		public AttributeTestAttribute() : base(TestCategoryNames.Attributes) { }
	}
}
