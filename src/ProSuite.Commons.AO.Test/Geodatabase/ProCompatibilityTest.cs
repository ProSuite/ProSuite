using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AGP.Hosting;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Testing;

namespace ProSuite.Commons.AO.Test.Geodatabase
{
	[TestFixture]
	[Ignore("Run in x64 solution: Swisstopo.Topgis.Server")]
	public class ProCompatibilityTest
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.ConfigureUnittestLogging();

			_msg.IsVerboseDebugEnabled = true;

			CoreHostProxy.Initialize();
		}

		[OneTimeTearDown]
		public void TeardownFixture() { }

		public static string GetProCompatibilityTestFileGdbPath()
		{
			//TODO: unzip
			var locator = new TestDataLocator();
			return locator.GetPath("procompatibilitytest.gdb");
		}

		[Test]
		[Ignore("Passes or fails depending on license/runtime")]
		public void LearningTestAnnotationFeatureClass()
		{
			IWorkspace workspace =
				WorkspaceUtils.OpenFileGdbWorkspace(GetProCompatibilityTestFileGdbPath());

			//Ao10 AnnotationFeatureClass
			IFeatureClass fClass = DatasetUtils.OpenFeatureClass(
				workspace, "Lines_Anno");

			Assert.NotNull(fClass);

			LogFeatureClassProperties(fClass);

			//Ao11 AnnotationFeatureClass (using "Upgrade Dataset" from Pro)
			IFeatureClass fClassUpgraded = DatasetUtils.OpenFeatureClass(
				workspace, "Lines_Anno_Upgraded");

			Assert.NotNull(fClassUpgraded);

			LogFeatureClassProperties(fClassUpgraded);

			//TODO read/write access to data
		}

		[Test]
		[Ignore("Learning test: needs local data")]
		public void Compare_annotation_FeatureClass_properties_AO10_and_AO11()
		{
			IWorkspace workspace = WorkspaceUtils.OpenSDEWorkspace(
				@"C:\Users\daro\AppData\Roaming\Esri\ArcGISPro\Favorites\gen10_as_geniusdb_dkm25.sde");

			// FeatureClass
			IFeatureClass fc = DatasetUtils.OpenFeatureClass(
				workspace, "GENIUSDB_DKM25.DKM25_SIEDLUNGSNAME");

			Assert.NotNull(fc);
			LogFeatureClassProperties(fc);
			Console.WriteLine();

			// AO10 AnnotationFeatureClass
			IFeatureClass annotationFc_AO10 = DatasetUtils.OpenFeatureClass(
				workspace, "GENIUSDB_DKM25.DKM25_GEBIETSNAME_ANNO");

			Assert.NotNull(annotationFc_AO10);
			LogFeatureClassProperties(annotationFc_AO10);
			Console.WriteLine();

			// AO1 AnnotationFeatureClass
			IFeatureClass annotationFc_AO11 = DatasetUtils.OpenFeatureClass(
				workspace, "GENIUSDB_DKM25.DKM25_SIEDLUNGSNAME_ANNO");

			Assert.NotNull(annotationFc_AO11);
			LogFeatureClassProperties(annotationFc_AO11);
		}

		[Test]
		[Ignore("Learning test: can create annotation feature class with AO11")]
		public void Can_create_annotation_FeatureClass_with_AO11()
		{
			IWorkspace workspace = WorkspaceUtils.OpenSDEWorkspace(
				@"C:\Users\daro\AppData\Roaming\Esri\ArcGISPro\Favorites\gen10_as_geniusdb_dkm25.sde");

			var name = "GENIUSDB_DKM25.DKM25_SIEDLUNGSNAME_ANNO_created";
			try
			{
				DatasetUtils.DeleteFeatureClass(workspace, name);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}

			IFeatureClass fc = DatasetUtils.OpenFeatureClass(
				workspace, "GENIUSDB_DKM25.DKM25_SIEDLUNGSNAME_ANNO");

			IFields fields = fc.Fields;

			IFeatureWorkspace fws = (IFeatureWorkspace) workspace;

			IFeatureDataset featureDataset = fws.OpenFeatureDataset("GENIUSDB_DKM25.DKM25_NAMEN");

			IFeatureClass annoFc =
				DatasetUtils.CreateAnnotationFeatureClass(featureDataset, name, fields);

			Assert.NotNull(annoFc);
			LogFeatureClassProperties(annoFc);
		}

		private static void LogFeatureClassProperties([NotNull] IFeatureClass fClass)
		{
			Console.WriteLine("AliasName: {0}", fClass.AliasName);
			Console.WriteLine("FeatureType: {0}", fClass.FeatureType);
			Console.WriteLine("CLSID: {0}", fClass.CLSID.Value);
			if (fClass.EXTCLSID != null)
			{
				Console.WriteLine("EXTCLSID: {0}", fClass.EXTCLSID.Value);
			}

			if (fClass.ExtensionProperties == null)
			{
				return;
			}

			IDictionary<string, object> propertyDict =
				PropertySetUtils.GetDictionary(fClass.ExtensionProperties);

			foreach (KeyValuePair<string, object> keyValuePair in propertyDict)
			{
				string displayKey = keyValuePair.Key;
				string displayValue =
					keyValuePair.Value == null ? "null" : keyValuePair.Value.ToString();
				Console.WriteLine("  {0}: {1}", displayKey, displayValue);
			}
		}
	}
}
