using System;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom.EsriShape;
using ProSuite.Commons.Globalization;
using ProSuite.Commons.IO;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.AO.QA.SpecificationReport;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.Geodatabase;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainModel.AO.Test.QA.SpecificationReport
{
	[TestFixture]
	public class SpecificationReportUtilsTest
	{
		[Test]
		public void CanCreateReport()
		{
			CultureInfoUtils.ExecuteUsing(CultureInfo.InvariantCulture,
			                              CultureInfo.InvariantCulture,
			                              () => CreateReport());
		}

		[Test]
		public void CanCreateReportDe()
		{
			CultureInfoUtils.ExecuteUsing(CultureInfo.GetCultureInfo("de-De"),
			                              CultureInfo.GetCultureInfo("de-De"),
			                              () => CreateReport("de-De"));
		}

		private static void CreateReport([CanBeNull] string reportNameSuffix = null)
		{
			QualitySpecification qualitySpecification = CreateQualitySpecification();

			HtmlQualitySpecification htmlSpecification =
				SpecificationReportUtils.CreateHtmlQualitySpecification(
					qualitySpecification,
					new HtmlDataQualityCateogryOptionsProvider());

			string reportName = string.Format("qspec{0}.html", reportNameSuffix);

			string reportPath = SpecificationReportUtils.RenderHtmlQualitySpecification(
				htmlSpecification,
				@"QA\SpecificationReport\qspec_template.html",
				reportName,
				throwTemplateErrors: true);

			Console.WriteLine(Path.GetFullPath(reportPath));

			Console.WriteLine(FileSystemUtils.ReadTextFile(reportPath));
		}

		[NotNull]
		private static QualitySpecification CreateQualitySpecification()
		{
			var cat1 = new DataQualityCategory("1", "1");
			var cat2 = new DataQualityCategory("2", "2");
			var cat1_1 = new DataQualityCategory("1", "1.1");
			var cat1_2 = new DataQualityCategory("2", "1.2");
			var cat1_1_1 = new DataQualityCategory("1", "1.1.1");

			cat1.AddSubCategory(cat1_1);
			cat1.AddSubCategory(cat1_2);
			cat1_1.AddSubCategory(cat1_1_1);

			var geometryType = new GeometryTypeShape("Polygon",
			                                         ProSuiteGeometryType.Polygon);

			DdxModel m = new TestModel("testmodel");
			m.UserConnectionProvider = new FileGdbConnectionProvider("C:\\doesnotexist.gdb");
			Dataset ds0 = m.AddDataset(new TestVectorDataset("Dataset0"));
			Dataset ds1 = m.AddDataset(new TestVectorDataset("Dataset1"));

			ds0.GeometryType = geometryType;
			ds1.GeometryType = geometryType;

			// spec 0
			var qs = new QualitySpecification("spec0")
			         {
				         Category = cat2,
				         Description = "lorem ipsum blah blah",
				         Url = "some_url.html"
			         };

			var qaMinLength = new TestDescriptor(
				"MinLength(0)",
				new ClassDescriptor(
					"EsriDE.ProSuite.QA.Tests.QaMinLength",
					"EsriDE.ProSuite.QA.Tests"), 0, false, false);
			qaMinLength.Description = "my own description for this test";

			var qaCurve = new TestDescriptor(
				"Curve(0)",
				new ClassDescriptor(
					"ProSuite.QA.Tests.QaCurve",
					"ProSuite.QA.Tests"), 0, false, true);

			var cond0 = new QualityCondition("cond0", qaMinLength)
			            {
				            Category = cat1_1_1,
				            Url = "another_url.html",
				            Description = "This test does absolutely nothing"
			            };
			InstanceConfigurationUtils.AddScalarParameterValue(cond0, "limit", "0.5");
			TestParameterValueUtils.AddParameterValue(cond0, "featureClass", ds0);

			qs.AddElement(cond0);

			var cond1 = new QualityCondition("cond1", qaMinLength)
			            {
				            Category = cat1_1,
				            Url = "yet_another_url.html"
			            };
			InstanceConfigurationUtils.AddScalarParameterValue(
				cond1, "limit", "0.5");
			TestParameterValueUtils.AddParameterValue(cond1, "featureClass", ds1);

			qs.AddElement(cond1, allowErrorsOverride: true);

			var cond2 = new QualityCondition("cond2", qaCurve)
			            {
				            Category = cat1_1_1,
				            Description = "Show reporting list parameters as well as optional parameters"
			};
			TestParameterValueUtils.AddParameterValue(cond2, "AllowedNonLinearSegmentTypes", 0);
			TestParameterValueUtils.AddParameterValue(cond2, "AllowedNonLinearSegmentTypes", 1);
			TestParameterValueUtils.AddParameterValue(cond2, "featureClass", ds1);

			qs.AddElement(cond2, allowErrorsOverride: true);
			return qs;
		}

		private class TestVectorDataset : VectorDataset
		{
			public TestVectorDataset([NotNull] string name) : base(name) { }
		}

		private class TestModel : DdxModel, IModelMasterDatabase
		{
			public TestModel([NotNull] string name) : base(name) { }

			public override string QualifyModelElementName(string modelElementName)
			{
				return ModelUtils.QualifyModelElementName(this, modelElementName);
			}

			public override string TranslateToModelElementName(string masterDatabaseDatasetName)
			{
				return ModelUtils.TranslateToModelElementName(this, masterDatabaseDatasetName);
			}

			IWorkspaceContext IModelMasterDatabase.CreateMasterDatabaseWorkspaceContext()
			{
				return ModelUtils.CreateDefaultMasterDatabaseWorkspaceContext(this);
			}

			protected override void CheckAssignSpecialDatasetCore(Dataset dataset) { }
		}
	}
}
