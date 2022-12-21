using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaMinMeanSegmentLengthTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanTestPolylineSimple()
		{
			var featureClassMock = new FeatureClassMock("mock",
			                                            esriGeometryType.esriGeometryPolyline, 1);

			CurveConstruction construction = CurveConstruction.StartLine(0, 0)
			                                                  .LineTo(1, 0)
			                                                  .LineTo(1, 1);
			IFeature row1 = featureClassMock.CreateFeature(construction.Curve);

			var errorRunner =
				new QaTestRunner(new QaMinMeanSegmentLength(
					                 ReadOnlyTableFactory.Create(featureClassMock), 1.5, false));
			errorRunner.Execute(row1);
			Assert.AreEqual(1, errorRunner.Errors.Count);

			var noErrorRunner =
				new QaTestRunner(new QaMinMeanSegmentLength(
					                 ReadOnlyTableFactory.Create(featureClassMock), 0.9, false));
			noErrorRunner.Execute(row1);
			Assert.AreEqual(0, noErrorRunner.Errors.Count);
		}

		[Test]
		public void CanTestPolylinePerPart()
		{
			var featureClassMock = new FeatureClassMock("mock",
			                                            esriGeometryType.esriGeometryPolyline, 1);

			CurveConstruction construction = CurveConstruction.StartLine(0, 0)
			                                                  .LineTo(1, 0)
			                                                  .LineTo(1, 1)
			                                                  .MoveTo(20, 0)
			                                                  .LineTo(20, 1)
			                                                  .LineTo(21, 0);
			IFeature row1 = featureClassMock.CreateFeature(construction.Curve);

			var noErrorNoPartsRunner =
				new QaTestRunner(new QaMinMeanSegmentLength(
					                 ReadOnlyTableFactory.Create(featureClassMock), 0.9, false));
			noErrorNoPartsRunner.Execute(row1);
			Assert.AreEqual(0, noErrorNoPartsRunner.Errors.Count);

			var oneErrorNoPartsRunner =
				new QaTestRunner(new QaMinMeanSegmentLength(
					                 ReadOnlyTableFactory.Create(featureClassMock), 1.5, false));
			oneErrorNoPartsRunner.Execute(row1);
			Assert.AreEqual(1, oneErrorNoPartsRunner.Errors.Count);

			var noErrorPerPartsRunner =
				new QaTestRunner(new QaMinMeanSegmentLength(
					                 ReadOnlyTableFactory.Create(featureClassMock), 0.9, true));
			noErrorPerPartsRunner.Execute(row1);
			Assert.AreEqual(0, noErrorPerPartsRunner.Errors.Count);

			var oneErrorRunner =
				new QaTestRunner(new QaMinMeanSegmentLength(
					                 ReadOnlyTableFactory.Create(featureClassMock), 1.1, true));
			oneErrorRunner.Execute(row1);
			Assert.AreEqual(1, oneErrorRunner.Errors.Count);

			var twoErrorRunner =
				new QaTestRunner(new QaMinMeanSegmentLength(
					                 ReadOnlyTableFactory.Create(featureClassMock), 1.5, true));
			twoErrorRunner.Execute(row1);
			Assert.AreEqual(2, twoErrorRunner.Errors.Count);
		}

		[Test]
		public void CanTestMultiPatchesSimple()
		{
			var featureClassMock = new FeatureClassMock("mock",
			                                            esriGeometryType.esriGeometryMultiPatch, 1);

			var construction = new MultiPatchConstruction();

			construction.StartRing(0, 0, 0)
			            .Add(1, 0, 0)
			            .Add(1, 0, 1)
			            .Add(0, 0, 1);

			IFeature row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			var errorRunner =
				new QaTestRunner(new QaMinMeanSegmentLength(
					                 ReadOnlyTableFactory.Create(featureClassMock), 1.5, false));
			errorRunner.Execute(row1);
			Assert.AreEqual(1, errorRunner.Errors.Count);

			var noErrorRunner =
				new QaTestRunner(new QaMinMeanSegmentLength(
					                 ReadOnlyTableFactory.Create(featureClassMock), 0.9, false));
			noErrorRunner.Execute(row1);
			Assert.AreEqual(0, noErrorRunner.Errors.Count);
		}

		[Test]
		public void CanTestMultiPatchesPerPart()
		{
			var featureClassMock = new FeatureClassMock("mock",
			                                            esriGeometryType.esriGeometryMultiPatch, 1);

			var construction = new MultiPatchConstruction();

			construction.StartRing(0, 0, 0)
			            .Add(1, 0, 0)
			            .Add(1, 0, 1)
			            .Add(0, 0, 1)
			            .StartFan(2, 0, 0)
			            .Add(2, 1, 0)
			            .Add(3, 0, 0);

			IFeature row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			var noErrorNoPartsRunner =
				new QaTestRunner(new QaMinMeanSegmentLength(
					                 ReadOnlyTableFactory.Create(featureClassMock), 0.9, false));
			noErrorNoPartsRunner.Execute(row1);
			Assert.AreEqual(0, noErrorNoPartsRunner.Errors.Count);

			var oneErrorNoPartsRunner =
				new QaTestRunner(new QaMinMeanSegmentLength(
					                 ReadOnlyTableFactory.Create(featureClassMock), 1.5, false));
			oneErrorNoPartsRunner.Execute(row1);
			Assert.AreEqual(1, oneErrorNoPartsRunner.Errors.Count);

			var noErrorPerPartsRunner =
				new QaTestRunner(new QaMinMeanSegmentLength(
					                 ReadOnlyTableFactory.Create(featureClassMock), 0.9, true));
			noErrorPerPartsRunner.Execute(row1);
			Assert.AreEqual(0, noErrorPerPartsRunner.Errors.Count);

			var oneErrorRunner =
				new QaTestRunner(new QaMinMeanSegmentLength(
					                 ReadOnlyTableFactory.Create(featureClassMock), 1.1, true));
			oneErrorRunner.Execute(row1);
			Assert.AreEqual(1, oneErrorRunner.Errors.Count);

			var twoErrorRunner =
				new QaTestRunner(new QaMinMeanSegmentLength(
					                 ReadOnlyTableFactory.Create(featureClassMock), 1.5, true));
			twoErrorRunner.Execute(row1);
			Assert.AreEqual(2, twoErrorRunner.Errors.Count);
		}
	}
}
