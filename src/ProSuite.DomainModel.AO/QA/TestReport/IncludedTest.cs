using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.QA.TestReport
{
	internal abstract class IncludedTest : IncludedTestBase
	{
		private readonly TestFactory _testFactory;

		protected IncludedTest([NotNull] string title,
		                       [NotNull] TestFactory testFactory,
		                       [NotNull] Assembly assembly,
		                       bool obsolete,
		                       bool internallyUsed)
			: base(title, assembly, obsolete, internallyUsed, testFactory.TestCategories)
		{
			Assert.ArgumentNotNull(testFactory, nameof(testFactory));

			_testFactory = testFactory;
		}

		[NotNull]
		public TestFactory TestFactory
		{
			get { return _testFactory; }
		}

		public override string Description
		{
			get { return _testFactory.GetTestDescription(); }
		}
	}
}