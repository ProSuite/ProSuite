using System;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Hosting;
using NUnit.Framework;

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

			InsertTestRows();
		}

		[TearDown]
		public void TearDown()
		{
			DeleteTestRows();
		}

		private readonly string _path =
			@"C:\git\ProSuite\src\ProSuite.AGP.WorkList.Test\TestData\issues.gdb";

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
				return geodatabase.OpenDataset<FeatureClass>("IssuePolygons");
			}
		}

		private void InsertTestRows()
		{
			Polygon polygon = PolygonConstruction
			                  .StartPolygon(0, 0)
			                  .LineTo(0, 20)
			                  .LineTo(20, 20)
			                  .LineTo(20, 0)
			                  .ClosePolygon();

			var uri = new Uri(_path, UriKind.Absolute);
			using (var geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(uri)))
			{
				using (var featureClass = geodatabase.OpenDataset<FeatureClass>("IssuePolygons"))
				{
					FeatureClassDefinition definition = featureClass.GetDefinition();

					using (RowBuffer buffer = featureClass.CreateRowBuffer())
					{
						buffer[definition.GetShapeField()] = polygon;
						featureClass.CreateRow(buffer);
					}
				}
			}
		}

		private void DeleteTestRows()
		{
			var uri = new Uri(_path, UriKind.Absolute);
			using (var geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(uri)))
			{
				using (var featureClass = geodatabase.OpenDataset<FeatureClass>("IssuePolygons"))
				{
					// delete all
					featureClass.DeleteRows(new QueryFilter());
					Assert.True(featureClass.GetCount() == 0);
				}
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

		//		var errorItem = new ErrorItem(feature);
		//		Envelope current = errorItem.GetExtent();

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
