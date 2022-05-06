using System.ComponentModel;

namespace ProSuite.QA.Core.TestCategories
{
	public class SchemaTestAttribute : CategoryAttribute
	{
		public SchemaTestAttribute() : base(TestCategoryNames.Schema) { }
	}
}
