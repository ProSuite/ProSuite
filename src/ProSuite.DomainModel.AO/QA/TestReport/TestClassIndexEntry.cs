using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.QA.TestReport
{
	internal class TestClassIndexEntry : TestIndexEntry
	{
		public TestClassIndexEntry([NotNull] IncludedInstanceClass includedTestClass)
			: base(includedTestClass) { }
	}
}
