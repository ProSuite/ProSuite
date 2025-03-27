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
		private Polygon _poly0;
		private Polygon _poly1;
		private Geodatabase _geodatabase;
		private Table _table0;
		private Table _table1;

		[SetUp]
		public void SetUp()
		{
			// http://stackoverflow.com/questions/8245926/the-current-synchronizationcontext-may-not-be-used-as-a-taskscheduler
			SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
		}

		[TearDown]
		public void TearDown()
		{
			_table0?.Dispose();
			_table1?.Dispose();
			_geodatabase?.Dispose();
			//_repository?.Dispose();
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

		#region same as in WorkListTest

		//private static void InsertFeature(string featureClassName, Polygon polygon)
		//{
		//	TestUtils.InsertRows(_emptyIssuesGdb, featureClassName, polygon, 1);
		//}

		//private static void UpdateFeatureGeometry(string featureClassName, Polygon polygon)
		//{
		//	TestUtils.UpdateFeatureGeometry(_emptyIssuesGdb, featureClassName, polygon, 1);
		//}

		//private static void DeleteRow(string featureClassName)
		//{
		//	TestUtils.DeleteRow(_emptyIssuesGdb, featureClassName, 1);
		//}

		//private static void DeleteAllRows(string featureClassName)
		//{
		//	TestUtils.DeleteAllRows(_emptyIssuesGdb, featureClassName);
		//}

		#endregion
	}
}
