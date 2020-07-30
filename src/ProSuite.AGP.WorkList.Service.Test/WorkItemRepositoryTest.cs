using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Hosting;
using NUnit.Framework;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.DomainModel.DataModel;

namespace ProSuite.AGP.WorkList.Service.Test
{
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class WorkItemRepositoryTest
	{
		private string _name = "TLM_ERRORS_POLYGON";
		private string _path = @"C:\git\ProSuite\src\ProSuite.AGP.WorkList.Service.Test\TestData\errors.gdb";
		private Uri _uri;

		[SetUp]
		public void SetUp()
		{
			// http://stackoverflow.com/questions/8245926/the-current-synchronizationcontext-may-not-be-used-as-a-taskscheduler
			SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
		}

		[OneTimeSetUp]
		public void SetupFixture()
		{
			// Host must be initialized on an STA thread:
			Host.Initialize();
			
			_uri = new Uri(_path, UriKind.Absolute);
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			// Nothing to do. There's no Host.Shutdown or similar
		}

		private static IWorkItemRepository CreateWorkItemRepository(
			Geodatabase geodatabase, string datasetName)
		{
			IWorkItemRepository repository = new ErrorItemRepository(new WorkspaceContext(geodatabase));
			repository.Register(new VectorDatasetMock {Name = datasetName});
			return repository;
		}

		private static IReadOnlyList<Field> GetFields(
			Geodatabase geodatabase, string featureClassName)
		{
			var definition = geodatabase.GetDefinition<FeatureClassDefinition>(featureClassName);
			return definition.GetFields();
		}

		[Test]
		public void CanGetCertainFields()
		{
			var fieldNames = new List<string> {"OBJECTID", "STATUS"};

			using (var geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(_uri)))
			{
				var expected = GetFields(geodatabase, _name).TakeWhile(field => fieldNames.Contains(field.Name))
				                                            .Select(field => field.Name);

				IWorkItemRepository repository = CreateWorkItemRepository(geodatabase, _name);
				var actual = repository.GetFields(fieldNames).Select(field => field.Name);

				Assert.AreEqual(expected, actual);
			}
		}

		[Test]
		public void CanGetFields()
		{
			using (var geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(_uri)))
			{
				var expected = GetFields(geodatabase, _name).Select(field => field.Name);

				IWorkItemRepository repository = CreateWorkItemRepository(geodatabase, _name);
				var actual = repository.GetFields().Select(field => field.Name);

				Assert.AreEqual(expected, actual);
			}
		}
	}
}
