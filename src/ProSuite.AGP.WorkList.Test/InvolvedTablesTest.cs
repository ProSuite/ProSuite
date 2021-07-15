using System.IO;
using System.Threading;
using NUnit.Framework;
using ProSuite.Commons.AGP.Hosting;
using ProSuite.Commons.Testing;

namespace ProSuite.AGP.WorkList.Test
{
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class InvolvedTablesTest
	{
		private string issueGdbPath =
			@"c:\git\ProSuite\src\ProSuite.AGP.WorkList.Test\TestData\sample_issues\issues.gdb";

		private string _featureClassName = "IssuePolygons";

		//[SetUp]
		//public void SetUp()
		//{
		//	// http://stackoverflow.com/questions/8245926/the-current-synchronizationcontext-may-not-be-used-as-a-taskscheduler
		//	SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
		//}

		[OneTimeSetUp]
		public void SetupFixture()
		{
			CoreHostProxy.Initialize();
		}

		[Test]
		public void CanFindTestData()
		{
			var path = new TestDataLocator().GetPath("IssuePolygons.xml");
			Assert.True(File.Exists(path));
		}

		//[Test]
		//public void InvolvedTablesParsingTest()
		//{
		//	var issueFeatureClass = "IssuePolygons";
		//	var issueDefinition =
		//		new IssueWorkListDefinition()
		//		{
		//			FgdbPath = issueGdbPath,
		//			Path = "",
		//			VisitedItems = { }
		//		};

		//	var issueRepository = new IssuePolygonsGdbRepository(issueDefinition, issueFeatureClass);
		//	var issues = issueRepository.GetAll();

		//	Assert.AreEqual(9, issues.Count);

		//	//var failedIssues = issues.Count(i => (i.InIssueInvolvedTables.Any<InvolvedTable>(t => t.KeyField == null)));
		//	//Assert.AreEqual(0, failedIssues);
		//}
	}
}
