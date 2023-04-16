using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using NUnit.Framework;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Testing;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Xml;
using ProSuite.DomainServices.AO.QA.Standalone.XmlBased;
using ProSuite.DomainServices.AO.QA.VerifiedDataModel;

namespace ProSuite.DomainServices.AO.Test.QA.Standalone.XmlBased
{
	[TestFixture]
	public class XmlBasedQualitySpecificationFactoryTest
	{
		private XmlWorkspace _xmlWorkspace;
		private XmlTestDescriptor _xmlTestDescriptorSimpleGeometry;
		private XmlTestDescriptor _xmlTestDescriptorMinArea;
		private XmlTransformerDescriptor _xmlTransformerDescriptor;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			Commons.Test.Testing.TestUtils.ConfigureUnitTestLogging();
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[SetUp]
		public void SetUp()
		{
			_xmlWorkspace = new XmlWorkspace { ID = "ws", ModelName = "mn" };

			_xmlTestDescriptorSimpleGeometry =
				new XmlTestDescriptor
				{
					Name = "qaSimpleGeometry(0)",
					TestClass = new XmlClassDescriptor
					            {
						            TypeName = "ProSuite.QA.Tests.QaSimpleGeometry",
						            AssemblyName = "ProSuite.QA.Tests",
						            ConstructorId = 0
					            }
				};

			_xmlTestDescriptorMinArea =
				new XmlTestDescriptor
				{
					Name = "QaMinArea(1)",
					TestClass = new XmlClassDescriptor
					            {
						            TypeName = "EsriDE.ProSuite.QA.Tests.QaMinArea",
						            AssemblyName = "EsriDE.ProSuite.QA.Tests",
						            ConstructorId = 1
					            }
				};

			_xmlTransformerDescriptor =
				new XmlTransformerDescriptor
				{
					Name = "TrMultipolygonToPolygon(0)",
					TransformerClass = new XmlClassDescriptor
					                   {
						                   TypeName =
							                   "ProSuite.QA.Tests.Transformers.TrMultipolygonToPolygon",
						                   AssemblyName = "ProSuite.QA.Tests",
						                   ConstructorId = 0
					                   }
				};
		}

		[Test]
		public void CanCreateQualitySpecificationForCultureCH()
		{
			CultureInfo origCulture = Thread.CurrentThread.CurrentCulture;

			Thread.CurrentThread.CurrentCulture = new CultureInfo("de-CH");

			try
			{
				Assert.AreEqual("12.34", string.Format("{0}", 12.34));

				CanCreateQualitySpecificationCore();
			}
			finally
			{
				Thread.CurrentThread.CurrentCulture = origCulture;
			}
		}

		[Test]
		public void CanCreateQualitySpecificationForCultureDE()
		{
			CultureInfo origCulture = Thread.CurrentThread.CurrentCulture;

			Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");

			try
			{
				Assert.AreEqual("12,34", string.Format("{0}", 12.34));

				CanCreateQualitySpecificationCore();
			}
			finally
			{
				Thread.CurrentThread.CurrentCulture = origCulture;
			}
		}

		[Test]
		public void CanCreateQualitySpecificationIgnoringUnknownDatasets()
		{
			string catalogPath = TestDataPreparer.ExtractZip("QATestData.gdb.zip", @"QA\TestData")
			                                     .GetPath();

			var xmlQCon = new XmlQualityCondition
			              {
				              Name = "Simple",
				              TestDescriptorName = _xmlTestDescriptorSimpleGeometry.Name
			              };

			xmlQCon.ParameterValues.Add(new XmlDatasetTestParameterValue
			                            {
				                            TestParameterName = "featureClass",
				                            Value = "UNKNOWN",
				                            WorkspaceId = _xmlWorkspace.ID
			                            });

			var xmlQSpec = new XmlQualitySpecification { Name = "qspec" };
			xmlQSpec.Elements.Add(new XmlQualitySpecificationElement
			                      { QualityConditionName = xmlQCon.Name });

			var xmlDocument = new XmlDataQualityDocument();

			xmlDocument.AddQualitySpecification(xmlQSpec);
			xmlDocument.AddQualityCondition(xmlQCon);
			xmlDocument.AddTestDescriptor(_xmlTestDescriptorSimpleGeometry);
			xmlDocument.AddWorkspace(_xmlWorkspace);

			var modelFactory =
				new VerifiedModelFactory(new MasterDatabaseWorkspaceContextFactory(),
				                         new SimpleVerifiedDatasetHarvester());

			var factory = new XmlBasedQualitySpecificationFactory(modelFactory);

			var dataSource = new DataSource(_xmlWorkspace) { WorkspaceAsText = catalogPath };

			const bool ignoreConditionsForUnknownDatasets = true;

			QualitySpecification qualitySpecification =
				factory.CreateQualitySpecification(xmlDocument, xmlQSpec.Name,
				                                   new[] { dataSource },
				                                   ignoreConditionsForUnknownDatasets);

			Assert.AreEqual(xmlQSpec.Name, qualitySpecification.Name);
			Assert.AreEqual(0, qualitySpecification.Elements.Count);
		}

		[Test]
		public void CanCreateEmptyQualitySpecification()
		{
			var xmlQSpec = new XmlQualitySpecification { Name = "Empty" };

			var xmlDocument = new XmlDataQualityDocument();
			xmlDocument.AddQualitySpecification(xmlQSpec);

			var modelFactory =
				new VerifiedModelFactory(new MasterDatabaseWorkspaceContextFactory(),
				                         new SimpleVerifiedDatasetHarvester());

			var factory = new XmlBasedQualitySpecificationFactory(modelFactory);

			QualitySpecification qualitySpecification =
				factory.CreateQualitySpecification(xmlDocument, xmlQSpec.Name,
				                                   new DataSource[] { });

			Assert.AreEqual(xmlQSpec.Name, qualitySpecification.Name);
		}

		[Test]
		[Ignore("Uses local data")]
		public void CanReadQualitySpecifications()
		{
			XmlDataQualityDocument xmlDocument;
			IList<XmlQualitySpecification> qualitySpecifications;
			using (StreamReader xmlReader =
			       new StreamReader(@"c:\temp\QaConfigWithEmptyDatasetsParameters.xml"))
			{
				xmlDocument =
					XmlDataQualityUtils.ReadXmlDocument(xmlReader, out qualitySpecifications);
			}

			var modelFactory =
				new VerifiedModelFactory(new MasterDatabaseWorkspaceContextFactory(),
				                         new SimpleVerifiedDatasetHarvester());

			var factory = new XmlBasedQualitySpecificationFactory(modelFactory);

			const string ws = @"c:\temp\user@topgist.sde";
			DataSource[] dataSources =
			{
				new DataSource("TLM_QualityAssurance", "TLM_QualityAssurance")
				{ WorkspaceAsText = ws },
				new DataSource("PRODAS", "PRODAS") { WorkspaceAsText = ws }
			};

			foreach (XmlQualitySpecification spec in qualitySpecifications)
			{
				var qualitySpecification = factory.CreateQualitySpecification(
					xmlDocument, spec.Name ?? string.Empty,
					dataSources, ignoreConditionsForUnknownDatasets: true);

				Assert.NotNull(qualitySpecification);
			}
		}

		private void CanCreateQualitySpecificationCore()
		{
			string catalogPath = TestDataPreparer.ExtractZip("QATestData.gdb.zip", @"QA\TestData")
			                                     .GetPath();

			var xmlCategory = new XmlDataQualityCategory { Name = "Category A" };
			var xmlSubCategory = new XmlDataQualityCategory { Name = "Category A.1" };
			var xmlSubSubCategory = new XmlDataQualityCategory { Name = "Category A.1.1" };
			var xmlSubSubCategory2 = new XmlDataQualityCategory { Name = "Category A.1.2" };

			xmlCategory.AddSubCategory(xmlSubCategory);
			xmlSubCategory.AddSubCategory(xmlSubSubCategory);
			xmlSubCategory.AddSubCategory(xmlSubSubCategory2);

			var xmlTrans = new XmlTransformerConfiguration
			               {
				               Name = "TrMultipolygonToPolygon",
				               TransformerDescriptorName = _xmlTransformerDescriptor.Name
			               };
			xmlTrans.ParameterValues.Add(new XmlDatasetTestParameterValue
			                             {
				                             TestParameterName = "featureClass",
				                             Value = "polygons",
				                             WorkspaceId = _xmlWorkspace.ID
			                             });

			var xmlQCon = new XmlQualityCondition
			              {
				              Name = "MinArea",
				              TestDescriptorName = _xmlTestDescriptorMinArea.Name
			              };

			xmlQCon.ParameterValues.Add(new XmlDatasetTestParameterValue
			                            {
				                            TestParameterName = "polygonClass",
				                            TransformerName = xmlTrans.Name
			                            });
			xmlQCon.ParameterValues.Add(new XmlScalarTestParameterValue
			                            {
				                            TestParameterName = "limit",
				                            Value = "12.34"
			                            });

			var xmlQSpec = new XmlQualitySpecification { Name = "qspec" };
			xmlQSpec.Elements.Add(new XmlQualitySpecificationElement
			                      { QualityConditionName = xmlQCon.Name });

			xmlSubCategory.AddQualitySpecification(xmlQSpec);
			xmlSubSubCategory.AddQualityCondition(xmlQCon);
			xmlSubSubCategory.AddTransformer(xmlTrans);

			var xmlDocument = new XmlDataQualityDocument();

			//add top-level elements to doc (xmlQSpec, etc. are contained in xmlCategory)
			xmlDocument.AddCategory(xmlCategory);
			xmlDocument.AddWorkspace(_xmlWorkspace);
			xmlDocument.AddTestDescriptor(_xmlTestDescriptorMinArea);
			xmlDocument.AddTransformerDescriptor(_xmlTransformerDescriptor);

			var modelFactory =
				new VerifiedModelFactory(new MasterDatabaseWorkspaceContextFactory(),
				                         new SimpleVerifiedDatasetHarvester());

			var factory = new XmlBasedQualitySpecificationFactory(modelFactory);

			var dataSource = new DataSource(_xmlWorkspace) { WorkspaceAsText = catalogPath };

			QualitySpecification qualitySpecification =
				factory.CreateQualitySpecification(xmlDocument, xmlQSpec.Name,
				                                   new[] { dataSource });

			Assert.NotNull(qualitySpecification.Category);
			Assert.AreEqual(xmlQSpec.Name, qualitySpecification.Name);

			if (qualitySpecification.Category != null)
			{
				Assert.AreEqual(xmlSubCategory.Name, qualitySpecification.Category.Name);
				Assert.NotNull(qualitySpecification.Category.ParentCategory);
				if (qualitySpecification.Category.ParentCategory != null)
				{
					Assert.AreEqual(xmlCategory.Name,
					                qualitySpecification.Category.ParentCategory.Name);
				}

				Assert.AreEqual(2, qualitySpecification.Category.SubCategories.Count);
			}

			Assert.AreEqual(1, qualitySpecification.Elements.Count);

			QualityCondition qualityCondition = qualitySpecification.Elements[0].QualityCondition;
			Assert.AreEqual(xmlQCon.Name, qualityCondition.Name);
			Assert.NotNull(qualityCondition.Category);
			if (qualityCondition.Category != null)
			{
				Assert.AreEqual(xmlSubSubCategory.Name, qualityCondition.Category.Name);
				Assert.NotNull(qualityCondition.Category.ParentCategory);
				if (qualityCondition.Category.ParentCategory != null)
				{
					Assert.AreEqual(xmlSubCategory.Name,
					                qualityCondition.Category.ParentCategory.Name);
				}
			}

			Assert.AreEqual(_xmlTestDescriptorMinArea.Name, qualityCondition.TestDescriptor.Name);

			var scalarValue =
				(ScalarTestParameterValue) qualityCondition.GetParameterValues("limit")[0];
			Assert.AreEqual(12.34, scalarValue.GetValue(typeof(double)));

			var transValue =
				(DatasetTestParameterValue) qualityCondition.GetParameterValues("polygonClass")[0];

			TransformerConfiguration transformerConfiguration = transValue.ValueSource;
			Assert.NotNull(transformerConfiguration);
			Assert.AreEqual(xmlTrans.Name, transformerConfiguration.Name);
			Assert.NotNull(transformerConfiguration.Category);
			if (transformerConfiguration.Category != null)
			{
				Assert.AreEqual(xmlSubSubCategory.Name, transformerConfiguration.Category.Name);
				Assert.NotNull(transformerConfiguration.Category.ParentCategory);
				if (transformerConfiguration.Category.ParentCategory != null)
				{
					Assert.AreEqual(xmlSubCategory.Name,
					                transformerConfiguration.Category.ParentCategory.Name);
				}
			}

			var datasetValue =
				(DatasetTestParameterValue) transformerConfiguration.ParameterValues[0];
			Assert.NotNull(datasetValue.DatasetValue);

			Assert.AreEqual(_xmlTransformerDescriptor.Name,
			                transformerConfiguration.TransformerDescriptor.Name);
		}
	}
}
