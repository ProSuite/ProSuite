using System.Collections.Generic;
using System.Reflection;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Test.Geodatabase
{
	[TestFixture]
	public class DomainUtilsTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private string _simpleGdbPath;

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		[OneTimeSetUp]
		public void SetupFixture()
		{
			// TestEnvironment.ConfigureLogging();
			_msg.IsVerboseDebugEnabled = true;

			_lic.Checkout();
			_simpleGdbPath = TestData.GetGdb1Path();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		[Category(TestCategory.Fast)]
		public void TestGetCodedValueDomain()
		{
			IFeatureWorkspace workspace =
				WorkspaceUtils.OpenPgdbFeatureWorkspace(_simpleGdbPath);
			IDomain domain = DomainUtils.GetDomain(workspace, "TestCodedValueDomain");

			Assert.IsNotNull(domain);
			Assert.IsNotNull(domain as ICodedValueDomain);
		}

		[Test]
		[Category(TestCategory.Fast)]
		public void TestGetCodedValues()
		{
			IWorkspace workspace = WorkspaceUtils.OpenPgdbWorkspace(_simpleGdbPath);
			SortedDictionary<int, string> list = DomainUtils.GetCodedValueMap<int>(
				workspace, "TestCodedValueDomain");

			Assert.AreEqual(5, list.Count);
			Assert.AreEqual("Value7", list[7]);
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void TestIsOwnedByConnectedUser()
		{
			IWorkspace workspace = WorkspaceUtils.OpenSDEWorkspace(
				"PROSUITE_DDX", DirectConnectDriver.SqlServer, @".\SQLEXPRESS");

			var domain = new CodedValueDomainClass
			             {
				             Name = "Test",
				             FieldType = esriFieldType.esriFieldTypeInteger,
				             Owner = "DBO"
			             };

			Assert.True(DomainUtils.IsOwnedByConnectedUser(domain, workspace));

			workspace = WorkspaceUtils.OpenPgdbWorkspace(_simpleGdbPath);
			domain.Owner = string.Empty; // or null

			Assert.True(DomainUtils.IsOwnedByConnectedUser(domain, workspace));
		}
	}
}
