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
	public class QaEdgeMatchBorderingLinesTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private IFeatureWorkspace _featureWorkspace;
		private ISpatialReference _spatialReference;
		private const string _issueCodePrefix = "BorderingLines.";
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
				"QaEdgeMatchBorderingLinesTest");
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanIgnoreConnectedFeatures()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1, out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// connected to border:
			AddLineFeature(fcLine1, 5, 0, 7, 0, stateId: "A");
			// dummy to run
			AddLineFeature(fcLine1, 5, 1, 7, 1, stateId: "A");

			// connected to border, exact match:
			AddLineFeature(fcLine2, 7, 0, 5, 0, stateId: "B");

			var test = new QaEdgeMatchBorderingLines(fcLine1, fcBorder1,
			                                         fcLine2, fcBorder2, 0);

			AssertNoErrors(Run(test, 1000));

			AssertNoErrors(Run(test, 5));
		}

		[Test]
		public void CanIgnoreConnectedFeaturesWithMatchConditions()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1, out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// connected to border:
			AddLineFeature(fcLine1, 5, 0, 7, 0, stateId: "A");
			// dummy to run
			AddLineFeature(fcLine1, 5, 1, 7, 1, stateId: "A");

			// connected to border, exact match:
			AddLineFeature(fcLine2, 7, 0, 5, 0, stateId: "B");

			var test = new QaEdgeMatchBorderingLines(fcLine1, fcBorder1,
			                                         fcLine2, fcBorder2, 0)
			           {
				           LineClass1BorderMatchCondition = "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition = "LINE.STATE = BORDER.STATE",
				           BorderingLineMatchCondition = "LINE1.STATE <> LINE2.STATE"
			           };

			AssertNoErrors(Run(test, 1000));

			AssertNoErrors(Run(test, 5));
		}

		[Test]
		public void CanDetectMissingPartWithBorderMatchConditions()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1, out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// connected to border:
			AddLineFeature(fcLine1, 5, 0, 7, 0, stateId: "NoMatch");
			// dummy to run
			AddLineFeature(fcLine1, 5, 1, 7, 1, stateId: "A");

			// connected to border, exact match:
			AddLineFeature(fcLine2, 7, 0, 5, 0, stateId: "B");

			var test = new QaEdgeMatchBorderingLines(fcLine1, fcBorder1,
			                                         fcLine2, fcBorder2, 0)
			           {
				           LineClass1BorderMatchCondition = "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition = "LINE.STATE = BORDER.STATE",
				           BorderingLineMatchCondition = "LINE1.STATE <> LINE2.STATE"
			           };

			AssertUtils.ExpectedErrors(1, Run(test, 1000));

			AssertUtils.ExpectedErrors(1, Run(test, 5));
		}

		[Test]
		public void CanDetectMissingPartWithLineMatchConditions()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1, out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// connected to border:
			AddLineFeature(fcLine1, 5, 0, 7, 0, stateId: "A");
			// dummy to run
			AddLineFeature(fcLine1, 5, 1, 7, 1, stateId: "A");

			// connected to border, exact match:
			AddLineFeature(fcLine2, 7, 0, 5, 0, stateId: "B");

			var test = new QaEdgeMatchBorderingLines(fcLine1, fcBorder1,
			                                         fcLine2, fcBorder2, 0)
			           {
				           LineClass1BorderMatchCondition = "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition = "LINE.STATE = BORDER.STATE",
				           BorderingLineMatchCondition = "LINE1.STATE = LINE2.STATE"
				           // --> will not be fulfilled
			           };

			AssertUtils.ExpectedErrors(2, Run(test, 1000));

			AssertUtils.ExpectedErrors(2, Run(test, 5));
		}

		[Test]
		public void CanDetectAttributeConstraintError()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1, out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// connected to border:
			AddLineFeature(fcLine1, 5, 0, 7, 0, stateId: "A");
			// dummy to run
			AddLineFeature(fcLine1, 5, 1, 7, 1, stateId: "A");

			// connected to border, exact match:
			AddLineFeature(fcLine2, 7, 0, 5, 0, stateId: "B");

			var test = new QaEdgeMatchBorderingLines(fcLine1, fcBorder1,
			                                         fcLine2, fcBorder2, 0)
			           {
				           LineClass1BorderMatchCondition = "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition = "LINE.STATE = BORDER.STATE",
				           BorderingLineMatchCondition = "LINE1.STATE <> LINE2.STATE",
				           BorderingLineAttributeConstraint = "LINE1.STATE = LINE2.STATE"
			           };

			AssertUtils.ExpectedErrors(1, Run(test, 1000));

			AssertUtils.ExpectedErrors(1, Run(test, 5));
		}

		[Test]
		public void CanAttributeConstraintErrorsIndividually()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1, out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// connected to border:
			AddLineFeature(fcLine1, 5, 0, 7, 0, stateId: "A", textFieldValue: "X");
			// dummy to run
			AddLineFeature(fcLine1, 5, 1, 7, 1, stateId: "A");

			// connected to border, exact match:
			AddLineFeature(fcLine2, 7, 0, 5, 0, stateId: "B", textFieldValue: "Y");

			using (AssertUtils.UseInvariantCulture())
			{
				var test = new QaEdgeMatchBorderingLines(fcLine1, fcBorder1,
				                                         fcLine2, fcBorder2, 0)
				           {
					           LineClass1BorderMatchCondition = "LINE.STATE = BORDER.STATE",
					           LineClass2BorderMatchCondition = "LINE.STATE = BORDER.STATE",
					           BorderingLineMatchCondition = "LINE1.STATE <> LINE2.STATE",
					           BorderingLineAttributeConstraint = "LINE1.STATE = LINE2.STATE",
					           BorderingLineEqualAttributes = _textFieldName,
					           ReportIndividualAttributeConstraintViolations = true
				           };

				AssertUtils.ExpectedErrors(2, Run(test, 1000),
				                           e => e.Description ==
				                                "Values are not equal (FLD_TEXT:'X','Y')" &&
				                                e.AffectedComponent == "FLD_TEXT" &&
				                                e.IssueCode?.ID ==
				                                "BorderingLines.Match.ConstraintsNotFulfilled",
				                           e => e.Description ==
				                                "Constraint is not fulfilled (LINE1.STATE:'A';LINE2.STATE:'B')" &&
				                                e.AffectedComponent == "STATE" &&
				                                e.IssueCode?.ID ==
				                                "BorderingLines.Match.ConstraintsNotFulfilled"
				);

				AssertUtils.ExpectedErrors(2, Run(test, 5));
			}
		}

		[Test]
		public void CanIgnoreConnectedFeaturesWithAreaBorders()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1, out fcBorder2, true);

			// border lines are coincident
			AddFeature(fcBorder1,
			           CurveConstruction.StartPoly(0, 0).LineTo(10, 0).LineTo(5, 5)
			                            .ClosePolygon(),
			           stateId: "A");
			AddFeature(fcBorder2,
			           CurveConstruction.StartPoly(10, 0).LineTo(0, 0).LineTo(5, -5)
			                            .ClosePolygon(),
			           stateId: "B");

			// connected to border:
			AddLineFeature(fcLine1, 5, 0, 7, 0, stateId: "A");

			// connected to border, exact match:
			AddLineFeature(fcLine2, 7, 0, 5, 0, stateId: "B");

			var test = new QaEdgeMatchBorderingLines(fcLine1, fcBorder1,
			                                         fcLine2, fcBorder2, 0)
			           {
				           LineClass1BorderMatchCondition = "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition = "LINE.STATE = BORDER.STATE",
				           BorderingLineMatchCondition = "LINE1.STATE <> LINE2.STATE"
			           };

			AssertNoErrors(Run(test, 1000));

			AssertNoErrors(Run(test, 5));
		}

		[Test]
		public void CanDetectMissingPart()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1, out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// connected to border:
			AddFeature(fcLine1,
			           CurveConstruction.StartLine(4, 2).LineTo(4, 0)
			                            .LineTo(7, 0).LineTo(7, 2).Curve,
			           stateId: "A");

			// connected to border, exact match:
			AddFeature(fcLine2,
			           CurveConstruction.StartLine(8, -2).LineTo(8, 0)
			                            .LineTo(5, 0).LineTo(5, -2).Curve,
			           stateId: "B");

			var test = new QaEdgeMatchBorderingLines(fcLine1, fcBorder1,
			                                         fcLine2, fcBorder2, 0)
			           {
				           LineClass1BorderMatchCondition = "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition = "LINE.STATE = BORDER.STATE",
				           BorderingLineMatchCondition = "LINE1.STATE <> LINE2.STATE"
			           };

			AssertUtils.ExpectedErrors(2, Run(test, 1000));

			AssertUtils.ExpectedErrors(2, Run(test, 5));
		}

		[Test]
		public void CanDetectMissingFull()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1, out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");

			// connected to border:
			AddLineFeature(fcLine1, 5, 0, 7, 0, stateId: "A");
			// dummy to run
			AddLineFeature(fcLine1, 5, 1, 7, 1, stateId: "A");

			var test = new QaEdgeMatchBorderingLines(fcLine1, fcBorder1,
			                                         fcLine2, fcBorder2, 0)
			           {
				           LineClass1BorderMatchCondition = "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition = "LINE.STATE = BORDER.STATE",
				           BorderingLineMatchCondition = "LINE1.STATE <> LINE2.STATE"
			           };

			AssertUtils.ExpectedErrors(1, Run(test, 1000));

			AssertUtils.ExpectedErrors(1, Run(test, 5));
		}

		[Test]
		public void CanIgnoreMissingFull()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1, out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");

			// connected to border:
			AddLineFeature(fcLine1, 5, 0, 7, 0, stateId: "A");
			// dummy to run
			AddLineFeature(fcLine1, 5, 1, 7, 1, stateId: "A");

			var test = new QaEdgeMatchBorderingLines(fcLine1, fcBorder1,
			                                         fcLine2, fcBorder2, 0)
			           {
				           LineClass1BorderMatchCondition = "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition = "LINE.STATE = BORDER.STATE",
				           BorderingLineMatchCondition = "LINE1.STATE <> LINE2.STATE",
				           AllowNoFeatureWithinSearchDistance = true
			           };

			AssertUtils.ExpectedErrors(0, Run(test, 1000));

			AssertUtils.ExpectedErrors(0, Run(test, 5));
		}

		[Test]
		public void CanDetectEndNotCoincident()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1, out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// connected to border:
			AddFeature(fcLine1,
			           CurveConstruction.StartLine(4, 2).LineTo(4, 0).LineTo(8, 0).Curve,
			           stateId: "A");

			// connected to border, exact match:
			AddLineFeature(fcLine2, 8, 0, 5, 0, stateId: "B");
			AddLineFeature(fcLine2, 5, 0, 4, 0, stateId: "B");

			var test = new QaEdgeMatchBorderingLines(fcLine1, fcBorder1,
			                                         fcLine2, fcBorder2, 0)
			           {
				           LineClass1BorderMatchCondition = "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition = "LINE.STATE = BORDER.STATE",
				           BorderingLineMatchCondition = "LINE1.STATE <> LINE2.STATE"
			           };

			AssertUtils.ExpectedErrors(2, Run(test, 1000));

			AssertUtils.ExpectedErrors(2, Run(test, 5));
		}

		[Test]
		public void CanIgnoreEndNotCoincident()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1, out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// connected to border:
			AddFeature(fcLine1,
			           CurveConstruction.StartLine(4, 2).LineTo(4, 0).LineTo(8, 0).Curve,
			           stateId: "A");

			// connected to border, exact match:
			AddLineFeature(fcLine2, 8, 0, 5, 0, stateId: "B");
			AddLineFeature(fcLine2, 5, 0, 4, 0, stateId: "B");

			var test = new QaEdgeMatchBorderingLines(fcLine1, fcBorder1,
			                                         fcLine2, fcBorder2, 0)
			           {
				           LineClass1BorderMatchCondition = "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition = "LINE.STATE = BORDER.STATE",
				           BorderingLineMatchCondition = "LINE1.STATE <> LINE2.STATE",
				           AllowNonCoincidentEndPointsOnBorder = true
			           };

			AssertUtils.ExpectedErrors(0, Run(test, 1000));

			AssertUtils.ExpectedErrors(0, Run(test, 5));
		}

		[Test]
		public void CanDetectNearCoincidentFeatures()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1, out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0.1, 10, 0.1, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// connected to border:
			AddLineFeature(fcLine1, 5, 0.1, 7, 0.1, stateId: "A");

			// connected to border, exact match:
			AddLineFeature(fcLine2, 7, 0, 5, 0, stateId: "B");

			var test = new QaEdgeMatchBorderingLines(fcLine1, fcBorder1,
			                                         fcLine2, fcBorder2, 0.3)
			           {
				           LineClass1BorderMatchCondition = "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition = "LINE.STATE = BORDER.STATE",
				           BorderingLineMatchCondition = "LINE1.STATE <> LINE2.STATE"
			           };

			AssertUtils.ExpectedErrors(1, Run(test, 1000));

			AssertUtils.ExpectedErrors(1, Run(test, 5));
		}

		[Test]
		public void CanIgnoreNearCoincidentFeatures()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1, out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0.1, 10, 0.1, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// connected to border:
			AddLineFeature(fcLine1, 5, 0.1, 7, 0.1, stateId: "A");

			// connected to border, exact match:
			AddLineFeature(fcLine2, 7, 0, 5, 0, stateId: "B");

			var test = new QaEdgeMatchBorderingLines(fcLine1, fcBorder1,
			                                         fcLine2, fcBorder2, 0.3)
			           {
				           LineClass1BorderMatchCondition = "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition = "LINE.STATE = BORDER.STATE",
				           BorderingLineMatchCondition = "LINE1.STATE <> LINE2.STATE",
				           AllowDisjointCandidateFeatureIfBordersAreNotCoincident = true
			           };

			AssertUtils.ExpectedErrors(0, Run(test, 1000));

			AssertUtils.ExpectedErrors(0, Run(test, 5));
		}

		[Test]
		public void
			CanIgnoreWhenAllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			int ticks = CreateFeatureClasses(out fcLine1, out fcLine2,
			                                 out fcBorder1, out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 100, 0, stateId: "A");
			AddLineFeature(fcBorder2, 100, -0.1, 0, -0.1, stateId: "B");

			// connected to border:
			AddLineFeature(fcLine1, 10, 0, 30, 0, stateId: "A", textFieldValue: "Y");
			AddLineFeature(fcLine1, 30, 0, 70, 0, stateId: "A", textFieldValue: "Y");
			AddLineFeature(fcLine1, 70, 0, 90, 0, stateId: "A", textFieldValue: "Y");
			AddLineFeature(fcLine2, 10, -0.1, 15, -0.1, stateId: "B", textFieldValue: "Y");
			AddLineFeature(fcLine2, 15, -0.1, 50, -0.1, stateId: "B", textFieldValue: "Y");
			AddLineFeature(fcLine2, 50, -0.1, 55, -0.1, stateId: "B", textFieldValue: "Y");
			AddLineFeature(fcLine2, 55, -0.1, 90, -0.1, stateId: "B", textFieldValue: "Y");

			var test = new QaEdgeMatchBorderingLines(fcLine1, fcBorder1,
			                                         fcLine2, fcBorder2,
			                                         0.5)
			           {
				           LineClass1BorderMatchCondition = "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition = "LINE.STATE = BORDER.STATE",
				           BorderingLineMatchCondition = "LINE1.STATE <> LINE2.STATE",
				           BorderingLineEqualAttributes = "FLD_TEXT",
				           AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled = true
			           };

			AssertUtils.ExpectedErrors(0, Run(test, 1000));

			AssertUtils.ExpectedErrors(0, Run(test, 2));
		}

		private int CreateFeatureClasses([NotNull] out IFeatureClass fcLine1,
		                                 [NotNull] out IFeatureClass fcLine2,
		                                 [NotNull] out IFeatureClass fcBorder1,
		                                 [NotNull] out IFeatureClass fcBorder2,
		                                 bool borderIsPolygon = false)
		{
			// use tick count as suffix to make names unique within test fixture run

			Thread.Sleep(60);
			// make sure that TickCount is unique for each call (increase is non-continuous)
			int ticks = Environment.TickCount;

			fcLine1 = CreateLineClass(string.Format("l1_{0}", ticks));
			fcLine2 = CreateLineClass(string.Format("l2_{0}", ticks));
			if (! borderIsPolygon)
			{
				fcBorder1 = CreateLineClass(string.Format("b1_{0}", ticks));
				fcBorder2 = CreateLineClass(string.Format("b2_{0}", ticks));
			}
			else
			{
				fcBorder1 = CreateAreaClass(string.Format("b1_{0}", ticks));
				fcBorder2 = CreateAreaClass(string.Format("b2_{0}", ticks));
			}

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

		private IFeatureClass CreateLineClass([NotNull] string name)
		{
			return CreateFeatureClass(name, esriGeometryType.esriGeometryPolyline);
		}

		private IFeatureClass CreateAreaClass([NotNull] string name)
		{
			return CreateFeatureClass(name, esriGeometryType.esriGeometryPolygon);
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
