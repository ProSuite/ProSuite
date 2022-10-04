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
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaEdgeMatchCrossingLinesTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private IFeatureWorkspace _featureWorkspace;
		private ISpatialReference _spatialReference;
		private const string _issueCodePrefix = "CrossingLines.";
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
				"QaEdgeMatchCrossingLinesTest");
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
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1,
			                     out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// connected to border:
			AddLineFeature(fcLine1, 5, 0, 5, 5, stateId: "A");
			// connected to border, exact match:
			AddLineFeature(fcLine2, 5, 0, 5, -5, stateId: "B");

			var test = new QaEdgeMatchCrossingLines(
				           ReadOnlyTableFactory.Create(fcLine1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcLine2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           LineClass1BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           CrossingLineMatchCondition = "LINE1.STATE <> LINE2.STATE"
			           };

			AssertErrors(0, Run(test));
		}

		[Test]
		public void CanDetectAttributeConstraintError()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1,
			                     out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// connected to border:
			AddLineFeature(fcLine1, 5, 0, 5, 5, stateId: "A", doubleValue: 3,
			               textFieldValue: "X");
			// connected to border, exact match:
			AddLineFeature(fcLine2, 5, 0, 5, -5, stateId: "B", doubleValue: 1,
			               textFieldValue: "Y");

			var test = new QaEdgeMatchCrossingLines(
				           ReadOnlyTableFactory.Create(fcLine1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcLine2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           LineClass1BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           CrossingLineMatchCondition = "LINE1.STATE <> LINE2.STATE",
				           CrossingLineAttributeConstraint =
					           string.Format(
						           "(LINE1.{0} = 2 * LINE2.{0}) OR (LINE2.{0} = 2 * LINE1.{0}) ",
						           _doubleFieldName),
				           CrossingLineEqualAttributes =
					           string.Format("{0},{1}", _doubleFieldName,
					                         _textFieldName)
			           };

			AssertErrors(1, Run(test));
		}

		[Test]
		public void CanAttributeConstraintErrorsIndividually()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2,
			                     out fcBorder1, out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// connected to border:
			AddLineFeature(fcLine1, 5, 0, 5, 5, stateId: "A", doubleValue: 3,
			               textFieldValue: "X");
			// connected to border, exact match:
			AddLineFeature(fcLine2, 5, 0, 5, -5, stateId: "B", doubleValue: 1,
			               textFieldValue: "Y");

			using (AssertUtils.UseInvariantCulture())
			{
				var test = new QaEdgeMatchCrossingLines(
					           ReadOnlyTableFactory.Create(fcLine1), ReadOnlyTableFactory.Create(fcBorder1),
					           ReadOnlyTableFactory.Create(fcLine2), ReadOnlyTableFactory.Create(fcBorder2),
					                                       searchDistance: 0.5)
				           {
					           LineClass1BorderMatchCondition = "LINE.STATE = BORDER.STATE",
					           LineClass2BorderMatchCondition = "LINE.STATE = BORDER.STATE",
					           CrossingLineMatchCondition = "LINE1.STATE <> LINE2.STATE",
					           CrossingLineAttributeConstraint =
						           string.Format(
							           "(LINE1.{0} = 2 * LINE2.{0}) OR (LINE2.{0} = 2 * LINE1.{0}) ",
							           _doubleFieldName),
					           CrossingLineEqualAttributes = $"{_doubleFieldName},{_textFieldName}",
					           ReportIndividualAttributeConstraintViolations = true
				           };

				AssertUtils.ExpectedErrors(
					3, Run(test),
					e => e.Description ==
					     "Constraint is not fulfilled (LINE1.FLD_DOUBLE:3;LINE2.FLD_DOUBLE:1)" &&
					     e.AffectedComponent == "FLD_DOUBLE" &&
					     e.IssueCode?.ID == "CrossingLines.Match.ConstraintsNotFulfilled",
					e => e.Description ==
					     "Values are not equal (FLD_DOUBLE:1,3)" &&
					     e.AffectedComponent == "FLD_DOUBLE" &&
					     e.IssueCode?.ID == "CrossingLines.Match.ConstraintsNotFulfilled",
					e => e.Description ==
					     "Values are not equal (FLD_TEXT:'X','Y')" &&
					     e.AffectedComponent == "FLD_TEXT" &&
					     e.IssueCode?.ID == "CrossingLines.Match.ConstraintsNotFulfilled");
			}
		}

		[Test]
		public void CanDetectAttributeConstraintErrorForPolygonBorders()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1,
			                     out fcBorder2, bordersAsPolygon: true);

			// border lines are coincident
			AddPolyFeature(fcBorder1, 0, 0, 10, 10, stateId: "A");
			AddPolyFeature(fcBorder2, 0, -10, 10, 0, stateId: "B");

			// connected to border:
			AddLineFeature(fcLine1, 5, 0, 5, 5, stateId: "A", doubleValue: 3,
			               textFieldValue: "X");
			// connected to border, exact match:
			AddLineFeature(fcLine2, 5, 0, 5, -5, stateId: "B", doubleValue: 1,
			               textFieldValue: "Y");

			var test = new QaEdgeMatchCrossingLines(
				           ReadOnlyTableFactory.Create(fcLine1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcLine2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           LineClass1BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           CrossingLineMatchCondition = "LINE1.STATE <> LINE2.STATE",
				           CrossingLineAttributeConstraint =
					           string.Format(
						           "(LINE1.{0} = 2 * LINE2.{0}) OR (LINE2.{0} = 2 * LINE1.{0}) ",
						           _doubleFieldName),
				           CrossingLineEqualAttributes =
					           string.Format("{0},{1}", _doubleFieldName,
					                         _textFieldName)
			           };

			AssertErrors(1, Run(test));
		}

		[Test]
		public void CanIgnoreEndPointsConnectingAtInterior()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1,
			                     out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// connected to border:
			AddLineFeature(fcLine1, 1, 0, 5, 0, 5, 5, stateId: "A",
			               textFieldValue: "A");
			// connected to border, touching interior, attribute constraints fulfilled:
			AddLineFeature(fcLine2, 5, 0, 5, -5, stateId: "B", textFieldValue: "A");

			var test = new QaEdgeMatchCrossingLines(
				           ReadOnlyTableFactory.Create(fcLine1),
				           ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcLine2),
				           ReadOnlyTableFactory.Create(fcBorder2),
				           searchDistance: 0.5)
			           {
				           LineClass1BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           CrossingLineMatchCondition = "LINE1.STATE <> LINE2.STATE",
				           CrossingLineEqualAttributes = "FLD_TEXT",
				           AllowEndPointsConnectingToInteriorOfValidNeighborLine =
					           true,
				           IgnoreNeighborLinesWithBorderConnectionOutsideSearchDistance
					           = false,
				           IgnoreEndPointsOfBorderingLines = false
			           };

			// the border connection at 1,0 of the first line is reported, 
			// but the end point of the second line at 5,0 is not (since it connects to the 
			// interior of the first line)
			AssertErrors(1, Run(test), "NoMatch.NoCandidate");
		}

		[Test]
		public void CanDetectNearEndPointsWithIgnoreEndPointsConnectingAtInterior()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1,
			                     out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// connected to border:
			AddLineFeature(fcLine1, 4.8, 0, 5, 0, 5, 5, stateId: "A",
			               textFieldValue: "A");
			// connected to border, touching interior, attribute constraints fulfilled:
			AddLineFeature(fcLine2, 5, 0, 5, -5, stateId: "B", textFieldValue: "A");

			var test = new QaEdgeMatchCrossingLines(
				           ReadOnlyTableFactory.Create(fcLine1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcLine2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           LineClass1BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           CrossingLineMatchCondition = "LINE1.STATE <> LINE2.STATE",
				           CrossingLineEqualAttributes = "FLD_TEXT",
				           AllowEndPointsConnectingToInteriorOfValidNeighborLine =
					           true,
				           IgnoreNeighborLinesWithBorderConnectionOutsideSearchDistance
					           = false,
				           IgnoreEndPointsOfBorderingLines = false
			           };

			// the border connection at 4.8,0 of the first line must be reported, 
			// but the end point of the second line at 5,0 is not (since it connects to the 
			// interior of the first line)
			AssertErrors(1, Run(test), "NoMatch.CandidateExists.ConstraintsFulfilled");

			// Invert the order to check independence on sort order
			test = new QaEdgeMatchCrossingLines(
				       ReadOnlyTableFactory.Create(fcLine2), ReadOnlyTableFactory.Create(fcBorder2),
				       ReadOnlyTableFactory.Create(fcLine1), ReadOnlyTableFactory.Create(fcBorder1),
				                                   searchDistance: 0.5)
			       {
				       LineClass1BorderMatchCondition = "LINE.STATE = BORDER.STATE",
				       LineClass2BorderMatchCondition = "LINE.STATE = BORDER.STATE",
				       CrossingLineMatchCondition = "LINE1.STATE <> LINE2.STATE",
				       CrossingLineEqualAttributes = "FLD_TEXT",
				       AllowEndPointsConnectingToInteriorOfValidNeighborLine = true,
				       IgnoreNeighborLinesWithBorderConnectionOutsideSearchDistance =
					       false,
				       IgnoreEndPointsOfBorderingLines = false
			       };

			AssertErrors(1, Run(test), "NoMatch.CandidateExists.ConstraintsFulfilled");
		}

		[Test]
		public void CanDetectEndPointsConnectingAtInteriorIfConstraintsAreViolated()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1,
			                     out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// connected to border:
			AddLineFeature(fcLine1, 1, 0, 5, 0, 5, 5, stateId: "A",
			               textFieldValue: "value1");
			// connected to border, touching interior:
			AddLineFeature(fcLine2, 5, 0, 5, -5, stateId: "B",
			               textFieldValue: "value2");

			var test = new QaEdgeMatchCrossingLines(
				           ReadOnlyTableFactory.Create(fcLine1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcLine2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           LineClass1BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           CrossingLineMatchCondition = "LINE1.STATE <> LINE2.STATE",
				           CrossingLineEqualAttributes = "FLD_TEXT",
				           AllowEndPointsConnectingToInteriorOfValidNeighborLine =
					           true,
				           IgnoreNeighborLinesWithBorderConnectionOutsideSearchDistance
					           = false,
				           IgnoreEndPointsOfBorderingLines = false
			           };

			// the border connection at 1,0 of the first line is reported, 
			// the connection of the second line to the interior of the first line is also reported, 
			// since the attribute constraints are not fulfilled
			AssertErrors(2, Run(test),
			             "NoMatch.NoCandidate",
			             "Match.ConstraintsNotFulfilled");
		}

		[Test]
		public void CanDetectEndPointsConnectingAtInterior()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1,
			                     out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// connected to border:
			AddLineFeature(fcLine1, 1, 0, 5, 0, 5, 5, stateId: "A");
			// connected to border, touching interior:
			AddLineFeature(fcLine2, 5, 0, 5, -5, stateId: "B");

			var test = new QaEdgeMatchCrossingLines(
				           ReadOnlyTableFactory.Create(fcLine1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcLine2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           LineClass1BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           CrossingLineMatchCondition = "LINE1.STATE <> LINE2.STATE",
				           AllowEndPointsConnectingToInteriorOfValidNeighborLine =
					           false,
				           IgnoreNeighborLinesWithBorderConnectionOutsideSearchDistance
					           = false,
				           IgnoreEndPointsOfBorderingLines = false
			           };

			// BOTH the border connection at 1,0 of the first line is reported, 
			// AND the end point of the second line at 5,0 (even if it connects to the 
			// interior of the first line)
			AssertErrors(2, Run(test),
			             "NoMatch.CandidateExists.ConstraintsFulfilled",
			             "NoMatch.NoCandidate");
		}

		[Test]
		public void CanDetectConnectedFeaturesWithDifferingAttributes()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1,
			                     out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			DateTime dateValue = DateTime.Now;
			// connected to border:
			AddLineFeature(fcLine1, 5, 0, 5, 5, "A", "Y#X", 10.001, dateValue);
			// connected to border, exact match:
			AddLineFeature(fcLine2, 5, 0, 5, -5, "B", "X#Y", 10.002, dateValue);

			var test = new QaEdgeMatchCrossingLines(
				           ReadOnlyTableFactory.Create(fcLine1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcLine2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           LineClass1BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           CrossingLineMatchCondition = "LINE1.STATE <> LINE2.STATE",
				           CrossingLineEqualAttributes =
					           "FLD_TEXT:#, FLD_DOUBLE, FLD_DATE"
			           };

			AssertErrors(1, Run(test),
			             "Match.ConstraintsNotFulfilled");
		}

		[Test]
		public void CanDetectNoCandidateFeature()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1,
			                     out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// connected to border
			AddLineFeature(fcLine1, 5, 0, 5, 5, stateId: "A");

			var test = new QaEdgeMatchCrossingLines(
				           ReadOnlyTableFactory.Create(fcLine1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcLine2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           LineClass1BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           CrossingLineMatchCondition = "LINE1.STATE <> LINE2.STATE"
			           };

			AssertErrors(1, Run(test), "NoMatch.NoCandidate");
		}

		[Test]
		public void CanIgnoreNoCandidateFeature()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1,
			                     out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// connected to border
			AddLineFeature(fcLine1, 5, 0, 5, 5, stateId: "A");

			var test = new QaEdgeMatchCrossingLines(
				           ReadOnlyTableFactory.Create(fcLine1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcLine2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           LineClass1BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           CrossingLineMatchCondition = "LINE1.STATE <> LINE2.STATE",
				           AllowNoFeatureWithinSearchDistance = true
			           };

			AssertErrors(0, Run(test));
		}

		[Test]
		public void CanDetectConnectionOnSameSide()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1,
			                     out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// connected to border
			AddLineFeature(fcLine1, 5, 0, 5, 5, stateId: "A");
			// second line on same side, connected to border
			AddLineFeature(fcLine1, 5, 0, 5, 10, stateId: "A");

			var test = new QaEdgeMatchCrossingLines(
				           ReadOnlyTableFactory.Create(fcLine1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcLine2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           LineClass1BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           CrossingLineMatchCondition = "LINE1.STATE <> LINE2.STATE",
				           AllowNoFeatureWithinSearchDistanceIfConnectedOnSameSide =
					           false
			           };

			AssertErrors(2, Run(test),
			             "NoMatch.NoCandidate.ConnectedOnSameSide");
		}

		[Test]
		public void CanIgnoreConnectionOnSameSide()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1,
			                     out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// connected to border
			AddLineFeature(fcLine1, 5, 0, 5, 5, stateId: "A");
			// second line on same side, connected to border
			AddLineFeature(fcLine1, 5, 0, 5, 10, stateId: "A");

			var test = new QaEdgeMatchCrossingLines(
				           ReadOnlyTableFactory.Create(fcLine1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcLine2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           LineClass1BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           CrossingLineMatchCondition = "LINE1.STATE <> LINE2.STATE",
				           AllowNoFeatureWithinSearchDistanceIfConnectedOnSameSide =
					           true
				           // default
			           };

			AssertErrors(0, Run(test));
		}

		[Test]
		public void CanIgnoreUnrelatedFeatures()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1,
			                     out fcBorder2);

			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");

			// connected to border, but different state id
			AddLineFeature(fcLine1, 5, 0, 5, 5, stateId: "B");

			var test = new QaEdgeMatchCrossingLines(
				           ReadOnlyTableFactory.Create(fcLine1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcLine2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           LineClass1BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           CrossingLineMatchCondition = "LINE1.STATE <> LINE2.STATE"
			           };

			AssertErrors(0, Run(test));
		}

		[Test]
		public void CanDetectNonConnectedCandidateFeature()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1,
			                     out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// connected to border
			AddLineFeature(fcLine1, 5, 0, 5, 5, stateId: "A");
			// connected to border, lateral offset
			AddLineFeature(fcLine2, 4.9, 0, 5, -5, stateId: "B");

			var test = new QaEdgeMatchCrossingLines(
				           ReadOnlyTableFactory.Create(fcLine1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcLine2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           LineClass1BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           CrossingLineMatchCondition = "LINE1.STATE <> LINE2.STATE"
			           };

			AssertErrors(1, Run(test), "NoMatch.CandidateExists.ConstraintsFulfilled");
		}

		[Test]
		public void CanIgnoreNonConnectedCandidateFeatureWithinCoincidenceTolerance()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1,
			                     out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// connected to border
			AddLineFeature(fcLine1, 5, 0, 5, 5, stateId: "A");
			// connected to border, lateral offset
			AddLineFeature(fcLine2, 4.9, 0, 5, -5, stateId: "B");

			var test = new QaEdgeMatchCrossingLines(
				           ReadOnlyTableFactory.Create(fcLine1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcLine2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           LineClass1BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           CrossingLineMatchCondition = "LINE1.STATE <> LINE2.STATE",
				           CoincidenceTolerance = 0.11
			           };

			AssertErrors(0, Run(test));
		}

		[Test]
		public void CanDetectNonConnectedCandidateFeatureWithDifferingAttributes()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1,
			                     out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			DateTime dateValue1 = DateTime.Now;
			DateTime dateValue2 = DateTime.Now - TimeSpan.FromHours(1);

			// connected to border
			AddLineFeature(fcLine1, 5, 0, 5, 5, "A", "X#Y", 100, dateValue1);
			// connected to border, lateral offset
			// - text field different; double difference not significant; date field different
			AddLineFeature(fcLine2, 4.9, 0, 5, -5, "B", "Y", 100.00000000000001,
			               dateValue2);

			var test = new QaEdgeMatchCrossingLines(
				           ReadOnlyTableFactory.Create(fcLine1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcLine2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           LineClass1BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           CrossingLineMatchCondition = "LINE1.STATE <> LINE2.STATE",
				           CrossingLineEqualAttributes =
					           "FLD_TEXT:#, FLD_DOUBLE, FLD_DATE"
			           };

			AssertErrors(1, Run(test),
			             "NoMatch.CandidateExists.ConstraintsNotFulfilled");
		}

		[Test]
		public void CanDetectNonConnectedCandidateFeatureDuetoBorderGap()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1,
			                     out fcBorder2);

			// border lines are NOT coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, -0.3, 0, -0.3, stateId: "B");

			// connected to border
			AddLineFeature(fcLine1, 5, 0, 5, 5, stateId: "A");
			// connected to border
			AddLineFeature(fcLine2, 5, -0.3, 5, -5, stateId: "B");

			var test = new QaEdgeMatchCrossingLines(
				           ReadOnlyTableFactory.Create(fcLine1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcLine2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           LineClass1BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           CrossingLineMatchCondition = "LINE1.STATE <> LINE2.STATE"
			           };

			AssertErrors(1, Run(test),
			             "NoMatch.CandidateExists.BordersNotCoincident+ConstraintsFulfilled");
		}

		[Test]
		public void
			CanIgnoreNonConnectedCandidateFeatureDuetoBorderGapWithinCoincidenceTolerance
			()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1,
			                     out fcBorder2);

			// border lines are NOT coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, -0.1, 0, -0.1, stateId: "B");

			// connected to border
			AddLineFeature(fcLine1, 5, 0, 5, 5, stateId: "A");
			// connected to border
			AddLineFeature(fcLine2, 5, -0.1, 5, -5, stateId: "B");

			var test = new QaEdgeMatchCrossingLines(
				           ReadOnlyTableFactory.Create(fcLine1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcLine2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           LineClass1BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           CrossingLineMatchCondition = "LINE1.STATE <> LINE2.STATE",
				           CoincidenceTolerance = 0.11
			           };

			AssertErrors(0, Run(test));
		}

		[Test]
		public void CanIgnoreConnectedFeaturesWithBorderPointCoincidence()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1,
			                     out fcBorder2);

			// border lines are coincident only at connection point
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddFeature(fcBorder2,
			           CurveConstruction.StartLine(10, -0.3)
			                            .LineTo(5, 0)
			                            .LineTo(0, -0.3)
			                            .Curve,
			           stateId: "B");

			// connected to border
			AddLineFeature(fcLine1, 5, 0, 5, 5, stateId: "A");
			// connected to border
			AddLineFeature(fcLine2, 5, 0, 5, -5, stateId: "B");

			var test = new QaEdgeMatchCrossingLines(
				           ReadOnlyTableFactory.Create(fcLine1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcLine2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           LineClass1BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           CrossingLineMatchCondition = "LINE1.STATE <> LINE2.STATE"
			           };

			var r = new QaContainerTestRunner(1000, test);
			Assert.AreEqual(0, r.Execute());

			// Bug in InMemoryWorkspace?
			// Fails because fcBorder2.Search(filter) returns no feature for filter.geometry = point(5,0) !!
			// AssertErrors(0, Run(test));
		}

		[Test]
		public void CanIgnoreNonConnectedCandidateFeatureDuetoBorderGap()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1,
			                     out fcBorder2);

			// border lines are NOT coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, -0.3, 0, -0.3, stateId: "B");

			// connected to border
			AddLineFeature(fcLine1, 5, 0, 5, 5, stateId: "A");
			// connected to border
			AddLineFeature(fcLine2, 5, -0.3, 5, -5, stateId: "B");

			var test = new QaEdgeMatchCrossingLines(
				           ReadOnlyTableFactory.Create(fcLine1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcLine2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           LineClass1BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           CrossingLineMatchCondition = "LINE1.STATE <> LINE2.STATE",
				           AllowDisjointCandidateFeatureIfBordersAreNotCoincident =
					           true
			           };

			AssertErrors(0, Run(test));
		}

		[Test]
		public void CanDetectNonConnectedCandidateFeatureDuetoBorderOverlap()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1,
			                     out fcBorder2);

			// border lines are NOT coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0.3, 0, 0.3, stateId: "B");

			// connected to border
			AddLineFeature(fcLine1, 5, 0, 5, 5, stateId: "A");
			// connected to border
			AddLineFeature(fcLine2, 5, 0.3, 5, -5, stateId: "B");

			var test = new QaEdgeMatchCrossingLines(
				           ReadOnlyTableFactory.Create(fcLine1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcLine2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           LineClass1BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           CrossingLineMatchCondition = "LINE1.STATE <> LINE2.STATE"
			           };

			// No error currently reported, since the lines touch (end point on interior of other line)
			AssertErrors(1, Run(test),
			             "NoMatch.CandidateExists.BordersNotCoincident+ConstraintsFulfilled");
		}

		[Test]
		public void CanDetectNoCandidateFeatureDueToOffsetFromBorder()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1,
			                     out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// connected to border
			AddLineFeature(fcLine1, 5, 0, 5, 5, stateId: "A");
			// NOT connected to border
			AddLineFeature(fcLine2, 5, 0.1, 5, -5, stateId: "B");

			var test = new QaEdgeMatchCrossingLines(
				           ReadOnlyTableFactory.Create(fcLine1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcLine2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           LineClass1BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           CrossingLineMatchCondition = "LINE1.STATE <> LINE2.STATE"
			           };

			AssertErrors(1, Run(test), "NoMatch.NoCandidate");
		}

		[Test]
		public void CanDetectNoCandidateFeaturesDueToLateralOffset()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1,
			                     out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// connected to border
			AddLineFeature(fcLine1, 5, 0, 5, 5, stateId: "A");
			// connected to border, offset > tol
			AddLineFeature(fcLine2, 4.4, 0, 5, -5, stateId: "B");

			var test = new QaEdgeMatchCrossingLines(
				           ReadOnlyTableFactory.Create(fcLine1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcLine2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           LineClass1BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           CrossingLineMatchCondition = "LINE1.STATE <> LINE2.STATE"
			           };

			// two errors expected, one for each line (both end on the border)
			AssertErrors(2, Run(test), "NoMatch.NoCandidate");
		}

		[Test]
		public void
			CanIgnoreWhenAllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled
			()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1,
			                     out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// connected to border
			AddLineFeature(fcLine1, 5, 0, 5, 5, stateId: "A", textFieldValue: "R");
			// connected to border, different point
			AddLineFeature(fcLine2, 5.2, 0, 5, -5, stateId: "B", textFieldValue: "R");

			var test = new QaEdgeMatchCrossingLines(
				           ReadOnlyTableFactory.Create(fcLine1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcLine2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           LineClass1BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           CrossingLineMatchCondition = "LINE1.STATE <> LINE2.STATE",
				           CrossingLineEqualAttributes = "FLD_TEXT",
				           AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled
					           = true
			           };

			AssertErrors(0, Run(test));
		}

		[Test]
		public void CanIgnoreAttributesWhenMultiConnection()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1,
			                     out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// connected to border
			AddLineFeature(fcLine1, 5, 0, 5, 5, stateId: "A", textFieldValue: "R");
			AddLineFeature(fcLine1, 5, 0, 7, 5, stateId: "A", textFieldValue: "R");
			// connected to border, same point
			AddLineFeature(fcLine2, 5, 0, 5, -5, stateId: "B", textFieldValue: "S");

			var test = new QaEdgeMatchCrossingLines(
				           ReadOnlyTableFactory.Create(fcLine1), ReadOnlyTableFactory.Create(fcBorder1),
			                                        ReadOnlyTableFactory.Create(fcLine2), ReadOnlyTableFactory.Create(fcBorder2),
				                                        searchDistance: 0.5)
			           {
				           LineClass1BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           CrossingLineMatchCondition = "LINE1.STATE <> LINE2.STATE",
				           CrossingLineEqualAttributes = "FLD_TEXT",
				           IgnoreAttributeConstraintsIfThreeOrMoreConnected = true
			           };

			AssertErrors(0, Run(test));
		}

		[Test]
		public void CanGetAllErrorPairsWhenMultiConnection()
		{
			IFeatureClass fcLine1;
			IFeatureClass fcLine2;
			IFeatureClass fcBorder1;
			IFeatureClass fcBorder2;
			CreateFeatureClasses(out fcLine1, out fcLine2, out fcBorder1,
			                     out fcBorder2);

			// border lines are coincident
			AddLineFeature(fcBorder1, 0, 0, 10, 0, stateId: "A");
			AddLineFeature(fcBorder2, 10, 0, 0, 0, stateId: "B");

			// connected to border
			AddLineFeature(fcLine1, 5, 0, 5, 5, stateId: "A", textFieldValue: "Q");
			AddLineFeature(fcLine1, 5, 0, 7, 5, stateId: "A", textFieldValue: "R");
			// connected to border, same point
			AddLineFeature(fcLine2, 5, 0, 5, -5, stateId: "B", textFieldValue: "S");
			// connected to border, different point
			AddLineFeature(fcLine2, 5.05, 0, 5, -5, stateId: "B",
			               textFieldValue: "T");

			var test = new QaEdgeMatchCrossingLines(
				           ReadOnlyTableFactory.Create(fcLine1), ReadOnlyTableFactory.Create(fcBorder1),
				           ReadOnlyTableFactory.Create(fcLine2), ReadOnlyTableFactory.Create(fcBorder2),
				                                       searchDistance: 0.5)
			           {
				           LineClass1BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           LineClass2BorderMatchCondition =
					           "LINE.STATE = BORDER.STATE",
				           CrossingLineMatchCondition = "LINE1.STATE <> LINE2.STATE",
				           CrossingLineEqualAttributes = "FLD_TEXT"
			           };

			AssertErrors(4, Run(test),
			             "Match.ConstraintsNotFulfilled",
			             "NoMatch.CandidateExists.ConstraintsNotFulfilled");
		}

		private void CreateFeatureClasses([NotNull] out IFeatureClass fcLine1,
		                                  [NotNull] out IFeatureClass fcLine2,
		                                  [NotNull] out IFeatureClass fcBorder1,
		                                  [NotNull] out IFeatureClass fcBorder2,
		                                  bool bordersAsPolygon = false)
		{
			// use tick count as suffix to make names unique within test fixture run

			Thread.Sleep(60);
			// make sure that TickCount is unique for each call (increase is non-continuous)
			int ticks = Environment.TickCount;

			fcLine1 = CreateLineClass(string.Format("l1_{0}", ticks));
			fcLine2 = CreateLineClass(string.Format("l2_{0}", ticks));
			if (! bordersAsPolygon)
			{
				fcBorder1 = CreateLineClass(string.Format("b1_{0}", ticks));
				fcBorder2 = CreateLineClass(string.Format("b2_{0}", ticks));
			}
			else
			{
				fcBorder1 = CreatePolyClass(string.Format("b1_{0}", ticks));
				fcBorder2 = CreatePolyClass(string.Format("b2_{0}", ticks));
			}
		}

		[NotNull]
		private static IList<QaError> Run([NotNull] ITest test)
		{
			var runner = new QaTestRunner(test) {KeepGeometry = true};

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

		private static void AddLineFeature([NotNull] IFeatureClass featureClass,
		                                   double x1, double y1,
		                                   double x2, double y2,
		                                   double x3, double y3,
		                                   [CanBeNull] string stateId = null,
		                                   [CanBeNull] string textFieldValue = null,
		                                   double? doubleValue = null,
		                                   DateTime? dateValue = null)
		{
			IPolycurve polyline = CurveConstruction.StartLine(x1, y1)
			                                       .LineTo(x2, y2)
			                                       .LineTo(x3, y3)
			                                       .Curve;

			AddFeature(featureClass, polyline,
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

		private static void AddPolyFeature([NotNull] IFeatureClass featureClass,
		                                   double x1, double y1,
		                                   double x2, double y2,
		                                   [CanBeNull] string stateId = null,
		                                   [CanBeNull] string textFieldValue = null,
		                                   double? doubleValue = null,
		                                   DateTime? dateValue = null)
		{
			AddFeature(featureClass,
			           GeometryFactory.CreatePolygon(x1, y1, x2, y2),
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

		private IFeatureClass CreatePolyClass([NotNull] string name)
		{
			return CreateFeatureClass(name, esriGeometryType.esriGeometryPolygon);
		}

		private IFeatureClass CreateFeatureClass([NotNull] string name,
		                                         esriGeometryType geomType)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", geomType, _spatialReference, 1000));

			fields.AddField(FieldUtils.CreateTextField(_stateIdFieldName, 100));

			fields.AddField(FieldUtils.CreateTextField(_textFieldName, 200));
			fields.AddField(FieldUtils.CreateDoubleField(_doubleFieldName));
			fields.AddField(FieldUtils.CreateDateField(_dateFieldName));

			return DatasetUtils.CreateSimpleFeatureClass(
				_featureWorkspace, name, fields);
		}
	}
}
