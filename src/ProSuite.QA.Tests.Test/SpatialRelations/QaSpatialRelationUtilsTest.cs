using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Tests.SpatialRelations;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Test.SpatialRelations
{
	[TestFixture]
	public class QaSpatialRelationUtilsTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private ISpatialReference _spatialReference;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout(EsriProduct.ArcEditor);
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			_lic.Release();
		}

		[SetUp]
		public void SetUp()
		{
			_spatialReference = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);
		}

		[Test]
		public void CanDetectNonIntersecting()
		{
			FeatureClassMock fc = CreateFeatureClassMock(
				esriGeometryType.esriGeometryPolyline);

			var f1 = fc.CreateFeature(new Pt(0, 0), new Pt(10, 0));
			var f2 = fc.CreateFeature(new Pt(0, 5), new Pt(10, 5));

			var reporter = new TestErrorReporter();

			QaSpatialRelationUtils.ReportIntersections(
				f1, 0, f2, 0,
				reporter, null, null, false, null);

			Assert.AreEqual(0, reporter.Errors.Count);
		}

		[Test]
		public void CanDetectIntersecting()
		{
			FeatureClassMock fc = CreateFeatureClassMock(
				esriGeometryType.esriGeometryPolyline);

			var f1 = fc.CreateFeature(new Pt(0, 5, 1), new Pt(10, 5, 1));
			var f2 = fc.CreateFeature(new Pt(4, 0, 2), new Pt(4, 10, 2));

			var reporter = new TestErrorReporter();

			QaSpatialRelationUtils.ReportIntersections(
				f1, 0, f2, 0,
				reporter, null, null, false, null);

			AssertUtils.ExpectedErrors(
				1, reporter.Errors,
				e => GeometryUtils.AreEqual(
					     CreateMultipoint(4, 5, 1),
					     e.Geometry) && e.Description == "Features intersect");
		}

		[Test]
		public void CanDetectIntersectionAtEndpoints()
		{
			FeatureClassMock fc = CreateFeatureClassMock(
				esriGeometryType.esriGeometryPolyline);

			var f1 = fc.CreateFeature(new Pt(1, 5, 10), new Pt(10, 5, 11));
			var f2 = fc.CreateFeature(new Pt(10, 5, 12), new Pt(20, 5, 13));

			var reporter = new TestErrorReporter();

			QaSpatialRelationUtils.ReportIntersections(
				f1, 0, f2, 0,
				reporter, null, null, false, null,
				GeometryComponent.LineEndPoint,
				GeometryComponent.LineStartPoint);

			AssertUtils.ExpectedErrors(
				1, reporter.Errors,
				e => GeometryUtils.AreEqual(
					     CreatePoint(10, 5, 11),
					     e.Geometry) && e.Description == "Features intersect");

			reporter.Reset();

			// no intersection expected for start point of first line and end point of second line
			QaSpatialRelationUtils.ReportIntersections(
				f1, 0, f2, 0,
				reporter, null, null, false, null,
				GeometryComponent.LineStartPoint,
				GeometryComponent.LineEndPoint);

			Assert.AreEqual(0, reporter.Errors.Count);
		}

		[Test]
		public void CanSuppressIntersectionWithValidRelationConstraint()
		{
			FeatureClassMock fc = CreateFeatureClassMock(
				esriGeometryType.esriGeometryPolyline);
			const string fieldName = "NAME";
			fc.AddField(fieldName, esriFieldType.esriFieldTypeString);
			var fieldIndex = fc.FindField(fieldName);
			var f1 = fc.CreateFeature(new Pt(0, 5, 1), new Pt(10, 5, 1));
			f1.set_Value(fieldIndex, "a");

			var f2 = fc.CreateFeature(new Pt(4, 0, 2), new Pt(4, 10, 2));
			f2.set_Value(fieldIndex, "b");

			var f3 = fc.CreateFeature(new Pt(4, 0, 2), new Pt(4, 10, 2));
			f3.set_Value(fieldIndex, "a");

			var reporter = new TestErrorReporter();

			var validRelationConstraint = new ValidRelationConstraint(
				"G1.NAME <> G2.NAME",
				false,
				false);

			QaSpatialRelationUtils.ReportIntersections(
				f1, 0, f2, 0,
				reporter, null, validRelationConstraint, false, null);

			Assert.AreEqual(0, reporter.Errors.Count);

			reporter.Reset();

			// f1 and f3 have same NAME value, intersection error is expected
			QaSpatialRelationUtils.ReportIntersections(
				f1, 0, f3, 0,
				reporter, null, validRelationConstraint, false, null);

			AssertUtils.ExpectedErrors(
				1, reporter.Errors,
				e => GeometryUtils.AreEqual(
					     CreateMultipoint(4, 5, 1),
					     e.Geometry) &&
				     e.Description ==
				     "Features intersect and constraint is not fulfilled: G1.NAME = 'a'; G2.NAME = 'a'");
		}

		[NotNull]
		private FeatureClassMock CreateFeatureClassMock(
			esriGeometryType geometryType)
		{
			return new FeatureClassMock(1, "fc", geometryType,
			                            esriFeatureType.esriFTSimple, _spatialReference);
		}

		[NotNull]
		private IMultipoint CreateMultipoint(double x, double y, double z)
		{
			var result = GeometryFactory.CreateMultipoint(
				GeometryFactory.CreatePoint(x, y, z));

			result.SpatialReference = _spatialReference;

			return result;
		}

		[NotNull]
		private IPoint CreatePoint(double x, double y, double z)
		{
			var result = GeometryFactory.CreatePoint(x, y, z);

			result.SpatialReference = _spatialReference;

			return result;
		}
	}
}
