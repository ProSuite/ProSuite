using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.QA.TestReport
{
	internal class TestFactoryIndexEntry : TestIndexEntry
	{
		public TestFactoryIndexEntry([NotNull] IncludedTestFactory includedTestFactory)
			: base(includedTestFactory) { }
	}
}