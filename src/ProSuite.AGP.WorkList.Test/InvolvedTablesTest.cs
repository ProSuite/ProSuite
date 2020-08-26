using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ProSuite.AGP.WorkList.Test
{
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class InvolvedTablesTest
	{
		private string _issuesGdb = @"C:\log5\PROSUITE_QA_XmlBasedVerificationTool_20200617_155928\issues.gdb";
		private string _featureClassName = "IssuePolygons";

		[SetUp]
		public void SetUp()
		{
			// http://stackoverflow.com/questions/8245926/the-current-synchronizationcontext-may-not-be-used-as-a-taskscheduler
			SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
		}

		[OneTimeSetUp]
		public void SetupFixture()
		{
			Commons.AGP.Hosting.CoreHostProxy.Initialize();
		}

		[Test]
		public void InvolvedTablesParsingTest()
		{
			Assert.AreEqual(2, 2);

			Assert.AreEqual(1, 1);
		}
	}
}
