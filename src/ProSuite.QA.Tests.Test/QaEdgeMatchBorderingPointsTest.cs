using System;
using System.Collections.Generic;
using System.Threading;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaEdgeMatchBorderingPointsTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private IFeatureWorkspace _featureWorkspace;
		private ISpatialReference _spatialReference;
		private const string _issueCodePrefix = "BorderingPoints.";
		private const string _stateIdFieldName = "STATE";
		private const string _textFieldName = "FLD_TEXT";
		private const string _doubleFieldName = "FLD_DOUBLE";
		private const string _dateFieldName = "FLD_DATE";

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();

			_spatialReference = CreateLV95();
			_featureWorkspace = TestWorkspaceUtils.CreateInMemoryWorkspace(
				"QaEdgeMatchBorderingPointsTest");
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanIgnoreCoincidentFeatures()
		{
			IFeatureClass fcPoint1;
			IFeatureClass fcPoint2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcPoint1, out fcPoint2, out fcBorder1, out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// point on border:
			AddPointFeature(fcPoint1, 5, 0, stateId: "A");
			// point on border, exact match:
			AddPointFeature(fcPoint2, 5, 0, stateId: "B");

			var test = new QaEdgeMatchBorderingPoints(
				           ReadOnlyTableFactory.Create(fcPoint1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcPoint2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           PointClass1BorderMatchCondition = "POINT.STATE = BORDER.STATE",
				           PointClass2BorderMatchCondition = "POINT.STATE = BORDER.STATE",
				           BorderingPointMatchCondition = "POINT1.STATE <> POINT2.STATE"
			           };

			AssertErrors(0, Run(test));
		}

		[Test]
		public void CanDetectCoincidentFeaturesWithDifferingAttributes()
		{
			IFeatureClass fcPoint1;
			IFeatureClass fcPoint2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcPoint1, out fcPoint2, out fcBorder1, out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			DateTime dateValue = DateTime.Now;
			// point on border:
			AddPointFeature(fcPoint1, 5, 0, "A", "Y#X", 10.001, dateValue);
			// point on border, exact match:
			AddPointFeature(fcPoint2, 5, 0, "B", "X#Y", 10.002, dateValue);

			var test = new QaEdgeMatchBorderingPoints(
				           ReadOnlyTableFactory.Create(fcPoint1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcPoint2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           PointClass1BorderMatchCondition = "POINT.STATE = BORDER.STATE",
				           PointClass2BorderMatchCondition = "POINT.STATE = BORDER.STATE",
				           BorderingPointMatchCondition = "POINT1.STATE <> POINT2.STATE",
				           BorderingPointEqualAttributes = "FLD_TEXT:#, FLD_DOUBLE, FLD_DATE"
			           };

			AssertErrors(1, Run(test),
			             "Match.ConstraintsNotFulfilled");
		}

		[Test]
		public void CanAttributeConstraintErrorsIndividually()
		{
			IFeatureClass fcPoint1;
			IFeatureClass fcPoint2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcPoint1, out fcPoint2, out fcBorder1, out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			DateTime dateValue = new DateTime(2000, 12, 31);
			// point on border:
			AddPointFeature(fcPoint1, 5, 0, "A", "Y#X", 10.001, dateValue);
			// point on border, exact match:
			AddPointFeature(fcPoint2, 5, 0, "B", "X#Y", 10.002, dateValue + TimeSpan.FromDays(1));

			using (AssertUtils.UseInvariantCulture())
			{
				var test = new QaEdgeMatchBorderingPoints(
					           ReadOnlyTableFactory.Create(fcPoint1), ReadOnlyTableFactory.Create(fcBorder1),
					           ReadOnlyTableFactory.Create(fcPoint2), ReadOnlyTableFactory.Create(fcBorder2),
					                                       searchDistance: 0.5)
				           {
					           PointClass1BorderMatchCondition = "POINT.STATE = BORDER.STATE",
					           PointClass2BorderMatchCondition = "POINT.STATE = BORDER.STATE",
					           BorderingPointMatchCondition = "POINT1.STATE <> POINT2.STATE",
					           BorderingPointEqualAttributes = "FLD_TEXT:#, FLD_DOUBLE, FLD_DATE",
					           ReportIndividualAttributeConstraintViolations = true
				           };

				AssertUtils.ExpectedErrors(
					2, Run(test),
					e => e.Description ==
					     "Values are not equal (FLD_DOUBLE:10.001,10.002)" &&
					     e.AffectedComponent == "FLD_DOUBLE" &&
					     e.IssueCode?.ID == "BorderingPoints.Match.ConstraintsNotFulfilled",
					e => e.Description ==
					     "Values are not equal (FLD_DATE:01/01/2001 00:00:00,12/31/2000 00:00:00)" &&
					     e.AffectedComponent == "FLD_DATE" &&
					     e.IssueCode?.ID == "BorderingPoints.Match.ConstraintsNotFulfilled");
			}
		}

		[Test]
		public void CanDetectNoCandidateFeature()
		{
			IFeatureClass fcPoint1;
			IFeatureClass fcPoint2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcPoint1, out fcPoint2, out fcBorder1, out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// point on border
			AddPointFeature(fcPoint1, 5, 0, stateId: "A");

			var test = new QaEdgeMatchBorderingPoints(
				           ReadOnlyTableFactory.Create(fcPoint1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcPoint2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           PointClass1BorderMatchCondition = "POINT.STATE = BORDER.STATE",
				           PointClass2BorderMatchCondition = "POINT.STATE = BORDER.STATE",
				           BorderingPointMatchCondition = "POINT1.STATE <> POINT2.STATE"
			           };

			AssertErrors(1, Run(test), "NoMatch.NoCandidate");
		}

		[Test]
		public void CanIgnoreNoCandidateFeature()
		{
			IFeatureClass fcPoint1;
			IFeatureClass fcPoint2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcPoint1, out fcPoint2, out fcBorder1, out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// point on border
			AddPointFeature(fcPoint1, 5, 0, stateId: "A");

			var test = new QaEdgeMatchBorderingPoints(
				           ReadOnlyTableFactory.Create(fcPoint1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcPoint2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           PointClass1BorderMatchCondition = "POINT.STATE = BORDER.STATE",
				           PointClass2BorderMatchCondition = "POINT.STATE = BORDER.STATE",
				           BorderingPointMatchCondition = "POINT1.STATE <> POINT2.STATE",
				           AllowNoFeatureWithinSearchDistance = true
			           };

			AssertErrors(0, Run(test));
		}

		[Test]
		public void CanIgnoreUnrelatedFeatures()
		{
			IFeatureClass fcPoint1;
			IFeatureClass fcPoint2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcPoint1, out fcPoint2, out fcBorder1, out fcBorder2);

			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");

			// point on border, but different state id
			AddPointFeature(fcPoint1, 5, 0, stateId: "B");

			var test = new QaEdgeMatchBorderingPoints(
				           ReadOnlyTableFactory.Create(fcPoint1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcPoint2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           PointClass1BorderMatchCondition = "POINT.STATE = BORDER.STATE",
				           PointClass2BorderMatchCondition = "POINT.STATE = BORDER.STATE",
				           BorderingPointMatchCondition = "POINT1.STATE <> POINT2.STATE"
			           };

			AssertErrors(0, Run(test));
		}

		[Test]
		public void CanDetectNonCoincidentCandidateFeature()
		{
			IFeatureClass fcPoint1;
			IFeatureClass fcPoint2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcPoint1, out fcPoint2, out fcBorder1, out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// point on border
			AddPointFeature(fcPoint1, 5, 0, stateId: "A");
			// point on border, lateral offset
			AddPointFeature(fcPoint2, 4.9, 0, stateId: "B");

			var test = new QaEdgeMatchBorderingPoints(
				           ReadOnlyTableFactory.Create(fcPoint1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcPoint2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           PointClass1BorderMatchCondition = "POINT.STATE = BORDER.STATE",
				           PointClass2BorderMatchCondition = "POINT.STATE = BORDER.STATE",
				           BorderingPointMatchCondition = "POINT1.STATE <> POINT2.STATE"
			           };

			AssertErrors(1, Run(test), "NoMatch.CandidateExists.ConstraintsFulfilled");
		}

		[Test]
		public void CanIgnoreNonCoincidentCandidateFeatureWithinCoincidenceTolerance()
		{
			IFeatureClass fcPoint1;
			IFeatureClass fcPoint2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcPoint1, out fcPoint2, out fcBorder1, out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// point on border
			AddPointFeature(fcPoint1, 5, 0, stateId: "A");
			// point on border, lateral offset
			AddPointFeature(fcPoint2, 4.9, 0, stateId: "B");

			var test = new QaEdgeMatchBorderingPoints(
				           ReadOnlyTableFactory.Create(fcPoint1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcPoint2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           PointClass1BorderMatchCondition = "POINT.STATE = BORDER.STATE",
				           PointClass2BorderMatchCondition = "POINT.STATE = BORDER.STATE",
				           BorderingPointMatchCondition = "POINT1.STATE <> POINT2.STATE",
				           CoincidenceTolerance = 0.11
			           };

			AssertErrors(0, Run(test));
		}

		[Test]
		public void CanDetectNonCoincidentCandidateFeatureWithDifferingAttributes()
		{
			IFeatureClass fcPoint1;
			IFeatureClass fcPoint2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcPoint1, out fcPoint2, out fcBorder1, out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			DateTime dateValue1 = DateTime.Now;
			DateTime dateValue2 = DateTime.Now - TimeSpan.FromHours(1);

			// point on border
			AddPointFeature(fcPoint1, 5, 0, "A", "X#Y", 100, dateValue1);
			// point on border, lateral offset
			// - text field different; double difference not significant; date field different
			AddPointFeature(fcPoint2, 4.9, 0, "B", "Y", 100.00000000000001, dateValue2);

			var test = new QaEdgeMatchBorderingPoints(
				           ReadOnlyTableFactory.Create(fcPoint1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcPoint2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           PointClass1BorderMatchCondition = "POINT.STATE = BORDER.STATE",
				           PointClass2BorderMatchCondition = "POINT.STATE = BORDER.STATE",
				           BorderingPointMatchCondition = "POINT1.STATE <> POINT2.STATE",
				           BorderingPointEqualAttributes = "FLD_TEXT:#, FLD_DOUBLE, FLD_DATE"
			           };

			AssertErrors(1, Run(test), "NoMatch.CandidateExists.ConstraintsNotFulfilled");
		}

		[Test]
		public void
			CanIgnoreNonCoincientCandidateFeatureDuetoBorderGapWithinCoincidenceTolerance()
		{
			IFeatureClass fcPoint1;
			IFeatureClass fcPoint2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcPoint1, out fcPoint2, out fcBorder1, out fcBorder2);

			// border lines are NOT coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, -0.1, 0, -0.1, stateId: "B");

			// point on border
			AddPointFeature(fcPoint1, 5, 0, stateId: "A");
			// point on border
			AddPointFeature(fcPoint2, 5, -0.1, stateId: "B");

			var test = new QaEdgeMatchBorderingPoints(
				           ReadOnlyTableFactory.Create(fcPoint1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcPoint2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           PointClass1BorderMatchCondition = "POINT.STATE = BORDER.STATE",
				           PointClass2BorderMatchCondition = "POINT.STATE = BORDER.STATE",
				           BorderingPointMatchCondition = "POINT1.STATE <> POINT2.STATE",
				           CoincidenceTolerance = 0.11
			           };

			AssertErrors(0, Run(test));
		}

		[Test]
		public void CanIgnoreNonCoincientCandidateFeatureDuetoBorderGap()
		{
			IFeatureClass fcPoint1;
			IFeatureClass fcPoint2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcPoint1, out fcPoint2, out fcBorder1, out fcBorder2);

			// border lines are NOT coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, -0.3, 0, -0.3, stateId: "B");

			// point on border
			AddPointFeature(fcPoint1, 5, 0, stateId: "A");
			// point on border
			AddPointFeature(fcPoint2, 5, -0.3, stateId: "B");

			var test = new QaEdgeMatchBorderingPoints(
				           ReadOnlyTableFactory.Create(fcPoint1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcPoint2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           PointClass1BorderMatchCondition = "POINT.STATE = BORDER.STATE",
				           PointClass2BorderMatchCondition = "POINT.STATE = BORDER.STATE",
				           BorderingPointMatchCondition = "POINT1.STATE <> POINT2.STATE",
				           AllowDisjointCandidateFeatureIfBordersAreNotCoincident = true
			           };

			AssertErrors(0, Run(test));
		}

		[Test]
		public void CanDetectNonCoincidentCandidateFeatureDuetoBorderOverlap()
		{
			IFeatureClass fcPoint1;
			IFeatureClass fcPoint2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcPoint1, out fcPoint2, out fcBorder1, out fcBorder2);

			// border lines are NOT coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0.3, 0, 0.3, stateId: "B");

			// point on border
			AddPointFeature(fcPoint1, 5, 0, stateId: "A");
			// point on border
			AddPointFeature(fcPoint2, 5, 0.3, stateId: "B");

			var test = new QaEdgeMatchBorderingPoints(
				           ReadOnlyTableFactory.Create(fcPoint1), ReadOnlyTableFactory.Create(fcBorder1),
						   ReadOnlyTableFactory.Create(fcPoint2), ReadOnlyTableFactory.Create(fcBorder2),
						                               searchDistance: 0.5)
			           {
				           PointClass1BorderMatchCondition = "POINT.STATE = BORDER.STATE",
				           PointClass2BorderMatchCondition = "POINT.STATE = BORDER.STATE",
				           BorderingPointMatchCondition = "POINT1.STATE <> POINT2.STATE"
			           };

			// No error currently reported, since the lines touch (end point on interior of other line)
			AssertErrors(1, Run(test),
			             "NoMatch.CandidateExists.BordersNotCoincident+ConstraintsFulfilled");
		}

		[Test]
		public void CanDetectNoCandidateFeatureDueToOffsetFromBorder()
		{
			IFeatureClass fcPoint1;
			IFeatureClass fcPoint2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcPoint1, out fcPoint2, out fcBorder1, out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// point on border
			AddPointFeature(fcPoint1, 5, 0, stateId: "A");
			// point NOT on border
			AddPointFeature(fcPoint2, 5, 0.1, stateId: "B");

			var test = new QaEdgeMatchBorderingPoints(
				           ReadOnlyTableFactory.Create(fcPoint1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcPoint2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           PointClass1BorderMatchCondition = "POINT.STATE = BORDER.STATE",
				           PointClass2BorderMatchCondition = "POINT.STATE = BORDER.STATE",
				           BorderingPointMatchCondition = "POINT1.STATE <> POINT2.STATE"
			           };

			AssertErrors(1, Run(test),
			             "NoMatch.NoCandidate");
		}

		[Test]
		public void CanDetectNoCandidateFeaturesDueToLateralOffset()
		{
			IFeatureClass fcPoint1;
			IFeatureClass fcPoint2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcPoint1, out fcPoint2, out fcBorder1, out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// point on border
			AddPointFeature(fcPoint1, 5, 0, stateId: "A");
			// point on border, offset > search tolerance
			AddPointFeature(fcPoint2, 4.4, 0, stateId: "B");

			var test = new QaEdgeMatchBorderingPoints(
				           ReadOnlyTableFactory.Create(fcPoint1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcPoint2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           PointClass1BorderMatchCondition = "POINT.STATE = BORDER.STATE",
				           PointClass2BorderMatchCondition = "POINT.STATE = BORDER.STATE",
				           BorderingPointMatchCondition = "POINT1.STATE <> POINT2.STATE"
			           };

			// two errors expected, one for each line (both end on the border)
			AssertErrors(2, Run(test), "NoMatch.NoCandidate");
		}

		[Test]
		public void
			CanIgnoreWhenAllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled()
		{
			IFeatureClass fcPoint1;
			IFeatureClass fcPoint2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcPoint1, out fcPoint2, out fcBorder1, out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// point on border
			AddPointFeature(fcPoint1, 5, 0, stateId: "A", textFieldValue: "X");
			// point on border, offset > search tolerance
			AddPointFeature(fcPoint2, 4.8, 0, stateId: "B", textFieldValue: "X");

			var test = new QaEdgeMatchBorderingPoints(
				           ReadOnlyTableFactory.Create(fcPoint1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcPoint2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           PointClass1BorderMatchCondition = "POINT.STATE = BORDER.STATE",
				           PointClass2BorderMatchCondition = "POINT.STATE = BORDER.STATE",
				           BorderingPointMatchCondition = "POINT1.STATE <> POINT2.STATE",
				           BorderingPointEqualAttributes = "FLD_TEXT",
				           AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled = true
			           };

			AssertErrors(0, Run(test));
		}

		private void CreateFeatureClasses([NotNull] out IFeatureClass fcPoint1,
		                                  [NotNull] out IFeatureClass fcPoint2,
		                                  [NotNull] out IFeatureClass fcBorder1,
		                                  [NotNull] out IFeatureClass fcBorder2)
		{
			// use tick count as suffix to make names unique within test fixture run

			Thread.Sleep(60);
			// make sure that TickCount is unique for each call (increase is non-continuous)
			int ticks = Environment.TickCount;

			fcPoint1 = CreatePointClass(string.Format("p1_{0}", ticks));
			fcPoint2 = CreatePointClass(string.Format("p2_{0}", ticks));
			fcBorder1 = CreateLineClass(string.Format("b1_{0}", ticks));
			fcBorder2 = CreateLineClass(string.Format("b2_{0}", ticks));
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

		private static void AssertErrors(int expectedErrorCount,
		                                 [NotNull] ICollection<QaError> errors,
		                                 [NotNull] params string[] expectedIssueCodes)
		{
			AssertUtils.ExpectedErrors(expectedErrorCount, _issueCodePrefix, errors,
			                           expectedIssueCodes);
		}

		[NotNull]
		private static ISpatialReference CreateLV95()
		{
			ISpatialReference result = SpatialReferenceUtils.CreateSpatialReference
				((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, true);
			SpatialReferenceUtils.SetXYDomain(result, -10000, -10000, 10000, 10000,
			                                  0.0001, 0.001);
			return result;
		}

		private static void AddPointFeature([NotNull] IFeatureClass featureClass,
		                                    double x, double y,
		                                    [CanBeNull] string stateId = null,
		                                    [CanBeNull] string textFieldValue = null,
		                                    double? doubleValue = null,
		                                    DateTime? dateValue = null)
		{
			AddFeature(featureClass,
			           GeometryFactory.CreatePoint(x, y),
			           stateId, textFieldValue, doubleValue, dateValue);
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

		[NotNull]
		private IFeatureClass CreatePointClass([NotNull] string name)
		{
			return CreateFeatureClass(name, esriGeometryType.esriGeometryPoint);
		}

		[NotNull]
		private IFeatureClass CreateLineClass([NotNull] string name)
		{
			return CreateFeatureClass(name, esriGeometryType.esriGeometryPolyline);
		}

		[NotNull]
		private IFeatureClass CreateFeatureClass(string name, esriGeometryType geometryType)
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
