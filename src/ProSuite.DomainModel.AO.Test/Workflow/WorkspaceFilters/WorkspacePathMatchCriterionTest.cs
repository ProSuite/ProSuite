using System;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.Workflow.WorkspaceFilters;

namespace ProSuite.DomainModel.AO.Test.Workflow.WorkspaceFilters
{
	[TestFixture]
	public class WorkspacePathMatchCriterionTest
	{
		[Test]
		public void CantMatchFileGdbNameWithoutParent()
		{
			AssertNoMatch(@"C:\workspaces\checkout_1234.gdb", @"checkout_*.gdb");
		}

		[Test]
		public void CanMatchFileGdbName()
		{
			AssertMatch(@"C:\workspaces\checkout_1234.gdb", @"*\checkout_*.gdb");
		}

		[Test]
		public void CanMatchFileGdbNameUnderParentDirectory()
		{
			AssertMatch(@"C:\workspaces\project1\userA\checkout_1234.gdb",
			            @"C:\workspaces\*\checkout_*.gdb");
		}

		[Test]
		public void CanMatchParentDirectory()
		{
			AssertMatch(@"C:\workspaces\checkout_1234.gdb", @"C:\workspaces\*");
		}

		private static void AssertMatch([CanBeNull] string workspacePath,
		                                [NotNull] string pattern)
		{
			AssertResult(workspacePath, pattern, expectSatisfied: true);
		}

		private static void AssertNoMatch([CanBeNull] string workspacePath,
		                                  [NotNull] string pattern)
		{
			AssertResult(workspacePath, pattern, expectSatisfied: false);
		}

		private static void AssertResult(string workspacePath,
		                                 string pattern,
		                                 bool expectSatisfied)
		{
			IWorkspace workspace = GetWorkspaceMock(workspacePath);

			var criterion = new WorkspacePathMatchCriterion(new[] { pattern });

			bool isSatisfied = criterion.IsSatisfied(workspace, out string reason);

			Console.WriteLine(reason);

			if (expectSatisfied)
			{
				Assert.IsTrue(isSatisfied);
			}
			else
			{
				Assert.IsFalse(isSatisfied);
			}
		}

		[NotNull]
		private static IWorkspace GetWorkspaceMock([CanBeNull] string pathName)
		{
			var result = new WorkspaceMock(pathName);

			return result;
		}
	}
}
