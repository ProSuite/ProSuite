using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry;
using ProSuite.DomainServices.AO.QA.Exceptions;
using ProSuite.DomainServices.AO.QA.Issues;
using ProSuite.QA.Container.Geometry;

namespace ProSuite.DomainServices.AO.Test.QA.Exceptions
{
	[TestFixture]
	public class ExceptionObjectSpatialIndexTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout(EsriProduct.ArcEditor);
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanSearchCoincidentPoints()
		{
			IPoint point = GeometryFactory.CreatePoint(1000, 1000);

			var spatialIndex = new ExceptionObjectSpatialIndex();

			const int count = 100;
			for (int i = 0; i < count; i++)
			{
				spatialIndex.Add(CreateExceptionObject(1, GeometryFactory.Clone(point)));
			}

			List<ExceptionObject> exceptionObjects = spatialIndex.Search(point).ToList();

			Assert.AreEqual(count, exceptionObjects.Count);
		}

		[Test]
		public void CanSearchSinglePoint()
		{
			AssertCanFindGeometry(GeometryFactory.CreatePoint(1000, 1000));
		}

		[Test]
		public void CanSearchSingleMultipoint()
		{
			AssertCanFindGeometry(GeometryFactory.CreateMultipoint(
				                      GeometryFactory.CreatePoint(1000, 1000),
				                      GeometryFactory.CreatePoint(2000, 1000),
				                      GeometryFactory.CreatePoint(3000, 1000)));
		}

		[Test]
		public void CanSearchSinglePolyline()
		{
			AssertCanFindGeometry(GeometryFactory.CreatePolyline(1000, 1000, 2000, 2000));
		}

		[Test]
		public void CanSearchSinglePolygon()
		{
			AssertCanFindGeometry(GeometryFactory.CreatePolygon(1000, 1000, 2000, 2000));
		}

		[Test]
		public void CanSearchSingleMultipatch()
		{
			AssertCanFindGeometry(
				GeometryFactory.CreateMultiPatch(
					GeometryFactory.CreatePolygon(1000, 1000,
					                              2000, 2000,
					                              100)));
		}

		[Test]
		public void CanSearchEmpty()
		{
			var spatialIndex = new ExceptionObjectSpatialIndex();

			IPoint point = GeometryFactory.CreatePoint(1000, 1000);

			List<ExceptionObject> exceptionObjects = spatialIndex.Search(point).ToList();

			Assert.AreEqual(0, exceptionObjects.Count);
		}

		private static void AssertCanFindGeometry([NotNull] IGeometry geometry)
		{
			ExceptionObject exceptionObject = CreateExceptionObject(1, geometry);

			var spatialIndex = new ExceptionObjectSpatialIndex();
			spatialIndex.Add(exceptionObject);

			List<ExceptionObject> exceptionObjects = spatialIndex.Search(geometry).ToList();

			Assert.AreEqual(1, exceptionObjects.Count);
		}

		[NotNull]
		private static ExceptionObject CreateExceptionObject(int id,
		                                                     [NotNull] IGeometry geometry)
		{
			Box box = QaGeometryUtils.CreateBox(geometry);

			return new ExceptionObject(id, new Guid(), new Guid(),
			                           box, 0.001,
			                           geometry.GeometryType,
			                           ShapeMatchCriterion.EqualEnvelope,
			                           "Issue.Code", "SHAPE",
			                           new InvolvedTable[] { });
		}
	}
}
