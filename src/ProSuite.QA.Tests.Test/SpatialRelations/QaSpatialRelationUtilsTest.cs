using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Tests.ParameterTypes;
using ProSuite.QA.Tests.SpatialRelations;

namespace ProSuite.QA.Tests.Test.SpatialRelations
{
	[TestFixture]
	public class QaSpatialRelationUtilsTest
	{
		private ISpatialReference _spatialReference;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			TestUtils.ReleaseLicense();
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
				ReadOnlyRow.Create(f1), 0, ReadOnlyRow.Create(f2), 0,
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
				ReadOnlyRow.Create(f1), 0, ReadOnlyRow.Create(f2), 0,
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
				ReadOnlyRow.Create(f1), 0, ReadOnlyRow.Create(f2), 0,
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
				ReadOnlyRow.Create(f1), 0, ReadOnlyRow.Create(f2), 0,
				reporter, null, null, false, null,
				GeometryComponent.LineStartPoint,
				GeometryComponent.LineEndPoint);

			Assert.AreEqual(0, reporter.Errors.Count);
		}

		[Test]
		[Category(Commons.Test.TestCategory.FixMe)]
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
				ReadOnlyRow.Create(f1), 0, ReadOnlyRow.Create(f2), 0,
				reporter, null, validRelationConstraint, false, null);

			Assert.AreEqual(0, reporter.Errors.Count);

			reporter.Reset();

			// f1 and f3 have same NAME value, intersection error is expected
			QaSpatialRelationUtils.ReportIntersections(
				ReadOnlyRow.Create(f1), 0, ReadOnlyRow.Create(f3), 0,
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
			return new FeatureClassMock("fc", geometryType,
			                            null, esriFeatureType.esriFTSimple, _spatialReference);
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
