using System;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.DomainServices.AO.QA.IssuePersistence;

namespace ProSuite.DomainServices.AO.Test.QA
{
	[TestFixture]
	public class ErrorRepositoryUtilsTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		private readonly esriGeometryType[] _storedGeometryTypes =
		{
			esriGeometryType.esriGeometryMultipoint,
			esriGeometryType.esriGeometryPolyline,
			esriGeometryType.esriGeometryPolygon,
			esriGeometryType.esriGeometryMultiPatch
		};

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanGetGeometryToStoreForZeroLengthLine()
		{
			IPolyline polyline = GeometryFactory.CreatePolyline(0, 10, 100,
			                                                    0, 10, 100,
			                                                    dontSimplify: true);
			polyline.SpatialReference = CreateSpatialReference();

			IGeometry result = ErrorRepositoryUtils.GetGeometryToStore(polyline,
			                                                           polyline
				                                                           .SpatialReference,
			                                                           _storedGeometryTypes);

			Console.WriteLine(GeometryUtils.ToString(result));

			var multipoint = result as IMultipoint;

			Assert.NotNull(multipoint);
			Assert.AreEqual(1, ((IPointCollection) multipoint).PointCount);
			IPoint point = ((IPointCollection) multipoint).Point[0];

			Assert.AreEqual(0, point.X);
			Assert.AreEqual(10, point.Y);
			Assert.AreEqual(100, point.Z);
			Assert.AreEqual(polyline.SpatialReference, point.SpatialReference);
		}

		[Test]
		public void CanGetGeometryToStoreForZeroAreaLinearPolygon()
		{
			IPolygon polygon = GeometryFactory.CreatePolygon(CreateSpatialReference());

			((ISegmentCollection) polygon).SetRectangle(
				GeometryFactory.CreateEnvelope(0, 0, 10, 0, polygon.SpatialReference));

			GeometryUtils.MakeZAware(polygon);
			GeometryUtils.ApplyConstantZ(polygon, 100);

			IGeometry result = ErrorRepositoryUtils.GetGeometryToStore(polygon,
			                                                           polygon.SpatialReference,
			                                                           _storedGeometryTypes);

			Console.WriteLine(GeometryUtils.ToString(result));

			IPolyline expectedPolyline = GeometryFactory.CreatePolyline(0, 0, 10, 0);
			GeometryUtils.MakeZAware(expectedPolyline);
			GeometryUtils.ApplyConstantZ(expectedPolyline, 100);
			expectedPolyline.SpatialReference = polygon.SpatialReference;

			Assert.IsTrue(GeometryUtils.AreEqual(expectedPolyline, result));
		}

		[Test]
		public void CanGetGeometryToStoreForZeroAreaCollapsedPolygon()
		{
			IPolygon polygon = GeometryFactory.CreatePolygon(CreateSpatialReference());

			((ISegmentCollection) polygon).SetRectangle(
				GeometryFactory.CreateEnvelope(0, 10, 0, 10, polygon.SpatialReference));

			GeometryUtils.MakeZAware(polygon);
			GeometryUtils.ApplyConstantZ(polygon, 100);

			IGeometry result = ErrorRepositoryUtils.GetGeometryToStore(polygon,
			                                                           polygon.SpatialReference,
			                                                           _storedGeometryTypes);

			Console.WriteLine(GeometryUtils.ToString(result));

			var multipoint = result as IMultipoint;

			Assert.NotNull(multipoint);
			Assert.AreEqual(1, ((IPointCollection) multipoint).PointCount);
			IPoint point = ((IPointCollection) multipoint).Point[0];

			Assert.AreEqual(0, point.X);
			Assert.AreEqual(10, point.Y);
			Assert.AreEqual(100, point.Z);
			Assert.AreEqual(polygon.SpatialReference, point.SpatialReference);
		}

		[Test]
		public void CanGetGeometryToStoreForRegularLine()
		{
			IPolyline polyline = GeometryFactory.CreatePolyline(0, 10, 100,
			                                                    5, 10, 100,
			                                                    dontSimplify: true);
			polyline.SpatialReference = CreateSpatialReference();

			IGeometry result = ErrorRepositoryUtils.GetGeometryToStore(
				polyline,
				polyline.SpatialReference,
				_storedGeometryTypes);

			Assert.IsTrue(GeometryUtils.AreEqual(polyline, result));
		}

		[Test]
		public void CanGetGeometryToStoreForRegularPolygon()
		{
			IPolygon polygon = GeometryFactory.CreatePolygon(0, 0, 10, 10,
			                                                 CreateSpatialReference());
			GeometryUtils.MakeZAware(polygon);
			GeometryUtils.ApplyConstantZ(polygon, 100);

			IGeometry result = ErrorRepositoryUtils.GetGeometryToStore(polygon,
			                                                           polygon.SpatialReference,
			                                                           _storedGeometryTypes);

			Assert.IsTrue(GeometryUtils.AreEqual(polygon, result));
		}

		private static ISpatialReference CreateSpatialReference()
		{
			ISpatialReference spatialReference =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);
			SpatialReferenceUtils.SetXYDomain(spatialReference,
			                                  -1000, -1000, 1000, 1000,
			                                  0.0001, 0.001);
			return spatialReference;
		}
	}
}
