using ProSuite.Commons.Essentials.CodeAnnotations;

namespace EsriDE.ProSuite.DomainModel.QA.TestReport
{
	internal class TestClassIndexEntry : TestIndexEntry
	{
		public TestClassIndexEntry([NotNull] IncludedTestClass includedTestClass)
			: base(includedTestClass) { }
	}
}