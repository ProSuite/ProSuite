using System;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.GeoDb;
using ProSuite.DomainModel.AO.Workflow.WorkspaceFilters;

namespace ProSuite.DomainModel.AO.Test.Workflow.WorkspaceFilters
{
	[TestFixture]
	public class WorkspaceDbTypeMatchCriterionTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		[Category(TestCategory.x86)]
		public void CanMatchPgdb()
		{
			var criterion = new WorkspaceDbTypeMatchCriterion(
				new[] { new WorkspaceDbTypeInfo("pgdb", WorkspaceDbType.PersonalGeodatabase) });

			string gdbPath = TestData.GetMdb1Path();

			IWorkspace workspace = WorkspaceUtils.OpenPgdbWorkspace(gdbPath);

			string reason;
			bool isSatisfied = criterion.IsSatisfied(workspace, out reason);

			Console.WriteLine(reason);
			Assert.IsTrue(isSatisfied);
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CantMatchSde()
		{
			var criterion = new WorkspaceDbTypeMatchCriterion(
				new[] { new WorkspaceDbTypeInfo("sde", WorkspaceDbType.ArcSDE) });

			IWorkspace workspace = TestUtils.OpenUserWorkspaceOracle();

			bool isSatisfied = criterion.IsSatisfied(workspace, out string reason);

			Console.WriteLine(reason);
			Assert.IsTrue(isSatisfied);
		}

		[Test]
		public void CanMatchFileGdb()
		{
			var criterion = new WorkspaceDbTypeMatchCriterion(
				new[] { new WorkspaceDbTypeInfo("fgdb", WorkspaceDbType.FileGeodatabase) });

			string gdbPath = TestData.GetGdb1Path();

			IWorkspace workspace = WorkspaceUtils.OpenFileGdbWorkspace(gdbPath);

			bool isSatisfied = criterion.IsSatisfied(workspace, out string reason);

			Console.WriteLine(reason);
			Assert.IsTrue(isSatisfied);

			//test mismatch
			criterion = new WorkspaceDbTypeMatchCriterion(
				new[]
				{
					new WorkspaceDbTypeInfo("sde", WorkspaceDbType.ArcSDE),
					new WorkspaceDbTypeInfo("sde-oracle", WorkspaceDbType.ArcSDEOracle)
				});

			isSatisfied = criterion.IsSatisfied(workspace, out reason);

			Console.WriteLine(reason);
			Assert.IsFalse(isSatisfied);
		}

		[Test]
		public void CanMatchMobileGdb()
		{
			var criterion = new WorkspaceDbTypeMatchCriterion(
				new[] { new WorkspaceDbTypeInfo("mgdb", WorkspaceDbType.MobileGeodatabase) });

			string gdbPath = TestData.GetMobileGdbPath();

			IWorkspace workspace = WorkspaceUtils.OpenMobileGdbWorkspace(gdbPath);

			bool isSatisfied = criterion.IsSatisfied(workspace, out string reason);

			Console.WriteLine(reason);
			Assert.IsTrue(isSatisfied);

			//test mismatch
			criterion = new WorkspaceDbTypeMatchCriterion(
				new[]
				{
					new WorkspaceDbTypeInfo("sde", WorkspaceDbType.ArcSDE),
					new WorkspaceDbTypeInfo("sde-oracle", WorkspaceDbType.ArcSDEOracle)
				});

			isSatisfied = criterion.IsSatisfied(workspace, out reason);

			Console.WriteLine(reason);
			Assert.IsFalse(isSatisfied);
		}
	}
}
