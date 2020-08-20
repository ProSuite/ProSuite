using System;
using System.Linq;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Hosting;
using NUnit.Framework;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList.Test
{
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class WorkItemTest
	{
		[SetUp]
		public void SetUp()
		{
			// http://stackoverflow.com/questions/8245926/the-current-synchronizationcontext-may-not-be-used-as-a-taskscheduler
			SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

			Polygon polygon = PolygonConstruction
			                  .StartPolygon(0, 0)
			                  .LineTo(0, 20)
			                  .LineTo(20, 20)
			                  .LineTo(20, 0)
			                  .ClosePolygon();

			TestUtils.InsertRows(_path, _featureClassName, polygon, 1000);
		}

		[TearDown]
		public void TearDown()
		{
			TestUtils.DeleteAllRows(_path, _featureClassName);
		}

		private readonly string _path = @"C:\git\ProSuite\src\ProSuite.AGP.WorkList.Test\TestData\issues.gdb";
		private string _featureClassName = "IssuePolygons";

		[OneTimeSetUp]
		public void SetupFixture()
		{
			// Host must be initialized on an STA thread:
			Host.Initialize();
		}

		private Table OpenTable(out Geodatabase geodatabase)
		{
			var uri = new Uri(_path, UriKind.Absolute);
			using (geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(uri)))
			{
				return geodatabase.OpenDataset<FeatureClass>(_featureClassName);
			}
		}

		//[Test]
		//public void Can_Query_WorkItem_Extent()
		//{
		//	Table table = OpenTable(out Geodatabase geodatabase);
		//	try
		//	{
		//		Feature feature = GdbRowUtils.GetRows<Feature>(table, null, true).FirstOrDefault();
		//		Assert.NotNull(feature);

		//		var errorItem = new IssueItem(feature);
		//		Envelope current = errorItem.Extent;

		//		Envelope expected = feature.GetShape().Extent;

		//		Assert.True(expected.IsEqual(current));
		//	}
		//	finally
		//	{
		//		table.Dispose();
		//		geodatabase.Dispose();
		//	}
		//}
	}
}
