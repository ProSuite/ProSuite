using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestContainer;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.TestFactories;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaLineGroupConstraintsTest
	{
		private IFeatureWorkspace _testWs;
		private IFeatureWorkspace _relTestWs;
		private ISpatialReference _spatialReference;
		private const string _stateIdFieldName = "STATE";
		private const string _textFieldName = "FLD_TEXT";
		private const string _doubleFieldName = "FLD_DOUBLE";
		private const string _dateFieldName = "FLD_DATE";

		private const string _fkFieldName = "FLD_FK";

		private const double _xyTolerance = 0.001;

		private IFeatureWorkspace RelTestWs
		{
			get
			{
				return _relTestWs ??
				       (_relTestWs =
					        TestWorkspaceUtils.CreateTestFgdbWorkspace(
						        "QaGroupEndsNeighborhoodTest"));
			}
		}

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense(activateAdvancedLicense: true);

			_spatialReference = CreateLV95();
			_testWs = TestWorkspaceUtils.CreateInMemoryWorkspace(
				"QaGroupEndsNeighborhoodTest");
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanFindSmallGroupsDanglesAndGaps()
		{
			IFeatureClass fc1;
			IFeatureClass fc2;
			CreateFeatureClasses(out fc1, out fc2);

			AddFeature(fc1, CurveConstruction.StartLine(0, 0).LineTo(4, 0).Curve,
			           textFieldValue: "A");
			AddFeature(fc1, CurveConstruction.StartLine(4, 0).LineTo(4, 8).Curve,
			           textFieldValue: "A");
			AddFeature(fc1, CurveConstruction.StartLine(4, 0).LineTo(5, 0).Curve,
			           textFieldValue: "A");

			AddFeature(fc1, CurveConstruction.StartLine(5, 8).LineTo(10, 8).Curve,
			           textFieldValue: "A");

			// network connection, no group
			AddFeature(fc2, CurveConstruction.StartLine(4, 8).LineTo(5, 8).Curve);

			// small Ring
			AddFeature(fc1, CurveConstruction.StartLine(15, 4).LineTo(16, 4).Curve,
			           textFieldValue: "A");
			AddFeature(fc2, CurveConstruction.StartLine(16, 4).LineTo(16, 5).Curve,
			           textFieldValue: "A");
			AddFeature(fc1, CurveConstruction.StartLine(15, 4).LineTo(16, 5).Curve,
			           textFieldValue: "A");

			// small Ring with dangle
			AddFeature(fc1, CurveConstruction.StartLine(25, 4).LineTo(26, 4).Curve,
			           textFieldValue: "A");
			AddFeature(fc2, CurveConstruction.StartLine(26, 4).LineTo(26, 5).Curve,
			           textFieldValue: "A");
			AddFeature(fc1, CurveConstruction.StartLine(25, 4).LineTo(26, 5).Curve,
			           textFieldValue: "A");
			AddFeature(fc2, CurveConstruction.StartLine(26, 4).LineTo(28.1, 4).Curve,
			           textFieldValue: "A");

			var test = new QaLineGroupConstraints(
				           new[]
				           {
					           ReadOnlyTableFactory.Create(fc1),
					           ReadOnlyTableFactory.Create(fc2)
				           }, 2, 6, 2,
				           new[] { _textFieldName })
			           {
				           GroupConditions = new[] { $"{_textFieldName} IS NOT NULL" }
			           };

			AssertErrors(5, Run(test, 1000));
		}

		[Test]
		public void CanFindGapsToOtherGroupTypes()
		{
			IFeatureClass fc1;
			IFeatureClass fc2;
			CreateFeatureClasses(out fc1, out fc2);

			AddFeature(fc1,
			           CurveConstruction.StartLine(0, 0).LineTo(4, 0).Curve,
			           textFieldValue: "A");
			AddFeature(fc2,
			           CurveConstruction.StartLine(4, 0).LineTo(4, 8).Curve,
			           textFieldValue: "A");

			AddFeature(fc1,
			           CurveConstruction.StartLine(5, 8).LineTo(20, 4).Curve,
			           textFieldValue: "B");

			AddFeature(fc2,
			           CurveConstruction.StartLine(0, 8).LineTo(0, 0).Curve,
			           textFieldValue: "B");

			// add network connection, no group
			AddFeature(fc1, CurveConstruction.StartLine(4, 8).LineTo(5, 8).Curve);

			var test = new QaLineGroupConstraints(
				           new[]
				           {
					           ReadOnlyTableFactory.Create(fc1),
					           ReadOnlyTableFactory.Create(fc2)
				           }, 2, 6, 2,
				           new[] { _textFieldName })
			           {
				           MinGapToOtherGroupType = 2,
				           GroupConditions = new[] { $"{_textFieldName} IS NOT NULL" }
			           };

			AssertErrors(2, Run(test, 1000));
		}

		[Test]
		public void CanFindGaps()
		{
			IFeatureClass fc1;
			IFeatureClass fc2;
			CreateFeatureClasses(out fc1, out fc2);

			AddFeature(fc1, CurveConstruction.StartLine(0, 0).LineTo(4, 0).Curve,
			           textFieldValue: "A");
			AddFeature(fc2, CurveConstruction.StartLine(4, 0).LineTo(4, 8).Curve,
			           textFieldValue: "A");

			AddFeature(fc1,
			           CurveConstruction.StartLine(5, 8).LineTo(20, 4).Curve,
			           textFieldValue: "B");

			AddFeature(fc2,
			           CurveConstruction.StartLine(5, 8.5).LineTo(20, 8).Curve,
			           textFieldValue: "C");

			AddFeature(fc1, CurveConstruction.StartLine(4, 8).LineTo(4.5, 8).Curve);
			AddFeature(fc1, CurveConstruction.StartLine(4.5, 8).LineTo(5, 8).Curve);
			AddFeature(fc1, CurveConstruction.StartLine(4.5, 8)
			                                 .LineTo(4.5, 8.1)
			                                 .LineTo(4.4, 8.1)
			                                 .LineTo(4.5, 8)
			                                 .Curve);

			AddFeature(fc1, CurveConstruction.StartLine(4, 8)
			                                 .LineTo(4.5, 9.2)
			                                 .LineTo(5, 8.5)
			                                 .Curve);

			var test = new QaLineGroupConstraints(
				           new[]
				           {
					           ReadOnlyTableFactory.Create(fc1),
					           ReadOnlyTableFactory.Create(fc2)
				           }, 2, 6, 2,
				           new[] { _textFieldName })
			           {
				           MinGapToOtherGroupType = 2,
				           GroupConditions = new[] { $"{_textFieldName} IS NOT NULL" }
			           };

			AssertErrors(2, Run(test, 1000));
		}

		[Test]
		public void CanFindGapsToSelfConnected()
		{
			IFeatureClass fc1;
			IFeatureClass fc2;
			CreateFeatureClasses(out fc1, out fc2);

			AddFeature(fc1, CurveConstruction.StartLine(0, 0).LineTo(4, 0).Curve,
			           textFieldValue: "A");
			AddFeature(fc1, CurveConstruction.StartLine(4, 0).LineTo(4, 8).Curve,
			           textFieldValue: "A");
			AddFeature(fc1, CurveConstruction.StartLine(4, 8).LineTo(4, 10).Curve,
			           textFieldValue: "A");

			AddFeature(fc1,
			           CurveConstruction.StartLine(4, 10).LineTo(6, 12).Curve,
			           textFieldValue: "C");
			AddFeature(fc1,
			           CurveConstruction.StartLine(4, 10).LineTo(4.3, 9).LineTo(4, 8).Curve,
			           textFieldValue: "C");
			AddFeature(fc1,
			           CurveConstruction.StartLine(4, 8).LineTo(30, 12).Curve,
			           textFieldValue: "C");

			var testAtFork = new QaLineGroupConstraints(
				                 new[] { ReadOnlyTableFactory.Create(fc1) }, 2, 6, 2,
				                 new[] { _textFieldName })
			                 {
				                 MinGapToSameGroupTypeAtForkCovered = 3,
				                 GroupConditions = new[] { $"{_textFieldName} IS NOT NULL" }
			                 };

			AssertErrors(0, Run(testAtFork, 1000));

			var test = new QaLineGroupConstraints(
				           new[] { ReadOnlyTableFactory.Create(fc1) }, 2, 6, 2,
				           new[] { _textFieldName })
			           {
				           MinGapToSameGroupTypeCovered = 3,
				           GroupConditions = new[] { $"{_textFieldName} IS NOT NULL" }
			           };

			AssertErrors(0, Run(test, 1000));
		}

		[Test]
		public void CanFindDangles()
		{
			IFeatureClass fc1;
			IFeatureClass fc2;
			CreateFeatureClasses(out fc1, out fc2);

			AddFeature(fc1, CurveConstruction.StartLine(0, 0).LineTo(4, 0).Curve,
			           textFieldValue: "A");
			AddFeature(fc2, CurveConstruction.StartLine(4, 0).LineTo(4, 8).Curve,
			           textFieldValue: "A");

			AddFeature(fc1, CurveConstruction.StartLine(5, 8).LineTo(5, 9).Curve,
			           textFieldValue: "A");
			AddFeature(fc2, CurveConstruction.StartLine(5, 9).LineTo(5, 900).Curve,
			           textFieldValue: "A");
			AddFeature(fc1, CurveConstruction.StartLine(5, 9).LineTo(900, 9).Curve,
			           textFieldValue: "A");
			AddFeature(fc2, CurveConstruction.StartLine(5, 9).LineTo(6, 10.1).Curve,
			           textFieldValue: "A");

			// add network connection, no group
			AddFeature(fc2, CurveConstruction.StartLine(4, 8).LineTo(5, 8).Curve);

			var test = new QaLineGroupConstraints(
				           new[]
				           {
					           ReadOnlyTableFactory.Create(fc1),
					           ReadOnlyTableFactory.Create(fc2)
				           }, 2, 6, 2,
				           new[] { _textFieldName })
			           {
				           GroupConditions = new[] { $"{_textFieldName} IS NOT NULL" }
			           };

			AssertErrors(3, Run(test, 1000));

			AssertErrors(2, Run(test, 500, GeometryFactory.CreateEnvelope(-1, -1, 10, 10)));
		}

		[Test]
		public void CanIgnoreDanglesAtFork()
		{
			IFeatureClass fc1;
			IFeatureClass fc2;
			CreateFeatureClasses(out fc1, out fc2);

			AddFeature(fc1, CurveConstruction.StartLine(0, 0).LineTo(4, 0).Curve,
			           textFieldValue: "A");
			AddFeature(fc2, CurveConstruction.StartLine(4, 0).LineTo(8, 0).Curve,
			           textFieldValue: "A");

			AddFeature(fc1, CurveConstruction.StartLine(4, 0).LineTo(4, 1).Curve,
			           textFieldValue: "A");
			AddFeature(fc2, CurveConstruction.StartLine(4, 1).LineTo(4, 10).Curve);
			AddFeature(fc1, CurveConstruction.StartLine(4, 1).LineTo(5, 10).Curve);

			var test = new QaLineGroupConstraints(
				           new[]
				           {
					           ReadOnlyTableFactory.Create(fc1),
					           ReadOnlyTableFactory.Create(fc2)
				           }, 2, 6, 2,
				           new[] { _textFieldName })
			           {
				           GroupConditions = new[] { $"{_textFieldName} IS NOT NULL" }
			           };

			AssertErrors(1, Run(test, 1000));

			test.MinDangleLengthAtForkContinued = 0.1;
			// Not continued Fork at (4,1) -> no applied
			AssertErrors(1, Run(test, 1000));

			test.MinDangleLengthAtFork = 0.1;
			AssertErrors(0, Run(test, 1000));
		}

		[Test]
		public void CanIgnoreDiscontinuedDangles()
		{
			IFeatureClass fc1;
			IFeatureClass fc2;
			CreateFeatureClasses(out fc1, out fc2);

			AddFeature(fc1, CurveConstruction.StartLine(0, 0).LineTo(4, 0).Curve,
			           textFieldValue: "A");
			AddFeature(fc2, CurveConstruction.StartLine(4, 0).LineTo(8, 0).Curve,
			           textFieldValue: "A");

			AddFeature(fc1, CurveConstruction.StartLine(4, 0).LineTo(4, 1).Curve,
			           textFieldValue: "A");
			AddFeature(fc2, CurveConstruction.StartLine(4, 1).LineTo(5, 10).Curve,
			           textFieldValue: "B");

			var test = new QaLineGroupConstraints(
				           new[]
				           {
					           ReadOnlyTableFactory.Create(fc1),
					           ReadOnlyTableFactory.Create(fc2)
				           }, 2, 6, 2,
				           new[] { _textFieldName })
			           {
				           GroupConditions = new[] { $"{_textFieldName} IS NOT NULL" }
			           };

			AssertErrors(1, Run(test, 1000));

			test.MinDangleLengthAtForkContinued = 0.1;
			// No Fork at (4,1) -> no applied
			AssertErrors(1, Run(test, 1000));

			test.MinDangleLengthAtFork = 0.1;
			// No Fork at (4,1) -> no applied
			AssertErrors(1, Run(test, 1000));

			test.MinDangleLengthContinued = 0.1;
			AssertErrors(0, Run(test, 1000));
		}

		[Test]
		public void CanIgnoreCoveredGap()
		{
			IFeatureClass fc1;
			IFeatureClass fc2;
			CreateFeatureClasses(out fc1, out fc2);

			AddFeature(fc1, CurveConstruction.StartLine(0, 0).LineTo(4, 0).Curve,
			           textFieldValue: "A");
			AddFeature(fc2, CurveConstruction.StartLine(4, 0).LineTo(8, 0).Curve,
			           textFieldValue: "A");

			AddFeature(fc1, CurveConstruction.StartLine(4, 0).LineTo(4, 3).Curve,
			           textFieldValue: "A");
			AddFeature(fc2, CurveConstruction.StartLine(4, 3).LineTo(4, 4).Curve,
			           textFieldValue: "B");
			AddFeature(fc1, CurveConstruction.StartLine(4, 4).LineTo(4, 20).Curve,
			           textFieldValue: "A");
			AddFeature(fc2, CurveConstruction.StartLine(4, 3).LineTo(14, 3).Curve,
			           textFieldValue: "B");

			var test = new QaLineGroupConstraints(
				           new[]
				           {
					           ReadOnlyTableFactory.Create(fc1),
					           ReadOnlyTableFactory.Create(fc2)
				           }, 2, 6, 2,
				           new[] { _textFieldName })
			           {
				           GroupConditions = new[] { $"{_textFieldName} IS NOT NULL" }
			           };

			AssertErrors(2, Run(test, 1000));
			// NOTE: gap is currently reported twice (issue description is different from both sides)

			test.MinGapToSameGroupTypeCovered = 0.1;

			Console.WriteLine(@"Rerun test with MinGapToSameGroupTypeCovered = {0}:",
			                  test.MinGapToSameGroupTypeCovered);

			AssertErrors(0, Run(test, 1000));
		}

		[Test]
		public void CanHandleNotFullyCoveredGap()
		{
			IFeatureClass fc1;
			IFeatureClass fc2;
			CreateFeatureClasses(out fc1, out fc2);

			AddFeature(fc1, CurveConstruction.StartLine(0, 0).LineTo(4, 0).Curve,
			           textFieldValue: "A");
			AddFeature(fc1, CurveConstruction.StartLine(4, 0).LineTo(8, 0).Curve,
			           textFieldValue: "A");

			AddFeature(fc1, CurveConstruction.StartLine(4, 0).LineTo(4, 3).Curve,
			           textFieldValue: "A");
			AddFeature(fc2, CurveConstruction.StartLine(4, 3).LineTo(4, 3.5).Curve,
			           textFieldValue: "B");
			AddFeature(fc1, CurveConstruction.StartLine(4, 3.5).LineTo(4, 4).Curve);
			AddFeature(fc2, CurveConstruction.StartLine(4, 4).LineTo(4, 20).Curve,
			           textFieldValue: "A");
			AddFeature(fc1, CurveConstruction.StartLine(4, 3).LineTo(14, 3).Curve,
			           textFieldValue: "B");
			AddFeature(fc2, CurveConstruction.StartLine(4, 4).LineTo(14, 4).Curve,
			           textFieldValue: "C");

			var test = new QaLineGroupConstraints(
				           new[]
				           {
					           ReadOnlyTableFactory.Create(fc1),
					           ReadOnlyTableFactory.Create(fc2)
				           }, 2, 6, 2,
				           new[] { _textFieldName })
			           {
				           GroupConditions = new[] { $"{_textFieldName} IS NOT NULL" }
			           };

			AssertErrors(1, Run(test, 1000));

			test.MinGapToSameGroupTypeCovered = 0.1;
			AssertErrors(1, Run(test, 1000));

			test.MinGapToSameGroupTypeCovered = 0;
			test.MinGapToSameGroupTypeAtFork = 0.1;
			AssertErrors(0, Run(test, 1000));
		}

		[Test]
		public void CanIgnoreGapToOtherGroupTypeAtFork()
		{
			IFeatureClass fc1;
			IFeatureClass fc2;
			CreateFeatureClasses(out fc1, out fc2);

			AddFeature(fc1, CurveConstruction.StartLine(0, 0).LineTo(4, 0).Curve,
			           textFieldValue: "A");
			AddFeature(fc2, CurveConstruction.StartLine(4, 0).LineTo(8, 0).Curve,
			           textFieldValue: "A");

			AddFeature(fc1, CurveConstruction.StartLine(4, 0).LineTo(4, 3).Curve,
			           textFieldValue: "A");
			AddFeature(fc2, CurveConstruction.StartLine(4, 3).LineTo(4, 4).Curve);

			AddFeature(fc1, CurveConstruction.StartLine(4, 4).LineTo(4, 20).Curve,
			           textFieldValue: "B");

			AddFeature(fc2, CurveConstruction.StartLine(4, 4).LineTo(0, 4).Curve);
			AddFeature(fc1, CurveConstruction.StartLine(4, 3).LineTo(0, 4).Curve);

			var test = new QaLineGroupConstraints(
				           new[]
				           {
					           ReadOnlyTableFactory.Create(fc1),
					           ReadOnlyTableFactory.Create(fc2)
				           }, 2, 6, 2,
				           new[] { _textFieldName })
			           {
				           GroupConditions = new[] { $"{_textFieldName} IS NOT NULL" },
				           MinGapToOtherGroupType = 2
			           };

			AssertErrors(2, Run(test, 1000));

			test.MinGapToOtherGroupTypeAtFork = 0.1;
			AssertErrors(0, Run(test, 1000));
		}

		[Test]
		public void CanFindRings()
		{
			IFeatureClass fc1;
			IFeatureClass fc2;
			CreateFeatureClasses(out fc1, out fc2);

			// small ring with continuations on both sides -> no error
			AddFeature(fc1, CurveConstruction.StartLine(-10, 0).LineTo(4, 0).Curve,
			           textFieldValue: "A");
			AddFeature(fc2, CurveConstruction.StartLine(4, 0).LineTo(4, 1).LineTo(5, 0).Curve,
			           textFieldValue: "A");
			AddFeature(fc1, CurveConstruction.StartLine(4, 0).LineTo(4, -1).LineTo(5, 0).Curve,
			           textFieldValue: "A");
			AddFeature(fc2, CurveConstruction.StartLine(5, 0).LineTo(20, 0).Curve,
			           textFieldValue: "A");

			// small ring with 1 part -> Ring error
			AddFeature(
				fc1,
				CurveConstruction.StartLine(0, 10).LineTo(1, 11).LineTo(1, 9).LineTo(0, 10).Curve,
				textFieldValue: "B");

			// small ring with > 1 part -> Ring error
			AddFeature(fc2, CurveConstruction.StartLine(0, 15).LineTo(1, 16).LineTo(1, 14).Curve,
			           textFieldValue: "B");
			AddFeature(fc1, CurveConstruction.StartLine(1, 14).LineTo(0, 15).Curve,
			           textFieldValue: "B");

			// small ring with continuation on on side -> no error
			AddFeature(fc2, CurveConstruction.StartLine(-10, 20).LineTo(4, 20).Curve,
			           textFieldValue: "A");
			AddFeature(fc1, CurveConstruction.StartLine(4, 20).LineTo(5, 21).LineTo(5, 20)
			                                 .LineTo(4, 20).Curve,
			           textFieldValue: "A");

			// large ring with 1 part -> no error
			AddFeature(
				fc1,
				CurveConstruction.StartLine(0, 30).LineTo(2, 31).LineTo(2, 29).LineTo(0, 30).Curve,
				textFieldValue: "B");

			// large ring with > 1 parts -> no error
			AddFeature(fc1,
			           CurveConstruction.StartLine(0, 40).LineTo(2, 41).LineTo(2, 39).Curve,
			           textFieldValue: "B");
			AddFeature(fc2, CurveConstruction.StartLine(2, 39).LineTo(0, 40).Curve,
			           textFieldValue: "B");

			var test = new QaLineGroupConstraints(
				new[]
				{
					ReadOnlyTableFactory.Create(fc1),
					ReadOnlyTableFactory.Create(fc2)
				}, 2, 6, 6,
				new[] { _textFieldName });

			AssertErrors(2, Run(test, 1000));
		}

		[Test]
		public void CanHandleExtendedData()
		{
			IFeatureClass fc1;
			IFeatureClass fc3;
			CreateFeatureClasses(out fc1, out fc3);

			AddFeature(fc1, CurveConstruction.StartLine(0, 0).LineTo(8, 0).Curve,
			           textFieldValue: "A");

			AddFeature(fc1, CurveConstruction.StartLine(5, 8).LineTo(5, 16).Curve,
			           textFieldValue: "A");

			AddFeature(fc1, CurveConstruction.StartLine(4, 2).LineTo(4, 9.5).Curve,
			           textFieldValue: "A");
			AddFeature(fc1, CurveConstruction.StartLine(4, 9.5).LineTo(4, 10.5).Curve,
			           textFieldValue: "A");

			AddFeature(fc1, CurveConstruction.StartLine(6, 9.5).LineTo(6, 10.5).Curve,
			           textFieldValue: "A");

			AddFeature(fc1, CurveConstruction.StartLine(7, 9.5).LineTo(7, 10.5).Curve,
			           textFieldValue: "A");
			AddFeature(fc1,
			           CurveConstruction.StartLine(7, 9.5).LineTo(7.5, 5).LineTo(8, 9.5).Curve,
			           textFieldValue: "A");
			AddFeature(fc1, CurveConstruction.StartLine(8, 9.5).LineTo(8, 10.5).Curve,
			           textFieldValue: "A");

			var test = new QaLineGroupConstraints(
				new[] { ReadOnlyTableFactory.Create(fc1) }, 2, 6, 6,
				new[] { _textFieldName });

			AssertErrors(2, Run(test, 10));
		}

		[Test]
		public void TestRelated()
		{
			IFeatureWorkspace initTestWs = _testWs;
			try
			{
				_testWs = RelTestWs;

				IFeatureClass fc1;
				IFeatureClass fc2;
				CreateFeatureClasses(out fc1, out fc2);

				var ds1 = (IDataset) fc1;

				IFieldsEdit fields = new FieldsClass();
				fields.AddField(FieldUtils.CreateOIDField());
				fields.AddField(FieldUtils.CreateIntegerField(_fkFieldName));
				fields.AddField(FieldUtils.CreateTextField(_textFieldName, 200));

				ITable grpTable = TestWorkspaceUtils.CreateSimpleTable(_testWs, "Rel_" + ds1.Name,
					fields, null);

				var dsRel = (IDataset) grpTable;

				string relName = "relName" + Environment.TickCount;
				IRelationshipClass relClass =
					TestWorkspaceUtils.CreateSimpleMNRelationship(
						_testWs, relName, grpTable, (ITable) fc1, "fkGrp", "fkFc");

				((IWorkspaceEdit) _testWs).StartEditing(false);
				((IWorkspaceEdit) _testWs).StopEditing(true);

				((IWorkspaceEdit) _testWs).StartEditing(false);

				IFeature f = AddFeature(
					fc1, CurveConstruction.StartLine(0, 0).LineTo(4, 0).Curve);
				IRow r = AddRow(grpTable, textFieldValue: "A");
				relClass.CreateRelationship((IObject) r, f);
				r = AddRow(grpTable, textFieldValue: "B");
				relClass.CreateRelationship((IObject) r, f);

				f = AddFeature(
					fc1, CurveConstruction.StartLine(4, 0).LineTo(4, 8).Curve);
				r = AddRow(grpTable, textFieldValue: "A");
				relClass.CreateRelationship((IObject) r, f);
				((IWorkspaceEdit) _testWs).StopEditing(true);

				var queryFeatureClass = (IFeatureClass) TableJoinUtils.CreateQueryTable(
					relClass, JoinType.InnerJoin);

				string groupBy = string.Format("{0}.{1}", dsRel.Name, _textFieldName);
				var test = new QaLineGroupConstraints(
					new[] { ReadOnlyTableFactory.Create(queryFeatureClass) }, 2, 6, 2,
					new[] { groupBy });

				AssertErrors(1, Run(test, 1000));
			}
			finally
			{
				_testWs = initTestWs;
			}
		}

		[Test]
		public void TestRelatedFactoryMinParameters()
		{
			IFeatureWorkspace initTestWs = _testWs;
			try
			{
				_testWs = RelTestWs;

				IFeatureClass fc1;
				IFeatureClass fc2;
				CreateFeatureClasses(out fc1, out fc2);

				var ds1 = (IDataset) fc1;

				IFieldsEdit fields = new FieldsClass();
				fields.AddField(FieldUtils.CreateOIDField());
				fields.AddField(FieldUtils.CreateIntegerField(_fkFieldName));
				fields.AddField(FieldUtils.CreateTextField(_textFieldName, 200));

				ITable relTable = TestWorkspaceUtils.CreateSimpleTable(_testWs, "Rel_" + ds1.Name,
					fields);

				var dsRel = (IDataset) relTable;

				string relName = "relName" + Environment.TickCount;
				IRelationshipClass rel = TestWorkspaceUtils.CreateSimpleMNRelationship(
					_testWs, relName, relTable, (ITable) fc1, "fkGrp", "fkFc");

				((IWorkspaceEdit) _testWs).StartEditing(false);

				IFeature f = AddFeature(
					fc1, CurveConstruction.StartLine(0, 0).LineTo(4, 0).Curve);
				IRow r = AddRow(relTable, textFieldValue: "A");
				rel.CreateRelationship((IObject) r, f);
				r = AddRow(relTable, textFieldValue: "B");
				rel.CreateRelationship((IObject) r, f);

				f = AddFeature(
					fc1, CurveConstruction.StartLine(4, 0).LineTo(4, 8).Curve);
				r = AddRow(relTable, textFieldValue: "A");
				rel.CreateRelationship((IObject) r, f);

				((IWorkspaceEdit) _testWs).StopEditing(true);

				var model = new SimpleModel("model", fc1);
				Dataset mds1 = model.AddDataset(new ModelVectorDataset(ds1.Name));
				Dataset mdsRel = model.AddDataset(new ModelTableDataset(dsRel.Name));

				var clsDesc = new ClassDescriptor(typeof(QaRelLineGroupConstraints));
				var tstDesc = new TestDescriptor("GroupEnds", clsDesc);
				var condition = new QualityCondition("cndGroupEnds", tstDesc);
				InstanceConfigurationUtils.AddParameterValue(condition, "relationTables", mds1);
				InstanceConfigurationUtils.AddParameterValue(
					condition, "relationTables", mdsRel);
				InstanceConfigurationUtils.AddParameterValue(condition, "relation", relName);
				InstanceConfigurationUtils.AddParameterValue(
					condition, "join", JoinType.InnerJoin);
				InstanceConfigurationUtils.AddParameterValue(condition, "minGap", 1);
				InstanceConfigurationUtils.AddParameterValue(condition, "minGroupLength", 5);
				InstanceConfigurationUtils.AddParameterValue(condition, "minDangleLength", 2);
				InstanceConfigurationUtils.AddParameterValue(
					condition, "groupBy", string.Format("{0}.{1}", dsRel.Name, _textFieldName));

				var fact = new QaRelLineGroupConstraints();
				fact.Condition = condition;

				IList<ITest> tests =
					fact.CreateTests(new SimpleDatasetOpener(model.MasterDatabaseWorkspaceContext));
				Assert.AreEqual(1, tests.Count);

				AssertErrors(1, Run(tests[0], 1000));
			}
			finally
			{
				_testWs = initTestWs;
			}
		}

		[Test]
		public void TestRelatedFactoryAllParameters()
		{
			IFeatureWorkspace initTestWs = _testWs;
			try
			{
				_testWs = RelTestWs;

				IFeatureClass fc1;
				IFeatureClass fc2;
				CreateFeatureClasses(out fc1, out fc2);

				var ds1 = (IDataset) fc1;

				IFieldsEdit fields = new FieldsClass();
				fields.AddField(FieldUtils.CreateOIDField());
				fields.AddField(FieldUtils.CreateIntegerField(_fkFieldName));
				fields.AddField(FieldUtils.CreateTextField(_textFieldName, 200));

				ITable relTable = TestWorkspaceUtils.CreateSimpleTable(_testWs, "Rel_" + ds1.Name,
					fields, null);

				var dsRel = (IDataset) relTable;

				string relName = "relName" + Environment.TickCount;
				IRelationshipClass rel = TestWorkspaceUtils.CreateSimpleMNRelationship(
					_testWs, relName, relTable, (ITable) fc1, "fkGrp", "fkFc");

				((IWorkspaceEdit) _testWs).StartEditing(false);

				IFeature f = AddFeature(
					fc1, CurveConstruction.StartLine(0, 0).LineTo(4, 0).Curve);
				IRow r = AddRow(relTable, textFieldValue: "A");
				rel.CreateRelationship((IObject) r, f);
				r = AddRow(relTable, textFieldValue: "B");
				rel.CreateRelationship((IObject) r, f);

				f = AddFeature(
					fc1, CurveConstruction.StartLine(4, 0).LineTo(4, 8).Curve);
				r = AddRow(relTable, textFieldValue: "A");
				rel.CreateRelationship((IObject) r, f);

				((IWorkspaceEdit) _testWs).StopEditing(true);

				var model = new SimpleModel("model", fc1);
				Dataset mds1 = model.AddDataset(new ModelVectorDataset(ds1.Name));
				Dataset mdsRel = model.AddDataset(new ModelTableDataset(dsRel.Name));

				var clsDesc = new ClassDescriptor(typeof(QaRelLineGroupConstraints));
				var tstDesc = new TestDescriptor("GroupEnds", clsDesc);
				var condition = new QualityCondition("cndGroupEnds", tstDesc);
				InstanceConfigurationUtils.AddParameterValue(condition, "relationTables", mds1);
				InstanceConfigurationUtils.AddParameterValue(
					condition, "relationTables", mdsRel);
				InstanceConfigurationUtils.AddParameterValue(condition, "relation", relName);
				InstanceConfigurationUtils.AddParameterValue(
					condition, "join", JoinType.InnerJoin);
				InstanceConfigurationUtils.AddParameterValue(condition, "minGap", 1);
				InstanceConfigurationUtils.AddParameterValue(condition, "minGroupLength", 5);
				InstanceConfigurationUtils.AddParameterValue(condition, "minDangleLength", 2);
				InstanceConfigurationUtils.AddParameterValue(
					condition, "groupBy", string.Format("{0}.{1}", dsRel.Name, _textFieldName));

				InstanceConfigurationUtils.AddParameterValue(condition, "GroupCondition", "");
				InstanceConfigurationUtils.AddParameterValue(condition, "ValueSeparator", "#");
				InstanceConfigurationUtils.AddParameterValue(
					condition, "MinGapToOtherGroupType", 0);

				InstanceConfigurationUtils.AddParameterValue(
					condition, "MinDangleLengthContinued", 0);
				InstanceConfigurationUtils.AddParameterValue(
					condition, "MinDangleLengthAtForkContinued",
					0);
				InstanceConfigurationUtils.AddParameterValue(
					condition, "MinDangleLengthAtFork", 0);

				InstanceConfigurationUtils.AddParameterValue(
					condition, "MinGapToSameGroupTypeCovered",
					0);
				InstanceConfigurationUtils.AddParameterValue(
					condition, "MinGapToSameGroupTypeAtFork",
					0);
				InstanceConfigurationUtils.AddParameterValue(
					condition, "MinGapToSameGroupTypeAtForkCovered", 0);

				InstanceConfigurationUtils.AddParameterValue(
					condition, "MinGapToOtherGroupTypeAtFork",
					0);
				InstanceConfigurationUtils.AddParameterValue(condition, "MinGapToSameGroup", 0);

				var fact = new QaRelLineGroupConstraints();
				Assert.AreEqual(
					fact.Parameters.Count,
					condition.ParameterValues.Count - 1); // 'relationTables' exists twice

				Assert.IsNotNull(fact.TestDescription, "Description");
				foreach (TestParameter parameter in fact.Parameters)
				{
					Assert.IsNotNull(fact.GetParameterDescription(parameter.Name),
					                 parameter.Name);
				}

				fact.Condition = condition;

				IList<ITest> tests =
					fact.CreateTests(new SimpleDatasetOpener(model.MasterDatabaseWorkspaceContext));
				Assert.AreEqual(1, tests.Count);

				AssertErrors(1, Run(tests[0], 1000));
			}
			finally
			{
				_testWs = initTestWs;
			}
		}

		[Test]
		public void TestMultipart()
		{
			IFeatureClass fc1;
			IFeatureClass fc2;
			CreateFeatureClasses(out fc1, out fc2);

			AddFeature(
				fc1,
				CurveConstruction.StartLine(0, 0).LineTo(8, 0).MoveTo(10, 0).LineTo(15, 2).Curve,
				textFieldValue: "A");
			//AddFeature(fc1, CurveConstruction.StartLine(10, 2).LineTo(10, 3).Curve,
			//		   textFieldValue : "A");

			AddFeature(fc1, CurveConstruction.StartLine(8, 0).LineTo(10, 0).Curve,
			           textFieldValue: "B");
			AddFeature(fc1, CurveConstruction.StartLine(8, 0).LineTo(8, 9).Curve,
			           textFieldValue: "B");

			var test = new QaLineGroupConstraints(
				new[] { ReadOnlyTableFactory.Create(fc1) }, 3, 4, 4,
				new[] { _textFieldName });

			AssertErrors(2, Run(test, 10));
		}

		[Test]
		[Ignore("learing test")]
		public void TestMultiOidsKeyLeftJoinStrasseRoute()
		{
			// takes "forever"
			var ws = (IFeatureWorkspace) WorkspaceUtils.OpenSDEWorkspace(
				DirectConnectDriver.Oracle11g,
				"TOPGIST", "SDE.DEFAULT");

			IQueryDef queryDef = ws.CreateQueryDef();
			queryDef.SubFields = "*";

			queryDef.Tables =
				"TOPGIS_TLM.TLM_STRASSE " +
				"LEFT OUTER JOIN TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE ON TOPGIS_TLM.TLM_STRASSE.UUID=TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE.TLM_STRASSE_UUID " +
				"LEFT OUTER JOIN TOPGIS_TLM.TLM_STRASSENROUTE ON TOPGIS_TLM.TLM_STRASSENROUTE.UUID=TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE.TLM_STRASSENROUTE_UUID";
			queryDef.WhereClause = null;

			IQueryName2 queryName2 = new FeatureQueryNameClass();
			var featureClassName = (IFeatureClassName) queryName2;

			featureClassName.ShapeFieldName = "TOPGIS_TLM.TLM_STRASSE.SHAPE";
			featureClassName.ShapeType = esriGeometryType.esriGeometryPolyline;

			queryName2.PrimaryKey =
				"TOPGIS_TLM.TLM_STRASSENROUTE.OBJECTID,TOPGIS_TLM.TLM_STRASSE.OBJECTID";
			queryName2.QueryDef = queryDef;

			var datasetName = (IDatasetName) queryName2;
			datasetName.Name = "TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE RELATION";
			datasetName.WorkspaceName = (IWorkspaceName) ((IDataset) ws).FullName;

			var joined = (ITable) ((IName) queryName2).Open();
			Assert.NotNull(joined);
		}

		[Test]
		[Ignore("learing test")]
		public void TestMultiUuidsKeyLeftJoinStrasseRoute()
		{
			// throws Error in queryName.Open() : System.ArgumentException : Value does not fall within the expected range.
			var ws = (IFeatureWorkspace) WorkspaceUtils.OpenSDEWorkspace(
				DirectConnectDriver.Oracle11g,
				"TOPGIST", "SDE.DEFAULT");

			IQueryDef queryDef = ws.CreateQueryDef();
			queryDef.SubFields = "*";

			queryDef.Tables =
				"TOPGIS_TLM.TLM_STRASSE " +
				"LEFT OUTER JOIN TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE ON TOPGIS_TLM.TLM_STRASSE.UUID=TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE.TLM_STRASSE_UUID " +
				"LEFT OUTER JOIN TOPGIS_TLM.TLM_STRASSENROUTE ON TOPGIS_TLM.TLM_STRASSENROUTE.UUID=TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE.TLM_STRASSENROUTE_UUID";
			queryDef.WhereClause = null;

			IQueryName2 queryName2 = new FeatureQueryNameClass();
			var featureClassName = (IFeatureClassName) queryName2;

			featureClassName.ShapeFieldName = "TOPGIS_TLM.TLM_STRASSE.SHAPE";
			featureClassName.ShapeType = esriGeometryType.esriGeometryPolyline;

			queryName2.PrimaryKey =
				"TOPGIS_TLM.TLM_STRASSE.UUID,TOPGIS_TLM.TLM_STRASSENROUTE.UUID";
			queryName2.QueryDef = queryDef;

			var datasetName = (IDatasetName) queryName2;
			datasetName.Name = "TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE RELATION";
			datasetName.WorkspaceName = (IWorkspaceName) ((IDataset) ws).FullName;

			var joined = (ITable) ((IName) queryName2).Open();
			Assert.NotNull(joined);
		}

		[Test]
		[Ignore("learing test")]
		public void TestSingleKeyLeftJoinStrasseRoute()
		{
			var ws = (IFeatureWorkspace) WorkspaceUtils.OpenSDEWorkspace(
				DirectConnectDriver.Oracle11g,
				"TOPGIST", "SDE.DEFAULT");

			IQueryDef queryDef = ws.CreateQueryDef();
			queryDef.SubFields = "*";

			queryDef.Tables =
				"TOPGIS_TLM.TLM_STRASSE " +
				"LEFT OUTER JOIN TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE ON TOPGIS_TLM.TLM_STRASSE.UUID=TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE.TLM_STRASSE_UUID " +
				"LEFT OUTER JOIN TOPGIS_TLM.TLM_STRASSENROUTE ON TOPGIS_TLM.TLM_STRASSENROUTE.UUID=TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE.TLM_STRASSENROUTE_UUID";
			queryDef.WhereClause = null;

			IQueryName2 queryName2 = new FeatureQueryNameClass();
			var featureClassName = (IFeatureClassName) queryName2;

			featureClassName.ShapeFieldName = "TOPGIS_TLM.TLM_STRASSE.SHAPE";
			featureClassName.ShapeType = esriGeometryType.esriGeometryPolyline;

			queryName2.PrimaryKey =
				"TOPGIS_TLM.TLM_STRASSE.OBJECTID";
			queryName2.QueryDef = queryDef;

			var datasetName = (IDatasetName) queryName2;
			datasetName.Name = "TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE RELATION";
			datasetName.WorkspaceName = (IWorkspaceName) ((IDataset) ws).FullName;

			var joined = (ITable) ((IName) queryName2).Open();
			Assert.NotNull(joined);
		}

		[Test]
		[Ignore("learing test")]
		public void TestMultiUuidsKeyInnerJoinStrasseRoute()
		{
			// throws Error in queryName.Open() : System.ArgumentException : Value does not fall within the expected range.
			var ws = (IFeatureWorkspace) WorkspaceUtils.OpenSDEWorkspace(
				DirectConnectDriver.Oracle11g,
				"TOPGIST", "SDE.DEFAULT");

			IQueryDef queryDef = ws.CreateQueryDef();
			queryDef.SubFields = "*";

			queryDef.Tables =
				"TOPGIS_TLM.TLM_STRASSE " +
				"INNER JOIN TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE ON TOPGIS_TLM.TLM_STRASSE.UUID=TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE.TLM_STRASSE_UUID " +
				"INNER JOIN TOPGIS_TLM.TLM_STRASSENROUTE ON TOPGIS_TLM.TLM_STRASSENROUTE.UUID=TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE.TLM_STRASSENROUTE_UUID";
			queryDef.WhereClause = null;

			IQueryName2 queryName2 = new FeatureQueryNameClass();
			var featureClassName = (IFeatureClassName) queryName2;

			featureClassName.ShapeFieldName = "TOPGIS_TLM.TLM_STRASSE.SHAPE";
			featureClassName.ShapeType = esriGeometryType.esriGeometryPolyline;

			queryName2.PrimaryKey =
				"TOPGIS_TLM.TLM_STRASSE.UUID,TOPGIS_TLM.TLM_STRASSENROUTE.UUID";
			queryName2.QueryDef = queryDef;

			var datasetName = (IDatasetName) queryName2;
			datasetName.Name = "TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE RELATION";
			datasetName.WorkspaceName = (IWorkspaceName) ((IDataset) ws).FullName;

			var joined = (ITable) ((IName) queryName2).Open();
			Assert.NotNull(joined);
		}

		[Test]
		[Ignore("learing test")]
		public void TestMultiOidsKeyRightJoinStrasseRoute()
		{
			// works ~ 15"
			var ws = (IFeatureWorkspace) WorkspaceUtils.OpenSDEWorkspace(
				DirectConnectDriver.Oracle11g,
				"TOPGIST", "SDE.DEFAULT");

			IQueryDef queryDef = ws.CreateQueryDef();
			queryDef.SubFields = "*";

			queryDef.Tables =
				"TOPGIS_TLM.TLM_STRASSE " +
				"RIGHT OUTER JOIN TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE ON TOPGIS_TLM.TLM_STRASSE.UUID=TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE.TLM_STRASSE_UUID " +
				"RIGHT OUTER JOIN TOPGIS_TLM.TLM_STRASSENROUTE ON TOPGIS_TLM.TLM_STRASSENROUTE.UUID=TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE.TLM_STRASSENROUTE_UUID";

			queryDef.WhereClause = null;

			IQueryName2 queryName2 = new FeatureQueryNameClass();
			var featureClassName = (IFeatureClassName) queryName2;

			featureClassName.ShapeFieldName = "TOPGIS_TLM.TLM_STRASSE.SHAPE";
			featureClassName.ShapeType = esriGeometryType.esriGeometryPolyline;

			queryName2.PrimaryKey =
				"TOPGIS_TLM.TLM_STRASSENROUTE.OBJECTID,TOPGIS_TLM.TLM_STRASSE.OBJECTID";
			queryName2.QueryDef = queryDef;

			var datasetName = (IDatasetName) queryName2;
			datasetName.Name = "TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE RELATION";
			datasetName.WorkspaceName = (IWorkspaceName) ((IDataset) ws).FullName;

			var joined = (ITable) ((IName) queryName2).Open();
			Assert.NotNull(joined);
		}

		[Test]
		[Ignore("learing test")]
		public void TestMultiOidsKeyLeftJoinStrasseWanderweg()
		{
			// takes "forever"
			var ws = (IFeatureWorkspace) WorkspaceUtils.OpenSDEWorkspace(
				DirectConnectDriver.Oracle11g,
				"TOPGIST", "SDE.DEFAULT");

			IQueryDef queryDef = ws.CreateQueryDef();
			queryDef.SubFields = "*";

			queryDef.Tables =
				"TOPGIS_TLM.TLM_STRASSE " +
				"LEFT OUTER JOIN TOPGIS_TLM.TLM_WANDERWEG  ON TOPGIS_TLM.TLM_STRASSE.UUID = TOPGIS_TLM.TLM_WANDERWEG.TLM_STRASSE_UUID ";

			queryDef.WhereClause = null;

			IQueryName2 queryName2 = new FeatureQueryNameClass();
			var featureClassName = (IFeatureClassName) queryName2;

			featureClassName.ShapeFieldName = "TOPGIS_TLM.TLM_STRASSE.SHAPE";
			featureClassName.ShapeType = esriGeometryType.esriGeometryPolyline;

			queryName2.PrimaryKey =
				"TOPGIS_TLM.TLM_WANDERWEG.OBJECTID,TOPGIS_TLM.TLM_STRASSE.OBJECTID";
			queryName2.QueryDef = queryDef;

			var datasetName = (IDatasetName) queryName2;
			datasetName.Name = "TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE RELATION";
			datasetName.WorkspaceName = (IWorkspaceName) ((IDataset) ws).FullName;

			var joined = (ITable) ((IName) queryName2).Open();

			bool gaga = joined.HasOID;
			long t = joined.CreateRow().OID;
			IFeature f = null;
			t = f.OID;

			Assert.NotNull(joined);
		}

		[Test]
		[Ignore("learing test")]
		public void TestMultiOidsKeyRightJoinStrasseWanderweg()
		{
			// takes ~ 2 min
			var ws = (IFeatureWorkspace) WorkspaceUtils.OpenSDEWorkspace(
				DirectConnectDriver.Oracle11g,
				"TOPGIST", "SDE.DEFAULT");

			IQueryDef queryDef = ws.CreateQueryDef();
			queryDef.SubFields = "*";

			queryDef.Tables =
				"TOPGIS_TLM.TLM_STRASSE " +
				"RIGHT OUTER JOIN TOPGIS_TLM.TLM_WANDERWEG  ON TOPGIS_TLM.TLM_STRASSE.UUID = TOPGIS_TLM.TLM_WANDERWEG.TLM_STRASSE_UUID ";

			queryDef.WhereClause = null;

			//Wrapped

			IQueryName2 queryName2 = new FeatureQueryNameClass();
			var featureClassName = (IFeatureClassName) queryName2;

			featureClassName.ShapeFieldName = "TOPGIS_TLM.TLM_STRASSE.SHAPE";
			featureClassName.ShapeType = esriGeometryType.esriGeometryPolyline;

			queryName2.PrimaryKey =
				"TOPGIS_TLM.TLM_WANDERWEG.OBJECTID,TOPGIS_TLM.TLM_STRASSE.OBJECTID";
			queryName2.QueryDef = queryDef;

			var datasetName = (IDatasetName) queryName2;
			datasetName.Name = "TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE RELATION";
			datasetName.WorkspaceName = (IWorkspaceName) ((IDataset) ws).FullName;

			var joined = (ITable) ((IName) queryName2).Open();
			Assert.NotNull(joined);
		}

		[Test]
		[Ignore("uses local data")]
		public void TestStrassenRoute()
		{
			var ws = (IFeatureWorkspace) WorkspaceUtils.OpenSDEWorkspace(
				DirectConnectDriver.Oracle11g,
				"TOPGIST", "SDE.DEFAULT");

			IFeatureClass fc = ws.OpenFeatureClass("TOPGIS_TLM.TLM_STRASSE");
			var ds1 = (IDataset) fc;
			ITable tblRel = ws.OpenTable("TOPGIS_TLM.TLM_STRASSENROUTE");
			var dsRel = (IDataset) tblRel;
			const string relName = "TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE";

			var model = new SimpleModel("model", fc);
			Dataset mds1 = model.AddDataset(new ModelVectorDataset(ds1.Name));
			Dataset mdsRel = model.AddDataset(new ModelTableDataset(dsRel.Name));

			var clsDesc = new ClassDescriptor(typeof(QaRelLineGroupConstraints));
			var tstDesc = new TestDescriptor("GroupEnds", clsDesc);
			var condition = new QualityCondition("cndGroupEnds", tstDesc);
			InstanceConfigurationUtils.AddParameterValue(condition, "relationTables", mds1);
			InstanceConfigurationUtils.AddParameterValue(condition, "relationTables", mdsRel);
			InstanceConfigurationUtils.AddParameterValue(condition, "relation", relName);
			InstanceConfigurationUtils.AddParameterValue(condition, "join", JoinType.LeftJoin);
			InstanceConfigurationUtils.AddParameterValue(condition, "minGap", 10);
			InstanceConfigurationUtils.AddParameterValue(condition, "minGroupLength", 10);
			InstanceConfigurationUtils.AddParameterValue(condition, "minLeafLength", 20);
			InstanceConfigurationUtils.AddParameterValue(
				condition, "groupBy",
				string.Format("{0}", "TOPGIS_TLM.TLM_STRASSENROUTE.OBJECTID"));
			InstanceConfigurationUtils.AddParameterValue(condition, "GroupCondition",
			                                             "TOPGIS_TLM.TLM_STRASSENROUTE.OBJECTID IS NOT NULL");
			InstanceConfigurationUtils.AddParameterValue(condition, "MinGapToOtherGroup", 10);

			var fact = new QaRelLineGroupConstraints();
			fact.Condition = condition;

			IList<ITest> tests =
				fact.CreateTests(new SimpleDatasetOpener(model.MasterDatabaseWorkspaceContext));
			Assert.AreEqual(1, tests.Count);

			var testContainer = new TestContainer { TileSize = 10000 };
			testContainer.AddTest(tests[0]);

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline, _spatialReference,
				                1000));
			fields.AddField(FieldUtils.CreateTextField(_stateIdFieldName, 100));
			fields.AddField(FieldUtils.CreateTextField(_textFieldName, 200));

			int ticks = Environment.TickCount;
			string errFcName = string.Format("err_{0}", ticks);

			//_errorFc = DatasetUtils.CreateSimpleFeatureClass(
			//	RelTestWs, errFcName, fields);

			testContainer.QaError += testContainer_QaError;

			//testContainer.Execute(GeometryFactory.CreateEnvelope(
			//	new WKSEnvelope {XMin = 2480000, YMin = 1070000, XMax = 2900000, YMax = 1320000}));

			//testContainer.Execute(GeometryFactory.CreateEnvelope(
			//	new WKSEnvelope { XMin = 2612000, YMin = 1271000, XMax = 2612500, YMax = 1272000 }));

			//int errorCount = testContainer.Execute(GeometryFactory.CreateEnvelope(
			//	new WKSEnvelope {XMin = 2695079, YMin = 1264570, XMax = 2695094, YMax = 1264585}));

			//int errorCount = testContainer.Execute(GeometryFactory.CreatePolygon(GeometryFactory.CreateEnvelope(
			//	new WKSEnvelope { XMin = 2689990, YMin = 1259990, XMax = 2698760, YMax = 1266010 })));

			double dx = 10;
			int errorCount =
				testContainer.Execute(
					GeometryFactory.CreatePolygon(GeometryFactory.CreateEnvelope(
						                              new WKSEnvelope
						                              {
							                              XMin = 2696880 - dx,
							                              YMin = 1262110 - dx,
							                              XMax = 2696880 + dx,
							                              YMax = 1262110 + dx
						                              })));

			//int errorCount = testContainer.Execute(GeometryFactory.CreateEnvelope(
			//	new WKSEnvelope {XMin = 2695110, YMin = 1264440, XMax = 2695120, YMax = 1264450}));
			Console.WriteLine(errorCount);
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void TestFindDiffs()
		{
			var ws = (IFeatureWorkspace) WorkspaceUtils.OpenSDEWorkspace(
				DirectConnectDriver.Oracle11g, "TOPGIST",
				"sde", "sde", "SDE.DEFAULT");

			//IQueryDef queryDef = ws.CreateQueryDef();
			//queryDef.SubFields =
			//	// "*";
			//	"TOPGIS_TLM.TLM_STRASSENROUTE.OBJECTID,TOPGIS_TLM.TLM_STRASSENROUTE.UUID,TOPGIS_TLM.TLM_STRASSENROUTE.OPERATEUR,TOPGIS_TLM.TLM_STRASSENROUTE.DATUM_ERSTELLUNG,TOPGIS_TLM.TLM_STRASSENROUTE.DATUM_AENDERUNG,TOPGIS_TLM.TLM_STRASSENROUTE.HERKUNFT,TOPGIS_TLM.TLM_STRASSENROUTE.HERKUNFT_JAHR,TOPGIS_TLM.TLM_STRASSENROUTE.HERKUNFT_MONAT,TOPGIS_TLM.TLM_STRASSENROUTE.ERSTELLUNG_JAHR,TOPGIS_TLM.TLM_STRASSENROUTE.ERSTELLUNG_MONAT,TOPGIS_TLM.TLM_STRASSENROUTE.REVISION_JAHR,TOPGIS_TLM.TLM_STRASSENROUTE.REVISION_MONAT,TOPGIS_TLM.TLM_STRASSENROUTE.GRUND_AENDERUNG,TOPGIS_TLM.TLM_STRASSENROUTE.OBJEKTART,TOPGIS_TLM.TLM_STRASSENROUTE.NAME,TOPGIS_TLM.TLM_STRASSENROUTE.ROUTENNUMMER,TOPGIS_TLM.TLM_STRASSENROUTE.RC_ID,TOPGIS_TLM.TLM_STRASSENROUTE.WU_ID,TOPGIS_TLM.TLM_STRASSENROUTE.RC_ID_CREATION,TOPGIS_TLM.TLM_STRASSENROUTE.WU_ID_CREATION,TOPGIS_TLM.TLM_STRASSENROUTE.REVISION_QUALITAET,TOPGIS_TLM.TLM_STRASSENROUTE.ORIGINAL_HERKUNFT,TOPGIS_TLM.TLM_STRASSENROUTE.FELD_BEARBEITUNG,TOPGIS_TLM.TLM_STRASSENROUTE.INTEGRATION_OBJECT_UUID,TOPGIS_TLM.TLM_STRASSE.OBJECTID,TOPGIS_TLM.TLM_STRASSE.ENABLED,TOPGIS_TLM.TLM_STRASSE.GRUND_AENDERUNG,TOPGIS_TLM.TLM_STRASSE.HERKUNFT,TOPGIS_TLM.TLM_STRASSE.HERKUNFT_JAHR,TOPGIS_TLM.TLM_STRASSE.HERKUNFT_MONAT,TOPGIS_TLM.TLM_STRASSE.DATUM_ERSTELLUNG,TOPGIS_TLM.TLM_STRASSE.ERSTELLUNG_JAHR,TOPGIS_TLM.TLM_STRASSE.ERSTELLUNG_MONAT,TOPGIS_TLM.TLM_STRASSE.REVISION_JAHR,TOPGIS_TLM.TLM_STRASSE.REVISION_MONAT,TOPGIS_TLM.TLM_STRASSE.UUID,TOPGIS_TLM.TLM_STRASSE.OPERATEUR,TOPGIS_TLM.TLM_STRASSE.DATUM_AENDERUNG,TOPGIS_TLM.TLM_STRASSE.OBJEKTART,TOPGIS_TLM.TLM_STRASSE.PANNENSTREIFEN,TOPGIS_TLM.TLM_STRASSE.VERKEHRSBESCHRAENKUNG,TOPGIS_TLM.TLM_STRASSE.KUNSTBAUTE,TOPGIS_TLM.TLM_STRASSE.RICHTUNGSGETRENNT,TOPGIS_TLM.TLM_STRASSE.TLM_STRASSEN_NAME_UUID,TOPGIS_TLM.TLM_STRASSE.BELAGSART,TOPGIS_TLM.TLM_STRASSE.RADSTREIFEN,TOPGIS_TLM.TLM_STRASSE.ANZAHL_STREIFEN,TOPGIS_TLM.TLM_STRASSE.MINIMALBREITE,TOPGIS_TLM.TLM_STRASSE.TROTTOIR,TOPGIS_TLM.TLM_STRASSE.KREISEL,TOPGIS_TLM.TLM_STRASSE.EINBAHN,TOPGIS_TLM.TLM_STRASSE.EROEFFNUNGSDATUM,TOPGIS_TLM.TLM_STRASSE.EIGENTUEMER,TOPGIS_TLM.TLM_STRASSE.STUFE,TOPGIS_TLM.TLM_STRASSE.RC_ID,TOPGIS_TLM.TLM_STRASSE.WU_ID,TOPGIS_TLM.TLM_STRASSE.RC_ID_CREATION,TOPGIS_TLM.TLM_STRASSE.WU_ID_CREATION,TOPGIS_TLM.TLM_STRASSE.BEFAHRBARKEIT,TOPGIS_TLM.TLM_STRASSE.REVISION_QUALITAET,TOPGIS_TLM.TLM_STRASSE.ORIGINAL_HERKUNFT,TOPGIS_TLM.TLM_STRASSE.FELD_BEARBEITUNG,TOPGIS_TLM.TLM_STRASSE.INTEGRATION_OBJECT_UUID,TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE.TLM_STRASSENROUTE_UUID,TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE.TLM_STRASSE_UUID,TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE.RID,TOPGIS_TLM.TLM_STRASSE.SHAPE";
			//	//"TOPGIS_TLM.TLM_STRASSENROUTE.OBJECTID,TOPGIS_TLM.TLM_STRASSENROUTE.UUID,TOPGIS_TLM.TLM_STRASSENROUTE.OPERATEUR,TOPGIS_TLM.TLM_STRASSENROUTE.DATUM_ERSTELLUNG,TOPGIS_TLM.TLM_STRASSENROUTE.DATUM_AENDERUNG,TOPGIS_TLM.TLM_STRASSENROUTE.HERKUNFT,TOPGIS_TLM.TLM_STRASSENROUTE.HERKUNFT_JAHR,TOPGIS_TLM.TLM_STRASSENROUTE.HERKUNFT_MONAT,TOPGIS_TLM.TLM_STRASSENROUTE.ERSTELLUNG_JAHR,TOPGIS_TLM.TLM_STRASSENROUTE.ERSTELLUNG_MONAT,TOPGIS_TLM.TLM_STRASSENROUTE.REVISION_JAHR,TOPGIS_TLM.TLM_STRASSENROUTE.REVISION_MONAT,TOPGIS_TLM.TLM_STRASSENROUTE.GRUND_AENDERUNG,TOPGIS_TLM.TLM_STRASSENROUTE.OBJEKTART,TOPGIS_TLM.TLM_STRASSENROUTE.NAME,TOPGIS_TLM.TLM_STRASSENROUTE.ROUTENNUMMER,TOPGIS_TLM.TLM_STRASSENROUTE.RC_ID,TOPGIS_TLM.TLM_STRASSENROUTE.WU_ID,TOPGIS_TLM.TLM_STRASSENROUTE.RC_ID_CREATION,TOPGIS_TLM.TLM_STRASSENROUTE.WU_ID_CREATION,TOPGIS_TLM.TLM_STRASSENROUTE.REVISION_QUALITAET,TOPGIS_TLM.TLM_STRASSENROUTE.ORIGINAL_HERKUNFT,TOPGIS_TLM.TLM_STRASSENROUTE.FELD_BEARBEITUNG,TOPGIS_TLM.TLM_STRASSENROUTE.INTEGRATION_OBJECT_UUID,TOPGIS_TLM.TLM_STRASSE.OBJECTID,TOPGIS_TLM.TLM_STRASSE.ENABLED,TOPGIS_TLM.TLM_STRASSE.GRUND_AENDERUNG,TOPGIS_TLM.TLM_STRASSE.HERKUNFT,TOPGIS_TLM.TLM_STRASSE.HERKUNFT_JAHR,TOPGIS_TLM.TLM_STRASSE.HERKUNFT_MONAT,TOPGIS_TLM.TLM_STRASSE.DATUM_ERSTELLUNG,TOPGIS_TLM.TLM_STRASSE.ERSTELLUNG_JAHR,TOPGIS_TLM.TLM_STRASSE.ERSTELLUNG_MONAT,TOPGIS_TLM.TLM_STRASSE.REVISION_JAHR,TOPGIS_TLM.TLM_STRASSE.REVISION_MONAT,TOPGIS_TLM.TLM_STRASSE.UUID,TOPGIS_TLM.TLM_STRASSE.OPERATEUR,TOPGIS_TLM.TLM_STRASSE.DATUM_AENDERUNG,TOPGIS_TLM.TLM_STRASSE.OBJEKTART,TOPGIS_TLM.TLM_STRASSE.PANNENSTREIFEN,TOPGIS_TLM.TLM_STRASSE.VERKEHRSBESCHRAENKUNG,TOPGIS_TLM.TLM_STRASSE.KUNSTBAUTE,TOPGIS_TLM.TLM_STRASSE.RICHTUNGSGETRENNT,TOPGIS_TLM.TLM_STRASSE.TLM_STRASSEN_NAME_UUID,TOPGIS_TLM.TLM_STRASSE.BELAGSART,TOPGIS_TLM.TLM_STRASSE.RADSTREIFEN,TOPGIS_TLM.TLM_STRASSE.ANZAHL_STREIFEN,TOPGIS_TLM.TLM_STRASSE.MINIMALBREITE,TOPGIS_TLM.TLM_STRASSE.TROTTOIR,TOPGIS_TLM.TLM_STRASSE.KREISEL,TOPGIS_TLM.TLM_STRASSE.EINBAHN,TOPGIS_TLM.TLM_STRASSE.EROEFFNUNGSDATUM,TOPGIS_TLM.TLM_STRASSE.EIGENTUEMER,TOPGIS_TLM.TLM_STRASSE.STUFE,TOPGIS_TLM.TLM_STRASSE.RC_ID,TOPGIS_TLM.TLM_STRASSE.WU_ID,TOPGIS_TLM.TLM_STRASSE.RC_ID_CREATION,TOPGIS_TLM.TLM_STRASSE.WU_ID_CREATION,TOPGIS_TLM.TLM_STRASSE.BEFAHRBARKEIT,TOPGIS_TLM.TLM_STRASSE.REVISION_QUALITAET,TOPGIS_TLM.TLM_STRASSE.ORIGINAL_HERKUNFT,TOPGIS_TLM.TLM_STRASSE.FELD_BEARBEITUNG,TOPGIS_TLM.TLM_STRASSE.INTEGRATION_OBJECT_UUID,TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE.TLM_STRASSENROUTE_UUID,TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE.TLM_STRASSE_UUID,TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE.RID,TOPGIS_TLM.TLM_STRASSE.SHAPE";

			//string[] fields = queryDef.SubFields.Split(',');

			//queryDef.Tables =
			//	"TOPGIS_TLM.TLM_STRASSE " +
			//	"LEFT OUTER JOIN TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE ON TOPGIS_TLM.TLM_STRASSE.UUID=TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE.TLM_STRASSE_UUID " +
			//	"LEFT OUTER JOIN TOPGIS_TLM.TLM_STRASSENROUTE ON TOPGIS_TLM.TLM_STRASSENROUTE.UUID=TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE.TLM_STRASSENROUTE_UUID";

			//queryDef.WhereClause = null;

			//IQueryName2 queryName2 = new FeatureQueryNameClass();
			//var featureClassName = (IFeatureClassName)queryName2;

			//featureClassName.ShapeFieldName = "TOPGIS_TLM.TLM_STRASSE.SHAPE";
			//featureClassName.ShapeType = esriGeometryType.esriGeometryPolyline;

			//queryName2.PrimaryKey = "TOPGIS_TLM.TLM_STRASSE.OBJECTID";
			//queryName2.QueryDef = queryDef;

			//var datasetName = (IDatasetName)queryName2;
			//datasetName.Name = "TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE RELATION";
			//datasetName.WorkspaceName = (IWorkspaceName)((IDataset)ws).FullName;

			//ITable joined = (ITable)((IName)queryName2).Open();

			//ITable joined =
			//	TableJoinUtils.CreateQueryTable(
			//		ws.OpenRelationshipClass("TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE"),
			//		JoinType.RightJoin);

			ISpatialFilter filter = new SpatialFilterClass();

			filter.Geometry = GeometryFactory.CreatePolygon(GeometryFactory.CreateEnvelope(
				                                                new WKSEnvelope
				                                                {
					                                                XMin = 2689980,
					                                                YMin = 1259980,
					                                                XMax = 2698770,
					                                                YMax = 1266020
				                                                }));

			//filter.Geometry = GeometryFactory.CreateEnvelope(
			//	new WKSEnvelope { XMin = 2690800, YMin = 1263500, XMax = 2691400, YMax = 1263900 });

			//filter.Geometry.SpatialReference = ((IGeoDataset) joined).SpatialReference;

			filter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;

			var subfieldsList = new List<string>();

			////int i = 0;
			//for (int i = fields.Length - 2; i < fields.Length - 1; i++)
			//{
			//	StringBuilder subfields = new StringBuilder();
			//	for (int j = 0; j <= i; j++)
			//	{
			//		subfields.Append(fields[j]);
			//		subfields.Append(",");
			//	}
			//	// add shape field
			//	subfields.Append(fields[fields.Length - 1]);
			//	if (i == 0)
			//	{
			//		subfields.Clear();
			//		for (int j = 0; j < joined.Fields.FieldCount; j++)
			//		{
			//			subfields.Append(joined.Fields.Field[j].Name);
			//			subfields.Append(",");
			//		}
			//		subfields.Remove(subfields.Length - 1, 1);
			//		//subfields = new StringBuilder("*");
			//	}			
			//	subfieldsList.Add(subfields.ToString());
			//}
			//subfieldsList.Clear();
			subfieldsList.Add("*");
			subfieldsList.Add("TOPGIS_TLM.TLM_STRASSE.OBJECTID");
			subfieldsList.Add("TOPGIS_TLM.TLM_STRASSE.OBJECTID,TOPGIS_TLM.TLM_STRASSE.SHAPE");

			subfieldsList.Add(
				"TOPGIS_TLM.TLM_STRASSENROUTE.OBJECTID,TOPGIS_TLM.TLM_STRASSE.OBJECTID");
			subfieldsList.Add(
				"TOPGIS_TLM.TLM_STRASSE.OBJECTID,TOPGIS_TLM.TLM_STRASSENROUTE.OBJECTID");
			subfieldsList.Add(
				"TOPGIS_TLM.TLM_STRASSE.OBJECTID,TOPGIS_TLM.TLM_STRASSENROUTE.OBJECTID,TOPGIS_TLM.TLM_STRASSE.SHAPE");
			subfieldsList.Add(
				"TOPGIS_TLM.TLM_STRASSE.OBJECTID,TOPGIS_TLM.TLM_STRASSENROUTE.OBJECTID,TOPGIS_TLM.TLM_STRASSE.OBJEKTART,TOPGIS_TLM.TLM_STRASSE.SHAPE");
			subfieldsList.Add(
				"TOPGIS_TLM.TLM_STRASSE.OBJECTID,TOPGIS_TLM.TLM_STRASSENROUTE.OBJECTID,TOPGIS_TLM.TLM_STRASSENROUTE.OBJEKTART,TOPGIS_TLM.TLM_STRASSE.SHAPE");
			subfieldsList.Add(
				"TOPGIS_TLM.TLM_STRASSENROUTE.OBJECTID,TOPGIS_TLM.TLM_STRASSE.OBJECTID,TOPGIS_TLM.TLM_STRASSENROUTE.OBJEKTART,TOPGIS_TLM.TLM_STRASSE.SHAPE");

			subfieldsList.Add(
				"TOPGIS_TLM.TLM_STRASSENROUTE.OBJECTID,TOPGIS_TLM.TLM_STRASSE.OBJECTID,TOPGIS_TLM.TLM_STRASSE.SHAPE");
			foreach (string subfields in subfieldsList)
			{
				ITable joined =
					TableJoinUtils.CreateQueryTable(
						ws.OpenRelationshipClass("TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE"),
						JoinType.RightJoin);

				filter.SubFields =
					// "*";
					//"TOPGIS_TLM.TLM_STRASSENROUTE.OBJECTID,TOPGIS_TLM.TLM_STRASSENROUTE.UUID,TOPGIS_TLM.TLM_STRASSENROUTE.OPERATEUR,TOPGIS_TLM.TLM_STRASSENROUTE.DATUM_ERSTELLUNG,TOPGIS_TLM.TLM_STRASSENROUTE.DATUM_AENDERUNG,TOPGIS_TLM.TLM_STRASSENROUTE.HERKUNFT,TOPGIS_TLM.TLM_STRASSENROUTE.HERKUNFT_JAHR,TOPGIS_TLM.TLM_STRASSENROUTE.HERKUNFT_MONAT,TOPGIS_TLM.TLM_STRASSENROUTE.ERSTELLUNG_JAHR,TOPGIS_TLM.TLM_STRASSENROUTE.ERSTELLUNG_MONAT,TOPGIS_TLM.TLM_STRASSENROUTE.REVISION_JAHR,TOPGIS_TLM.TLM_STRASSENROUTE.REVISION_MONAT,TOPGIS_TLM.TLM_STRASSENROUTE.GRUND_AENDERUNG,TOPGIS_TLM.TLM_STRASSENROUTE.OBJEKTART,TOPGIS_TLM.TLM_STRASSENROUTE.NAME,TOPGIS_TLM.TLM_STRASSENROUTE.ROUTENNUMMER,TOPGIS_TLM.TLM_STRASSENROUTE.RC_ID,TOPGIS_TLM.TLM_STRASSENROUTE.WU_ID,TOPGIS_TLM.TLM_STRASSENROUTE.RC_ID_CREATION,TOPGIS_TLM.TLM_STRASSENROUTE.WU_ID_CREATION,TOPGIS_TLM.TLM_STRASSENROUTE.REVISION_QUALITAET,TOPGIS_TLM.TLM_STRASSENROUTE.ORIGINAL_HERKUNFT,TOPGIS_TLM.TLM_STRASSENROUTE.FELD_BEARBEITUNG,TOPGIS_TLM.TLM_STRASSENROUTE.INTEGRATION_OBJECT_UUID,TOPGIS_TLM.TLM_STRASSE.OBJECTID,TOPGIS_TLM.TLM_STRASSE.ENABLED,TOPGIS_TLM.TLM_STRASSE.GRUND_AENDERUNG,TOPGIS_TLM.TLM_STRASSE.HERKUNFT,TOPGIS_TLM.TLM_STRASSE.HERKUNFT_JAHR,TOPGIS_TLM.TLM_STRASSE.HERKUNFT_MONAT,TOPGIS_TLM.TLM_STRASSE.DATUM_ERSTELLUNG,TOPGIS_TLM.TLM_STRASSE.ERSTELLUNG_JAHR,TOPGIS_TLM.TLM_STRASSE.ERSTELLUNG_MONAT,TOPGIS_TLM.TLM_STRASSE.REVISION_JAHR,TOPGIS_TLM.TLM_STRASSE.REVISION_MONAT,TOPGIS_TLM.TLM_STRASSE.UUID,TOPGIS_TLM.TLM_STRASSE.OPERATEUR,TOPGIS_TLM.TLM_STRASSE.DATUM_AENDERUNG,TOPGIS_TLM.TLM_STRASSE.OBJEKTART,TOPGIS_TLM.TLM_STRASSE.PANNENSTREIFEN,TOPGIS_TLM.TLM_STRASSE.VERKEHRSBESCHRAENKUNG,TOPGIS_TLM.TLM_STRASSE.KUNSTBAUTE,TOPGIS_TLM.TLM_STRASSE.RICHTUNGSGETRENNT,TOPGIS_TLM.TLM_STRASSE.TLM_STRASSEN_NAME_UUID,TOPGIS_TLM.TLM_STRASSE.BELAGSART,TOPGIS_TLM.TLM_STRASSE.RADSTREIFEN,TOPGIS_TLM.TLM_STRASSE.ANZAHL_STREIFEN,TOPGIS_TLM.TLM_STRASSE.MINIMALBREITE,TOPGIS_TLM.TLM_STRASSE.TROTTOIR,TOPGIS_TLM.TLM_STRASSE.KREISEL,TOPGIS_TLM.TLM_STRASSE.EINBAHN,TOPGIS_TLM.TLM_STRASSE.EROEFFNUNGSDATUM,TOPGIS_TLM.TLM_STRASSE.EIGENTUEMER,TOPGIS_TLM.TLM_STRASSE.STUFE,TOPGIS_TLM.TLM_STRASSE.RC_ID,TOPGIS_TLM.TLM_STRASSE.WU_ID,TOPGIS_TLM.TLM_STRASSE.RC_ID_CREATION,TOPGIS_TLM.TLM_STRASSE.WU_ID_CREATION,TOPGIS_TLM.TLM_STRASSE.BEFAHRBARKEIT,TOPGIS_TLM.TLM_STRASSE.REVISION_QUALITAET,TOPGIS_TLM.TLM_STRASSE.ORIGINAL_HERKUNFT,TOPGIS_TLM.TLM_STRASSE.FELD_BEARBEITUNG,TOPGIS_TLM.TLM_STRASSE.INTEGRATION_OBJECT_UUID,TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE.TLM_STRASSENROUTE_UUID,TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE.TLM_STRASSE_UUID,TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE.RID,TOPGIS_TLM.TLM_STRASSE.SHAPE";
					//"TOPGIS_TLM.TLM_STRASSE.OBJECTID,TOPGIS_TLM.TLM_STRASSE.SHAPE";
					subfields;

				var joinedRows = new Dictionary<long, IList<IRow>>();
				foreach (IRow row in new EnumCursor(joined, filter, false))
				{
					IList<IRow> rows;
					if (! joinedRows.TryGetValue(row.OID, out rows))
					{
						rows = new List<IRow>();
						joinedRows.Add(row.OID, rows);
					}

					rows.Add(row);
				}

				filter.SubFields = "*";

				var simpleRows = new Dictionary<long, IList<IRow>>();
				foreach (
					IRow row in new EnumCursor(ws.OpenTable("TOPGIS_TLM.TLM_STRASSE"), filter,
					                           false)
				)
				{
					IList<IRow> rows;
					if (! simpleRows.TryGetValue(row.OID, out rows))
					{
						rows = new List<IRow>();
						simpleRows.Add(row.OID, rows);
					}

					rows.Add(row);
				}

				var missingIds = new List<long>();

				foreach (KeyValuePair<long, IList<IRow>> pair in simpleRows)
				{
					if (! joinedRows.ContainsKey(pair.Key))
					{
						missingIds.Add(pair.Key);
					}
				}

				var joinedCount = 0;
				foreach (IList<IRow> rows in joinedRows.Values)
				{
					joinedCount += rows.Count;
				}

				Console.WriteLine("Subfields :" + subfields);
				Console.WriteLine(
					"Simple Features : {0}; Joined OIDS: {1}; Joined Features: {2}; Missing Features: {3};",
					simpleRows.Count, joinedRows.Count, joinedCount, +missingIds.Count);
			}

			//missingIds.Sort();
			//foreach (int missingId in missingIds)
			//{
			//	Console.WriteLine(missingId);
			//}
		}

		[Test]
		[Ignore("uses local data")]
		public void TestStrassenWanderweg()
		{
			var ws = (IFeatureWorkspace) WorkspaceUtils.OpenSDEWorkspace(
				DirectConnectDriver.Oracle11g,
				"TOPGIST", "SDE.DEFAULT");

			IFeatureClass fc = ws.OpenFeatureClass("TOPGIS_TLM.TLM_STRASSE");
			var ds1 = (IDataset) fc;
			ITable tblRel = ws.OpenTable("TOPGIS_TLM.TLM_WANDERWEG");
			var dsRel = (IDataset) tblRel;
			const string relName = "TOPGIS_TLM.TLM_WANDERWEG_STRASSE";

			var model = new SimpleModel("model", fc);
			Dataset mds1 = model.AddDataset(new ModelVectorDataset(ds1.Name));
			Dataset mdsRel = model.AddDataset(new ModelTableDataset(dsRel.Name));

			var clsDesc = new ClassDescriptor(typeof(QaRelLineGroupConstraints));
			var tstDesc = new TestDescriptor("GroupEnds", clsDesc);
			var condition = new QualityCondition("cndGroupEnds", tstDesc);
			InstanceConfigurationUtils.AddParameterValue(condition, "relationTables", mds1);
			InstanceConfigurationUtils.AddParameterValue(condition, "relationTables", mdsRel);
			InstanceConfigurationUtils.AddParameterValue(condition, "relation", relName);
			InstanceConfigurationUtils.AddParameterValue(condition, "join", JoinType.LeftJoin);
			InstanceConfigurationUtils.AddParameterValue(condition, "minGap", 10);
			InstanceConfigurationUtils.AddParameterValue(condition, "minGroupLength", 10);
			InstanceConfigurationUtils.AddParameterValue(condition, "minLeafLength", 20);
			InstanceConfigurationUtils.AddParameterValue(
				condition, "groupBy", string.Format("{0}", "TOPGIS_TLM.TLM_WANDERWEG.OBJEKTART"));
			InstanceConfigurationUtils.AddParameterValue(condition, "GroupCondition",
			                                             "TOPGIS_TLM.TLM_WANDERWEG.OBJEKTART IS NOT NULL");
			InstanceConfigurationUtils.AddParameterValue(condition, "MinGapToOtherGroup", 10);

			var fact = new QaRelLineGroupConstraints();
			fact.Condition = condition;

			IList<ITest> tests =
				fact.CreateTests(new SimpleDatasetOpener(model.MasterDatabaseWorkspaceContext));
			Assert.AreEqual(1, tests.Count);

			var testContainer = new TestContainer { TileSize = 20000 };
			testContainer.AddTest(tests[0]);

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline, _spatialReference,
				                1000));
			fields.AddField(FieldUtils.CreateTextField(_stateIdFieldName, 100));
			fields.AddField(FieldUtils.CreateTextField(_textFieldName, 200));

			int ticks = Environment.TickCount;
			string errFcName = string.Format("err_{0}", ticks);

			//_errorFc = DatasetUtils.CreateSimpleFeatureClass(
			//	RelTestWs, errFcName, fields);

			testContainer.QaError += testContainer_QaError;

			//testContainer.Execute(GeometryFactory.CreateEnvelope(
			//	new WKSEnvelope {XMin = 2480000, YMin = 1070000, XMax = 2900000, YMax = 1320000}));

			//testContainer.Execute(GeometryFactory.CreateEnvelope(
			//	new WKSEnvelope { XMin = 2612000, YMin = 1271000, XMax = 2612500, YMax = 1272000 }));

			//int errorCount = testContainer.Execute(GeometryFactory.CreateEnvelope(
			//	new WKSEnvelope {XMin = 2695110, YMin = 1264440, XMax = 2695120, YMax = 1264450}));

			int errorCount = testContainer.Execute(GeometryFactory.CreateEnvelope(
				                                       new WKSEnvelope
				                                       {
					                                       XMin = 2480000,
					                                       YMin = 1060000,
					                                       XMax = 2860000,
					                                       YMax = 1320000
				                                       }));

			Console.WriteLine(errorCount);
		}

		[Test]
		[Ignore("uses local data")]
		public void TestWanderwege()
		{
			var ws = (IFeatureWorkspace) WorkspaceUtils.OpenSDEWorkspace(
				DirectConnectDriver.Oracle11g,
				"TOPGIST", "SDE.DEFAULT");

			IFeatureClass fc = ws.OpenFeatureClass("TOPGIS_TLM.TLM_STRASSE");
			var ds1 = (IDataset) fc;
			ITable tblRel = ws.OpenTable("TOPGIS_TLM.TLM_WANDERWEG");
			var dsRel = (IDataset) tblRel;
			const string relName = "TOPGIS_TLM.TLM_WANDERWEG_STRASSE";

			var model = new SimpleModel("model", fc);
			Dataset mds1 = model.AddDataset(new ModelVectorDataset(ds1.Name));
			Dataset mdsRel = model.AddDataset(new ModelTableDataset(dsRel.Name));

			var clsDesc = new ClassDescriptor(typeof(QaRelLineGroupConstraints));
			var tstDesc = new TestDescriptor("GroupEnds", clsDesc);
			var condition = new QualityCondition("cndGroupEnds", tstDesc);
			InstanceConfigurationUtils.AddParameterValue(condition, "relationTables", mds1);
			InstanceConfigurationUtils.AddParameterValue(condition, "relationTables", mdsRel);
			InstanceConfigurationUtils.AddParameterValue(condition, "relation", relName);
			InstanceConfigurationUtils.AddParameterValue(condition, "join", JoinType.LeftJoin);
			InstanceConfigurationUtils.AddParameterValue(condition, "minGap", 10);
			InstanceConfigurationUtils.AddParameterValue(condition, "minGroupLength", 10);
			InstanceConfigurationUtils.AddParameterValue(condition, "minLeafLength", 20);
			InstanceConfigurationUtils.AddParameterValue(
				condition, "groupBy", string.Format("{0}", "TOPGIS_TLM.TLM_WANDERWEG.OBJEKTART"));
			InstanceConfigurationUtils.AddParameterValue(
				condition, "GroupCondition", "TOPGIS_TLM.TLM_WANDERWEG.OBJEKTART IS NOT NULL");
			InstanceConfigurationUtils.AddParameterValue(condition, "MinGapToOtherGroup", 10);

			var fact = new QaRelLineGroupConstraints();
			fact.Condition = condition;

			IList<ITest> tests =
				fact.CreateTests(new SimpleDatasetOpener(model.MasterDatabaseWorkspaceContext));
			Assert.AreEqual(1, tests.Count);

			var testContainer = new TestContainer { TileSize = 10000 };
			testContainer.AddTest(tests[0]);

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline, _spatialReference,
				                1000));
			fields.AddField(FieldUtils.CreateTextField(_stateIdFieldName, 100));
			fields.AddField(FieldUtils.CreateTextField(_textFieldName, 200));

			int ticks = Environment.TickCount;
			string errFcName = string.Format("err_{0}", ticks);

			_errorFc = DatasetUtils.CreateSimpleFeatureClass(
				RelTestWs, errFcName, fields);

			testContainer.QaError += testContainer_QaError;

			//testContainer.Execute(GeometryFactory.CreateEnvelope(
			//	new WKSEnvelope {XMin = 2480000, YMin = 1070000, XMax = 2900000, YMax = 1320000}));

			//testContainer.Execute(GeometryFactory.CreateEnvelope(
			//	new WKSEnvelope { XMin = 2612000, YMin = 1271000, XMax = 2612500, YMax = 1272000 }));

			int errorCount = testContainer.Execute(GeometryFactory.CreateEnvelope(
				                                       new WKSEnvelope
				                                       {
					                                       XMin = 2695110,
					                                       YMin = 1264440,
					                                       XMax = 2695120,
					                                       YMax = 1264450
				                                       }));
			Console.WriteLine(errorCount);
		}

		private IFeatureClass _errorFc;

		private void testContainer_QaError(object sender, QaErrorEventArgs e)
		{
			if (e.QaError.InvolvedRows.Count == 2 && e.QaError.InvolvedRows[0].OID == 2540815) { }

			if (_errorFc == null)
			{
				return;
			}

			IGeometry geom =
				Commons.Essentials.Assertions.Assert.NotNull(e.QaError.Geometry);
			IssueCode issueCode =
				Commons.Essentials.Assertions.Assert.NotNull(e.QaError.IssueCode);

			((IZAware) geom).ZAware = false;
			AddFeature(_errorFc, geom, issueCode.ID, e.QaError.Description);
		}

		[NotNull]
		private static IList<QaError> Run([NotNull] ITest test, double? tileSize = null,
		                                  IEnvelope testExtent = null)
		{
			Console.WriteLine(@"Tile size: {0}",
			                  tileSize == null ? "<null>" : tileSize.ToString());
			const string newLine = "\n";
			// r# unit test output adds 2 lines for Environment.NewLine
			Console.Write(newLine);

			QaTestRunnerBase runner;
			if (tileSize == null)
			{
				var testRunner = new QaTestRunner(test);
				testRunner.Execute();
				runner = testRunner;
			}
			else
			{
				var testRunner = new QaContainerTestRunner(tileSize.Value, test)
				                 {
					                 KeepGeometry = true
				                 };
				int errorCount = testExtent == null
					                 ? testRunner.Execute()
					                 : testRunner.Execute(testExtent);
				runner = testRunner;
			}

			return runner.Errors;
		}

		private static void AssertErrors(
			int expectedErrorCount,
			[NotNull] ICollection<QaError> errors,
			[NotNull] params Predicate<QaError>[] expectedErrorPredicates)
		{
			Assert.AreEqual(expectedErrorCount, errors.Count);

			var unmatched = new List<int>();

			for (var i = 0; i < expectedErrorPredicates.Length; i++)
			{
				Predicate<QaError> predicate = expectedErrorPredicates[i];

				bool matched = errors.Any(error => predicate(error));

				if (! matched)
				{
					unmatched.Add(i);
				}
			}

			if (unmatched.Count > 0)
			{
				Assert.Fail("Unmatched predicate index(es): {0}",
				            StringUtils.Concatenate(unmatched, "; "));
			}
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

		private void CreateFeatureClasses([NotNull] out IFeatureClass fcLine1,
		                                  [NotNull] out IFeatureClass fcLine2)
		{
			Thread.Sleep(100);

			// make sure that TickCount is unique for each call (increase is non-continuous)
			int ticks = Environment.TickCount;

			fcLine1 = CreateFeatureClass(string.Format("l1_{0}", ticks),
			                             esriGeometryType.esriGeometryPolyline);
			fcLine2 = CreateFeatureClass(string.Format("l2_{0}", ticks),
			                             esriGeometryType.esriGeometryPolyline);
		}

		[NotNull]
		private static IFeature AddFeature(
			[NotNull] IFeatureClass featureClass,
			[NotNull] IGeometry geometry,
			[CanBeNull] string stateId = null,
			[CanBeNull] string textFieldValue = null,
			double? doubleValue = null,
			DateTime? dateValue = null)
		{
			IFeature feature = featureClass.CreateFeature();
			feature.Shape = geometry;

			SetValues(feature, null, stateId, textFieldValue, doubleValue, dateValue);

			feature.Store();

			return feature;
		}

		[NotNull]
		private static IRow AddRow(
			[NotNull] ITable table,
			[CanBeNull] int? fkValue = null,
			[CanBeNull] string stateId = null,
			[CanBeNull] string textFieldValue = null,
			double? doubleValue = null,
			DateTime? dateValue = null)
		{
			IRow row = table.CreateRow();

			SetValues(row, fkValue, stateId, textFieldValue, doubleValue, dateValue);

			row.Store();

			return row;
		}

		private static void SetValues([NotNull] IRow row,
		                              [CanBeNull] int? fkValue = null,
		                              [CanBeNull] string stateId = null,
		                              [CanBeNull] string textFieldValue = null,
		                              double? doubleValue = null,
		                              DateTime? dateValue = null)
		{
			if (fkValue != null)
			{
				SetValue(row, _fkFieldName, fkValue);
			}

			if (stateId != null)
			{
				SetValue(row, _stateIdFieldName, stateId);
			}

			if (textFieldValue != null)
			{
				SetValue(row, _textFieldName, textFieldValue);
			}

			if (doubleValue != null)
			{
				SetValue(row, _doubleFieldName, doubleValue.Value);
			}

			if (dateValue != null)
			{
				SetValue(row, _dateFieldName, dateValue.Value);
			}
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
				_testWs, name, fields);
		}
	}
}
