using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Xml;
using ProSuite.DomainServices.AO.QA.Standalone.XmlBased;
using ProSuite.DomainServices.AO.QA.VerifiedDataModel;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using ProSuite.QA.Tests.Transformers;
using ProSuite.QA.Tests.Transformers.Filters;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test.Transformer
{
	public class XmlConfigTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense(activateAdvancedLicense: true);
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanRunFromXmlWithIssueFilters()
		{
			// Init
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateTestFgdbWorkspace("CustomExFactoryTest");

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

			//
			var xmlWs = new XmlWorkspace { ID = "ws", ModelName = "mn" };

			XmlClassDescriptor xmlCdContainsOther =
				new XmlClassDescriptor
				{
					AssemblyName = typeof(QaContainsOther).Assembly.GetName().Name,
					TypeName = typeof(QaContainsOther).FullName,
					ConstructorId = 1
				};
			XmlClassDescriptor xmlCdIgnore =
				new XmlClassDescriptor
				{
					AssemblyName = typeof(IgnoreErrorArea).Assembly.GetName().Name,
					TypeName = typeof(IgnoreErrorArea).FullName,
					ConstructorId = 0
				};

			XmlTestDescriptor xmlTdContainsOther =
				new XmlTestDescriptor { Name = "co", TestClass = xmlCdContainsOther };
			XmlIssueFilterDescriptor xmlIdIgnore =
				new XmlIssueFilterDescriptor { Name = "ig", IssueFilterClass = xmlCdIgnore };

			XmlQualityCondition xmlQc =
				new XmlQualityCondition
				{ Name = "qc", TestDescriptorName = xmlTdContainsOther.Name };
			xmlQc.ParameterValues.Add(
				new XmlDatasetTestParameterValue
				{ TestParameterName = "contains", Value = "areaFc", WorkspaceId = xmlWs.ID });
			xmlQc.ParameterValues.Add(
				new XmlDatasetTestParameterValue
				{ TestParameterName = "isWithin", Value = "bbFc", WorkspaceId = xmlWs.ID });

			XmlIssueFilterConfiguration xmlIssueFilter =
				new XmlIssueFilterConfiguration
				{ Name = "pp", IssueFilterDescriptorName = xmlIdIgnore.Name };
			xmlIssueFilter.ParameterValues.Add(
				new XmlDatasetTestParameterValue
				{ TestParameterName = "areaFc", Value = "ignoreFc", WorkspaceId = xmlWs.ID });

			xmlQc.Filters = new List<XmlFilter>
			                { new XmlFilter { IssueFilterName = xmlIssueFilter.Name } };

			var xmlSpec = new XmlQualitySpecification { TileSize = 10000, Name = "spec" };
			xmlSpec.Elements.Add(new XmlQualitySpecificationElement
			                     { QualityConditionName = xmlQc.Name });

			var xmlDocument = new XmlDataQualityDocument();

			xmlDocument.AddWorkspace(xmlWs);
			xmlDocument.AddQualitySpecification(xmlSpec);

			xmlDocument.AddQualityCondition(xmlQc);
			xmlDocument.AddIssueFilter(xmlIssueFilter);

			xmlDocument.AddTestDescriptor(xmlTdContainsOther);

			xmlDocument.AddIssueFilterDescriptor(xmlIdIgnore);

			var modelFactory =
				new VerifiedModelFactory(new MasterDatabaseWorkspaceContextFactory(),
				                         new SimpleVerifiedDatasetHarvester());

			var specFct = new XmlBasedQualitySpecificationFactory(
				modelFactory);

			var dataSource = new DataSource(xmlWs)
			                 {
				                 WorkspaceAsText =
					                 WorkspaceUtils.GetConnectionString((IWorkspace) ws)
			                 };

			QualitySpecification qs =
				specFct.CreateQualitySpecification(xmlDocument, xmlSpec.Name,
				                                   new[] { dataSource });

			Assert.AreEqual(1, qs.Elements.Count);
			QualityCondition qc = qs.Elements[0].QualityCondition;
			Assert.AreEqual(1, qc.IssueFilterConfigurations.Count);

			TestFactory testFct = TestFactoryUtils.CreateTestFactory(qc);
			Assert.IsNotNull(testFct);

			IList<ITest> tests = testFct.CreateTests(
				new SimpleDatasetOpener(model.GetMasterDatabaseWorkspaceContext()));

			Assert.AreEqual(1, tests.Count);
			Assert.AreEqual(1, ((QaContainsOther) tests[0]).IssueFilters?.Count);
		}

		[Test]
		public void CanRunFromXmlWithFilterTransformer()
		{
			// Init
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateTestFgdbWorkspace("CustomExFactoryTest_PreProcs");

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

			//
			var xmlWs = new XmlWorkspace { ID = "ws", ModelName = "mn" };

			XmlClassDescriptor xmlCdContainsOther =
				new XmlClassDescriptor(typeof(QaContainsOther), constructorId: 1);
			XmlClassDescriptor xmlCdIgnore =
				new XmlClassDescriptor(typeof(TrOnlyIntersectingFeatures), constructorId: 0);

			XmlTestDescriptor xmlTdContainsOther =
				new XmlTestDescriptor { Name = "co", TestClass = xmlCdContainsOther };
			XmlTransformerDescriptor xmlRdIgnore =
				new XmlTransformerDescriptor { Name = "ig", TransformerClass = xmlCdIgnore };

			XmlTransformerConfiguration xmlRowFilter =
				new XmlTransformerConfiguration
				{ Name = "filterTransformed", TransformerDescriptorName = xmlRdIgnore.Name };
			xmlRowFilter.ParameterValues.Add(
				new XmlDatasetTestParameterValue
				{
					TestParameterName = "featureClassToFilter", Value = "areaFc",
					WorkspaceId = xmlWs.ID
				});
			xmlRowFilter.ParameterValues.Add(
				new XmlDatasetTestParameterValue
				{ TestParameterName = "intersecting", Value = "ignoreFc", WorkspaceId = xmlWs.ID });

			XmlQualityCondition xmlQc =
				new XmlQualityCondition
				{ Name = "qc", TestDescriptorName = xmlTdContainsOther.Name };
			xmlQc.ParameterValues.Add(
				new XmlDatasetTestParameterValue
				{
					TestParameterName = "contains", TransformerName = "filterTransformed",
					WorkspaceId = xmlWs.ID
				});
			xmlQc.ParameterValues.Add(
				new XmlDatasetTestParameterValue
				{
					TestParameterName = "isWithin",
					Value = "bbFc",
					WorkspaceId = xmlWs.ID
				});

			var xmlSpec = new XmlQualitySpecification { TileSize = 10000, Name = "spec" };
			xmlSpec.Elements.Add(new XmlQualitySpecificationElement
			                     { QualityConditionName = xmlQc.Name });

			var xmlDocument = new XmlDataQualityDocument();

			xmlDocument.AddWorkspace(xmlWs);
			xmlDocument.AddQualitySpecification(xmlSpec);

			xmlDocument.AddQualityCondition(xmlQc);
			xmlDocument.AddTransformer(xmlRowFilter);

			xmlDocument.AddTestDescriptor(xmlTdContainsOther);
			xmlDocument.AddTransformerDescriptor(xmlRdIgnore);

			var modelFactory =
				new VerifiedModelFactory(new MasterDatabaseWorkspaceContextFactory(),
				                         new SimpleVerifiedDatasetHarvester());

			var specFct = new XmlBasedQualitySpecificationFactory(
				modelFactory);

			var dataSource = new DataSource(xmlWs)
			                 {
				                 WorkspaceAsText =
					                 WorkspaceUtils.GetConnectionString((IWorkspace) ws)
			                 };

			QualitySpecification qs =
				specFct.CreateQualitySpecification(xmlDocument, xmlSpec.Name,
				                                   new[] { dataSource });

			Assert.AreEqual(1, qs.Elements.Count);
			QualityCondition qc = qs.Elements[0].QualityCondition;
			var filterTransformer = (DatasetTestParameterValue) qc.ParameterValues[0];
			Assert.IsNotNull(filterTransformer.ValueSource);
			Assert.AreEqual(2, filterTransformer.ValueSource.ParameterValues.Count);

			TestFactory testFct = TestFactoryUtils.CreateTestFactory(qc);
			Assert.IsNotNull(testFct);

			IList<ITest> tests = testFct.CreateTests(
				new SimpleDatasetOpener(model.GetMasterDatabaseWorkspaceContext()));

			Assert.AreEqual(1, tests.Count);
		}

		[Test]
		public void CanRunFromXmlWithTableTransformer()
		{
			// Init
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateTestFgdbWorkspace("CustomExFactoryTest_TableTransformer");

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
			ModelVectorDataset areaDs = model.AddDataset(
				new ModelVectorDataset(DatasetUtils.GetName(borderFc)));

			//
			var xmlWs = new XmlWorkspace { ID = "ws", ModelName = "mn" };

			XmlClassDescriptor xmlCdContainsOther =
				new XmlClassDescriptor
				{
					AssemblyName = typeof(QaContainsOther).Assembly.GetName().Name,
					TypeName = typeof(QaContainsOther).FullName,
					ConstructorId = 1
				};
			XmlClassDescriptor xmlBordTransform =
				new XmlClassDescriptor
				{
					AssemblyName = typeof(TrLineToPolygon).Assembly.GetName().Name,
					TypeName = typeof(TrLineToPolygon).FullName,
					ConstructorId = 0
				};

			XmlTestDescriptor xmlTdContainsOther =
				new XmlTestDescriptor { Name = "co", TestClass = xmlCdContainsOther };
			XmlTransformerDescriptor xmlTdTrans =
				new XmlTransformerDescriptor { Name = "bt", TransformerClass = xmlBordTransform };

			XmlTransformerConfiguration xmlTrans =
				new XmlTransformerConfiguration
				{ Name = "bo", TransformerDescriptorName = xmlTdTrans.Name };
			xmlTrans.ParameterValues.Add(
				new XmlDatasetTestParameterValue
				{ TestParameterName = "closedLineClass", Value = "borderFc", WorkspaceId = xmlWs.ID });

			XmlQualityCondition xmlQc =
				new XmlQualityCondition
				{ Name = "qc", TestDescriptorName = xmlTdContainsOther.Name };
			xmlQc.ParameterValues.Add(
				new XmlDatasetTestParameterValue
				{ TestParameterName = "contains", TransformerName = xmlTrans.Name });
			xmlQc.ParameterValues.Add(
				new XmlDatasetTestParameterValue
				{ TestParameterName = "isWithin", Value = "bbFc", WorkspaceId = xmlWs.ID });

			var xmlSpec = new XmlQualitySpecification { TileSize = 10000, Name = "spec" };
			xmlSpec.Elements.Add(new XmlQualitySpecificationElement
			                     { QualityConditionName = xmlQc.Name });

			var xmlDocument = new XmlDataQualityDocument();

			xmlDocument.AddWorkspace(xmlWs);
			xmlDocument.AddQualitySpecification(xmlSpec);

			xmlDocument.AddQualityCondition(xmlQc);
			xmlDocument.AddTransformer(xmlTrans);

			xmlDocument.AddTestDescriptor(xmlTdContainsOther);
			xmlDocument.AddTransformerDescriptor(xmlTdTrans);

			var modelFactory =
				new VerifiedModelFactory(new MasterDatabaseWorkspaceContextFactory(),
				                         new SimpleVerifiedDatasetHarvester());

			var specFct = new XmlBasedQualitySpecificationFactory(
				modelFactory);

			var dataSource = new DataSource(xmlWs)
			                 {
				                 WorkspaceAsText =
					                 WorkspaceUtils.GetConnectionString((IWorkspace) ws)
			                 };

			QualitySpecification qs =
				specFct.CreateQualitySpecification(xmlDocument, xmlSpec.Name,
				                                   new[] { dataSource });

			Assert.AreEqual(1, qs.Elements.Count);
			QualityCondition qc = qs.Elements[0].QualityCondition;
			Assert.IsNotNull(qc.ParameterValues[0].ValueSource);

			TestFactory testFct = TestFactoryUtils.CreateTestFactory(qc);
			Assert.IsNotNull(testFct);

			IList<ITest> tests = testFct.CreateTests(
				new SimpleDatasetOpener(model.GetMasterDatabaseWorkspaceContext()));

			Assert.AreEqual(1, tests.Count);
			Assert.IsTrue(
				((QaContainsOther) tests[0]).InvolvedTables[0].GetType().FullName ==
				typeof(TrGeometryTransform).FullName + "+ShapeTransformedFc");
		}

		[Test]
		[Ignore("eqTrans is a non IReadOnlyTable-Transformer. Reactivate test when that's supported")]
		public void CanRunFromXmlWithTableJoin()
		{
			// Init
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateTestFgdbWorkspace("CustomExFactoryTest_TableJoin");

			ISpatialReference sref = SpatialReferenceUtils.CreateSpatialReference(
				(int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, true);

			IFeatureClass lineFc = DatasetUtils.CreateSimpleFeatureClass(
				ws, "lineFc", null, FieldUtils.CreateOIDField(),
				FieldUtils.CreateIntegerField("fkRoute"),
				FieldUtils.CreateShapeField("SHAPE", esriGeometryType.esriGeometryPolyline, sref,
				                            1000));
			ITable tblRoute = DatasetUtils.CreateTable(
				ws, "routeTbl", FieldUtils.CreateOIDField(),
				FieldUtils.CreateIntegerField("id"), FieldUtils.CreateTextField("name", 20));
			string relName = "relRouteLine";
			TestWorkspaceUtils.CreateSimple1NRelationship(ws, relName, tblRoute, (ITable) lineFc,
			                                              "id", "fkRoute");

			{
				IFeature b = lineFc.CreateFeature();
				b.Shape = CurveConstruction.StartLine(10, 10).LineTo(10, 20).Curve;
				b.Value[1] = 1;
				b.Store();
			}
			{
				IFeature b = lineFc.CreateFeature();
				b.Shape = CurveConstruction.StartLine(20, 10).LineTo(20, 20).Curve;
				b.Value[1] = 2;
				b.Store();
			}
			{
				IFeature b = lineFc.CreateFeature();
				b.Shape = CurveConstruction.StartLine(20, 20).LineTo(20, 10).Curve;
				b.Value[1] = 3;
				b.Store();
			}
			{
				IRow b = tblRoute.CreateRow();
				b.Value[1] = 1;
				b.Value[2] = "A";
				b.Store();
			}
			{
				IRow b = tblRoute.CreateRow();
				b.Value[1] = 2;
				b.Value[2] = "B";
				b.Store();
			}
			{
				IRow b = tblRoute.CreateRow();
				b.Value[1] = 3;
				b.Value[2] = "B";
				b.Store();
			}

			var model = new SimpleModel("model", lineFc);
			ModelVectorDataset bbDs = model.AddDataset(
				new ModelVectorDataset(DatasetUtils.GetName(lineFc)));
			ModelTableDataset areaDs = model.AddDataset(
				new ModelTableDataset(DatasetUtils.GetName(tblRoute)));

			//
			var xmlWs = new XmlWorkspace { ID = "ws", ModelName = "mn" };

			XmlClassDescriptor xmlCdUnique =
				new XmlClassDescriptor
				{
					AssemblyName = typeof(QaUnique).Assembly.GetName().Name,
					TypeName = typeof(QaUnique).FullName,
					ConstructorId = 0
				};
			XmlClassDescriptor xmlCdJoin =
				new XmlClassDescriptor
				{
					AssemblyName = typeof(TableJoin).Assembly.GetName().Name,
					TypeName = typeof(TableJoin).FullName,
					ConstructorId = 0
				};
			XmlClassDescriptor xmlCdEqual =
				new XmlClassDescriptor
				{
					AssemblyName = typeof(EqualValue).Assembly.GetName().Name,
					TypeName = typeof(EqualValue).FullName,
					ConstructorId = 0
				};

			XmlTestDescriptor xmlTdUnique =
				new XmlTestDescriptor { Name = "co", TestClass = xmlCdUnique };
			XmlTransformerDescriptor xmlTdJoin =
				new XmlTransformerDescriptor { Name = "jn", TransformerClass = xmlCdJoin };
			XmlTransformerDescriptor xmlTdEqual =
				new XmlTransformerDescriptor { Name = "eq", TransformerClass = xmlCdEqual };

			XmlTransformerConfiguration eqTrans =
				new XmlTransformerConfiguration
				{ Name = "et", TransformerDescriptorName = xmlTdEqual.Name };
			eqTrans.ParameterValues.Add(
				new XmlScalarTestParameterValue
				{ TestParameterName = "value", Value = "routeTbl.name" }
			);
			XmlTransformerConfiguration xmlTrans =
				new XmlTransformerConfiguration
				{ Name = "bo", TransformerDescriptorName = xmlTdJoin.Name };
			xmlTrans.ParameterValues.Add(
				new XmlDatasetTestParameterValue
				{ TestParameterName = "t0", Value = "lineFc", WorkspaceId = xmlWs.ID });
			xmlTrans.ParameterValues.Add(
				new XmlDatasetTestParameterValue
				{ TestParameterName = "t1", Value = "routeTbl", WorkspaceId = xmlWs.ID });
			xmlTrans.ParameterValues.Add(
				new XmlScalarTestParameterValue
				{ TestParameterName = "relationName", Value = relName });
			xmlTrans.ParameterValues.Add(
				new XmlScalarTestParameterValue
				{ TestParameterName = "joinType", Value = nameof(JoinType.InnerJoin) });

			XmlQualityCondition xmlQc =
				new XmlQualityCondition { Name = "qc", TestDescriptorName = xmlTdUnique.Name };
			xmlQc.ParameterValues.Add(
				new XmlDatasetTestParameterValue
				{ TestParameterName = "table", TransformerName = xmlTrans.Name });
			xmlQc.ParameterValues.Add(
				new XmlScalarTestParameterValue
				{
					TestParameterName = "unique", /*Value = "routeTbl.name",*/
					TransformerName = eqTrans.Name
				});

			var xmlSpec = new XmlQualitySpecification { TileSize = 10000, Name = "spec" };
			xmlSpec.Elements.Add(new XmlQualitySpecificationElement
			                     { QualityConditionName = xmlQc.Name });

			var xmlDocument = new XmlDataQualityDocument();

			xmlDocument.AddWorkspace(xmlWs);
			xmlDocument.AddQualitySpecification(xmlSpec);

			xmlDocument.AddQualityCondition(xmlQc);
			xmlDocument.AddTransformer(xmlTrans);
			xmlDocument.AddTransformer(eqTrans);

			xmlDocument.AddTestDescriptor(xmlTdUnique);
			xmlDocument.AddTransformerDescriptor(xmlTdJoin);
			xmlDocument.AddTransformerDescriptor(xmlTdEqual);

			var modelFactory =
				new VerifiedModelFactory(new MasterDatabaseWorkspaceContextFactory(),
				                         new SimpleVerifiedDatasetHarvester());

			var specFct = new XmlBasedQualitySpecificationFactory(
				modelFactory);

			var dataSource = new DataSource(xmlWs)
			                 {
				                 WorkspaceAsText =
					                 WorkspaceUtils.GetConnectionString((IWorkspace) ws)
			                 };

			QualitySpecification qs =
				specFct.CreateQualitySpecification(xmlDocument, xmlSpec.Name,
				                                   new[] { dataSource });

			Assert.AreEqual(1, qs.Elements.Count);
			QualityCondition qc = qs.Elements[0].QualityCondition;
			Assert.IsNotNull(qc.ParameterValues[0].ValueSource);

			TestFactory testFct = TestFactoryUtils.CreateTestFactory(qc);
			Assert.IsNotNull(testFct);

			IList<ITest> tests = testFct.CreateTests(
				new SimpleDatasetOpener(model.GetMasterDatabaseWorkspaceContext()));

			Assert.AreEqual(1, tests.Count);

			QaContainerTestRunner runner = new QaContainerTestRunner(1000, tests[0]);
			runner.Execute();

			Assert.AreEqual(1, runner.Errors.Count);
		}

		private class IgnoreProcessArea : RowFilter
		{
			private IList<IFeatureClassFilter> _spatialFilters;
			private IList<QueryFilterHelper> _filterHelpers;

			public IgnoreProcessArea(IReadOnlyFeatureClass areaFc)
				: base(CastToTables(areaFc)) { }

			private void EnsureFilters()
			{
				if (_spatialFilters == null)
				{
					CopyFilters(out _spatialFilters, out _filterHelpers);
				}
			}

			public override bool VerifyExecute(IReadOnlyRow row)
			{
				if (! (row is IFeature f))
				{
					return true;
				}

				EnsureFilters();

				IReadOnlyTable table = InvolvedTables[0];
				IFeatureClassFilter filter = _spatialFilters[0];
				QueryFilterHelper helper = _filterHelpers[0];
				IGeometry ignoreGeom = f.Shape;
				filter.FilterGeometry = ignoreGeom;
				foreach (var searched in Search(table, filter, helper))
				{
					if (! ((IRelationalOperator) ignoreGeom).Disjoint(((IFeature) searched).Shape))
					{
						return false;
					}
				}

				return true;
			}
		}

		private class TableJoin : ITableTransformer<IReadOnlyTable>
		{
			private readonly IReadOnlyTable _t0;
			private readonly IReadOnlyTable _t1;
			private readonly string _relationName;
			private readonly JoinType _joinType;
			private readonly List<IReadOnlyTable> _involved;

			private IReadOnlyTable _joined;

			public TableJoin(IReadOnlyTable t0, IReadOnlyTable t1,
			                 string relationName, JoinType joinType)
			{
				_t0 = t0;
				_t1 = t1;
				_relationName = relationName;
				_joinType = joinType;
				_involved = new List<IReadOnlyTable> { t0, t1 };
			}

			IList<IReadOnlyTable> IInvolvesTables.InvolvedTables => _involved;
			string ITableTransformer.TransformerName { get; set; }

			public IReadOnlyTable GetTransformed()
			{
				if (_joined == null)
				{
					if (! (_t0 is ReadOnlyTable r0))
						throw new InvalidOperationException($"Invalid t0 {_t0.Name}");
					if (! (_t1 is ReadOnlyTable r1))
						throw new InvalidOperationException($"Invalid t1 {_t1.Name}");
					IRelationshipClass relClass =
						((IFeatureWorkspace) _t0.Workspace).OpenRelationshipClass(_relationName);
					ITable joined =
						RelationshipClassUtils.GetQueryTable(
							relClass, new[] { r0.BaseTable, r1.BaseTable }, _joinType,
							whereClause: null);
					_joined = ReadOnlyTableFactory.Create(joined);
				}

				return _joined;
			}

			object ITableTransformer.GetTransformed() => GetTransformed();

			void IInvolvesTables.SetConstraint(int tableIndex, string condition)
			{
				// TODO
			}

			void IInvolvesTables.SetSqlCaseSensitivity(int tableIndex, bool useCaseSensitiveQaSql)
			{
				//TODO
			}
		}

		private class EqualValue : ITableTransformer<object>
		{
			private readonly object _value;
			private readonly List<IReadOnlyTable> _empty;

			public EqualValue(string value)
			{
				_value = value;
				_empty = new List<IReadOnlyTable>();
			}

			IList<IReadOnlyTable> IInvolvesTables.InvolvedTables => _empty;

			public object GetTransformed() => _value;

			string ITableTransformer.TransformerName { get; set; }

			void IInvolvesTables.SetConstraint(int tableIndex, string condition) { }

			void IInvolvesTables.
				SetSqlCaseSensitivity(int tableIndex, bool useCaseSensitiveQaSql) { }
		}
	}
}
