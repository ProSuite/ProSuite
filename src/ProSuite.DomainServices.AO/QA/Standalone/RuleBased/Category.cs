using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Standalone.RuleBased
{
	public class Category
	{
		public Category([NotNull] string name)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			Name = name;
		}

		[NotNull]
		public string Name { get; }
	}
}
