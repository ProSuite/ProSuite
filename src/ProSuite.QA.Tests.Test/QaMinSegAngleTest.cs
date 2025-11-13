using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container.Geometry;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core.ParameterTypes;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaMinSegAngleTest
	{
		private IFeatureWorkspace _testWs;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();

			_testWs = TestWorkspaceUtils.CreateInMemoryWorkspace("QaMinSegAngleTest");
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanTestByPoints()
		{
			IFeatureClass fc = CreateLineClass(_testWs, "CanTestByPoints");

			IFeature row1 = fc.CreateFeature();
			row1.Shape =
				CurveConstruction.StartLine(0, 0, 0)
				                 .LineTo(0, 1, 0)
				                 .LineTo(0.1, 0, 0)
				                 .LineTo(0.1, 1, 100)
				                 .LineTo(0.1, 2, 100)
				                 .CircleTo(GeometryFactory.CreatePoint(0.2, 0, 100))
				                 .Curve;
			row1.Store();

			var test = new QaMinSegAngle(ReadOnlyTableFactory.Create(fc), 0.1, true);

			var runner = new QaContainerTestRunner(1000, test);

			runner.Execute();

			Assert.AreEqual(2, runner.Errors.Count);
		}

		[Test]
		public void CanTestInDegrees()
		{
			IFeatureClass fc = CreateLineClass(_testWs, "CanTestInDegrees");

			IFeature row1 = fc.CreateFeature();
			row1.Shape =
				CurveConstruction.StartLine(0, 0, 0)
				                 .LineTo(0, 1, 0)
				                 .LineTo(0.1, 0, 0)
				                 .LineTo(0.1, 1, 100)
				                 .LineTo(0.1, 2, 100)
				                 .CircleTo(GeometryFactory.CreatePoint(0.2, 0, 100))
				                 .Curve;
			row1.Store();

			double limit = FormatUtils.Radians2AngleInUnits(0.1, AngleUnit.Degree);
			var test = new QaMinSegAngle(ReadOnlyTableFactory.Create(fc), limit, true);
			test.AngularUnit = AngleUnit.Degree;

			var runner = new QaContainerTestRunner(1000, test);

			runner.Execute();

			Assert.AreEqual(2, runner.Errors.Count);
		}

		[Test]
		public void CanTestByTangents()
		{
			IFeatureClass fc = CreateLineClass(_testWs, "CanTestByTangents");

			IFeature row1 = fc.CreateFeature();
			row1.Shape =
				CurveConstruction.StartLine(0, 0, 0)
				                 .LineTo(0, 1, 0)
				                 .LineTo(0.1, 0, 0)
				                 .LineTo(0.1, 1, 100)
				                 .LineTo(0.1, 2, 100)
				                 .CircleTo(GeometryFactory.CreatePoint(0.2, 0, 100))
				                 .Curve;
			row1.Store();

			var test = new QaMinSegAngle(ReadOnlyTableFactory.Create(fc), 0.1, true)
			           { UseTangents = true };

			var runner = new QaContainerTestRunner(1000, test);

			runner.Execute();

			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void CanTest2SegmentCircle()
		{
			IFeatureClass fc = CreateLineClass(_testWs, "CanTest2SegmentCircle");

			IFeature row1 = fc.CreateFeature();

			IConstructCircularArc arc = new CircularArcClass();
			arc.ConstructCircle(GeometryFactory.CreatePoint(0, 0, 0), 3, false);

			IPolyline polyline = CreatePolyLine((ISegment) arc);

			int segmentIndex;
			int newPartIndex;
			bool splitHappened;
			polyline.SplitAtDistance(0.5, true, false,
			                         out splitHappened,
			                         out newPartIndex,
			                         out segmentIndex);
			Assert.True(splitHappened);

			row1.Shape = polyline;
			row1.Store();

			var test = new QaMinSegAngle(ReadOnlyTableFactory.Create(fc), 0.1, true);

			var runner = new QaContainerTestRunner(1000, test);

			runner.Execute();

			// assert that two segment closed curve does not always report two 
			// errors when using linearized segments
			Assert.AreEqual(0, runner.Errors.Count);
		}

		[Test]
		public void CanTestCircle()
		{
			IFeatureClass fc = CreateLineClass(_testWs, "CanTestCircle");

			IFeature row1 = fc.CreateFeature();

			IConstructCircularArc arc = new CircularArcClass();
			arc.ConstructCircle(GeometryFactory.CreatePoint(0, 0, 0), 3, false);

			row1.Shape = CreatePolyLine((ISegment) arc);
			row1.Store();

			var test = new QaMinSegAngle(ReadOnlyTableFactory.Create(fc), 0.1, true);

			var runner = new QaContainerTestRunner(1000, test);

			runner.Execute();

			Assert.AreEqual(0, runner.Errors.Count);
		}

		[Test]
		public void CanTestEllipticArc()
		{
			IFeatureClass fc = CreateLineClass(_testWs, "CanTestEllipticArc");

			IFeature row1 = fc.CreateFeature();

			IConstructEllipticArc arc = new EllipticArcClass();
			arc.ConstructEnvelope(GeometryFactory.CreateEnvelope(0, 0, 100, 10));

			row1.Shape = CreatePolyLine((ISegment) arc);
			row1.Store();

			var test = new QaMinSegAngle(ReadOnlyTableFactory.Create(fc), 0.1, true);

			var runner = new QaContainerTestRunner(1000, test);

			runner.Execute();

			Assert.AreEqual(0, runner.Errors.Count);
		}

		[NotNull]
		private static IPolyline CreatePolyLine([NotNull] ISegment segment)
		{
			object missing = Type.Missing;

			ISegmentCollection segments = new PolylineClass();
			segments.AddSegment(segment, ref missing, ref missing);

			((IZAware) segments).ZAware = true;
			((IZ) segments).SetConstantZ(0);

			return (IPolyline) segments;
		}

		private static IFeatureClass CreateLineClass([NotNull] IFeatureWorkspace ws,
		                                             [NotNull] string name)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, true, false));

			return DatasetUtils.CreateSimpleFeatureClass(ws, name, fields);
		}
	}
}
