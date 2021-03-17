using System.Threading;
using NUnit.Framework;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain.Persistence.Xml;

namespace ProSuite.AGP.WorkList.Test
{
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class WorklistUtilsTest
	{
		private string _path = @"C:\git\ProSuite\src\ProSuite.AGP.WorkList.Test\TestData\work_list_definition_pointing_to_sde.xml.swl";

		[SetUp]
		public void SetUp()
		{
			// http://stackoverflow.com/questions/8245926/the-current-synchronizationcontext-may-not-be-used-as-a-taskscheduler
			SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
		}

		[TearDown]
		public void TearDown()
		{
		}

		[OneTimeSetUp]
		public void SetupFixture()
		{
			// Host must be initialized on an STA thread:
			//Host.Initialize();
			Commons.AGP.Hosting.CoreHostProxy.Initialize();
		}

		[Test]
		public void Can_create_worklist_with_SDE_workspace_from_definition_file()
		{
			XmlWorkListDefinition definition = XmlWorkItemStateRepository.Import(_path);

			IWorkList worklist = WorkListUtils.Create(definition);
			Assert.NotNull(worklist);

			Assert.AreEqual(2, worklist.Count());
		}
	}
}
