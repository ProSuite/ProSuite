using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.AO.Geodatabase.Legacy;
using ProSuite.DomainModel.Core.Geodatabase;

namespace ProSuite.DomainModel.AO.Geodatabase
{
	public static class ConnectionProviderExtensions
	{
		/// <summary>
		/// Opens the workspace. 
		/// </summary>
		/// <param name="connectionProvider"></param>
		/// <param name="hWnd">The window handle of the parent window.</param>
		/// <remarks>Always opens the workspace from the factory. 
		/// Can therefore be used on background threads.</remarks>
		/// <returns></returns>
		public static IFeatureWorkspace OpenWorkspace(this ConnectionProvider connectionProvider,
		                                              int hWnd = 0)
		{
			if (connectionProvider is FileGdbConnectionProvider fileGdbConnectionProvider)
			{
				fileGdbConnectionProvider.AssertDirectoryExists();
				return WorkspaceUtils.OpenFileGdbFeatureWorkspace(fileGdbConnectionProvider.Path);
			}

			if (connectionProvider is MobileGdbConnectionProvider mobileGdbConnectionProvider)
			{
				mobileGdbConnectionProvider.AssertFileExists();
				return WorkspaceUtils.OpenMobileGdbFeatureWorkspace(
					mobileGdbConnectionProvider.Path);
			}

			if (connectionProvider is ConnectionFileConnectionProvider connectionFile)
			{
				return connectionFile.OpenWorkspace(null, hWnd);
			}

			if (connectionProvider is SdeDirectDbUserConnectionProvider sdeDirectDbUser)
			{
				return sdeDirectDbUser.OpenWorkspace(null, hWnd);
			}

			if (connectionProvider is SdeDirectOsaConnectionProvider sdeDirectOsa)
			{
				return sdeDirectOsa.OpenWorkspace(null, hWnd);
			}

			if (connectionProvider is OpenWorkspaceConnectionProvider
			    openWorkspaceConnectionProvider)
			{
				return openWorkspaceConnectionProvider.FeatureWorkspace;
			}

			if (connectionProvider is OleDbConnectionFileConnectionProvider oleDb)
			{
				oleDb.AssertFileExists();
				return (IFeatureWorkspace) WorkspaceUtils.OpenOleDbWorkspaceFromFile(
					oleDb.Path, hWnd);
			}

			if (connectionProvider is PgdbConnectionProvider)
			{
				throw new NotSupportedException(
					"The Personal Geodatabase Format is not supported in 64-bit applications, including " +
					"ArcGIS Pro. Please use ArcMap to convert it to a File Geodatabase.");
			}

			throw new NotSupportedException(string.Format("Unknown connection provider type: {0}",
			                                              connectionProvider.GetType().Name));
		}

		public static IFeatureWorkspace OpenWorkspace(
			this ConnectionFileConnectionProvider connectionProvider,
			[CanBeNull] string versionName, int hWnd = 0)
		{
			connectionProvider.AssertFileExists();
			return (IFeatureWorkspace) WorkspaceUtils.OpenSDEWorkspace(
				connectionProvider.Path, versionName, hWnd);
		}

		public static IFeatureWorkspace OpenWorkspace(
			this SdeConnectionProvider connectionProvider,
			[CanBeNull] string versionName, int hWnd = 0)
		{
			if (connectionProvider is SdeDirectConnectionProvider sdeDirect)
			{
				return sdeDirect.OpenWorkspace(versionName, hWnd);
			}

			throw new NotSupportedException(string.Format("Unknown connection provider type: {0}",
			                                              connectionProvider.GetType().Name));
		}

		public static IFeatureWorkspace OpenWorkspace(
			this SdeDirectConnectionProvider connectionProvider,
			[CanBeNull] string versionName, int hWnd = 0)
		{
			return connectionProvider.HasAlternateCredentials
				       ? OpenWorkspace(connectionProvider, connectionProvider.AlternateUserName,
				                       connectionProvider.AlternatePassword, versionName, hWnd)
				       : OpenWorkspaceCore(connectionProvider, versionName, hWnd);
		}
		
		private static IFeatureWorkspace OpenWorkspaceCore(
			this SdeDirectConnectionProvider connectionProvider,
			string versionName = null, int hWnd = 0)
		{
			if (connectionProvider is SdeDirectDbUserConnectionProvider sdeDirectDbUser)
				return OpenWorkspaceCore(sdeDirectDbUser, versionName, hWnd);

			if (connectionProvider is SdeDirectOsaConnectionProvider sdeDirectOsa)
				return OpenWorkspaceCore(sdeDirectOsa, versionName, hWnd);

			throw new NotSupportedException(string.Format("Unknown connection provider type: {0}",
			                                              connectionProvider.GetType().Name));
		}

		[NotNull]
		private static IFeatureWorkspace OpenWorkspace(
			this SdeDirectConnectionProvider connectionProvider,
			[NotNull] string userName, [NotNull] string password,
			[CanBeNull] string versionName = null, int hWnd = 0)
		{
			Assert.ArgumentNotNullOrEmpty(userName, nameof(userName));
			Assert.ArgumentNotNullOrEmpty(password, nameof(password));

			return (IFeatureWorkspace) WorkspaceUtils.OpenSDEWorkspace(
				connectionProvider.RepositoryName,
				GetDirectConnectDriver(connectionProvider.DatabaseType),
				connectionProvider.DatabaseName,
				userName, password,
				StringUtils.IsNotEmpty(versionName)
					? versionName
					: connectionProvider.VersionName,
				hWnd);
		}

		private static IFeatureWorkspace OpenWorkspaceCore(
			this SdeDirectDbUserConnectionProvider connectionProvider,
			string versionName = null, int hWnd = 0)
		{
			Assert.NotNull(connectionProvider.UserName, "username not defined");
			Assert.NotNullOrEmpty(connectionProvider.PlainTextPassword, "password not defined");

			return OpenWorkspace(connectionProvider, connectionProvider.UserName,
			                     connectionProvider.PlainTextPassword, versionName, hWnd);
		}

		private static IFeatureWorkspace OpenWorkspaceCore(
			this SdeDirectOsaConnectionProvider connectionProvider,
			string versionName = null, int hWnd = 0)
		{
			return (IFeatureWorkspace) WorkspaceUtils.OpenSDEWorkspace(
				connectionProvider.RepositoryName,
				GetDirectConnectDriver(connectionProvider.DatabaseType),
				connectionProvider.DatabaseName,
				StringUtils.IsNotEmpty(versionName)
					? versionName
					: connectionProvider.VersionName);
		}

		private static DirectConnectDriver GetDirectConnectDriver(DatabaseType databaseType)
		{
			switch (databaseType)
			{
				case DatabaseType.Oracle:
					return DirectConnectDriver.Oracle;

				case DatabaseType.Oracle9:
					return DirectConnectDriver.Oracle9i;

				case DatabaseType.Oracle10:
					return DirectConnectDriver.Oracle10g;

				case DatabaseType.Oracle11:
					return DirectConnectDriver.Oracle11g;

				case DatabaseType.SqlServer:
					return DirectConnectDriver.SqlServer;

				case DatabaseType.PostgreSQL:
					return DirectConnectDriver.PostgreSQL;

				default:
					throw new NotSupportedException(
						$"Unsupported database type: {databaseType}");
			}
		}
	}
}
