using System.ComponentModel;

namespace ProSuite.QA.Container.TestCategories
{
	public class AttributeTestAttribute : CategoryAttribute
	{
		public AttributeTestAttribute() : base(TestCategoryNames.Attributes) { }
	}
}
