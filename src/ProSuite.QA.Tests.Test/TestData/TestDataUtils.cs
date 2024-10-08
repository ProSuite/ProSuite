using System.IO;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Test.TestData
{
	public static class TestDataUtils
	{
		private const string _topgisTlmPath = "topgis\\topgis_tlm@topgist.sde";

		public static readonly string LocalDataPath = @"\\CROFTON\c$\data\unitTests";

		[NotNull]
		public static string TopgisTlmPath => GetFullPath(_topgisTlmPath);

		[NotNull]
		public static IWorkspace OpenFileGdb(string testDataPath)
		{
			string fullPath = GetFullPath(testDataPath);
			IWorkspace ws = WorkspaceUtils.OpenFileGdbWorkspace(fullPath);
			return ws;
		}

		[NotNull]
		[Category(TestCategory.x86)]
		public static IWorkspace OpenPgdb(string testDataPath)
		{
			string fullPath = GetFullPath(testDataPath);
			IWorkspace ws = WorkspaceUtils.OpenPgdbWorkspace(fullPath);
			return ws;
		}

		[NotNull]
		public static IWorkspace OpenTopgisTlm()
		{
			return WorkspaceUtils.OpenSDEWorkspace(
				DirectConnectDriver.Oracle11g, "TOPGIST",
				"sde", "sde", "SDE.DEFAULT");
		}

		[NotNull]
		public static IWorkspace OpenTopgisAlti()
		{
			return WorkspaceUtils.OpenSDEWorkspace(
				DirectConnectDriver.Oracle11g, "ALTIT",
				"sde", "sde", "SDE.DEFAULT");
		}

		[NotNull]
		public static string GetFullPath(string testDataPath)
		{
			string fullPath = Path.Combine(LocalDataPath, testDataPath);
			return fullPath;
		}
	}
}
