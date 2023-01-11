using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Test.Geodatabase
{
	[TestFixture]
	public class DomainUtilsTest
	{
		private string _simpleGdbPath;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			// TestEnvironment.ConfigureLogging();
			_msg.IsVerboseDebugEnabled = true;

			TestUtils.InitializeLicense();
			_simpleGdbPath = TestData.GetGdb1Path();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		[Category(TestCategory.Fast)]
		public void TestGetCodedValueDomain()
		{
			IFeatureWorkspace workspace =
				WorkspaceUtils.OpenFileGdbFeatureWorkspace(_simpleGdbPath);
			IDomain domain = DomainUtils.GetDomain(workspace, "TestCodedValueDomain");

			Assert.IsNotNull(domain);
			Assert.IsNotNull(domain as ICodedValueDomain);
		}

		[Test]
		[Category(TestCategory.Fast)]
		public void TestGetCodedValues()
		{
			IWorkspace workspace = WorkspaceUtils.OpenFileGdbWorkspace(_simpleGdbPath);
			SortedDictionary<int, string> list = DomainUtils.GetCodedValueMap<int>(
				workspace, "TestCodedValueDomain");

			Assert.AreEqual(5, list.Count);
			Assert.AreEqual("Value7", list[7]);
		}

		[Test]
		[Category(TestCategory.Sde)]
		[Ignore("requires PROSUITE_DDX in sql express")]
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

			workspace = WorkspaceUtils.OpenFileGdbWorkspace(_simpleGdbPath);
			domain.Owner = string.Empty; // or null

			Assert.True(DomainUtils.IsOwnedByConnectedUser(domain, workspace));
		}
	}
}
