using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;
using ProSuite.QA.TestFactories;
using ProSuite.QA.Tests.ParameterTypes;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaRegularExpressionTest
	{
		private IFeatureWorkspace _testWs;
		private IFeatureWorkspace _relTestWs;

		private ISpatialReference _spatialReference;

		private const double _xyTolerance = 0.001;
		private const string _textFieldName = "FLD_TEXT";
		private const string _textFieldName2 = "FLD_TEXT_2";
		private const string _fkFieldName = "FLD_FK";

		private IFeatureWorkspace RelTestWs
			=> _relTestWs ??
			   (_relTestWs =
				    TestWorkspaceUtils.CreateTestFgdbWorkspace("QaRegularExpressionTest")
			   );

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense(activateAdvancedLicense: true);

			_spatialReference = CreateLV95();
			_testWs = TestWorkspaceUtils.CreateInMemoryWorkspace(
				"QaRegularExpressionTest");
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void TestSimple()
		{
			IFeatureWorkspace initTestWs = _testWs;
			try
			{
				_testWs = TestWorkspaceUtils.CreateInMemoryWorkspace("QaRegularExpression");

				IFieldsEdit fields = new FieldsClass();
				fields.AddField(FieldUtils.CreateOIDField());
				fields.AddField(FieldUtils.CreateIntegerField(_fkFieldName));
				fields.AddField(FieldUtils.CreateTextField(_textFieldName, 200));
				fields.AddField(FieldUtils.CreateTextField(_textFieldName2, 200));

				const string tableName = "RegEx_table";
				ITable table = TestWorkspaceUtils.CreateSimpleTable(_testWs, tableName, fields);

				((IWorkspaceEdit) _testWs).StartEditing(false);

				AddRow(table, textFieldValue: "A", textFieldValue2: "X");
				AddRow(table, textFieldValue: "B", textFieldValue2: "X");
				AddRow(table, textFieldValue: "A", textFieldValue2: "X");

				((IWorkspaceEdit) _testWs).StopEditing(true);

				ITest test = new QaRegularExpression(
					ReadOnlyTableFactory.Create(table), "A", _textFieldName);

				IList<QaError> errors = Run(test, 1000);
				AssertErrors(1, errors);

				IList<InvolvedRow> involvedRows = errors[0].InvolvedRows;
				Assert.AreEqual(1, involvedRows.Count);
				Assert.AreEqual($"{_textFieldName}", errors[0].AffectedComponent);
				Assert.AreEqual("RegularExpression.FieldValueDoesNotMatchRegularExpression",
				                errors[0].IssueCode?.ID);
			}
			finally
			{
				_testWs = initTestWs;
			}
		}

		[Test]
		public void TestExcludedFields()
		{
			IFeatureWorkspace initTestWs = _testWs;
			try
			{
				_testWs = TestWorkspaceUtils.CreateInMemoryWorkspace("QaRegularExpression");

				IFieldsEdit fields = new FieldsClass();
				fields.AddField(FieldUtils.CreateOIDField());
				fields.AddField(FieldUtils.CreateIntegerField(_fkFieldName));
				fields.AddField(FieldUtils.CreateTextField(_textFieldName, 200));
				fields.AddField(FieldUtils.CreateTextField(_textFieldName2, 200));

				const string tableName = "RegEx_table_excluded";
				ITable table =
					TestWorkspaceUtils.CreateSimpleTable(_testWs, tableName, fields);

				((IWorkspaceEdit) _testWs).StartEditing(false);

				AddRow(table, textFieldValue: "A", textFieldValue2: "X");
				AddRow(table, textFieldValue: "B", textFieldValue2: "X");
				AddRow(table, textFieldValue: "A", textFieldValue2: "X");

				((IWorkspaceEdit) _testWs).StopEditing(true);

				var test = new QaRegularExpression(
					           ReadOnlyTableFactory.Create(table), "A", _textFieldName2)
				           {
					           FieldListType = FieldListType.IgnoredFields
				           };

				IList<QaError> errors = Run(test, 1000);
				AssertErrors(1, errors);

				IList<InvolvedRow> involvedRows = errors[0].InvolvedRows;
				Assert.AreEqual(1, involvedRows.Count);
				Assert.AreEqual($"{_textFieldName}", errors[0].AffectedComponent);
				Assert.AreEqual("RegularExpression.FieldValueDoesNotMatchRegularExpression",
				                errors[0].IssueCode?.ID);
			}
			finally
			{
				_testWs = initTestWs;
			}
		}

		[Test]
		public void TestRelatedFactory()
		{
			IFeatureWorkspace initTestWs = _testWs;
			try
			{
				_testWs = RelTestWs;

				IFeatureClass fc1 = CreateFeatureClass("TestRelatedFactory",
				                                       esriGeometryType.esriGeometryPolyline);

				var ds1 = (IDataset) fc1;

				IFieldsEdit fields = new FieldsClass();
				fields.AddField(FieldUtils.CreateOIDField());
				fields.AddField(FieldUtils.CreateIntegerField(_fkFieldName));
				fields.AddField(FieldUtils.CreateTextField(_textFieldName, 200));

				string tableName = ds1.Name + "_table";
				ITable table = TestWorkspaceUtils.CreateSimpleTable(_testWs, tableName, fields);

				var tableDataset = (IDataset) table;

				string relClassName = "relClass" + Math.Abs(Environment.TickCount);
				IRelationshipClass rel = TestWorkspaceUtils.CreateSimpleMNRelationship(
					_testWs, relClassName, table, (ITable) fc1, "fkGrp", "fkFc");

				((IWorkspaceEdit) _testWs).StartEditing(false);

				IFeature f = AddFeature(fc1,
				                        CurveConstruction.StartLine(0, 0)
				                                         .LineTo(4, 0)
				                                         .Curve);
				IRow r = AddRow(table, textFieldValue: "A");
				rel.CreateRelationship((IObject) r, f);
				r = AddRow(table, textFieldValue: "B");
				rel.CreateRelationship((IObject) r, f);

				f = AddFeature(fc1,
				               CurveConstruction.StartLine(4, 0)
				                                .LineTo(4, 8)
				                                .Curve);
				r = AddRow(table, textFieldValue: "A");
				rel.CreateRelationship((IObject) r, f);

				((IWorkspaceEdit) _testWs).StopEditing(true);

				var model = new SimpleModel("model", fc1);
				Dataset mds1 = model.AddDataset(new ModelVectorDataset(ds1.Name));
				Dataset mdsRel = model.AddDataset(new ModelTableDataset(tableDataset.Name));

				var clsDesc = new ClassDescriptor(typeof(QaRelRegularExpression));
				var tstDesc = new TestDescriptor("GroupEnds", clsDesc);
				var condition = new QualityCondition("cndGroupEnds", tstDesc);
				InstanceConfigurationUtils.AddParameterValue(condition, "relationTables", mds1);
				InstanceConfigurationUtils.AddParameterValue(
					condition, "relationTables", mdsRel);
				InstanceConfigurationUtils.AddParameterValue(
					condition, "relation", relClassName);
				InstanceConfigurationUtils.AddParameterValue(
					condition, "join", JoinType.InnerJoin);
				InstanceConfigurationUtils.AddParameterValue(condition, "pattern", "A");
				InstanceConfigurationUtils.AddParameterValue(condition, "fieldNames",
				                                             $"{tableName}.{_textFieldName}");
				InstanceConfigurationUtils.AddParameterValue(condition, "MatchIsError", false);
				//condition.AddParameterValue("PatternDescription", "Hallo");

				var factory = new QaRelRegularExpression { Condition = condition };

				IList<ITest> tests =
					factory.CreateTests(
						new SimpleDatasetOpener(model.GetMasterDatabaseWorkspaceContext()));
				Assert.AreEqual(1, tests.Count);

				IList<QaError> errors = Run(tests[0], 1000);
				AssertErrors(1, errors);

				IList<InvolvedRow> involvedRows = errors[0].InvolvedRows;
				Assert.AreEqual(2, involvedRows.Count);
				Assert.AreEqual($"{tableName}.{_textFieldName}", errors[0].AffectedComponent);
				Assert.AreEqual("RegularExpression.FieldValueDoesNotMatchRegularExpression",
				                errors[0].IssueCode?.ID);

				// TOP-4945: expected involved dataset name: base table name, not joined table name
				StringComparison cmp = StringComparison.InvariantCultureIgnoreCase;
				Assert.IsTrue(tableName.Equals(involvedRows[0].TableName, cmp) ||
				              tableName.Equals(involvedRows[1].TableName, cmp));
			}
			finally
			{
				_testWs = initTestWs;
			}
		}

		[NotNull]
		private IFeatureClass CreateFeatureClass([NotNull] string name,
		                                         esriGeometryType geometryType)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", geometryType, _spatialReference, 1000));

			fields.AddField(FieldUtils.CreateTextField(_textFieldName, 200));

			return DatasetUtils.CreateSimpleFeatureClass(
				_testWs, name, fields);
		}

		[NotNull]
		private static IFeature AddFeature(
			[NotNull] IFeatureClass featureClass,
			[NotNull] IGeometry geometry,
			[CanBeNull] string textFieldValue = null)
		{
			IFeature feature = featureClass.CreateFeature();
			feature.Shape = geometry;

			SetValues(feature, null, textFieldValue);

			feature.Store();

			return feature;
		}

		[NotNull]
		private static IRow AddRow(
			[NotNull] ITable table,
			[CanBeNull] int? fkValue = null,
			[CanBeNull] string textFieldValue = null,
			[CanBeNull] string textFieldValue2 = null)
		{
			IRow row = table.CreateRow();

			SetValues(row, fkValue, textFieldValue, textFieldValue2);

			row.Store();

			return row;
		}

		private static void SetValues([NotNull] IRow row,
		                              [CanBeNull] int? fkValue = null,
		                              [CanBeNull] string textFieldValue = null,
		                              [CanBeNull] string textField2Value = null)
		{
			if (fkValue != null)
			{
				SetValue(row, _fkFieldName, fkValue);
			}

			if (textFieldValue != null)
			{
				SetValue(row, _textFieldName, textFieldValue);
			}

			if (textField2Value != null)
			{
				SetValue(row, _textFieldName2, textField2Value);
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

		[NotNull]
		private static ISpatialReference CreateLV95()
		{
			ISpatialReference result = SpatialReferenceUtils.CreateSpatialReference
				((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, true);
			SpatialReferenceUtils.SetXYDomain(result, -10000, -10000, 10000, 10000,
			                                  0.0001, _xyTolerance);
			return result;
		}
	}
}
