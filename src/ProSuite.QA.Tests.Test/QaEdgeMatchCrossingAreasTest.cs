using System;
using System.Collections.Generic;
using System.Threading;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaEdgeMatchCrossingAreasTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private IFeatureWorkspace _featureWorkspace;
		private ISpatialReference _spatialReference;
		private const string _issueCodePrefix = "CrossingAreas.";
		private const string _stateIdFieldName = "STATE";
		private const string _textFieldName = "FLD_TEXT";
		private const string _doubleFieldName = "FLD_DOUBLE";
		private const string _dateFieldName = "FLD_DATE";
		private const double _xyTolerance = 0.001;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();

			_spatialReference = CreateLV95();
			_featureWorkspace = TestWorkspaceUtils.CreateInMemoryWorkspace(
				"QaEdgeMatchCrossingAreasTest");
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanIgnoreConnectedFeatures()
		{
			IFeatureClass fcArea1;
			IFeatureClass fcArea2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcArea1, out fcArea2, out fcBorder1, out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// connected to border:
			AddAreaFeature(fcArea1, 5, 0, 7, 5, stateId: "A");
			// connected to border, exact match:
			AddAreaFeature(fcArea2, 5, -5, 7, 0, stateId: "B");

			var test = new QaEdgeMatchCrossingAreas(fcArea1, fcBorder1,
			                                        fcArea2, fcBorder2,
			                                        searchDistance: 0.5,
			                                        boundingClasses1: null,
			                                        boundingClasses2: null)
			           {
				           AreaClass1BorderMatchCondition = "AREA.STATE = BORDER.STATE",
				           AreaClass2BorderMatchCondition = "AREA.STATE = BORDER.STATE",
				           CrossingAreaMatchCondition = "AREA1.STATE <> AREA2.STATE"
			           };

			AssertNoErrors(Run(test, 1000));

			AssertNoErrors(Run(test, 5));
		}

		[Test]
		public void CanIgnoreConnectedFeaturesAtOppositeSplitBorder()
		{
			IFeatureClass fcArea1;
			IFeatureClass fcArea2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcArea1, out fcArea2, out fcBorder1, out fcBorder2);

			// border lines are coincident BUT with a split at opposite side of area split!!
			AddLineFeature(fcBorder1, 0, 0, 5, 0, stateId: "A"); // split point at x=5
			AddLineFeature(fcBorder1, 5, 0, 10, 0, stateId: "A");

			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// connected to split border, one polygon
			AddAreaFeature(fcArea1, 2, -100, 8, 0, stateId: "A");

			// two polygons connected to non-split border
			AddAreaFeature(fcArea2, 2, 0, 5, 100, stateId: "B"); // split point at x=5
			AddAreaFeature(fcArea2, 5, 0, 8, 100, stateId: "B");

			var test = new QaEdgeMatchCrossingAreas(fcArea1, fcBorder1,
			                                        fcArea2, fcBorder2,
			                                        searchDistance: 1,
			                                        boundingClasses1: null,
			                                        boundingClasses2: null)
			           {
				           AreaClass1BorderMatchCondition = "AREA.STATE = BORDER.STATE",
				           AreaClass2BorderMatchCondition = "AREA.STATE = BORDER.STATE",
				           CrossingAreaMatchCondition = "AREA1.STATE <> AREA2.STATE"
			           };

			AssertNoErrors(Run(test, 1000));

			AssertNoErrors(Run(test, 5));
		}

		[Test]
		public void CanIgnoreConnectedFeaturesAtOppositeSplitBorderInEdge()
		{
			IFeatureClass fcArea1;
			IFeatureClass fcArea2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcArea1, out fcArea2, out fcBorder1, out fcBorder2);

			// border lines are coincident BUT with a split at opposite side of area split!!
			AddLineFeature(fcBorder1, 0, 0, 5, 0, stateId: "A"); // split point at x=5
			AddLineFeature(fcBorder1, 5, 0, 0, -5, stateId: "A");

			AddFeature(fcBorder2,
			           CurveConstruction.StartLine(0, -5).LineTo(5, 0).LineTo(0, 0).Curve,
			           stateId: "B");

			// connected to split border, one polygon
			AddFeature(fcArea1,
			           CurveConstruction.StartPoly(2, 0)
			                            .LineTo(5, 0)
			                            .LineTo(2, -3)
			                            .ClosePolygon(), stateId: "A");

			// two polygons connected to non-split border
			AddAreaFeature(fcArea2, 2, 0, 5, 100, stateId: "B"); // split point at x=5
			AddFeature(fcArea2,
			           CurveConstruction.StartPoly(2, -3)
			                            .LineTo(5, 0)
			                            .LineTo(5, -3)
			                            .ClosePolygon(), stateId: "B");

			var test = new QaEdgeMatchCrossingAreas(fcArea1, fcBorder1,
			                                        fcArea2, fcBorder2,
			                                        searchDistance: 1,
			                                        boundingClasses1: null,
			                                        boundingClasses2: null)
			           {
				           AreaClass1BorderMatchCondition = "AREA.STATE = BORDER.STATE",
				           AreaClass2BorderMatchCondition = "AREA.STATE = BORDER.STATE",
				           CrossingAreaMatchCondition = "AREA1.STATE <> AREA2.STATE"
			           };

			AssertNoErrors(Run(test, 1000));

			AssertNoErrors(Run(test, 5));
		}

		[Test]
		public void CanIgnoreConnectedFeaturesAtMultiPartBorders()
		{
			IFeatureClass fcArea1;
			IFeatureClass fcArea2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcArea1, out fcArea2, out fcBorder1, out fcBorder2);

			// border lines are coincident BUT with a split at opposite side of area split!!
			AddLineFeature(fcBorder1, 0, 9, 9, 0, stateId: "A"); // split point at x=5

			AddLineFeature(fcBorder2, 9, 0, 8, 1, stateId: "B");
			AddLineFeature(fcBorder2, 8, 1, 1, 8, stateId: "B");
			AddLineFeature(fcBorder2, 1, 8, 0, 9, stateId: "B");

			// connected to split border, one polygon
			AddFeature(fcArea1,
			           CurveConstruction.StartPoly(0, 9)
			                            .LineTo(9, 0)
			                            .LineTo(0, 0)
			                            .ClosePolygon(), stateId: "A");

			// two polygons connected to non-split border
			AddFeature(fcArea2, CurveConstruction.StartPoly(9, 0)
			                                     .LineTo(0, 9)
			                                     .LineTo(9, 9)
			                                     .ClosePolygon(), stateId: "B");

			var test = new QaEdgeMatchCrossingAreas(fcArea1, fcBorder1,
			                                        fcArea2, fcBorder2,
			                                        searchDistance: 1,
			                                        boundingClasses1: null,
			                                        boundingClasses2: null)
			           {
				           AreaClass1BorderMatchCondition = "AREA.STATE = BORDER.STATE",
				           AreaClass2BorderMatchCondition = "AREA.STATE = BORDER.STATE",
				           CrossingAreaMatchCondition = "AREA1.STATE <> AREA2.STATE"
			           };

			AssertNoErrors(Run(test, 1000));

			AssertNoErrors(Run(test, 2));
		}

		[Test]
		public void CanDetectNotConnected()
		{
			IFeatureClass fcArea1;
			IFeatureClass fcArea2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcArea1, out fcArea2, out fcBorder1, out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// connected to border:
			AddAreaFeature(fcArea1, 5, 0, 8, 5, stateId: "A");
			// connected to border, exact match:
			AddAreaFeature(fcArea2, 4, -5, 7, 0, stateId: "B");

			var test = new QaEdgeMatchCrossingAreas(fcArea1, fcBorder1,
			                                        fcArea2, fcBorder2,
			                                        searchDistance: 0.5,
			                                        boundingClasses1: null,
			                                        boundingClasses2: null)
			           {
				           AreaClass1BorderMatchCondition = "AREA.STATE = BORDER.STATE",
				           AreaClass2BorderMatchCondition = "AREA.STATE = BORDER.STATE",
				           CrossingAreaMatchCondition = "AREA1.STATE <> AREA2.STATE"
			           };

			var expectedErrors =
				new Predicate<QaError>[]
				{
					e => HasCode(e, "NoMatch.NoCandidate") &&
					     HasLength(e, 1) &&
					     HasEnvelope(e, xmin: 7, xmax: 8),
					e => HasCode(e, "NoMatch.NoCandidate") &&
					     HasLength(e, 1) &&
					     HasEnvelope(e, xmin: 4, xmax: 5)
				};

			AssertUtils.ExpectedErrors(2, Run(test, 1000), expectedErrors);

			AssertUtils.ExpectedErrors(2, Run(test, 5), expectedErrors);
		}

		[Test]
		public void CanIgnoreNotConnected()
		{
			IFeatureClass fcArea1;
			IFeatureClass fcArea2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcArea1, out fcArea2, out fcBorder1, out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// connected to border:
			AddAreaFeature(fcArea1, 5, 0, 8, 5, stateId: "A");
			// connected to border, exact match:
			AddAreaFeature(fcArea2, 4, -5, 7, 0, stateId: "B");

			var test = new QaEdgeMatchCrossingAreas(fcArea1, fcBorder1,
			                                        fcArea2, fcBorder2,
			                                        searchDistance: 0.5,
			                                        boundingClasses1: null,
			                                        boundingClasses2: null)
			           {
				           AreaClass1BorderMatchCondition = "AREA.STATE = BORDER.STATE",
				           AreaClass2BorderMatchCondition = "AREA.STATE = BORDER.STATE",
				           CrossingAreaMatchCondition = "AREA1.STATE <> AREA2.STATE",
				           AllowNoFeatureWithinSearchDistance = true
			           };

			AssertNoErrors(Run(test, 1000));
			AssertNoErrors(Run(test, 5));
		}

		[Test]
		public void CanDetectNotConnectedAndBorderGap()
		{
			IFeatureClass fcArea1;
			IFeatureClass fcArea2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcArea1, out fcArea2, out fcBorder1, out fcBorder2);

			// border lines are not coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, -0.3, 0, -0.3, stateId: "B");

			AddAreaFeature(fcArea1, 5, 0, 8, 5, stateId: "A"); // connected
			AddAreaFeature(fcArea2, 4, -5, 7, -0.3, stateId: "B"); // connected

			var test = new QaEdgeMatchCrossingAreas(fcArea1, fcBorder1,
			                                        fcArea2, fcBorder2,
			                                        searchDistance: 0.5,
			                                        boundingClasses1: null,
			                                        boundingClasses2: null)
			           {
				           AreaClass1BorderMatchCondition = "AREA.STATE = BORDER.STATE",
				           AreaClass2BorderMatchCondition = "AREA.STATE = BORDER.STATE",
				           CrossingAreaMatchCondition = "AREA1.STATE <> AREA2.STATE"
			           };

			var expectedErrors =
				new Predicate<QaError>[]
				{
					// TODO currently fails, revise subtraction of buffer from remainder:
					// When search distance is large compared to offset between borders, error 
					// geometry of "NoCandidate" is significantly reduced, to the point that 
					// it can become empty
					// (expected values may have to be adapted, maybe not achievable as defined below)
					e => HasCode(e, "NoMatch.NoCandidate") &&
					     HasLength(e, 1) &&
					     HasEnvelope(e, 4, -0.3, 5, -0.3),
					e =>
						HasCode(
							e, "NoMatch.CandidateExists.BordersNotCoincident.ConstraintsFulfilled") &&
						HasLength(e, 2 + 2) &&
						HasEnvelope(e, 5, -0.3, xmax: 7, ymax: 0),
					e => HasCode(e, "NoMatch.NoCandidate") &&
					     HasLength(e, 1) &&
					     HasEnvelope(e, xmin: 7, ymin: 0, xmax: 8, ymax: 0)
				};

			AssertUtils.ExpectedErrors(3, Run(test, 1000), expectedErrors);

			AssertUtils.ExpectedErrors(3, Run(test, 5), expectedErrors);
		}

		[Test]
		public void CanDetectBorderGapVertical()
		{
			IFeatureClass fcArea1;
			IFeatureClass fcArea2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcArea1, out fcArea2, out fcBorder1, out fcBorder2);

			// border lines are not coincident
			AddLineFeature(fcBorder1, 0, 0, 0, 10, stateId: "A");
			AddLineFeature(fcBorder1, 0.7, -0.3, 10, -0.3, stateId: "A");
			AddLineFeature(fcBorder2, 0.3, 10, 0.3, 0, stateId: "B");
			AddLineFeature(fcBorder2, 10, 0, 0.7, 0, stateId: "B");

			AddAreaFeature(fcArea1, -10, 0, 0, 10, stateId: "A"); // connected
			AddAreaFeature(fcArea1, 0.7, -5, 10, -0.3, stateId: "A"); // connected
			AddAreaFeature(fcArea2, 0.3, 0, 10, 10, stateId: "B"); // connected

			var test = new QaEdgeMatchCrossingAreas(fcArea1, fcBorder1,
			                                        fcArea2, fcBorder2,
			                                        searchDistance: 0.5,
			                                        boundingClasses1: null,
			                                        boundingClasses2: null)
			           {
				           AreaClass1BorderMatchCondition = "AREA.STATE = BORDER.STATE",
				           AreaClass2BorderMatchCondition = "AREA.STATE = BORDER.STATE",
				           CrossingAreaMatchCondition = "AREA1.STATE <> AREA2.STATE"
			           };

			AssertUtils.ExpectedErrors(2, Run(test, 1000));

			AssertUtils.ExpectedErrors(2, Run(test, 3));

			var runner = new QaContainerTestRunner(3, test)
			             {
				             KeepGeometry = true
			             };
			runner.Execute(GeometryFactory.CreateEnvelope(-9, -4, 9, 8));
			AssertUtils.ExpectedErrors(2, runner.Errors);
		}

		[Test]
		public void CanDetectBorderGapBackslashed()
		{
			IFeatureClass fcArea1;
			IFeatureClass fcArea2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcArea1, out fcArea2, out fcBorder1, out fcBorder2);

			// border lines are not coincident
			AddLineFeature(fcBorder1, -2, 2, 1, -2, stateId: "A");
			AddLineFeature(fcBorder2, 1.01, -2, -1.99, 2, stateId: "B");

			AddFeature(fcArea1,
			           CurveConstruction.StartPoly(-2, 2)
			                            .LineTo(1, -2)
			                            .LineTo(-5, -10)
			                            .ClosePolygon(), stateId: "A"); // connected
			AddFeature(fcArea2,
			           CurveConstruction.StartPoly(1.01, -2)
			                            .LineTo(-1.99, 2)
			                            .LineTo(5, 10)
			                            .ClosePolygon(), stateId: "B"); // connected

			var test = new QaEdgeMatchCrossingAreas(fcArea1, fcBorder1,
			                                        fcArea2, fcBorder2,
			                                        searchDistance: 0.2,
			                                        boundingClasses1: null,
			                                        boundingClasses2: null)
			           {
				           AreaClass1BorderMatchCondition = "AREA.STATE = BORDER.STATE",
				           AreaClass2BorderMatchCondition = "AREA.STATE = BORDER.STATE",
				           CrossingAreaMatchCondition = "AREA1.STATE <> AREA2.STATE"
			           };

			AssertUtils.ExpectedErrors(3, Run(test, 1000));

			AssertUtils.ExpectedErrors(3, Run(test, 1));

			var runner = new QaContainerTestRunner(1, test)
			             {
				             KeepGeometry = true
			             };
			runner.Execute(GeometryFactory.CreateEnvelope(-9, -4, 9, 8));
			AssertUtils.ExpectedErrors(3, runner.Errors);
		}

		[Test]
		public void CanDetectNotConnectedAndIgnoreBorderGap()
		{
			IFeatureClass fcArea1;
			IFeatureClass fcArea2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcArea1, out fcArea2, out fcBorder1, out fcBorder2);

			// border lines are not coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, -0.3, 0, -0.3, stateId: "B");

			AddAreaFeature(fcArea1, 5, 0, 8, 5, stateId: "A"); // connected
			AddAreaFeature(fcArea2, 4, -5, 7, -0.3, stateId: "B"); // connected

			var test = new QaEdgeMatchCrossingAreas(fcArea1, fcBorder1,
			                                        fcArea2, fcBorder2,
			                                        searchDistance: 0.5,
			                                        boundingClasses1: null,
			                                        boundingClasses2: null)
			           {
				           AreaClass1BorderMatchCondition = "AREA.STATE = BORDER.STATE",
				           AreaClass2BorderMatchCondition = "AREA.STATE = BORDER.STATE",
				           CrossingAreaMatchCondition = "AREA1.STATE <> AREA2.STATE",
				           AllowDisjointCandidateFeatureIfBordersAreNotCoincident = true
			           };

			var expectedErrors =
				new Predicate<QaError>[]
				{
					// TODO currently fails, revise subtraction of buffer from remainder:
					// When search distance is large compared to offset between borders, error 
					// geometry of "NoCandidate" is significantly reduced, to the point that 
					// it can become empty
					// (expected values may have to be adapted, maybe not achievable as defined below)
					e => HasCode(e, "NoMatch.NoCandidate") &&
					     HasLength(e, 1) &&
					     HasEnvelope(e, 4, -0.3, 5, -0.3),
					e => HasCode(e, "NoMatch.NoCandidate") &&
					     HasLength(e, 1) &&
					     HasEnvelope(e, xmin: 7, ymin: 0, xmax: 8, ymax: 0)
				};

			AssertUtils.ExpectedErrors(2, Run(test, 1000), expectedErrors);

			AssertUtils.ExpectedErrors(2, Run(test, 5), expectedErrors);
		}

		[Test]
		public void CanDetectConnectedAndMissing()
		{
			IFeatureClass fcArea1;
			IFeatureClass fcArea2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcArea1, out fcArea2, out fcBorder1, out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 100, 0, stateId: "A");
			AddLineFeature(fcBorder2, 100, 0, 0, 0, stateId: "B");

			// connected to border:
			AddAreaFeature(fcArea1, 10, 0, 90, 5, stateId: "A");

			AddAreaFeature(fcArea2, 15, -5, 30, 0, stateId: "B"); // connected 
			AddAreaFeature(fcArea2, 30, -5, 50, 0, stateId: "B"); // connected
			AddAreaFeature(fcArea2, 50, -5, 80, 0, stateId: "B"); // connected

			var test = new QaEdgeMatchCrossingAreas(fcArea1, fcBorder1,
			                                        fcArea2, fcBorder2,
			                                        searchDistance: 0.5,
			                                        boundingClasses1: null,
			                                        boundingClasses2: null)
			           {
				           AreaClass1BorderMatchCondition = "AREA.STATE = BORDER.STATE",
				           AreaClass2BorderMatchCondition = "AREA.STATE = BORDER.STATE",
				           CrossingAreaMatchCondition = "AREA1.STATE <> AREA2.STATE"
			           };

			var expectedErrors =
				new Predicate<QaError>[]
				{
					e => HasCode(e, "NoMatch.NoCandidate") &&
					     HasLength(e, 10) &&
					     HasEnvelope(e, xmin: 80, xmax: 90),
					e => HasCode(e, "NoMatch.NoCandidate") &&
					     HasLength(e, 5) &&
					     HasEnvelope(e, xmin: 10, xmax: 15)
				};

			AssertUtils.ExpectedErrors(2, Run(test, 1000), expectedErrors);

			AssertUtils.ExpectedErrors(2, Run(test, 10), expectedErrors);
		}

		[Test]
		public void CanDetectConnectedAndNotConnected()
		{
			IFeatureClass fcArea1;
			IFeatureClass fcArea2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcArea1, out fcArea2, out fcBorder1, out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 100, 0, stateId: "A");
			AddLineFeature(fcBorder2, 40, 0, 0, 0, stateId: "B");
			AddFeature(fcBorder2,
			           CurveConstruction.StartLine(100, 0.2)
			                            .LineTo(50, 0.2)
			                            .LineTo(50, 0)
			                            .LineTo(40, 0)
			                            .Curve,
			           stateId: "B");

			// connected to border:
			AddAreaFeature(fcArea1, 10, 0, 90, 5, stateId: "A");

			AddAreaFeature(fcArea2, 15, -5, 30, 0, stateId: "B"); // connected
			AddAreaFeature(fcArea2, 30, -5, 50, 0, stateId: "B"); // connected
			AddAreaFeature(fcArea2, 50, -5, 80, 0.2, stateId: "B"); // not connected

			var test = new QaEdgeMatchCrossingAreas(fcArea1, fcBorder1,
			                                        fcArea2, fcBorder2,
			                                        searchDistance: 0.5,
			                                        boundingClasses1: null,
			                                        boundingClasses2: null)
			           {
				           AreaClass1BorderMatchCondition = "AREA.STATE = BORDER.STATE",
				           AreaClass2BorderMatchCondition = "AREA.STATE = BORDER.STATE",
				           CrossingAreaMatchCondition = "AREA1.STATE <> AREA2.STATE"
			           };

			// 10-15: missing
			// 50-80: not coincident
			// 80-90: missing
			// TODO currently fails, revise subtraction of buffer from remainder:
			// When search distance is large compared to offset between borders, error 
			// geometry of "NoCandidate" is significantly reduced, to the point that 
			// it can become empty.
			// (expected values may have to be adapted, maybe not achievable as defined below)
			var expectedErrors =
				new Predicate<QaError>[]
				{
					e => HasCode(e, "NoMatch.NoCandidate") &&
					     HasLength(e, 5) &&
					     HasEnvelope(e, xmin: 10, xmax: 15),
					e =>
						HasCode(
							e, "NoMatch.CandidateExists.BordersNotCoincident.ConstraintsFulfilled") &&
						HasLength(e, 30 + 30.2 + 0.5) &&
						HasEnvelope(e, xmin: 49.5, xmax: 80, ymax: 0.2),
					e => HasCode(e, "NoMatch.NoCandidate") &&
					     HasLength(e, 10) &&
					     HasEnvelope(e, xmin: 80, xmax: 90)
				};

			AssertUtils.ExpectedErrors(3, Run(test, 1000), expectedErrors);

			AssertUtils.ExpectedErrors(3, Run(test, 10), expectedErrors);
		}

		[Test]
		public void CanDetectConnectedAndNotConnectedAtReentrantAngle()
		{
			IFeatureClass fcArea1;
			IFeatureClass fcArea2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcArea1, out fcArea2, out fcBorder1, out fcBorder2);

			// border lines are coincident
			AddFeature(fcBorder1,
			           CurveConstruction.StartLine(10, 100)
			                            .LineTo(0, 0)
			                            .LineTo(0, -2)
			                            .LineTo(100, -2)
			                            .Curve, stateId: "A");
			AddFeature(fcBorder2,
			           CurveConstruction.StartLine(100, -2)
			                            .LineTo(0, -2)
			                            .LineTo(0, 0)
			                            .LineTo(10, 100)
			                            .Curve, stateId: "B");

			// connected to border:
			AddFeature(fcArea1,
			           CurveConstruction.StartPoly(10, 100)
			                            .LineTo(0, 0)
			                            .LineTo(-10, 0)
			                            .LineTo(-10, 100)
			                            .ClosePolygon(), stateId: "A");
			AddFeature(fcArea1,
			           CurveConstruction.StartPoly(0, 0)
			                            .LineTo(0, -2)
			                            .LineTo(100, -2)
			                            .LineTo(100, -10)
			                            .LineTo(-10, -10)
			                            .LineTo(-10, 0)
			                            .ClosePolygon(), stateId: "A");

			AddAreaFeature(fcArea2, 0, -2, 100, 0, stateId: "B"); // connected
			AddFeature(fcArea2,
			           CurveConstruction.StartPoly(0.5, 5)
			                            .LineTo(50, 50)
			                            .Line(50, 100)
			                            .LineTo(10, 100)
			                            .ClosePolygon(), stateId: "B"); // connected

			var test = new QaEdgeMatchCrossingAreas(fcArea1, fcBorder1,
			                                        fcArea2, fcBorder2,
			                                        searchDistance: 10,
			                                        boundingClasses1: null,
			                                        boundingClasses2: null)
			           {
				           AreaClass1BorderMatchCondition = "AREA.STATE = BORDER.STATE",
				           AreaClass2BorderMatchCondition = "AREA.STATE = BORDER.STATE",
				           CrossingAreaMatchCondition = "AREA1.STATE <> AREA2.STATE"
			           };

			AssertUtils.ExpectedErrors(1, Run(test, 1000));
		}

		[Test]
		public void CanIgnoreNotConnectedWithBoundingFeature()
		{
			IFeatureClass fcArea1;
			IFeatureClass fcArea2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			int ticks = CreateFeatureClasses(out fcArea1, out fcArea2,
			                                 out fcBorder1, out fcBorder2);

			IFeatureClass boundingLineClass = CreateLineClass(string.Format("el1_{0}", ticks));
			IFeatureClass boundingPointClass =
				CreateFeatureClass(string.Format("ep1_{0}", ticks),
				                   esriGeometryType.esriGeometryPoint);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 100, 0, stateId: "A");
			AddLineFeature(fcBorder2, 100, 0, 0, 0, stateId: "B");

			// connected to border:
			AddAreaFeature(fcArea1, 10, 0, 90, 5, stateId: "A");
			AddAreaFeature(fcArea2, 20, -5, 40, 0, stateId: "B"); // connected
			AddAreaFeature(fcArea2, 60, -5, 80, 0, stateId: "B"); // connected
			AddAreaFeature(fcArea2, 0, -5, 8, 0, stateId: "B"); // not connected

			AddLineFeature(boundingLineClass, 12, 0, 16, 0);
			AddFeature(boundingPointClass, GeometryFactory.CreatePoint(85, 0));
			AddFeature(boundingPointClass, GeometryFactory.CreatePoint(4, 0));

			var test = new QaEdgeMatchCrossingAreas(fcArea1, fcBorder1,
			                                        fcArea2, fcBorder2,
			                                        0.5,
			                                        new[]
			                                        {
				                                        boundingLineClass,
				                                        boundingPointClass
			                                        },
			                                        boundingClasses2: null)
			           {
				           AreaClass1BorderMatchCondition = "AREA.STATE = BORDER.STATE",
				           AreaClass2BorderMatchCondition = "AREA.STATE = BORDER.STATE",
				           CrossingAreaMatchCondition = "AREA1.STATE <> AREA2.STATE"
			           };

			// fcArea1: 40-60: missing
			// fcArea2: 0 - 8: missing
			var expectedErrors =
				new Predicate<QaError>[]
				{
					e => HasCode(e, "NoMatch.NoCandidate") &&
					     HasLength(e, 8) &&
					     HasEnvelope(e, xmin: 0, xmax: 8),
					e => HasCode(e, "NoMatch.NoCandidate") &&
					     HasLength(e, 20) &&
					     HasEnvelope(e, xmin: 40, xmax: 60)
				};

			AssertUtils.ExpectedErrors(2, Run(test, 1000), expectedErrors);

			AssertUtils.ExpectedErrors(2, Run(test, 1000), expectedErrors);
		}

		[Test]
		public void CheckAreErrorsIndependentOnFeatureOrder()
		{
			IFeatureClass fcArea1;
			IFeatureClass fcArea2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcArea1, out fcArea2,
			                     out fcBorder1, out fcBorder2);

			var border11 =
				(IPolyline) CurveConstruction.StartLine(40, 60).LineTo(0, 50).Curve;
			var border12 =
				(IPolyline) CurveConstruction.StartLine(0, 50).LineTo(200, 0).Curve;

			var border21 =
				(IPolyline) CurveConstruction.StartLine(40, 60.01).LineTo(0, 50.01).Curve;
			var border22 =
				(IPolyline) CurveConstruction.StartLine(0, 50.01).LineTo(200, 0.01).Curve;

			// connected to border:
			IPolygon area11 = CurveConstruction.StartPoly(40, 60).LineTo(0, 50).LineTo(200, 0)
			                                   .ClosePolygon();

			IPolygon area21 =
				CurveConstruction.StartPoly(40, 60.01).LineTo(0, 50.01).LineTo(-100, 50)
				                 .ClosePolygon();

			IPolygon area22 =
				CurveConstruction.StartPoly(0, 50.01).LineTo(200, 0.01).LineTo(100, -50)
				                 .ClosePolygon();

			QaEdgeMatchCrossingAreas test = CreateTest(new[] {area22, area21},
			                                           new[] {border21, border22},
			                                           new[] {area11},
			                                           new[] {border11, border12});

			var errors1 = new List<QaError>(Run(test, 1000));

			test = CreateTest(new[] {area11}, new[] {border11, border12},
			                  new[] {area21, area22}, new[] {border21, border22});

			var errors2 = new List<QaError>(Run(test, 1000));

			Assert.AreEqual(errors1.Count, errors2.Count);
			// TODO : Compare Errors
		}

		[Test]
		public void
			CanIgnoreWhenAllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled()
		{
			IFeatureClass fcArea1;
			IFeatureClass fcArea2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcArea1, out fcArea2,
			                     out fcBorder1, out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 100, 0, stateId: "A");
			AddLineFeature(fcBorder2, 100, -0.1, 0, -0.1, stateId: "B");

			// connected to border:
			AddAreaFeature(fcArea1, 10, 0, 30, 5, stateId: "A", textFieldValue: "Y");
			AddAreaFeature(fcArea1, 30, 0, 70, 5, stateId: "A", textFieldValue: "Y");
			AddAreaFeature(fcArea1, 70, 0, 90, 5, stateId: "A", textFieldValue: "Y");
			AddAreaFeature(fcArea2, 10, -5, 15, -0.1, stateId: "B", textFieldValue: "Y");
			// not connected
			AddAreaFeature(fcArea2, 15, -5, 50, -0.1, stateId: "B", textFieldValue: "Y");
			// not connected
			AddAreaFeature(fcArea2, 50, -5, 55, -0.1, stateId: "B", textFieldValue: "Y");
			// not connected
			AddAreaFeature(fcArea2, 55, -5, 90, -0.1, stateId: "B", textFieldValue: "Y");
			// not connected

			var test = new QaEdgeMatchCrossingAreas(fcArea1, fcBorder1,
			                                        fcArea2, fcBorder2,
			                                        0.5,
			                                        boundingClasses1: null,
			                                        boundingClasses2: null)
			           {
				           AreaClass1BorderMatchCondition = "AREA.STATE = BORDER.STATE",
				           AreaClass2BorderMatchCondition = "AREA.STATE = BORDER.STATE",
				           CrossingAreaMatchCondition = "AREA1.STATE <> AREA2.STATE",
				           CrossingAreaEqualAttributes = "FLD_TEXT",
				           AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled = true
			           };

			AssertUtils.ExpectedErrors(0, Run(test, 1000));

			AssertUtils.ExpectedErrors(0, Run(test, 2));
		}

		private QaEdgeMatchCrossingAreas
			CreateTest(IEnumerable<IPolygon> areas1, IEnumerable<IPolyline> borders1,
			           IEnumerable<IPolygon> areas2, IEnumerable<IPolyline> borders2)
		{
			IFeatureClass fcArea1;
			IFeatureClass fcArea2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcArea1, out fcArea2,
			                     out fcBorder1, out fcBorder2);

			foreach (IPolygon area in areas1)
			{
				AddFeature(fcArea1, area, stateId: "A");
			}

			foreach (IPolyline border in borders1)
			{
				AddFeature(fcBorder1, border, stateId: "A");
			}

			foreach (IPolygon area in areas2)
			{
				AddFeature(fcArea2, area, stateId: "B");
			}

			foreach (IPolyline border in borders2)
			{
				AddFeature(fcBorder2, border, stateId: "B");
			}

			var test = new QaEdgeMatchCrossingAreas(fcArea1, fcBorder1,
			                                        fcArea2, fcBorder2,
			                                        10, null, null)
			           {
				           AreaClass1BorderMatchCondition = "AREA.STATE = BORDER.STATE",
				           AreaClass2BorderMatchCondition = "AREA.STATE = BORDER.STATE",
				           CrossingAreaMatchCondition = "AREA1.STATE <> AREA2.STATE"
			           };
			return test;
		}

		private int CreateFeatureClasses([NotNull] out IFeatureClass fcArea1,
		                                 [NotNull] out IFeatureClass fcArea2,
		                                 [NotNull] out IFeatureClass fcBorder1,
		                                 [NotNull] out IFeatureClass fcBorder2)
		{
			// use tick count as suffix to make names unique within test fixture run

			Thread.Sleep(60);

			// make sure that TickCount is unique for each call (increase is non-continuous)
			int ticks = Environment.TickCount;

			fcArea1 = CreateAreaClass(string.Format("a1_{0}", ticks));
			fcArea2 = CreateAreaClass(string.Format("a2_{0}", ticks));
			fcBorder1 = CreateLineClass(string.Format("b1_{0}", ticks));
			fcBorder2 = CreateLineClass(string.Format("b2_{0}", ticks));

			return ticks;
		}

		[NotNull]
		private static IList<QaError> Run([NotNull] ITest test, double? tileSize = null)
		{
			Console.WriteLine(@"Tile size: {0}",
			                  tileSize == null ? "<null>" : tileSize.ToString());
			const string newLine = "\n";
			// r# unit test output adds 2 lines for Environment.NewLine
			Console.Write(newLine);

			QaTestRunnerBase runner = tileSize == null
				                          ? (QaTestRunnerBase) new QaTestRunner(test)
				                          : new QaContainerTestRunner(tileSize.Value, test)
				                            {
					                            KeepGeometry = true
				                            };

			runner.Execute();

			return runner.Errors;
		}

		private static void AssertNoErrors([NotNull] IList<QaError> errors)
		{
			Assert.AreEqual(0, errors.Count);
		}

		private static bool HasCode([NotNull] QaError e,
		                            [NotNull] string expectedIssueCodeId)
		{
			string code = e.IssueCode?.ID;
			return StripPrefix(code) == expectedIssueCodeId;
		}

		private static bool HasLength([NotNull] QaError e, double expectedLength)
		{
			var polycurve = e.Geometry as IPolycurve;
			double length = polycurve?.Length ?? 0;

			return AreEqualWithinTolerance(expectedLength, length);
		}

		private static bool HasEnvelope([NotNull] QaError e,
		                                double? xmin = null,
		                                double? ymin = null,
		                                double? xmax = null,
		                                double? ymax = null)
		{
			IGeometry geometry = e.Geometry;
			if (geometry == null || geometry.IsEmpty)
			{
				// no defined envelope
				return false;
			}

			IEnvelope extent = geometry.Envelope;

			return AreEqualWithinTolerance(xmin, extent.XMin) &&
			       AreEqualWithinTolerance(ymin, extent.YMin) &&
			       AreEqualWithinTolerance(xmax, extent.XMax) &&
			       AreEqualWithinTolerance(ymax, extent.YMax);
		}

		private static bool AreEqualWithinTolerance(double? expected, double actual)
		{
			return expected == null || MathUtils.AreEqual(expected.Value, actual, _xyTolerance);
		}

		[CanBeNull]
		private static string StripPrefix([CanBeNull] string issueCodeId)
		{
			if (issueCodeId == null)
			{
				return null;
			}

			return issueCodeId.StartsWith(_issueCodePrefix)
				       ? issueCodeId.Substring(_issueCodePrefix.Length)
				       : issueCodeId;
		}

		[NotNull]
		private static ISpatialReference CreateLV95()
		{
			ISpatialReference result = SpatialReferenceUtils.CreateSpatialReference
				((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, true);
			SpatialReferenceUtils.SetXYDomain(result, -10000, -10000, 10000, 10000,
			                                  0.0001, _xyTolerance);
			return result;
		}

		private static void AddLineFeature([NotNull] IFeatureClass featureClass,
		                                   double x1, double y1,
		                                   double x2, double y2,
		                                   [CanBeNull] string stateId = null,
		                                   [CanBeNull] string textFieldValue = null,
		                                   double? doubleValue = null,
		                                   DateTime? dateValue = null)
		{
			AddFeature(featureClass,
			           GeometryFactory.CreatePolyline(x1, y1, x2, y2),
			           stateId, textFieldValue, doubleValue, dateValue);
		}

		private static void AddAreaFeature([NotNull] IFeatureClass featureClass,
		                                   double xmin, double ymin,
		                                   double xmax, double ymax,
		                                   [CanBeNull] string stateId = null,
		                                   [CanBeNull] string textFieldValue = null,
		                                   double? doubleValue = null,
		                                   DateTime? dateValue = null)
		{
			AddFeature(featureClass,
			           GeometryFactory.CreatePolygon(xmin, ymin, xmax, ymax),
			           stateId, textFieldValue, doubleValue, dateValue);
		}

		private static void AddFeature([NotNull] IFeatureClass featureClass,
		                               [NotNull] IGeometry geometry,
		                               [CanBeNull] string stateId = null,
		                               [CanBeNull] string textFieldValue = null,
		                               double? doubleValue = null,
		                               DateTime? dateValue = null)
		{
			IFeature feature = featureClass.CreateFeature();
			feature.Shape = geometry;

			if (stateId != null)
			{
				SetValue(feature, _stateIdFieldName, stateId);
			}

			if (textFieldValue != null)
			{
				SetValue(feature, _textFieldName, textFieldValue);
			}

			if (doubleValue != null)
			{
				SetValue(feature, _doubleFieldName, doubleValue.Value);
			}

			if (dateValue != null)
			{
				SetValue(feature, _dateFieldName, dateValue.Value);
			}

			feature.Store();
		}

		private static void SetValue([NotNull] IRow row,
		                             [NotNull] string fieldName,
		                             [CanBeNull] object value)
		{
			int index = row.Fields.FindField(fieldName);
			Assert.True(index >= 0);

			row.Value[index] = value ?? DBNull.Value;
		}

		private IFeatureClass CreateAreaClass([NotNull] string name)
		{
			return CreateFeatureClass(name, esriGeometryType.esriGeometryPolygon);
		}

		private IFeatureClass CreateLineClass([NotNull] string name)
		{
			return CreateFeatureClass(name, esriGeometryType.esriGeometryPolyline);
		}

		private IFeatureClass CreateFeatureClass([NotNull] string name,
		                                         esriGeometryType geometryType)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", geometryType, _spatialReference, 1000));

			fields.AddField(FieldUtils.CreateTextField(_stateIdFieldName, 100));

			fields.AddField(FieldUtils.CreateTextField(_textFieldName, 200));
			fields.AddField(FieldUtils.CreateDoubleField(_doubleFieldName));
			fields.AddField(FieldUtils.CreateDateField(_dateFieldName));

			return DatasetUtils.CreateSimpleFeatureClass(
				_featureWorkspace, name, fields);
		}
	}
}
