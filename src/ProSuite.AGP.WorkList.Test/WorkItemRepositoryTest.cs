using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Internal.CIM;
using NUnit.Framework;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain.Persistence;
using ProSuite.AGP.WorkList.Domain.Persistence.Xml;
using ProSuite.Commons.AGP.Hosting;
using ProSuite.Commons.Testing;
using Path = System.IO.Path;
using Polygon = ArcGIS.Core.Geometry.Polygon;

namespace ProSuite.AGP.WorkList.Test
{
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class WorkItemRepositoryTest
	{
		[SetUp]
		public void SetUp()
		{
			// http://stackoverflow.com/questions/8245926/the-current-synchronizationcontext-may-not-be-used-as-a-taskscheduler
			SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
		}

		[OneTimeSetUp]
		public void OneTimeSetup()
		{
			// Host must be initialized on an STA thread:
			//Host.Initialize();
			CoreHostProxy.Initialize();

			//_poly0 = PolygonConstruction
			//         .StartPolygon(0, 0, 0)
			//         .LineTo(0, 20, 0)
			//         .LineTo(20, 20, 0)
			//         .LineTo(20, 0, 0)
			//         .ClosePolygon();

			//_poly1 = PolygonConstruction
			//         .StartPolygon(0, 0, 0)
			//         .LineTo(0, 40, 0)
			//         .LineTo(40, 40, 0)
			//         .LineTo(40, 0, 0)
			//         .ClosePolygon();
		}

		[Test]
		public void Foo()
		{
			string path = TestDataPreparer.ExtractZip("issues.gdb.zip").GetPath();
			
			var geodatabase = new Geodatabase(
				new FileGeodatabaseConnectionPath(new Uri(path, UriKind.Absolute)));

			Table lines = geodatabase.OpenDataset<Table>("issueLines");
			Table issueRows = geodatabase.OpenDataset<Table>("issueRows");

			Uri uri = lines.GetDatastore().GetPath();
			string connectionString = lines.GetDatastore().GetConnectionString();

			var conn = new DatabaseConnectionProperties(EnterpriseDatabaseType.Unknown);
			conn.Instance = connectionString;
			var geodatabase1 = new Geodatabase(conn);

			IWorkItemStateRepository stateRepository =
				new XmlWorkItemStateRepository(Path.Join(Path.GetDirectoryName(path), "states.xml"), null, null);

		}
	}
}
