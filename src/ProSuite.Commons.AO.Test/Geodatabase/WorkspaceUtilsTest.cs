using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Test.Geodatabase
{
	[TestFixture]
	public class WorkspaceUtilsTest
	{
		private string _nonDefaultRepositoryName;
		private string _dbName;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			Commons.Test.Testing.TestUtils.ConfigureUnitTestLogging();

			_msg.IsVerboseDebugEnabled = true;

			TestUtils.InitializeLicense();

			_nonDefaultRepositoryName = "TOPGIS_DDX";
			_dbName = TestUtils.OracleDbNameSde;
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		[Category(TestCategory.x86)]
		public void CanOpenOleDbWorkspaceAccess()
		{
			string mdbPath = TestData.GetNonGdbAccessDatabase();

			string connectionString =
				string.Format(
					@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={0};Persist Security Info=False",
					mdbPath);

			IWorkspace workspace = WorkspaceUtils.OpenOleDbWorkspace(connectionString);

			Assert.True(WorkspaceUtils.IsOleDbWorkspace(workspace));
			Assert.False(WorkspaceUtils.UsesQualifiedDatasetNames(workspace));

			LogWorkspaceProperties(workspace);

			foreach (IDatasetName datasetName in DatasetUtils.GetDatasetNames(
				         workspace, esriDatasetType.esriDTTable))
			{
				Console.WriteLine(datasetName.Name);
			}
		}

		[Test]
		[Category(TestCategory.Sde)]
		[Ignore("requires PROSUITE_DDX in sql express")]
		public void CanOpenOleDbWorkspaceSqlServer()
		{
			var connectionString =
				@"Provider=SQLOLEDB.1;Integrated Security=SSPI;Persist Security Info=False;Initial Catalog=PROSUITE_DDX;Data Source=.\SQLEXPRESS";

			IWorkspace workspace = WorkspaceUtils.OpenOleDbWorkspace(connectionString);

			Assert.True(WorkspaceUtils.IsOleDbWorkspace(workspace));
			Assert.True(WorkspaceUtils.UsesQualifiedDatasetNames(workspace));

			LogWorkspaceProperties(workspace);

			foreach (IDatasetName datasetName in DatasetUtils.GetDatasetNames(
				         workspace, esriDatasetType.esriDTTable))
			{
				Console.WriteLine(datasetName.Name);
			}
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanGetWorkspaceDisplayTextForOracle()
		{
			IWorkspace workspace = TestUtils.OpenUserWorkspaceOracle();

			string text = WorkspaceUtils.GetWorkspaceDisplayText(workspace);

			Console.WriteLine(text);
		}

		[Test]
		[Category(TestCategory.Sde)]
		[Ignore("requires PROSUITE_DDX in sql express")]
		public void CanGetWorkspaceDisplayTextForSqlExpress()
		{
			IWorkspace workspace =
				WorkspaceUtils.OpenSDEWorkspace("PROSUITE_DDX",
				                                DirectConnectDriver.SqlServer,
				                                @".\SQLEXPRESS");

			string text = WorkspaceUtils.GetWorkspaceDisplayText(workspace);

			Console.WriteLine(text);
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void LearningTestDatabasePropertiesOracle()
		{
			IWorkspace workspace = TestUtils.OpenUserWorkspaceOracle();

			var connectionInfo = (IDatabaseConnectionInfo2) workspace;

			Console.WriteLine(@"ConnectionServer: {0}", connectionInfo.ConnectionServer);
			Console.WriteLine(@"ConnectedUser: {0}", connectionInfo.ConnectedUser);
			Console.WriteLine(@"ConnectedDatabase: {0}", connectionInfo.ConnectedDatabase);

			// not available for 10.0:
			// Console.WriteLine(@"ConnectedDatabaseEx: {0}", connectionInfo.ConnectedDatabaseEx);
			// - ConnectedDatabaseEx returns the TNS name for the database
		}

		[Test]
		[Category(TestCategory.Sde)]
		[Ignore("requires PROSUITE_DDX in sql express")]
		public void LearningTestDatabasePropertiesSqlExpress()
		{
			IWorkspace workspace =
				WorkspaceUtils.OpenSDEWorkspace("PROSUITE_DDX",
				                                DirectConnectDriver.SqlServer,
				                                @".\SQLEXPRESS");

			var connectionInfo = (IDatabaseConnectionInfo2) workspace;

			Console.WriteLine(@"ConnectionServer: {0}", connectionInfo.ConnectionServer);
			Console.WriteLine(@"ConnectedUser: {0}", connectionInfo.ConnectedUser);
			Console.WriteLine(@"ConnectedDatabase: {0}", connectionInfo.ConnectedDatabase);

			// Console.WriteLine(@"ConnectedDatabaseEx: {0}", connectionInfo.ConnectedDatabaseEx);
			// - ConnectedDatabaseEx returns the database name (same as ConnectedDatabase)
		}

		[Test]
		public void LearningTestMobileGdbWorkspace()
		{
			IWorkspace workspace =
				WorkspaceUtils.OpenMobileGdbWorkspace(TestData.GetMobileGdbPath());

			var versionedWorkspace = workspace as IVersionedWorkspace;
			Console.WriteLine("Is versioneWorkspace: {0}",
			                  versionedWorkspace != null ? "true" : "false");
			var version = workspace as IVersion;
			Console.WriteLine("Is version: {0}",
			                  version != null ? "true" : "false");
			Console.WriteLine("version name: {0}",
			                  version.VersionName);

			//everything else on version fails...
			Assert.Catch<COMException>(() =>
			{
				bool b = version.IsRedefined;
			});
		}

		[Test]
		[Category(TestCategory.Sde)]
		[Ignore("requires PROSUITE_DDX in sql express")]
		public void LearningTestSQLSyntaxSDESqlServer()
		{
			IWorkspace workspace =
				WorkspaceUtils.OpenSDEWorkspace("PROSUITE_DDX",
				                                DirectConnectDriver.SqlServer,
				                                @".\SQLEXPRESS");

			LogSqlSyntax(workspace);
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void LearningTestSQLSyntaxSDEOracle()
		{
			IWorkspace workspace = TestUtils.OpenUserWorkspaceOracle();

			LogSqlSyntax(workspace);
		}

		[Test]
		public void LearningTestSQLSyntaxFGDB()
		{
			IWorkspace workspace =
				WorkspaceUtils.OpenFileGdbWorkspace(TestData.GetGdbTableJointUtilsPath());

			LogSqlSyntax(workspace);
		}

		[Test]
		[Category(TestCategory.x86)]
		public void LearningTestSQLSyntaxPGDB()
		{
			IWorkspace workspace =
				WorkspaceUtils.OpenPgdbWorkspace(TestData.GetMdb1Path());

			LogSqlSyntax(workspace);
		}

		[Test]
		public void LearningTestSQLSyntaxMobileGdb()
		{
			IWorkspace workspace =
				WorkspaceUtils.OpenMobileGdbWorkspace(TestData.GetMobileGdbPath());

			LogSqlSyntax(workspace);
		}

		[Test]
		[Ignore("requires PROSUITE_DDX in sql express")]
		public void LearningTestWorkspacePropertiesSDESqlServer()
		{
			IWorkspace workspace =
				WorkspaceUtils.OpenSDEWorkspace(
					"PROSUITE_DDX",
					DirectConnectDriver.SqlServer,
					string.Format(@"{0}\SQLEXPRESS", Environment.MachineName));

			LogWorkspaceProperties(workspace);

			//esriWorkspacePropCanEdit: True (0.053500 ms)
			//esriWorkspacePropCanExecuteSQL: True (0.008600 ms)
			//esriWorkspacePropIsReadonly: False (0.005800 ms)
			//esriWorkspacePropMaxWhereClauseLength:  [not supported] (0.006300 ms)
			//esriWorkspacePropSupportsMoveEditsToBase: True (0.007800 ms)
			//esriWorkspacePropSupportsQualifiedNames: True (0.007400 ms)
			//esriWorkspacePropLastCompressDate:  (0.141300 ms)
			//esriWorkspacePropLastCompressStatus:  (0.090700 ms)
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void LearningTestWorkspacePropertiesSDEOracle()
		{
			IWorkspace workspace = TestUtils.OpenSDEWorkspaceOracle();

			LogWorkspaceProperties(workspace);

			//esriWorkspacePropCanEdit: True (0.054000 ms)
			//esriWorkspacePropCanExecuteSQL: True (0.006700 ms)
			//esriWorkspacePropIsReadonly: False (0.005500 ms)
			//esriWorkspacePropMaxWhereClauseLength: not supported (0.006100 ms)
			//esriWorkspacePropSupportsMoveEditsToBase: True (0.005700 ms)
			//esriWorkspacePropSupportsQualifiedNames: True (0.005500 ms)
			//esriWorkspacePropLastCompressDate:  (0.513900 ms)
			//esriWorkspacePropLastCompressStatus:  (0.443900 ms)
		}

		[Test]
		public void LearningTestWorkspacePropertiesFGDB()
		{
			IWorkspace workspace =
				WorkspaceUtils.OpenFileGdbWorkspace(TestData.GetGdbTableJointUtilsPath());

			LogWorkspaceProperties(workspace);

			//esriWorkspacePropCanEdit: True (0.098500 ms)
			//esriWorkspacePropCanExecuteSQL: True (0.006100 ms)
			//esriWorkspacePropIsReadonly: False (0.005800 ms)
			//esriWorkspacePropMaxWhereClauseLength: not supported (0.004700 ms)
			//esriWorkspacePropSupportsMoveEditsToBase: not supported (0.005000 ms)
			//esriWorkspacePropSupportsQualifiedNames: True (0.005700 ms)  !! not correct !!
			//esriWorkspacePropLastCompressDate: not supported (0.005100 ms)
			//esriWorkspacePropLastCompressStatus: not supported (0.007300 ms)
		}

		[Test]
		[Category(TestCategory.x86)]
		public void LearningTestWorkspacePropertiesPGDB()
		{
			IWorkspace workspace =
				WorkspaceUtils.OpenPgdbWorkspace(TestData.GetMdb1Path());

			LogWorkspaceProperties(workspace);

			//esriWorkspacePropCanEdit: True (0.007000 ms)
			//esriWorkspacePropCanExecuteSQL: True (0.007900 ms)
			//esriWorkspacePropIsReadonly: False (0.007500 ms)
			//esriWorkspacePropMaxWhereClauseLength: not supported (0.007100 ms)
			//esriWorkspacePropSupportsMoveEditsToBase: not supported (0.006500 ms)
			//esriWorkspacePropSupportsQualifiedNames: True (0.006500 ms) !! not correct !!
			//esriWorkspacePropLastCompressDate: not supported (0.007300 ms)
			//esriWorkspacePropLastCompressStatus: not supported (0.005400 ms)

			// PGDB with readonly flag set: IsReadOnly = true, CanEdit = false
		}

		[Test]
		public void LearningTestWorkspacePropertiesMobileGdb()
		{
			IWorkspace workspace =
				WorkspaceUtils.OpenMobileGdbWorkspace(TestData.GetMobileGdbPath());

			LogWorkspaceProperties(workspace);
		}

		private static void LogSqlSyntax([NotNull] IWorkspace workspace)
		{
			var sqlSyntax = (ISQLSyntax) workspace;
			Console.WriteLine(@"Invalid characters: {0}", sqlSyntax.GetInvalidCharacters());
			Console.WriteLine(@"Invalid starting characters: {0}",
			                  sqlSyntax.GetInvalidStartingCharacters());
			Console.WriteLine(@"Wildcard Many Match: {0}",
			                  sqlSyntax.GetSpecialCharacter(
				                  esriSQLSpecialCharacters.esriSQL_WildcardManyMatch));
			Console.WriteLine(@"Wildcard Single Match: {0}",
			                  sqlSyntax.GetSpecialCharacter(
				                  esriSQLSpecialCharacters.esriSQL_WildcardSingleMatch));
			Console.WriteLine(@"Supported clauses: {0}", sqlSyntax.GetSupportedClauses());
			Console.WriteLine(@"Supported predicates: {0}", sqlSyntax.GetSupportedPredicates());
			Console.WriteLine(@"Identifier case: {0}", sqlSyntax.GetIdentifierCase());
			Console.WriteLine(@"String comparison case: {0}",
			                  sqlSyntax.GetStringComparisonCase());
		}

		private static void LogWorkspaceProperties([NotNull] IWorkspace workspace)
		{
			var properties = workspace as IWorkspaceProperties;

			Console.WriteLine(WorkspaceUtils.GetConnectionString(workspace));
			Console.WriteLine();

			if (properties == null)
			{
				Console.WriteLine(@"IWorkspaceProperties not supported");
				return;
			}

			LogWorkspaceProperties(
				properties,
				esriWorkspacePropertyType.esriWorkspacePropCanEdit,
				esriWorkspacePropertyType.esriWorkspacePropCanExecuteSQL,
				esriWorkspacePropertyType.esriWorkspacePropIsReadonly,
				esriWorkspacePropertyType.esriWorkspacePropMaxWhereClauseLength,
				esriWorkspacePropertyType.esriWorkspacePropSupportsMoveEditsToBase,
				esriWorkspacePropertyType.esriWorkspacePropSupportsQualifiedNames,
				esriWorkspacePropertyType.esriWorkspacePropLastCompressDate,
				esriWorkspacePropertyType.esriWorkspacePropLastCompressStatus);
		}

		private static void LogWorkspaceProperties(
			[NotNull] IWorkspaceProperties props,
			params esriWorkspacePropertyType[] propertyTypes)
		{
			var watch = new Stopwatch();

			foreach (esriWorkspacePropertyType propertyType in propertyTypes)
			{
				watch.Reset();
				watch.Start();

				IWorkspaceProperty property =
					props.Property[
						esriWorkspacePropertyGroupType.esriWorkspacePropertyGroup,
						(int) propertyType];

				watch.Stop();

				if (property.IsSupported)
				{
					Console.WriteLine(@"{0}: {1} ({2:N6} ms)",
					                  propertyType,
					                  property.PropertyValue,
					                  watch.ElapsedTicks / (double) TimeSpan.TicksPerMillisecond);
				}
				else
				{
					Console.WriteLine(@"{0}: {1} [not supported] ({2:N6} ms)",
					                  propertyType,
					                  property.PropertyValue,
					                  watch.ElapsedTicks / (double) TimeSpan.TicksPerMillisecond);
				}
			}
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void AssertOpenVersionWithUnknownNameThrowsException()
		{
			IWorkspace workspace = TestUtils.OpenUserWorkspaceOracle();

			try
			{
				WorkspaceUtils.OpenVersion(workspace, "non_existent_version_name");

				Assert.Fail("exception expected");
			}
			catch (Exception e)
			{
				Console.WriteLine(@"Exception from OpenVersion(): {0}", e.Message);
			}
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanDetectSameDatabaseWithDifferentInstanceFormatAndDbNameAlias()
		{
			IWorkspace workspace1 =
				WorkspaceUtils.OpenSDEWorkspaceFromString(
					"INSTANCE=sde:oracle11g:/:SDE;USER=UNITTEST;PASSWORD=unittest@" +
					"PROSUITE_TEST_SERVER" +
					";VERSION=SDE.DEFAULT;AUTHENTICATION_MODE=DBMS");

			// different: shorter instance spec, different db name (alias for same db)
			IWorkspace workspace2 =
				WorkspaceUtils.OpenSDEWorkspaceFromString(
					"INSTANCE=sde:oracle11g;USER=UNITTEST;PASSWORD=unittest@PROSUITE_TEST_SERVER;VERSION=SDE.DEFAULT;AUTHENTICATION_MODE=DBMS");

			_msg.DebugFormat("workspace1: {0}",
			                 WorkspaceUtils.GetConnectionString(workspace1));
			_msg.DebugFormat("workspace2: {0}",
			                 WorkspaceUtils.GetConnectionString(workspace2));

			Assert.IsTrue(WorkspaceUtils.IsSameDatabase(workspace1, workspace2));
		}

		[Test]
		public void CanGetUnqualifiedVersionName()
		{
			string name =
				WorkspaceUtils.GetUnqualifiedVersionName(@"""ESRI-DE\USERNAME"".MyVersion");
			Assert.AreEqual("MyVersion", name);
		}

		[Test]
		public void CanGetVersionOwnerName()
		{
			string owner = WorkspaceUtils.GetVersionOwnerName(@"""ESRI-DE\USERNAME"".MyVersion");
			Assert.AreEqual(@"""ESRI-DE\USERNAME""", owner);
		}

		[Test]
		[Category(TestCategory.Sde)]
		[Ignore("requires PROSUITE_DDX in sql express")]
		public void CanOpenDefaultVersionSqlExpress()
		{
			IWorkspace workspace = WorkspaceUtils.OpenSDEWorkspace(
				"PROSUITE_DDX", DirectConnectDriver.SqlServer, @".\SQLEXPRESS");

			Assert.IsNotNull(workspace);
			Assert.AreEqual("dbo.DEFAULT",
			                workspace.ConnectionProperties.GetProperty("VERSION"));
		}

		[Test]
		[Category(TestCategory.Sde)]
		[Ignore("requires Oracle wallet for testserver")]
		public void CanOpenNonDefaultSDEWorkspaceOSA()
		{
			IWorkspace workspace =
				WorkspaceUtils.OpenSDEWorkspace(_nonDefaultRepositoryName,
				                                DirectConnectDriver.Oracle11g,
				                                _dbName);
			Assert.IsNotNull(workspace);
			Assert.AreEqual(string.Format("{0}.DEFAULT", _nonDefaultRepositoryName),
			                workspace.ConnectionProperties.GetProperty("VERSION"));
		}

		[Test]
		[Category(TestCategory.Sde)]
		[Ignore("requires Oracle wallet for testserver")]
		public void CanOpenNonDefaultSDEWorkspaceOSAWithVersionName()
		{
			const string versionName = "TOPGIS_DDX.DEFAULT";
			IWorkspace workspace =
				WorkspaceUtils.OpenSDEWorkspace(_nonDefaultRepositoryName,
				                                DirectConnectDriver.Oracle11g,
				                                _dbName, versionName);

			Assert.IsNotNull(workspace);
			Assert.AreEqual(versionName,
			                workspace.ConnectionProperties.GetProperty("VERSION"));
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanOpenNonDefaultSDEWorkspaceUidPwd()
		{
			IWorkspace workspace =
				WorkspaceUtils.OpenSDEWorkspace(_nonDefaultRepositoryName,
				                                DirectConnectDriver.Oracle11g,
				                                _dbName, "TOPGIS_DDX",
				                                "topgis_ddx");
			Assert.IsNotNull(workspace);
			Assert.AreEqual(string.Format("{0}.DEFAULT", _nonDefaultRepositoryName),
			                workspace.ConnectionProperties.GetProperty("VERSION"));
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanOpenNonDefaultSDEWorkspaceUidPwdWithVersionName()
		{
			const string versionName = "TOPGIS_DDX.DEFAULT";
			IWorkspace workspace =
				WorkspaceUtils.OpenSDEWorkspace(_nonDefaultRepositoryName,
				                                DirectConnectDriver.Oracle11g,
				                                _dbName, "TOPGIS_DDX",
				                                "topgis_ddx", versionName);

			Assert.IsNotNull(workspace);
			Assert.AreEqual(versionName,
			                workspace.ConnectionProperties.GetProperty("VERSION"));
		}

		[Test]
		public void CanReplaceEmptyPassword()
		{
			const string connectionString = "Blah=Blah;Password=;";

			Assert.AreEqual("Blah=Blah;Password=****;",
			                WorkspaceUtils.ReplacePassword(connectionString, "****"));
		}

		[Test]
		public void CanReplacePasswordAtEndOfConnectionString()
		{
			const string connectionString = "Blah=Blah;Password=abc";

			Assert.AreEqual("Blah=Blah;Password=****",
			                WorkspaceUtils.ReplacePassword(connectionString, "****"));
		}

		[Test]
		public void CanReplacePasswordAtWithoutPassword()
		{
			const string connectionString = "Blah=Blah;Bla=Bla";

			Assert.AreEqual(connectionString,
			                WorkspaceUtils.ReplacePassword(connectionString, "****"));
		}

		[Test]
		public void CanReplacePasswordInvalidConnectionString()
		{
			const string connectionString = "Blah=Blah;Password";

			Assert.AreEqual(connectionString,
			                WorkspaceUtils.ReplacePassword(connectionString, "****"));
		}

		[Test]
		public void CanReplacePasswordNotAtEndOfConnectionString()
		{
			const string connectionString = "Blah=Blah;Password=abc;Bla=Bla";

			Assert.AreEqual("Blah=Blah;Password=****;Bla=Bla",
			                WorkspaceUtils.ReplacePassword(connectionString, "****"));
		}

		[Test]
		public void CanReplaceEncryptedPasswordNotAtEndOfConnectionString()
		{
			const string connectionString = "Blah=Blah;ENCRYPTED_PASSWORD=abc;Bla=Bla";

			Assert.AreEqual("Blah=Blah;ENCRYPTED_PASSWORD=****;Bla=Bla",
			                WorkspaceUtils.ReplacePassword(connectionString, "****"));
		}

		[Test]
		public void CanReplacePasswordNotAtEndOfConnectionStringWithBlanks()
		{
			const string connectionString = "Blah = Blah ; Password = abc ; Bla = Bla";

			Assert.AreEqual("Blah=Blah;Password=****;Bla=Bla",
			                WorkspaceUtils.ReplacePassword(connectionString, "****"));
		}

		[Test]
		public void CanReplaceMultiplePasswords()
		{
			const string connectionString =
				"ENCRYPTED_PASSWORD_UTF8= pa$$word8 ; Blah = Blah ; Password = abc ; encrypted_Password=pa$$word;Bla = Bla";

			Assert.AreEqual(
				"ENCRYPTED_PASSWORD_UTF8=****;Blah=Blah;Password=****;encrypted_Password=****;Bla=Bla",
				WorkspaceUtils.ReplacePassword(connectionString, "****"));
		}

		[Test]
		[Category(TestCategory.x86)]
		public void CanDetermineIsPgdb()
		{
			IWorkspace workspace =
				WorkspaceUtils.OpenPgdbWorkspace(TestData.GetMdb1Path());

			Assert.IsTrue(WorkspaceUtils.IsPersonalGeodatabase(workspace));
		}

		[Test]
		public void CanDetermineSdeIsNotPgdb()
		{
			IWorkspace workspace = TestUtils.OpenUserWorkspaceOracle();

			Assert.IsFalse(WorkspaceUtils.IsPersonalGeodatabase(workspace));
			Assert.IsFalse(WorkspaceUtils.IsMobileGeodatabase(workspace));
			Assert.IsFalse(WorkspaceUtils.IsFileGeodatabase(workspace));
		}

		[Test]
		public void CanDetermineIsFileGdb()
		{
			IWorkspace workspace =
				WorkspaceUtils.OpenFileGdbWorkspace(TestData.GetGdbTableJointUtilsPath());

			Assert.IsFalse(WorkspaceUtils.IsPersonalGeodatabase(workspace));
			Assert.IsFalse(WorkspaceUtils.IsMobileGeodatabase(workspace));
			Assert.IsTrue(WorkspaceUtils.IsFileGeodatabase(workspace));
		}
	}
}
