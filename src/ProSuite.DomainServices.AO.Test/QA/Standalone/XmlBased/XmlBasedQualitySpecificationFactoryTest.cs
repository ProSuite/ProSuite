using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.AO.QA.Xml;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA.Standalone.XmlBased;
using ProSuite.DomainServices.AO.QA.VerifiedDataModel;

namespace ProSuite.DomainServices.AO.Test.QA.Standalone.XmlBased
{
	[TestFixture]
	public class XmlBasedQualitySpecificationFactoryTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private XmlWorkspace _xmlWorkspace;
		private XmlTestDescriptor _xmlTestDescriptorSimple;
		private XmlTestDescriptor _xmlTestDescriptorMinArea;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[SetUp]
		public void SetUp()
		{
			_xmlWorkspace =
				new XmlWorkspace
				{
					ID = "schemaTests",
					ModelName = "SchemaTests"
				};

			_xmlTestDescriptorSimple =
				new XmlTestDescriptor
				{
					Name = "qaSimpleGeometry(0)",
					TestClass = new XmlClassDescriptor
					            {
						            TypeName =
							            "EsriDE.ProSuite.QA.Tests.QaSimpleGeometry",
						            AssemblyName = "EsriDE.ProSuite.QA.Tests",
						            ConstructorId = 0
					            }
				};

			_xmlTestDescriptorMinArea =
				new XmlTestDescriptor
				{
					Name = "QaMinArea(0)",
					TestClass = new XmlClassDescriptor
					            {
						            TypeName =
							            "EsriDE.ProSuite.QA.Tests.QaMinArea",
						            AssemblyName = "EsriDE.ProSuite.QA.Tests",
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

				try
				{
					CanCreateQualitySpecificationCore();
				}
				catch (COMException)
				{
					// TODO: Move test data to different format
					if (EnvironmentUtils.Is64BitProcess)
					{
						Console.WriteLine("Expected exception: PGDB is not supported on x64");
					}
					else
					{
						throw;
					}
				}
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

				try
				{
					CanCreateQualitySpecificationCore();
				}
				catch (COMException)
				{
					// TODO: Move test data to different format
					if (EnvironmentUtils.Is64BitProcess)
					{
						Console.WriteLine("Expected exception: PGDB is not supported on x64");
					}
					else
					{
						throw;
					}
				}
			}
			finally
			{
				Thread.CurrentThread.CurrentCulture = origCulture;
			}
		}

		[Test]
		public void CanCreateQualitySpecificationIgnoringUnkownDatasets()
		{
			var locator = TestDataLocator.Create("ProSuite", @"QA\TestData");
			string catalogPath = locator.GetPath("QATestData.mdb");

			var xmlQCon = new XmlQualityCondition
			              {
				              Name = "Simple",
				              TestDescriptorName = _xmlTestDescriptorSimple.Name
			              };

			xmlQCon.ParameterValues.Add(new XmlDatasetTestParameterValue
			                            {
				                            TestParameterName = "featureClass",
				                            Value = "UNKNOWN",
				                            WorkspaceId = _xmlWorkspace.ID
			                            });

			var xmlQSpec = new XmlQualitySpecification {Name = "qspec"};
			xmlQSpec.Elements.Add(new XmlQualitySpecificationElement
			                      {
				                      QualityConditionName = xmlQCon.Name
			                      });

			var xmlDocument = new XmlDataQualityDocument();

			xmlDocument.AddWorkspace(_xmlWorkspace);
			xmlDocument.AddQualitySpecification(xmlQSpec);
			xmlDocument.AddQualityCondition(xmlQCon);
			xmlDocument.AddTestDescriptor(_xmlTestDescriptorSimple);

			var modelFactory =
				new VerifiedModelFactory(CreateWorkspaceContext,
				                         new SimpleVerifiedDatasetHarvester());

			var factory = new XmlBasedQualitySpecificationFactory(
				modelFactory, new SimpleDatasetOpener(new MasterDatabaseDatasetContext()));

			var dataSource = new DataSource(_xmlWorkspace)
			                 {
				                 WorkspaceAsText = catalogPath
			                 };

			const bool ignoreConditionsForUnknownDatasets = true;

			QualitySpecification qualitySpecification;
			try
			{
				qualitySpecification =
					factory.CreateQualitySpecification(xmlDocument, xmlQSpec.Name,
					                                   new[] {dataSource},
					                                   ignoreConditionsForUnknownDatasets);
			}
			catch (Exception)
			{
				// TODO: Move test data to different format
				if (EnvironmentUtils.Is64BitProcess)
				{
					Console.WriteLine("Expected exception: PGDB is not supported on x64");
					return;
				}

				throw;
			}

			Assert.AreEqual(xmlQSpec.Name, qualitySpecification.Name);
			Assert.AreEqual(0, qualitySpecification.Elements.Count);
		}

		[Test]
		public void CanCreateEmptyQualitySpecification()
		{
			var xmlQualitySpecification = new XmlQualitySpecification {Name = "Empty"};

			var xmlDocument = new XmlDataQualityDocument();
			xmlDocument.AddQualitySpecification(xmlQualitySpecification);

			var modelFactory =
				new VerifiedModelFactory(CreateWorkspaceContext,
				                         new SimpleVerifiedDatasetHarvester());

			var factory = new XmlBasedQualitySpecificationFactory(
				modelFactory, new SimpleDatasetOpener(new MasterDatabaseDatasetContext()));

			QualitySpecification qualitySpecification =
				factory.CreateQualitySpecification(xmlDocument, xmlQualitySpecification.Name,
				                                   new DataSource[] { });

			Assert.AreEqual(xmlQualitySpecification.Name, qualitySpecification.Name);
		}

		private void CanCreateQualitySpecificationCore()
		{
			var locator = TestDataLocator.Create("ProSuite", @"QA\TestData");
			string catalogPath = locator.GetPath("QATestData.mdb");

			var xmlCategory = new XmlDataQualityCategory {Name = "Category A"};
			var xmlSubCategory = new XmlDataQualityCategory {Name = "Category A.1"};
			var xmlSubSubCategory = new XmlDataQualityCategory {Name = "Category A.1.1"};
			var xmlSubSubCategory2 = new XmlDataQualityCategory {Name = "Category A.1.2"};

			xmlCategory.AddSubCategory(xmlSubCategory);
			xmlSubCategory.AddSubCategory(xmlSubSubCategory);
			xmlSubCategory.AddSubCategory(xmlSubSubCategory2);

			var xmlQCon = new XmlQualityCondition
			              {
				              Name = "MinArea",
				              TestDescriptorName = _xmlTestDescriptorMinArea.Name
			              };

			xmlQCon.ParameterValues.Add(new XmlDatasetTestParameterValue
			                            {
				                            TestParameterName = "polygonClass",
				                            Value = "polygons",
				                            WorkspaceId = _xmlWorkspace.ID
			                            });
			xmlQCon.ParameterValues.Add(new XmlScalarTestParameterValue
			                            {
				                            TestParameterName = "limit",
				                            Value = "12.34"
			                            });

			var xmlQSpec = new XmlQualitySpecification {Name = "qspec"};
			xmlQSpec.Elements.Add(new XmlQualitySpecificationElement
			                      {
				                      QualityConditionName = xmlQCon.Name
			                      });

			var xmlDocument = new XmlDataQualityDocument();

			xmlDocument.AddCategory(xmlCategory);
			xmlDocument.AddWorkspace(_xmlWorkspace);
			//xmlDocument.AddQualitySpecification(xmlQSpec);
			//xmlDocument.AddQualityCondition(xmlQCon);
			xmlDocument.AddTestDescriptor(_xmlTestDescriptorMinArea);

			xmlSubCategory.AddQualitySpecification(xmlQSpec);
			xmlSubSubCategory.AddQualityCondition(xmlQCon);

			var modelFactory =
				new VerifiedModelFactory(CreateWorkspaceContext,
				                         new SimpleVerifiedDatasetHarvester());

			var factory = new XmlBasedQualitySpecificationFactory(
				modelFactory, new SimpleDatasetOpener(new MasterDatabaseDatasetContext()));

			var dataSource = new DataSource(_xmlWorkspace)
			                 {
				                 WorkspaceAsText = catalogPath
			                 };

			QualitySpecification qualitySpecification =
				factory.CreateQualitySpecification(xmlDocument, xmlQSpec.Name,
				                                   new[] {dataSource});

			Assert.NotNull(qualitySpecification.Category);
			Assert.AreEqual(xmlQSpec.Name, qualitySpecification.Name);
			Assert.AreEqual(1, qualitySpecification.Elements.Count);
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

			QualityCondition qualityCondition =
				qualitySpecification.Elements[0].QualityCondition;
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

			var value =
				(ScalarTestParameterValue) qualityCondition.GetParameterValues("limit")[0];
			Assert.AreEqual(12.34, value.GetValue(typeof(double)));
		}

		private static IWorkspaceContext CreateWorkspaceContext(
			[NotNull] Model model,
			[NotNull] IFeatureWorkspace workspace)
		{
			return new MasterDatabaseWorkspaceContext(workspace, model);
		}
	}
}