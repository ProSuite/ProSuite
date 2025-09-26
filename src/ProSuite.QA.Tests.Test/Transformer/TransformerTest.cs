using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using ProSuite.QA.Tests.Transformers;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test.Transformer
{
	[TestFixture]
	public class TransformerTest
	{
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

		[Test]
		public void CanConfigureWithIssueFilters()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace("QaMeasuresFactoryTest");

			ISpatialReference sref = SpatialReferenceUtils.CreateSpatialReference(
				(int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, true);

			IFeatureClass areaFc = DatasetUtils.CreateSimpleFeatureClass(
				ws, "areaFc", null, FieldUtils.CreateOIDField(),
				FieldUtils.CreateShapeField("SHAPE", esriGeometryType.esriGeometryPolygon, sref,
				                            1000));
			IFeatureClass bbFc = DatasetUtils.CreateSimpleFeatureClass(
				ws, "bbFc", null, FieldUtils.CreateOIDField(),
				FieldUtils.CreateShapeField("SHAPE", esriGeometryType.esriGeometryPolygon, sref,
				                            1000));

			IFeatureClass ignoreFc = DatasetUtils.CreateSimpleFeatureClass(
				ws, "ignoreFc", null, FieldUtils.CreateOIDField(),
				FieldUtils.CreateShapeField("SHAPE", esriGeometryType.esriGeometryPolygon, sref,
				                            1000));

			{
				IFeature b = areaFc.CreateFeature();
				b.Shape = CurveConstruction
				          .StartPoly(10, 10).LineTo(10, 20).LineTo(20, 20).LineTo(20, 10)
				          .LineTo(10, 10).ClosePolygon();
				b.Store();
			}
			{
				// not within
				IFeature b = bbFc.CreateFeature();
				b.Shape = CurveConstruction
				          .StartPoly(9, 9).LineTo(10, 20).LineTo(20, 20).LineTo(20, 10)
				          .LineTo(9, 9).ClosePolygon();
				b.Store();
			}
			{
				// within
				IFeature b = bbFc.CreateFeature();
				b.Shape = CurveConstruction
				          .StartPoly(11, 11).LineTo(10, 20).LineTo(20, 20).LineTo(20, 10)
				          .LineTo(11, 11).ClosePolygon();
				b.Store();
			}

			var model = new SimpleModel("model", areaFc);
			ModelVectorDataset bbDs = model.AddDataset(
				new ModelVectorDataset(DatasetUtils.GetName(bbFc)));
			ModelVectorDataset areaDs = model.AddDataset(
				new ModelVectorDataset(DatasetUtils.GetName(areaFc)));
			ModelVectorDataset ignoreDs = model.AddDataset(
				new ModelVectorDataset(DatasetUtils.GetName(ignoreFc)));

			IssueFilterDescriptor postDescriptor =
				new IssueFilterDescriptor("pt", new ClassDescriptor(typeof(IgnoreErrorArea)), 0);
			IssueFilterConfiguration
				issueFilter =
					new IssueFilterConfiguration(
						"pt", postDescriptor); // TODO: rename QualityCondition?
			TestParameterValueUtils.AddParameterValue(issueFilter, "areaFc", ignoreDs);

			TestDescriptor td =
				new TestDescriptor("td", new ClassDescriptor(typeof(QaContainsOther)), 1);
			QualityCondition condition = new QualityCondition("qc", td);
			TestParameterValueUtils.AddParameterValue(condition, "contains", areaDs);
			TestParameterValueUtils.AddParameterValue(condition, "isWithin", bbDs);

			condition.AddIssueFilterConfiguration(issueFilter);

			TestFactory factory = TestFactoryUtils.CreateTestFactory(condition);
			Assert.IsNotNull(factory);

			IList<ITest> tests = factory.CreateTests(
				new SimpleDatasetOpener(model.GetMasterDatabaseWorkspaceContext()));

			QaContainerTestRunner runner = new QaContainerTestRunner(1000, tests[0]);
			runner.Execute();

			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void CanConfigureWithTableTransformer()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace("QaMeasuresFactoryTest");

			ISpatialReference sref = SpatialReferenceUtils.CreateSpatialReference(
				(int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, true);

			IFeatureClass borderFc = DatasetUtils.CreateSimpleFeatureClass(
				ws, "borderFc", null, FieldUtils.CreateOIDField(),
				FieldUtils.CreateShapeField("SHAPE", esriGeometryType.esriGeometryPolyline, sref,
				                            1000));
			IFeatureClass bbFc = DatasetUtils.CreateSimpleFeatureClass(
				ws, "bbFc", null, FieldUtils.CreateOIDField(),
				FieldUtils.CreateShapeField("SHAPE", esriGeometryType.esriGeometryPolygon, sref,
				                            1000));

			{
				IFeature b = borderFc.CreateFeature();
				b.Shape = CurveConstruction
				          .StartLine(10, 10).LineTo(10, 20).LineTo(20, 20).LineTo(20, 10)
				          .LineTo(10, 10).Curve;
				b.Store();
			}
			{
				// not within
				IFeature b = bbFc.CreateFeature();
				b.Shape = CurveConstruction
				          .StartPoly(9, 9).LineTo(10, 20).LineTo(20, 20).LineTo(20, 10)
				          .LineTo(9, 9).ClosePolygon();
				b.Store();
			}
			{
				// within
				IFeature b = bbFc.CreateFeature();
				b.Shape = CurveConstruction
				          .StartPoly(11, 11).LineTo(10, 20).LineTo(20, 20).LineTo(20, 10)
				          .LineTo(11, 11).ClosePolygon();
				b.Store();
			}

			var model = new SimpleModel("model", borderFc);
			ModelVectorDataset bbDs = model.AddDataset(
				new ModelVectorDataset(DatasetUtils.GetName(bbFc)));
			ModelVectorDataset borderDs = model.AddDataset(
				new ModelVectorDataset(DatasetUtils.GetName(borderFc)));

			TransformerDescriptor transformDescriptor =
				new TransformerDescriptor("bt", new ClassDescriptor(typeof(TrLineToPolygon)), 0);
			TransformerConfiguration transformerConfig =
				new TransformerConfiguration(
					"bt", transformDescriptor); // TODO: rename QualityCondition?

			TransformerFactory transformerFactory =
				InstanceFactoryUtils.CreateTransformerFactory(transformerConfig);
			Assert.NotNull(transformerFactory);

			TestParameterValueUtils.AddParameterValue(transformerConfig, "closedLineClass", borderDs);

			TestDescriptor td =
				new TestDescriptor("td", new ClassDescriptor(typeof(QaContainsOther)), 1);
			QualityCondition condition = new QualityCondition("qc", td);
			TestParameterValue containsParam =
				TestParameterValueUtils.AddParameterValue(condition, "contains", (Dataset) null);
			containsParam.ValueSource = transformerConfig;
			TestParameterValueUtils.AddParameterValue(condition, "isWithin", bbDs);

			TestFactory factory = TestFactoryUtils.CreateTestFactory(condition);
			Assert.IsNotNull(factory);

			IList<ITest> tests = factory.CreateTests(
				new SimpleDatasetOpener(model.GetMasterDatabaseWorkspaceContext()));

			QaContainerTestRunner runner = new QaContainerTestRunner(1000, tests[0]);
			runner.Execute();

			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void CanRunWithTableTransformer()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace("QaMeasuresFactoryTest");

			ISpatialReference sref = SpatialReferenceUtils.CreateSpatialReference(
				(int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, true);

			IFeatureClass borderFc = DatasetUtils.CreateSimpleFeatureClass(
				ws, "borderFc", null, FieldUtils.CreateOIDField(),
				FieldUtils.CreateShapeField("SHAPE", esriGeometryType.esriGeometryPolyline, sref,
				                            1000));
			IFeatureClass bbFc = DatasetUtils.CreateSimpleFeatureClass(
				ws, "bbFc", null, FieldUtils.CreateOIDField(),
				FieldUtils.CreateShapeField("SHAPE", esriGeometryType.esriGeometryPolygon, sref,
				                            1000));

			{
				IFeature b = borderFc.CreateFeature();
				b.Shape = CurveConstruction
				          .StartLine(10, 10).LineTo(10, 20).LineTo(20, 20).LineTo(20, 10)
				          .LineTo(10, 10).Curve;
				b.Store();
			}
			{
				// not within
				IFeature b = bbFc.CreateFeature();
				b.Shape = CurveConstruction
				          .StartPoly(9, 9).LineTo(10, 20).LineTo(20, 20).LineTo(20, 10)
				          .LineTo(9, 9).ClosePolygon();
				b.Store();
			}
			{
				// within
				IFeature b = bbFc.CreateFeature();
				b.Shape = CurveConstruction
				          .StartPoly(11, 11).LineTo(10, 20).LineTo(20, 20).LineTo(20, 10)
				          .LineTo(11, 11).ClosePolygon();
				b.Store();
			}

			var transformer = new TrLineToPolygon(ReadOnlyTableFactory.Create(borderFc));
			var polyFc = transformer.GetTransformed();
			var test = new QaContainsOther(
				polyFc, ReadOnlyTableFactory.Create(bbFc));
			var ctr = new QaContainerTestRunner(10000, test);
			ctr.Execute();
		}
	}
}
