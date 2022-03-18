using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Testing;

namespace ProSuite.Commons.AO.Test.Geodatabase
{
	[TestFixture]
	public class ProCompatibilityTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.ConfigureUnittestLogging();

			_msg.IsVerboseDebugEnabled = true;

			_lic.Checkout();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

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

		private static void LogFeatureClassProperties([NotNull] IFeatureClass fClass)
		{
			Console.WriteLine("AliasName: {0}", fClass.AliasName);
			Console.WriteLine("FeatureType: {0}", fClass.FeatureType);
			Console.WriteLine("CLSID: {0}", fClass.CLSID.Value);
			Console.WriteLine("EXTCLSID: {0}", fClass.EXTCLSID.Value);

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
