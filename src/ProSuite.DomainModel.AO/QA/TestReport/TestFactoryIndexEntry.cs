using ProSuite.Commons.Essentials.CodeAnnotations;

namespace EsriDE.ProSuite.DomainModel.QA.TestReport
{
	internal class TestFactoryIndexEntry : TestIndexEntry
	{
		public TestFactoryIndexEntry([NotNull] IncludedTestFactory includedTestFactory)
			: base(includedTestFactory) { }
	}
}