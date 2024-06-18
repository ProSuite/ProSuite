using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Internal.Geometry;
using NUnit.Framework;
using ProSuite.AGP.Editing.Picker;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Hosting;

namespace ProSuite.AGP.Editing.Test.Picker
{
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class PickerServiceTest
	{
		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			CoreHostProxy.Initialize();
		}

		[Test]
		public async Task Can_use_PickerService()
		{
			Polyline longPolyline =
				GeometryFactory.CreatePolyline(
					MapPointBuilder.CreateMapPoint(0, 0),
					MapPointBuilder.CreateMapPoint(0, 100));

			Polyline shortPolyline =
				GeometryFactory.CreatePolyline(
					MapPointBuilder.CreateMapPoint(0, 0),
					MapPointBuilder.CreateMapPoint(0, 20));

			Polyline somePolyline =
				GeometryFactory.CreatePolyline(
					MapPointBuilder.CreateMapPoint(0, 0),
					MapPointBuilder.CreateMapPoint(-100, 42));

			MapPoint referenceGeometry = MapPointBuilder.CreateMapPoint(0, 21);

			IPickerService picker = new PickerServiceMock();

			var polylines = new List<Polyline>
			                {
				                somePolyline, somePolyline, longPolyline, somePolyline,
				                shortPolyline
			                };

			IEnumerable<IPickableItem> items = Create(polylines);

			var pickerPrecedence = new StandardPickerPrecedenceMock();
			pickerPrecedence.SelectionGeometry = referenceGeometry;

			Task<IPickableItem> pickSingle =
				picker.PickSingle<IPickableItem>(items, new Point(42, 99),
				                                 pickerPrecedence);

			IPickableItem pickedItem = await pickSingle;

			Assert.AreEqual(shortPolyline, pickedItem.Geometry);
		}

		private IEnumerable<IPickableItem> Create(List<Polyline> polylines)
		{
			foreach (Polyline polyline in polylines)
			{
				yield return new PickableFeatureMock
				             {
					             Geometry = polyline
				             };
			}
		}
	}
}
