using System;
using System.IO;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Test.TestData
{
	[CLSCompliant(false)]
	public static class TestDataUtils
	{
		private const string _topgisTlmPath = "topgis\\topgis_tlm@topgist.sde";

		public static readonly string LocalDataPath = @"\\CROFTON\c$\data\unitTests";

		[NotNull]
		public static IWorkspace OpenFileGdb(string testDataPath)
		{
			string fullPath = GetFullPath(testDataPath);
			IWorkspace ws = WorkspaceUtils.OpenFileGdbWorkspace(fullPath);
			return ws;
		}

		[NotNull]
		public static IWorkspace OpenPgdb(string testDataPath)
		{
			string fullPath = GetFullPath(testDataPath);
			IWorkspace ws = WorkspaceUtils.OpenPgdbWorkspace(fullPath);
			return ws;
		}

		[NotNull]
		public static string TopgisTlmPath
		{
			get { return GetFullPath(_topgisTlmPath); }
		}

		[NotNull]
		public static IWorkspace OpenTopgisTlm()
		{
			return WorkspaceUtils.OpenSDEWorkspace(DirectConnectDriver.Oracle11g, "TOPGIST",
			                                       "SDE.DEFAULT");
		}

		[NotNull]
		public static string GetFullPath(string testDataPath)
		{
			string fullPath = Path.Combine(LocalDataPath, testDataPath);
			return fullPath;
		}

		[NotNull]
		public static TestDataLocator GetTestDataLocator()
		{
			var locator = TestDataLocator.Create("ProSuite");

			return locator;
		}
	}
}
